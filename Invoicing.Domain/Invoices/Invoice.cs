using Invoicing.Domain.Abstractions;
using Invoicing.Domain.Customers;

namespace Invoicing.Domain.Invoices;

public class Invoice : BaseEntity
{
    private readonly List<Payment> _payments = [];

    public Customer Customer { get; set; } = default!;
    public DateTime DueAt { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime SentAt { get; private set; }
    public InvoiceStatus Status { get; private set; } = InvoiceStatus.Unpaid;
    public List<InvoiceItem> Items { get; set; } = [];
    public IReadOnlyCollection<Payment> Payments => _payments.AsReadOnly();

    public decimal TotalAmount => Items.Sum(i => i.TotalPrice);
    public decimal AmountPaid => _payments.Sum(p => p.Amount);
    public decimal OutstandingBalance => TotalAmount - AmountPaid;

    public static Invoice CreateNew(Customer customer, List<InvoiceItem> items)
    {
        var invoice = new Invoice
        {
            Customer = customer,
            Items = items
        };
        return invoice;
    }

    public Payment RegisterPayment(decimal amount, string reference, DateTime receivedAt)
    {
        if (amount <= 0)
            throw new DomainException(InvoiceErrors.PaymentAmountNotPositive,
                "Payment amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(reference))
            throw new DomainException(InvoiceErrors.PaymentReferenceRequired,
                "Payment reference is required.");

        if (_payments.Any(p => p.Reference == reference))
            throw new DomainException(InvoiceErrors.PaymentReferenceDuplicate,
                $"Payment reference '{reference}' has already been used.");

        if (amount > OutstandingBalance)
            throw new DomainException(InvoiceErrors.PaymentExceedsOutstandingBalance,
                $"Payment amount {amount} exceeds the outstanding balance {OutstandingBalance}.");

        var payment = new Payment(amount, reference, receivedAt);
        _payments.Add(payment);

        if (OutstandingBalance == 0)
            Status = InvoiceStatus.Paid;

        return payment;
    }

    public void MarkAsSent()
    {
        Status = InvoiceStatus.Sent;
        SentAt = DateTime.UtcNow;
        DueAt = DateTime.UtcNow.AddMonths(1);
    }
}