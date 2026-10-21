using Invoicing.Domain.Abstractions;

namespace Invoicing.Domain.Invoices;

public class Payment : BaseEntity
{
    public decimal Amount { get; private set; }
    public string Reference { get; private set; }
    public DateTime ReceivedAt { get; private set; }

    internal Payment(decimal amount, string reference, DateTime receivedAt)
    {
        Amount = amount;
        Reference = reference;
        ReceivedAt = receivedAt;
    }
}
