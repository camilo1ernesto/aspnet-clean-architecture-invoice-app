using Invoicing.Application;
using Invoicing.Application.Invoices.GetInvoices;
using Invoicing.Application.Invoices.RegisterPayment;
using Invoicing.Domain.Invoices;
using MediatR;

namespace Invoicing.Api.Invoices;

public static class InvoiceApi
{
    public static void RegisterInvoiceApi(this WebApplication app)
    {
        app.MapPost("/invoices", (CreateInvoiceRequest request, IMediator mediator) =>
        {
            var command = new CreateInvoiceCommand(
                request.CustomerId, request.Items.Select(i => new InvoiceItem
                {
                    Description = i.Description,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList());

            var newInvoiceId = mediator.Send(command);
            return TypedResults.Created($"/invoices/{newInvoiceId}", newInvoiceId);
        }).WithName("CreateInvoice");

        app.MapGet("/invoices", async (IMediator mediator) =>
        {
            var invoices = await mediator.Send(new GetInvoicesQuery());
            return TypedResults.Ok(invoices);
        }).WithName("GetInvoices");

        app.MapPost("/invoices/{invoiceId:guid}/payments", async (
            Guid invoiceId, RegisterPaymentRequest request, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var command = new RegisterPaymentCommand(invoiceId, request.Amount, request.Reference);
            var payment = await mediator.Send(command, cancellationToken);
            return TypedResults.Created(default(string), payment);
        }).WithName("RegisterPayment");
    }
}