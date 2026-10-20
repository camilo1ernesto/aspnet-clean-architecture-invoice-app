# ADR-001: Enforce Clean Architecture Dependency Boundaries

## Status

Accepted — 2026-10-20

## Context

The solution is split into four projects: `Invoicing.Domain`, `Invoicing.Application`,
`Invoicing.Infrastructure` and `Invoicing.Api`. The project references already follow a
Clean Architecture layout:

- `Invoicing.Domain` has no project references.
- `Invoicing.Application` references `Invoicing.Domain`.
- `Invoicing.Infrastructure` references `Invoicing.Application` (and Domain transitively).
- `Invoicing.Api` references `Invoicing.Application` and `Invoicing.Infrastructure`.

This structure exists today, but the rules behind it have never been written down.
The code already shows boundary erosion that the project references alone don't prevent:

- `Invoicing.Api` constructs domain `InvoiceItem` entities, and `CreateInvoiceCommand`
  accepts them as input (`InvoiceApi.cs`, `CreateInvoiceCommand.cs`).
- Because `Invoicing.Api` references `Invoicing.Infrastructure`, and `ApplicationDbContext`
  is public, nothing stops an endpoint from accessing persistence directly.
- Namespaces don't consistently match their layer (for example, types in
  `Invoicing.Infrastructure/Data/` use the `Invoicing.Infrastructure` namespace,
  and types in `Invoicing.Application/Common/Abstractions/` use `Invoicing.Application`).

Both human engineers and AI coding agents change this repository. Both need boundaries
that are explicit, written down, and checkable, rather than inferred from existing code.
Existing code is not a reliable guide, because it contains deviations.

## Decision

We will enforce the following dependency boundaries. The detailed rules are in
[`docs/architecture/architecture.md`](../architecture.md) (§3 Layer responsibilities,
§4 Dependency rules).

1. **Domain** contains business rules (entities, value objects, invariants, domain behaviour)
   and must not depend on Application, Infrastructure or API, or on persistence technologies
   or external services.
2. **Application** orchestrates use cases and may depend on Domain. It defines the
   abstractions (ports) its use cases need, such as `IInvoiceRepository`,
   `ICustomerRepository` and `IUnitOfWork`, and contains no infrastructure implementation details.
3. **Infrastructure** implements persistence and external integration concerns and may depend
   on Application and Domain. It implements Application ports and does not define business rules.
4. **API** handles HTTP concerns, delegates use cases to Application, and must not directly
   access persistence. Its reference to Infrastructure exists only so that `Program.cs`
   can register services.
5. No dependency may point outward. When an inner layer needs a capability from an outer layer,
   the inner layer defines an abstraction and the outer layer implements it.
6. Future automated architecture tests should verify these dependency rules where practical.

`architecture.md` is the reference both humans and AI coding agents must consult before
changing code. Any change that relaxes or alters these boundaries requires a new ADR that
supersedes or amends this one.

## Consequences

### Positive

- **Business rules are isolated.** Domain logic can change, and be tested, without
  HTTP, persistence or the dependency injection container.
- **Infrastructure is replaceable.** The persistence provider (currently EF Core InMemory)
  can be changed behind the Application ports without changing Domain or Application code.
- **Changes have a predictable place.** Engineers and agents can decide where code
  belongs from a written rule rather than by copying nearby code, which may itself be a deviation.
- **Easier review.** A change that adds an outward reference, or persistence access in an
  endpoint, is a clear, citable rule violation, not a matter of taste.
- **Better testability.** Handlers depend on ports, so use cases can be tested against
  substitute implementations.

### Negative / Trade-offs

- **More indirection and mapping.** Keeping domain entities out of API contracts and commands
  means extra DTOs and mapping code at each boundary (HTTP request → command → domain → response).
- **Ceremony relative to current size.** Four projects and port interfaces add structure to an
  application that currently has three endpoints and three entities.
- **The compiler enforces only part of the rules.** Project references prevent outward
  references from Domain and Application. They do not prevent:
  - API endpoints using `ApplicationDbContext` or repositories, because API must reference
    Infrastructure for composition;
  - API using domain types, which are reachable transitively;
  - business rules being written in handlers or repositories.

  Until automated checks exist, these depend on review discipline.
- **Existing deviations must be corrected.** The current code doesn't fully conform
  (see `architecture.md` §10). Bringing it into line is additional work, and deviations
  that remain could be copied as precedent.
- **Architecture tests have a cost.** Adding automated checks later requires choosing a test
  approach and maintaining the tests as the structure evolves.

## Enforcement

Current mechanisms:

1. **Project references.** The `.csproj` reference graph is the primary, compile-time guard.
   Adding a project reference that points outward (for example Domain → anything, or
   Application → Infrastructure or Api) violates this ADR.
2. **Access modifiers.** Handlers and repository implementations are `internal`, which limits
   what outer layers can reach. New implementation types should default to `internal`.
3. **Code review.** Changes, whether made by humans or AI agents, are reviewed against the
   rules in `architecture.md` §3–§7. Reviews cite the rule ID (for example `API-3`, `DOM-1`)
   when rejecting a violation.
4. **Recorded deviations.** Known non-conformance is listed in `architecture.md` §10.
   A deviation is either fixed or tracked there; it is never treated as precedent.

Planned:

5. **Automated architecture tests.** Where practical, the dependency rules in
   `architecture.md` §4 (and rules such as API-3) will be verified by automated tests
   that fail the build on violation. The test framework and tooling have not been selected;
   that choice will be recorded in a separate ADR.
