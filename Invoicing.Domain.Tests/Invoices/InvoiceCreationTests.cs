using Invoicing.Domain.Abstractions;
using Invoicing.Domain.Customers;
using Invoicing.Domain.Invoices;

namespace Invoicing.Domain.Tests.Invoices;

public class InvoiceCreationTests
{
    private static readonly Customer Customer = new("John Doe", "johndoe@example.com");

    [Fact]
    public void Creates_items_from_lines()
    {
        var invoice = Invoice.CreateNew(Customer,
        [
            new InvoiceLine("Service", 2, 30m),
            new InvoiceLine("Support", 1, 40m)
        ]);

        Assert.Collection(invoice.Items,
            i => Assert.Equal(("Service", 2, 30m, 60m), (i.Description, i.Quantity, i.UnitPrice, i.TotalPrice)),
            i => Assert.Equal(("Support", 1, 40m, 40m), (i.Description, i.Quantity, i.UnitPrice, i.TotalPrice)));
        Assert.Equal(100m, invoice.TotalAmount);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
    }

    [Fact]
    public void Free_item_with_zero_unit_price_is_allowed()
    {
        var invoice = Invoice.CreateNew(Customer, [new InvoiceLine("Onboarding", 1, 0m)]);

        Assert.Equal(0m, invoice.TotalAmount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Item_description_is_required(string? description)
    {
        var ex = Assert.Throws<DomainException>(() =>
            Invoice.CreateNew(Customer, [new InvoiceLine(description!, 1, 10m)]));

        Assert.Equal(InvoiceErrors.ItemDescriptionRequired, ex.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Item_quantity_must_be_greater_than_zero(int quantity)
    {
        var ex = Assert.Throws<DomainException>(() =>
            Invoice.CreateNew(Customer, [new InvoiceLine("Service", quantity, 10m)]));

        Assert.Equal(InvoiceErrors.ItemQuantityNotPositive, ex.Code);
    }

    [Fact]
    public void Item_unit_price_cannot_be_negative()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Invoice.CreateNew(Customer, [new InvoiceLine("Service", 1, -0.01m)]));

        Assert.Equal(InvoiceErrors.ItemUnitPriceNegative, ex.Code);
    }

    [Fact]
    public void Changing_the_source_lines_after_creation_does_not_affect_the_invoice()
    {
        var lines = new List<InvoiceLine> { new("Service", 1, 100m) };
        var invoice = Invoice.CreateNew(Customer, lines);

        lines.Add(new InvoiceLine("Extra", 1, 50m));
        lines.Clear();

        Assert.Single(invoice.Items);
        Assert.Equal(100m, invoice.TotalAmount);
    }

    [Fact]
    public void Items_cannot_be_modified_through_the_exposed_collection()
    {
        var invoice = Invoice.CreateNew(Customer, [new InvoiceLine("Service", 1, 100m)]);

        var asList = Assert.IsAssignableFrom<ICollection<InvoiceItem>>(invoice.Items);
        Assert.True(asList.IsReadOnly);
        Assert.Throws<NotSupportedException>(() => asList.Clear());
    }

    [Fact]
    public void Unpaid_invoice_can_be_marked_as_sent()
    {
        var invoice = Invoice.CreateNew(Customer, [new InvoiceLine("Service", 1, 100m)]);

        invoice.MarkAsSent();

        Assert.Equal(InvoiceStatus.Sent, invoice.Status);
        Assert.True(invoice.DueAt > invoice.SentAt);
    }

    [Fact]
    public void Sent_invoice_cannot_be_sent_again()
    {
        var invoice = Invoice.CreateNew(Customer, [new InvoiceLine("Service", 1, 100m)]);
        invoice.MarkAsSent();

        var ex = Assert.Throws<DomainException>(invoice.MarkAsSent);

        Assert.Equal(InvoiceErrors.InvoiceAlreadySent, ex.Code);
    }

    [Fact]
    public void Paid_invoice_cannot_be_moved_back_to_sent()
    {
        var invoice = Invoice.CreateNew(Customer, [new InvoiceLine("Service", 1, 100m)]);
        invoice.RegisterPayment(100m, "REF-1", new DateTime(2026, 1, 15, 0, 0, 0, DateTimeKind.Utc));

        var ex = Assert.Throws<DomainException>(invoice.MarkAsSent);

        Assert.Equal(InvoiceErrors.InvoiceAlreadySent, ex.Code);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }
}
