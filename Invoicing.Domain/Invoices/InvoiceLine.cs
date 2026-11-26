namespace Invoicing.Domain.Invoices;

/// <summary>Data for one invoice line; <see cref="Invoice"/> turns it into a validated <see cref="InvoiceItem"/>.</summary>
public record InvoiceLine(string Description, int Quantity, decimal UnitPrice);
