namespace Invoicing.Domain.Invoices;

public static class InvoiceErrors
{
    public const string PaymentAmountNotPositive = "invoice.payment.amount_not_positive";
    public const string PaymentExceedsOutstandingBalance = "invoice.payment.exceeds_outstanding_balance";
    public const string PaymentReferenceRequired = "invoice.payment.reference_required";
    public const string PaymentReferenceDuplicate = "invoice.payment.reference_duplicate";
}
