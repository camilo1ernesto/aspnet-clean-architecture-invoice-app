using Invoicing.Application.Common.Exceptions;
using Invoicing.Application.Invoices.RegisterPayment;
using Invoicing.Application.Tests.Fakes;
using Invoicing.Domain.Abstractions;
using Invoicing.Domain.Customers;
using Invoicing.Domain.Invoices;

namespace Invoicing.Application.Tests.Invoices;

public class RegisterPaymentCommandHandlerTests
{
    private readonly FakeInvoiceRepository _repository = new();
    private readonly RegisterPaymentCommandHandler _handler;
    private readonly Invoice _invoice;

    public RegisterPaymentCommandHandlerTests()
    {
        _handler = new RegisterPaymentCommandHandler(_repository);

        // Total = 100
        _invoice = Invoice.CreateNew(new Customer("John Doe", "johndoe@example.com"),
            [new InvoiceItem { Description = "Service", Quantity = 1, UnitPrice = 100m }]);
        _repository.Invoices.Add(_invoice);
    }

    [Fact]
    public async Task Registers_partial_payment_commits_once_and_returns_balance()
    {
        var response = await _handler.Handle(
            new RegisterPaymentCommand(_invoice.Id, 40m, "REF-1"), CancellationToken.None);

        Assert.Equal(1, _repository.FakeUnitOfWork.CommitCount);
        Assert.Equal(_invoice.Id, response.InvoiceId);
        Assert.Equal(40m, response.Amount);
        Assert.Equal("REF-1", response.Reference);
        Assert.Equal(60m, response.OutstandingBalance);
        Assert.Equal(nameof(InvoiceStatus.Unpaid), response.InvoiceStatus);
        Assert.Equal(_invoice.Payments.Single().Id, response.PaymentId);
    }

    [Fact]
    public async Task Registers_full_payment_and_returns_paid_status()
    {
        var response = await _handler.Handle(
            new RegisterPaymentCommand(_invoice.Id, 100m, "REF-1"), CancellationToken.None);

        Assert.Equal(1, _repository.FakeUnitOfWork.CommitCount);
        Assert.Equal(0m, response.OutstandingBalance);
        Assert.Equal(nameof(InvoiceStatus.Paid), response.InvoiceStatus);
    }

    [Fact]
    public async Task Unknown_invoice_throws_not_found_and_does_not_commit()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Handle(
            new RegisterPaymentCommand(Guid.NewGuid(), 40m, "REF-1"), CancellationToken.None));

        Assert.Equal(0, _repository.FakeUnitOfWork.CommitCount);
    }

    [Fact]
    public async Task Reference_used_on_another_invoice_throws_conflict_and_does_not_commit()
    {
        _repository.ExistingPaymentReferences.Add("REF-1");

        var ex = await Assert.ThrowsAsync<ConflictException>(() => _handler.Handle(
            new RegisterPaymentCommand(_invoice.Id, 40m, "REF-1"), CancellationToken.None));

        Assert.Equal(InvoiceErrors.PaymentReferenceDuplicate, ex.Code);
        Assert.Equal(0, _repository.FakeUnitOfWork.CommitCount);
        Assert.Empty(_invoice.Payments);
    }

    [Fact]
    public async Task Domain_rule_violation_propagates_and_does_not_commit()
    {
        var ex = await Assert.ThrowsAsync<DomainException>(() => _handler.Handle(
            new RegisterPaymentCommand(_invoice.Id, 150m, "REF-1"), CancellationToken.None));

        Assert.Equal(InvoiceErrors.PaymentExceedsOutstandingBalance, ex.Code);
        Assert.Equal(0, _repository.FakeUnitOfWork.CommitCount);
    }

    [Fact]
    public async Task Cancellation_token_is_passed_to_repository_and_unit_of_work()
    {
        using var cts = new CancellationTokenSource();

        await _handler.Handle(new RegisterPaymentCommand(_invoice.Id, 40m, "REF-1"), cts.Token);

        Assert.NotEmpty(_repository.ReceivedTokens);
        Assert.All(_repository.ReceivedTokens, t => Assert.Equal(cts.Token, t));
        Assert.Equal(cts.Token, Assert.Single(_repository.FakeUnitOfWork.ReceivedTokens));
    }
}
