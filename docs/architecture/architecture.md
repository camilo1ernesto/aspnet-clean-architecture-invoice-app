# Invoicing — Architecture

> Status: Accepted baseline
> Scope: `Invoicing.sln` (Invoicing.Domain, Invoicing.Application, Invoicing.Infrastructure, Invoicing.Api)

This document is the architectural source of truth for this repository. It describes the
**intended** architecture. Where the current code differs from the rules below, the rules take
precedence and the difference is recorded in [Known deviations](#10-known-deviations-from-the-intended-architecture).

---

## 1. Purpose

The solution is a Web API for managing customers and invoices. Its architecture must:

- keep business rules for invoicing independent of frameworks, persistence and delivery mechanisms;
- make dependencies explicit and point them toward the business core;
- allow business behaviour to be tested without HTTP or infrastructure;
- allow infrastructure (for example the persistence provider) to be replaced without changing
  business or use-case code;
- make significant architectural change deliberate and traceable.

## 2. Architectural principles

| ID | Principle |
|----|-----------|
| AP-1 | The system follows **Clean Architecture**: business rules sit at the centre; delivery and infrastructure sit at the edges. |
| AP-2 | **Dependencies point inward.** Outer layers may depend on inner layers, never the reverse. |
| AP-3 | **Dependency inversion.** When an inner layer needs an outer capability (for example persistence), the inner layer defines the abstraction and the outer layer implements it. |
| AP-4 | **Explicit boundaries.** Each layer exposes a deliberate public surface; implementation types are not public unless another layer must reference them. |
| AP-5 | **Business behaviour lives in the Domain**, not in handlers, endpoints or persistence code. |
| AP-6 | **Testability.** Business behaviour must be testable in isolation; use cases must be testable against substituted ports. |
| AP-7 | **Governed change.** Material architectural changes are recorded as ADRs (see §9). |

## 3. Layer responsibilities

### 3.1 Domain — `Invoicing.Domain`

Contains the core business model:

- core business concepts (currently: `Customer`, `Invoice`, `InvoiceItem`, `InvoiceStatus`);
- entities and aggregate roots (`Invoice` is the aggregate root for its `InvoiceItem`s);
- value objects where a concept is defined by its value rather than identity;
- business invariants, enforced by the entities themselves;
- domain behaviour (for example invoice creation and status transitions such as `MarkAsSent`, `MarkAsPaid`);
- domain events, only where a domain occurrence must be observed outside the aggregate.

Rules:

- **DOM-1** Domain has no project references and no dependency on Application, Infrastructure, Api,
  persistence technologies or external services.
- **DOM-2** Entities protect their own state. State changes go through domain methods or factories;
  properties that participate in invariants must not be publicly settable.
- **DOM-3** An entity must not be constructible or mutable into an invalid state.
- **DOM-4** Domain types carry no persistence or serialization concerns (no ORM attributes,
  no persistence-specific base types).
- **DOM-5** Aggregates are modified only through their root. Child collections are not exposed
  as mutable collections.
- **DOM-6** References between aggregates should be by identity, not by object reference,
  unless an ADR records a different decision.

### 3.2 Application — `Invoicing.Application`

Orchestrates use cases:

- one use case per command or query, organised as vertical slices
  (`Invoices/CreateInvoice`, `Invoices/GetInvoices`, `Customers/GetCustomers`);
- use cases are dispatched through MediatR (`IRequest` / `IRequestHandler`);
- defines the ports use cases need (`IInvoiceRepository`, `ICustomerRepository`, `IUnitOfWork`);
- defines use-case input (commands/queries) and output (response DTOs).

Rules:

- **APP-1** Application may depend on Domain only (plus libraries that do not tie it to infrastructure).
- **APP-2** Application must not contain infrastructure implementation details: no ORM types,
  no database queries, no HTTP types.
- **APP-3** Handlers orchestrate: load aggregates through ports, call domain behaviour,
  persist through ports, map results. They do not implement business rules themselves.
- **APP-4** Command and query contracts use primitives or Application DTOs, not domain entities.
- **APP-5** Responses returned to callers are Application DTOs, never domain entities.
- **APP-6** A command completes as one unit of work. The handler decides when to commit.
- **APP-7** `CancellationToken` is accepted by handlers and propagated to ports.
- **APP-8** Handlers are internal; the public surface is commands, queries, DTOs, ports and the
  `AddApplication()` registration entry point.

### 3.3 Infrastructure — `Invoicing.Infrastructure`

Implements technical concerns:

- persistence via EF Core (`ApplicationDbContext`, repositories);
- implementations of Application ports;
- persistence mapping and schema concerns;
- data initialisation;
- registration through `AddInfrastructure()`.

Rules:

- **INF-1** Infrastructure may depend on Application and Domain.
- **INF-2** Infrastructure must not define business rules. Repositories load and store aggregates;
  they do not decide business outcomes.
- **INF-3** Persistence mapping is configured in Infrastructure, not in Domain types.
- **INF-4** Implementations are internal; only registration entry points and types the composition
  root needs are public.
- **INF-5** Within a request scope, all repositories and the unit of work share the same
  persistence context.

### 3.4 API — `Invoicing.Api`

Handles HTTP concerns and acts as the composition root:

- Minimal API endpoints grouped by feature (`InvoiceApi`, `CustomerApi`);
- HTTP request/response contracts (for example `CreateInvoiceRequest`);
- mapping HTTP requests to Application commands/queries and results to HTTP responses;
- application startup, configuration and dependency registration (`Program.cs`).

Rules:

- **API-1** Endpoints delegate every use case to Application via `IMediator`.
- **API-2** API must not contain core business rules.
- **API-3** API must not access persistence directly: no `DbContext`, repositories or
  unit of work in endpoints.
- **API-4** API must not construct or manipulate domain entities. It maps HTTP contracts to
  Application commands/queries.
- **API-5** API may reference Infrastructure **only** for composition (service registration and
  startup wiring) in `Program.cs`.
- **API-6** Asynchronous use cases are awaited before a response is produced; the HTTP response
  must reflect the actual use-case outcome.
- **API-7** Failures from Application/Domain are translated to HTTP responses at the API boundary,
  consistently.

## 4. Dependency rules

| From \ To      | Domain | Application | Infrastructure | Api |
|----------------|:------:|:-----------:|:--------------:|:---:|
| Domain         |   —    |     ✗       |       ✗        |  ✗  |
| Application    |   ✓    |     —       |       ✗        |  ✗  |
| Infrastructure |   ✓    |     ✓       |       —        |  ✗  |
| Api            | ✗ (¹)  |     ✓       |   ✓ (²)        |  —  |

(¹) Api must not use domain types directly (API-4). It reaches Domain only transitively.
(²) Composition only (API-5).

Additional rules:

- **DEP-1** Project references must match the table above. No reference may point outward.
- **DEP-2** A port is owned by the layer that consumes it (Application), never by the layer that
  implements it.
- **DEP-3** Namespaces follow the project and folder structure, so a type's layer is visible from
  its namespace.

## 5. Business logic placement

| Kind of logic | Location |
|---------------|----------|
| Invariants (for example valid quantities, prices, non-empty invoices) | Domain entities / value objects |
| State transitions and their preconditions (`InvoiceStatus`) | Domain (`Invoice`) |
| Derived business values (for example `InvoiceItem.TotalPrice`) | Domain |
| Business policies (for example due date on sending) | Domain |
| Use-case flow: load → act → persist → respond | Application handler |
| Existence checks that need a port (for example "customer exists") | Application handler, using the port |
| Input shape validation (required fields, formats) | Application boundary (or API boundary for pure HTTP format) |
| Mapping domain → response DTO | Application |
| Mapping HTTP ↔ command/query | Api |
| Querying, tracking, mapping to storage | Infrastructure |

If the same rule could be placed in more than one layer, it belongs in the innermost layer able
to enforce it.

## 6. Persistence boundaries

- **PER-1** Persistence is accessed only through Application-defined ports
  (`IInvoiceRepository`, `ICustomerRepository`, `IUnitOfWork`).
- **PER-2** Repositories are defined per aggregate root. There is no repository for
  `InvoiceItem`; it is persisted through `Invoice`.
- **PER-3** `IUnitOfWork` commits all changes made in the current scope. It is implemented by
  `ApplicationDbContext` and must resolve to the same instance the repositories use (INF-5).
- **PER-4** ORM types (`DbContext`, `DbSet`, `IQueryable` from the provider) do not cross out of
  Infrastructure.
- **PER-5** Read operations that do not modify state should not track entities.
- **PER-6** The persistence provider is an Infrastructure decision. The current provider is
  EF Core InMemory. Changing the provider, adopting migrations, or changing transactional
  behaviour requires an ADR.
- **PER-7** Schema creation and data seeding are Infrastructure responsibilities. The API
  only triggers them as part of composition.

## 7. API boundaries

- **BND-1** HTTP contracts (request/response shapes, routes, status codes) are owned by Api and
  versioned independently of Application DTOs where they differ.
- **BND-2** Endpoint → `IMediator.Send(command/query)` → result → HTTP response is the only path.
- **BND-3** A `201 Created` response contains the identifier actually produced by the use case,
  and its location refers to a resource the API can serve.
- **BND-4** Domain entities are never serialized directly to HTTP responses.
- **BND-5** Error translation (for example "customer not found") into HTTP status codes happens
  at the API boundary, using one consistent convention (to be recorded in an ADR).

## 8. Testing principles

- **TST-1** Domain behaviour (invariants, state transitions, derived values) is testable with
  plain unit tests: no HTTP, no persistence, no dependency injection container.
- **TST-2** Application handlers are testable by substituting their ports. No real database
  or HTTP is required.
- **TST-3** Infrastructure implementations are verified by integration tests against the
  persistence provider.
- **TST-4** API behaviour (routing, status codes, contract mapping) is verified by tests that
  host the API.
- **TST-5** Dependency rules (§4) should be verified automatically, so violations fail the build
  instead of being found in review.
- **TST-6** Time-dependent business behaviour must be testable deterministically. The source of
  "now" must be controllable in tests.
- **TST-7** Adding business behaviour requires adding tests at the innermost layer that owns it.

## 9. Architectural decision governance

- **GOV-1** An ADR is required for any change that materially affects:
  system structure, integration, persistence, security, scalability, or cross-cutting behaviour
  (for example error handling, validation, logging, time, identity).
- **GOV-2** ADRs are stored in `docs/architecture/adr/`, numbered sequentially
  (`ADR-NNN-<short-title>.md`, for example `ADR-001-clean-architecture-boundaries.md`),
  and never deleted. A replaced decision is marked *Superseded* and
  links to its successor.
- **GOV-3** Each ADR records: Status, Context, Decision, Consequences, and the rules in this
  document it adds, changes or relaxes.
- **GOV-4** This document is updated in the same change as any ADR that alters a rule here.
- **GOV-5** Deviations from these rules are either fixed, or recorded in §10 with a reference
  to the ADR or work item that addresses them.

Implicit decisions in the current code that should be captured as initial ADRs:

1. Clean Architecture layering and dependency rules (this document).
2. MediatR for use-case dispatch and command/query separation.
3. Repository + Unit of Work, with `ApplicationDbContext` as the unit of work.
4. EF Core InMemory as a temporary persistence provider; schema created and seeded at startup.
5. Client-generated `Guid` identifiers for entities.
6. Invoice aggregate boundary and how `Invoice` references `Customer`.
7. Invoice lifecycle (`InvoiceStatus`) and allowed transitions.
8. Money representation (`decimal`, no currency, no rounding policy) and integer quantities.
9. Payment-terms policy (due date = sent + 1 month).
10. Error-handling convention between Domain/Application and the API.
11. Time convention (UTC) and source of current time.

## 10. Known deviations from the intended architecture

Recorded at baseline. Each should be resolved or covered by an ADR.

| Rule(s) | Deviation | Location |
|---------|-----------|----------|
| API-6, BND-3 | `mediator.Send` is not awaited in `POST /invoices`; the response contains a `Task`, not the ID, and the use case runs after the response is returned. | `Invoicing.Api/Invoices/InvoiceApi.cs` |
| API-4, APP-4 | Api constructs `InvoiceItem` domain entities; `CreateInvoiceCommand` takes `List<InvoiceItem>`. | `InvoiceApi.cs`, `CreateInvoiceCommand.cs` |
| INF-5, PER-3 | `IUnitOfWork` is registered as a separate `ApplicationDbContext` instance from the one repositories use. | `Invoicing.Infrastructure/DependencyInjection.cs` |
| DOM-2, DOM-5 | `Invoice.Customer`, `Status`, `Items` and all `InvoiceItem` properties are publicly settable; `Items` is a mutable `List`. | `Invoice.cs`, `InvoiceItem.cs` |
| DOM-3 | No invariants enforced (empty items, non-positive quantity, negative price). | `Invoice.cs`, `InvoiceItem.cs` |
| DOM-2 | Status transitions have no preconditions; `Overdue` is never assigned. | `Invoice.cs` |
| DOM-6 | `Invoice` references the `Customer` object rather than its identity. | `Invoice.cs` |
| API-7, BND-5 | Missing customer raises `ArgumentException`; no consistent error translation. | `CreateInvoiceCommandHandler.cs` |
| APP-7 | Repository ports do not accept `CancellationToken`. | `ICustomerRepository.cs`, `IInvoiceRepository.cs` |
| PER-5 | Read-only invoice query uses tracking. | `InvoiceRepository.cs` |
| BND-3 | No `GET /invoices/{id}` resource for the `Created` location. | `InvoiceApi.cs` |
| DEP-3 | Several namespaces don't match their folders (for example `Invoicing.Application`, `Invoicing.Infrastructure` at the root). | `Application/Common/Abstractions/*`, `Application/Invoices/CreateInvoice/*`, `Infrastructure/Data/*` |
| TST-6 | Domain reads `DateTime.UtcNow` directly. | `Invoice.cs` |
| TST-1…5 | No automated tests exist. | — |

## 11. Dependency diagram

```
                ┌───────────────────────────────────────────┐
                │ Invoicing.Api   (HTTP + composition root) │
                │  Endpoints → IMediator.Send(...)          │
                │  Program.cs: AddApplication(),            │
                │              AddInfrastructure()          │
                └──────────┬─────────────────────┬──────────┘
                           │ uses                │ composes only (API-5)
                           ▼                     ▼
┌──────────────────────────────────┐   ┌──────────────────────────────────┐
│ Invoicing.Application            │   │ Invoicing.Infrastructure         │
│  Commands / Queries / Handlers   │   │  ApplicationDbContext (UoW)      │
│  Response DTOs                   │◄──┤  InvoiceRepository               │
│  Ports: IInvoiceRepository,      │   │  CustomerRepository              │
│         ICustomerRepository,     │   │  Mapping, seeding                │
│         IUnitOfWork              │   │  implements Application ports    │
└───────────────┬──────────────────┘   └───────────────┬──────────────────┘
                │ depends on                           │ depends on
                ▼                                      ▼
        ┌────────────────────────────────────────────────────┐
        │ Invoicing.Domain   (no dependencies)               │
        │  Invoice (aggregate root) ──1:N──► InvoiceItem     │
        │  Customer        InvoiceStatus                     │
        │  invariants · behaviour · value objects · events   │
        └────────────────────────────────────────────────────┘

  All arrows point inward. Nothing points from Domain outward,
  and nothing points from Application to Infrastructure or Api.
```
