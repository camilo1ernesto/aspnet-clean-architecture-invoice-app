namespace Invoicing.Api.Invoices;

public record RegisterPaymentRequest(decimal Amount, string Reference);
