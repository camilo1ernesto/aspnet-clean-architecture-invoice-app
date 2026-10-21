using Invoicing.Domain.Abstractions;
using Invoicing.Domain.Customers;
using Invoicing.Domain.Invoices;

namespace Invoicing.Domain.Tests.Invoices;

public class InvoicePaymentTests
{
    private static readonly DateTime ReceivedAt = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    // Total = 2 * 30 + 1 * 40 = 100
    private static Invoice CreateInvoice() =>
        Invoice.CreateNew(new Customer("John Doe", "johndoe@example.com"),
        [
            new InvoiceItem { Description = "Service", Quantity = 2, UnitPrice = 30m },
            new InvoiceItem { Description = "Support", Quantity = 1, UnitPrice = 40m }
        ]);

    [Fact]
    public void New_invoice_has_outstanding_balance_equal_to_sum_of_items()
    {
        var invoice = CreateInvoice();

        Assert.Equal(100m, invoice.TotalAmount);
        Assert.Equal(0m, invoice.AmountPaid);
        Assert.Equal(100m, invoice.OutstandingBalance);
        Assert.Empty(invoice.Payments);
    }

    [Fact]
    public void Partial_payment_reduces_outstanding_balance_and_keeps_status()
    {
        var invoice = CreateInvoice();

        var payment = invoice.RegisterPayment(40m, "REF-1", ReceivedAt);

        Assert.Equal(60m, invoice.OutstandingBalance);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
        Assert.Equal(40m, payment.Amount);
        Assert.Equal("REF-1", payment.Reference);
        Assert.Equal(ReceivedAt, payment.ReceivedAt);
        Assert.Contains(payment, invoice.Payments);
    }

    [Fact]
    public void Payment_of_full_outstanding_balance_marks_invoice_as_paid()
    {
        var invoice = CreateInvoice();

        invoice.RegisterPayment(100m, "REF-1", ReceivedAt);

        Assert.Equal(0m, invoice.OutstandingBalance);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
    }

    [Fact]
    public void Partial_payments_that_settle_the_balance_mark_invoice_as_paid()
    {
        var invoice = CreateInvoice();

        invoice.RegisterPayment(25.50m, "REF-1", ReceivedAt);
        invoice.RegisterPayment(34.50m, "REF-2", ReceivedAt);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);

        invoice.RegisterPayment(40m, "REF-3", ReceivedAt);

        Assert.Equal(0m, invoice.OutstandingBalance);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Equal(3, invoice.Payments.Count);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-50)]
    public void Payment_amount_must_be_greater_than_zero(decimal amount)
    {
        var invoice = CreateInvoice();

        var ex = Assert.Throws<DomainException>(() => invoice.RegisterPayment(amount, "REF-1", ReceivedAt));

        Assert.Equal(InvoiceErrors.PaymentAmountNotPositive, ex.Code);
        AssertUnchanged(invoice);
    }

    [Fact]
    public void Payment_cannot_exceed_outstanding_balance()
    {
        var invoice = CreateInvoice();

        var ex = Assert.Throws<DomainException>(() => invoice.RegisterPayment(100.01m, "REF-1", ReceivedAt));

        Assert.Equal(InvoiceErrors.PaymentExceedsOutstandingBalance, ex.Code);
        AssertUnchanged(invoice);
    }

    [Fact]
    public void Payment_cannot_exceed_remaining_balance_after_partial_payment()
    {
        var invoice = CreateInvoice();
        invoice.RegisterPayment(70m, "REF-1", ReceivedAt);

        var ex = Assert.Throws<DomainException>(() => invoice.RegisterPayment(31m, "REF-2", ReceivedAt));

        Assert.Equal(InvoiceErrors.PaymentExceedsOutstandingBalance, ex.Code);
        Assert.Equal(30m, invoice.OutstandingBalance);
        Assert.Single(invoice.Payments);
    }

    [Fact]
    public void Paid_invoice_rejects_further_payments()
    {
        var invoice = CreateInvoice();
        invoice.RegisterPayment(100m, "REF-1", ReceivedAt);

        var ex = Assert.Throws<DomainException>(() => invoice.RegisterPayment(1m, "REF-2", ReceivedAt));

        Assert.Equal(InvoiceErrors.PaymentExceedsOutstandingBalance, ex.Code);
        Assert.Equal(InvoiceStatus.Paid, invoice.Status);
        Assert.Single(invoice.Payments);
    }

    [Fact]
    public void Payment_reference_must_be_unique_within_invoice()
    {
        var invoice = CreateInvoice();
        invoice.RegisterPayment(10m, "REF-1", ReceivedAt);

        var ex = Assert.Throws<DomainException>(() => invoice.RegisterPayment(10m, "REF-1", ReceivedAt));

        Assert.Equal(InvoiceErrors.PaymentReferenceDuplicate, ex.Code);
        Assert.Equal(90m, invoice.OutstandingBalance);
        Assert.Single(invoice.Payments);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Payment_reference_is_required(string? reference)
    {
        var invoice = CreateInvoice();

        var ex = Assert.Throws<DomainException>(() => invoice.RegisterPayment(10m, reference!, ReceivedAt));

        Assert.Equal(InvoiceErrors.PaymentReferenceRequired, ex.Code);
        AssertUnchanged(invoice);
    }

    private static void AssertUnchanged(Invoice invoice)
    {
        Assert.Empty(invoice.Payments);
        Assert.Equal(100m, invoice.OutstandingBalance);
        Assert.Equal(InvoiceStatus.Unpaid, invoice.Status);
    }
}
