using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Invoicing.Application.Invoices.RegisterPayment;
using Invoicing.Domain.Invoices;
using Invoicing.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Invoicing.Api.IntegrationTests.Invoices;

// The API uses a process-wide InMemory database, so every test seeds its own invoice
// and uses unique payment references.
public class RegisterPaymentEndpointTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Partial_payment_returns_201_and_persists_reduced_balance()
    {
        var invoiceId = await SeedInvoiceAsync(totalAmount: 100m);
        var reference = UniqueReference();

        var response = await PostPaymentAsync(invoiceId, 40m, reference);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterPaymentResponse>();
        Assert.NotNull(body);
        Assert.Equal(invoiceId, body.InvoiceId);
        Assert.Equal(40m, body.Amount);
        Assert.Equal(reference, body.Reference);
        Assert.Equal(60m, body.OutstandingBalance);
        Assert.Equal(nameof(InvoiceStatus.Unpaid), body.InvoiceStatus);

        var stored = await LoadInvoiceAsync(invoiceId);
        var payment = Assert.Single(stored.Payments);
        Assert.Equal(body.PaymentId, payment.Id);
        Assert.Equal(60m, stored.OutstandingBalance);
        Assert.Equal(InvoiceStatus.Unpaid, stored.Status);
    }

    [Fact]
    public async Task Payments_settling_the_balance_persist_paid_status()
    {
        var invoiceId = await SeedInvoiceAsync(totalAmount: 100m);

        Assert.Equal(HttpStatusCode.Created, (await PostPaymentAsync(invoiceId, 30m, UniqueReference())).StatusCode);
        var response = await PostPaymentAsync(invoiceId, 70m, UniqueReference());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var stored = await LoadInvoiceAsync(invoiceId);
        Assert.Equal(2, stored.Payments.Count);
        Assert.Equal(0m, stored.OutstandingBalance);
        Assert.Equal(InvoiceStatus.Paid, stored.Status);
    }

    [Fact]
    public async Task Unknown_invoice_returns_404()
    {
        var response = await PostPaymentAsync(Guid.NewGuid(), 40m, UniqueReference());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Non_positive_amount_returns_422(decimal amount)
    {
        var invoiceId = await SeedInvoiceAsync(totalAmount: 100m);

        var response = await PostPaymentAsync(invoiceId, amount, UniqueReference());

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, InvoiceErrors.PaymentAmountNotPositive);
        Assert.Empty((await LoadInvoiceAsync(invoiceId)).Payments);
    }

    [Fact]
    public async Task Amount_exceeding_outstanding_balance_returns_422()
    {
        var invoiceId = await SeedInvoiceAsync(totalAmount: 100m);

        var response = await PostPaymentAsync(invoiceId, 100.01m, UniqueReference());

        await AssertProblemAsync(response, HttpStatusCode.UnprocessableEntity, InvoiceErrors.PaymentExceedsOutstandingBalance);
        Assert.Empty((await LoadInvoiceAsync(invoiceId)).Payments);
    }

    [Fact]
    public async Task Duplicate_reference_on_another_invoice_returns_409()
    {
        var firstInvoiceId = await SeedInvoiceAsync(totalAmount: 100m);
        var secondInvoiceId = await SeedInvoiceAsync(totalAmount: 100m);
        var reference = UniqueReference();
        Assert.Equal(HttpStatusCode.Created, (await PostPaymentAsync(firstInvoiceId, 10m, reference)).StatusCode);

        var response = await PostPaymentAsync(secondInvoiceId, 10m, reference);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, InvoiceErrors.PaymentReferenceDuplicate);
        Assert.Empty((await LoadInvoiceAsync(secondInvoiceId)).Payments);
    }

    [Fact]
    public async Task Duplicate_reference_on_same_invoice_returns_409()
    {
        var invoiceId = await SeedInvoiceAsync(totalAmount: 100m);
        var reference = UniqueReference();
        Assert.Equal(HttpStatusCode.Created, (await PostPaymentAsync(invoiceId, 10m, reference)).StatusCode);

        var response = await PostPaymentAsync(invoiceId, 10m, reference);

        await AssertProblemAsync(response, HttpStatusCode.Conflict, InvoiceErrors.PaymentReferenceDuplicate);
        Assert.Single((await LoadInvoiceAsync(invoiceId)).Payments);
    }

    private Task<HttpResponseMessage> PostPaymentAsync(Guid invoiceId, decimal amount, string reference) =>
        _client.PostAsJsonAsync($"/invoices/{invoiceId}/payments", new { amount, reference });

    private static string UniqueReference() => $"REF-{Guid.NewGuid():N}";

    private async Task<Guid> SeedInvoiceAsync(decimal totalAmount)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var customer = await db.Customers.FirstAsync();

        var invoice = Invoice.CreateNew(customer,
            [new InvoiceItem { Description = "Service", Quantity = 1, UnitPrice = totalAmount }]);
        db.Invoices.Add(invoice);
        await db.SaveChangesAsync();
        return invoice.Id;
    }

    private async Task<Invoice> LoadInvoiceAsync(Guid invoiceId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.Invoices
            .AsNoTracking()
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .SingleAsync(i => i.Id == invoiceId);
    }

    private static async Task AssertProblemAsync(
        HttpResponseMessage response, HttpStatusCode expectedStatus, string expectedCode)
    {
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal((int)expectedStatus, problem.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(expectedCode, problem.RootElement.GetProperty("code").GetString());
    }
}
