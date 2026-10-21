using Invoicing.Application.Common.Exceptions;
using Invoicing.Domain.Invoices;
using MediatR;

namespace Invoicing.Application.Invoices.RegisterPayment;

internal class RegisterPaymentCommandHandler(IInvoiceRepository invoiceRepository)
    : IRequestHandler<RegisterPaymentCommand, RegisterPaymentResponse>
{
    public async Task<RegisterPaymentResponse> Handle(
        RegisterPaymentCommand request, CancellationToken cancellationToken)
    {
        var invoice = await invoiceRepository.GetByIdAsync(request.InvoiceId, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{request.InvoiceId}' was not found.");

        // Uniqueness across all invoices needs the repository; the per-invoice check is a Domain invariant.
        if (await invoiceRepository.PaymentReferenceExistsAsync(request.Reference, cancellationToken))
            throw new ConflictException(InvoiceErrors.PaymentReferenceDuplicate,
                $"Payment reference '{request.Reference}' has already been used.");

        var payment = invoice.RegisterPayment(request.Amount, request.Reference, DateTime.UtcNow);

        await invoiceRepository.UnitOfWork.CommitAsync(cancellationToken);

        return new RegisterPaymentResponse(
            payment.Id,
            invoice.Id,
            payment.Amount,
            payment.Reference,
            invoice.OutstandingBalance,
            invoice.Status.ToString());
    }
}
