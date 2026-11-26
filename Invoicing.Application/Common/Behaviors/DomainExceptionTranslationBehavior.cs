using Invoicing.Application.Common.Exceptions;
using Invoicing.Domain.Abstractions;
using MediatR;

namespace Invoicing.Application.Common.Behaviors;

/// <summary>
/// Translates domain rule violations into an Application exception so that outer layers
/// can handle them without depending on Domain types (ADR-003).
/// </summary>
internal class DomainExceptionTranslationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse> where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (DomainException ex)
        {
            throw new BusinessRuleViolationException(ex.Code, ex.Message, ex);
        }
    }
}
