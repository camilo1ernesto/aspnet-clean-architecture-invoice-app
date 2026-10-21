using Invoicing.Application;
using Invoicing.Domain;
using Invoicing.Domain.Invoices;
using Microsoft.EntityFrameworkCore;

namespace Invoicing.Infrastructure;

internal class InvoiceRepository(ApplicationDbContext dbContext)
    : IInvoiceRepository
{
    public void Add(Invoice invoice) => dbContext.Add(invoice);

    public async Task<IEnumerable<Invoice>> GetAllAsync() =>
        await dbContext.Invoices
            .AsTracking()
            .Include(i => i.Customer)
            .Include(i => i.Items)
            .ToListAsync();

    public async Task<Invoice?> GetByIdAsync(Guid invoiceId, CancellationToken cancellationToken) =>
        await dbContext.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.Id == invoiceId, cancellationToken);

    public async Task<bool> PaymentReferenceExistsAsync(string reference, CancellationToken cancellationToken) =>
        await dbContext.Set<Payment>()
            .AnyAsync(p => p.Reference == reference, cancellationToken);

    public IUnitOfWork UnitOfWork => dbContext;
}