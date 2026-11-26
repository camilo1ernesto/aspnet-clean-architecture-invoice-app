using Invoicing.Domain;
using Invoicing.Domain.Invoices;

namespace Invoicing.Application;

public interface IInvoiceRepository
{
    void Add(Invoice invoice);
    Task<IEnumerable<Invoice>> GetAllAsync();
    Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken);
    Task<bool> PaymentReferenceExistsAsync(string reference, CancellationToken cancellationToken);
    IUnitOfWork UnitOfWork { get; }
}