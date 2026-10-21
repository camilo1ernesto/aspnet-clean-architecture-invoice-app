# ADR-002: Test Tooling

## Status

Accepted — 2026-10-20

## Context

The repository had no automated tests. SPEC-001 (Register Payment) requires domain unit
tests, application/use-case tests and an integration test. `architecture.md` §8
(TST-1…TST-4) requires business behaviour to be testable without HTTP or infrastructure,
handlers to be testable with substituted ports, and API behaviour to be verified by tests
that host the API. ADR-001 deferred the choice of test tooling to a separate ADR, and
`CLAUDE.md` requires new dependencies to be explained.

## Decision

1. **Framework:** xUnit 2.x (`xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5), run through
   `Microsoft.NET.Test.Sdk` 17.14.1 (`dotnet test`).
2. **In-process API tests:** `Microsoft.AspNetCore.Mvc.Testing` 8.0.x (`WebApplicationFactory<Program>`),
   matching the ASP.NET Core 8 target of `Invoicing.Api`. `Program.cs` declares
   `public partial class Program;` so the factory can reference the entry point.
3. **Test doubles:** hand-written fakes for Application ports. No mocking library.
4. **Projects** (repository root, matching the existing flat layout), one per layer being tested:

   | Project | References | Purpose |
   |---|---|---|
   | `Invoicing.Domain.Tests` | Domain only | Domain behaviour and invariants (TST-1) |
   | `Invoicing.Application.Tests` | Application (+ Domain transitively) | Handlers and pipeline behaviours against fakes (TST-2) |
   | `Invoicing.Api.IntegrationTests` | Api (+ Application, Infrastructure, Domain transitively) | HTTP endpoints hosted in-process against the configured persistence provider (TST-3, TST-4) |

5. `Invoicing.Application` exposes its internals to `Invoicing.Application.Tests` only
   (`InternalsVisibleTo`), so handlers stay `internal` (APP-8).

## Consequences

### Positive

- Every layer named in SPEC-001 can be tested in isolation, using the layer's own references.
- Domain tests need no packages beyond the test framework.
- Hand-written fakes keep the dependency footprint small and make port contracts explicit.
- Integration tests run in-process, without a deployed environment.

### Negative / Trade-offs

- Hand-written fakes must be updated when a port changes.
- Integration tests use the application's process-wide EF Core InMemory database (`"InvoiceDb"`).
  Tests share state, so each test seeds its own data and uses unique identifiers.
- InMemory does not behave like a relational database (no enforced unique indexes, no real
  transactions). Integration tests therefore don't prove relational behaviour.
- xUnit 2.x is used rather than xUnit v3. Moving to v3 later would be a separate change.
- Automated architecture tests (TST-5) are still not in place. Their tooling is not chosen here.
