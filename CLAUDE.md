Read docs/architecture/architecture.md before making
architecturally significant changes.

Respect Clean Architecture dependency boundaries.

Domain must contain business rules.

API must not contain domain/business logic.

Do not access persistence directly from API.

Before implementation:
1. Understand the requirement.
2. Explore the relevant code.
3. Produce a plan for non-trivial changes.

After implementation:
1. Build the solution.
2. Run relevant tests.
3. Report failures.
4. Do not modify unrelated code.

Do not introduce new dependencies without explaining why.