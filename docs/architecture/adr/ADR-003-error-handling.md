# ADR-003: Error Handling Convention

## Status

Accepted — 2026-10-20

## Context

Before SPEC-001, there was no convention for signalling failures. The create-invoice use case
throws `ArgumentException` for a missing customer. `architecture.md` requires failures to be
translated to HTTP at the API boundary, consistently (API-7, BND-5), and requires the API not
to use Domain types directly (§4, API-4).

SPEC-001 (Register Payment) has several distinct failures — invoice not found, invalid amount,
amount over the outstanding balance, duplicate payment reference — and clients must be able to
tell them apart.

## Decision

1. **Domain** signals business rule violations by throwing `DomainException`
   (`Invoicing.Domain/Abstractions/DomainException.cs`). Each violation has a stable, machine-readable
   `Code` (for example `InvoiceErrors.PaymentExceedsOutstandingBalance`).
2. **Application** defines its own exceptions in `Invoicing.Application/Common/Exceptions/`:
   - `NotFoundException`: a resource the use case needs does not exist.
   - `ConflictException` (with `Code`): the request conflicts with existing state that the
     use case checks through a port (for example a payment reference used on another invoice).
   - `BusinessRuleViolationException` (with `Code`): a domain rule was violated.
3. A MediatR pipeline behaviour, `DomainExceptionTranslationBehavior`, wraps every request and
   rethrows a `DomainException` as a `BusinessRuleViolationException`, keeping the code and message.
   This way the API only ever handles Application exceptions.
4. **API** translates Application exceptions into RFC 7807 ProblemDetails responses in one place,
   `Invoicing.Api/ErrorHandling/ApplicationExceptionHandler.cs`. It uses ASP.NET Core 8's built-in
   `IExceptionHandler`, `AddProblemDetails()` and `UseExceptionHandler()`. No new package is added.

   | Exception | HTTP status | `code` extension |
   |---|---|---|
   | `NotFoundException` | 404 Not Found | — |
   | `ConflictException` | 409 Conflict | yes |
   | `BusinessRuleViolationException` | 422 Unprocessable Entity | yes |
   | anything else | 500 (default ProblemDetails) | — |

   Malformed requests that can't be bound (for example invalid JSON) keep the framework's 400 response.
5. Endpoints contain no `try/catch` for these exceptions.

## Consequences

### Positive

- One mapping from failure to HTTP status, in one place (API-7, BND-5).
- The API depends only on Application exception types. Domain stays independent of HTTP, and
  API stays independent of Domain.
- Clients can tell failures apart using `status` and the stable `code`.
- Domain and Application tests assert on exception types and codes without HTTP.

### Negative / Trade-offs

- Control flow for expected failures uses exceptions, which costs more than returning result values.
  This is acceptable at the current scale.
- Every domain rule violation maps to 422, including input-like rules such as
  "amount must be greater than zero". Distinguishing 400 from 422 would need a further category.
- The translation behaviour runs for every MediatR request, including queries that never throw
  `DomainException`.
- The existing `ArgumentException` in `CreateInvoiceCommandHandler` is not covered by the
  convention, and would surface as a 500. It is recorded as a deviation in `architecture.md` §10.
