using Invoicing.Domain.Invoices;

namespace Invoicing.Application.Tests.Fakes;

internal class FakeInvoiceRepository : IInvoiceRepository
{
    public List<Invoice> Invoices { get; } = [];
    public HashSet<string> ExistingPaymentReferences { get; } = [];
    public List<CancellationToken> ReceivedTokens { get; } = [];
    public FakeUnitOfWork FakeUnitOfWork { get; } = new();

    public IUnitOfWork UnitOfWork => FakeUnitOfWork;

    public void Add(Invoice invoice) => Invoices.Add(invoice);

    public Task<IEnumerable<Invoice>> GetAllAsync() => Task.FromResult<IEnumerable<Invoice>>(Invoices);

    public Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken)
    {
        ReceivedTokens.Add(cancellationToken);
        return Task.FromResult(Invoices.SingleOrDefault(i => i.Id == invoiceId));
    }

    public Task<bool> PaymentReferenceExistsAsync(string reference, CancellationToken cancellationToken)
    {
        ReceivedTokens.Add(cancellationToken);
        return Task.FromResult(ExistingPaymentReferences.Contains(reference));
    }
}

internal class FakeUnitOfWork : IUnitOfWork
{
    public int CommitCount { get; private set; }
    public List<CancellationToken> ReceivedTokens { get; } = [];

    public Task CommitAsync(CancellationToken cancellationToken)
    {
        CommitCount++;
        ReceivedTokens.Add(cancellationToken);
        return Task.CompletedTask;
    }
}
