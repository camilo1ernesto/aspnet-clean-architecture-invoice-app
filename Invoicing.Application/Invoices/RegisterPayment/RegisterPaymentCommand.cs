using MediatR;

namespace Invoicing.Application.Invoices.RegisterPayment;

public record RegisterPaymentCommand(
    Guid InvoiceId,
    decimal Amount,
    string Reference) : IRequest<RegisterPaymentResponse>;
