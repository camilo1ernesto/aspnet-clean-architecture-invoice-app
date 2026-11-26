namespace Invoicing.Domain.Invoices;

public static class InvoiceErrors
{
    public const string PaymentAmountNotPositive = "invoice.payment.amount_not_positive";
    public const string PaymentExceedsOutstandingBalance = "invoice.payment.exceeds_outstanding_balance";
    public const string PaymentReferenceRequired = "invoice.payment.reference_required";
    public const string PaymentReferenceDuplicate = "invoice.payment.reference_duplicate";
    public const string InvoiceAlreadySent = "invoice.already_sent";
    public const string ItemDescriptionRequired = "invoice.item.description_required";
    public const string ItemQuantityNotPositive = "invoice.item.quantity_not_positive";
    public const string ItemUnitPriceNegative = "invoice.item.unit_price_negative";
}
