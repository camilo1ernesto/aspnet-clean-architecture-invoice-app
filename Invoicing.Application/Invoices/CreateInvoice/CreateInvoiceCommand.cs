using Invoicing.Domain;
using Invoicing.Domain.Invoices;
using MediatR;

namespace Invoicing.Application;

public record CreateInvoiceCommand(
    Guid CustomerId,
    IReadOnlyList<CreateInvoiceLine> Items) : IRequest<Guid>;

public record CreateInvoiceLine(string Description, int Quantity, decimal UnitPrice);