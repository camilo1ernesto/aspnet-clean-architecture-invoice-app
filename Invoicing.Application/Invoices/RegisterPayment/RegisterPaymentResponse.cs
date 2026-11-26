namespace Invoicing.Application.Invoices.RegisterPayment;

public record RegisterPaymentResponse(
    Guid PaymentId,
    Guid InvoiceId,
    decimal Amount,
    string Reference,
    decimal OutstandingBalance,
    string InvoiceStatus);
