using Invoicing.Domain.Abstractions;

namespace Invoicing.Domain.Invoices;

public class InvoiceItem
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string Description { get; private set; } = string.Empty;
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TotalPrice => Quantity * UnitPrice;

    private InvoiceItem() { } // For EF Core

    internal InvoiceItem(string description, int quantity, decimal unitPrice)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new DomainException(InvoiceErrors.ItemDescriptionRequired,
                "Invoice item description is required.");

        if (quantity <= 0)
            throw new DomainException(InvoiceErrors.ItemQuantityNotPositive,
                "Invoice item quantity must be greater than zero.");

        if (unitPrice < 0)
            throw new DomainException(InvoiceErrors.ItemUnitPriceNegative,
                "Invoice item unit price cannot be negative.");

        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
