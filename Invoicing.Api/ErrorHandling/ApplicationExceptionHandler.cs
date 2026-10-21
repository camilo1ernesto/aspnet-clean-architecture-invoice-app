using Invoicing.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace Invoicing.Api.ErrorHandling;

/// <summary>
/// Translates Application exceptions into ProblemDetails responses (ADR-003).
/// Exceptions not listed here are left to the default handler and become 500.
/// </summary>
internal sealed class ApplicationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, code) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found", null),
            ConflictException e => (StatusCodes.Status409Conflict, "Conflict", e.Code),
            BusinessRuleViolationException e => (StatusCodes.Status422UnprocessableEntity, "Business rule violation", e.Code),
            _ => (0, null, null)
        };

        if (status == 0) return false;

        httpContext.Response.StatusCode = status;

        var context = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title, Detail = exception.Message }
        };
        if (code is not null) context.ProblemDetails.Extensions["code"] = code;

        return await problemDetailsService.TryWriteAsync(context);
    }
}
