---
name: implement-feature
description: Implement a feature specification from docs/specifications (for example SPEC-001) in this invoicing solution, following the repository's architecture source of truth, with tests, build, architecture guardrails and a structured report. Use when asked to implement, build or deliver a SPEC or a feature described in docs/specifications.
argument-hint: "[SPEC-ID or path to specification]"
---

# Implement a feature specification

Target specification: $ARGUMENTS

This skill defines the **workflow** only. The architectural rules live in the source-of-truth
documents below. Read them; do not rely on memory or on this file for the rules themselves.

| Source of truth | Location |
|---|---|
| Repository instructions | `CLAUDE.md` |
| Feature specifications | `docs/specifications/` |
| Architecture rules (rule IDs, layer responsibilities, known deviations) | `docs/architecture/architecture.md` |
| Architectural decisions | `docs/architecture/adr/` |

## 1. Understand

1. Read `CLAUDE.md`.
2. Read the target specification in `docs/specifications/`. If none was named or it is ambiguous
   which one is meant, ask before continuing.
3. Read `docs/architecture/architecture.md`. Note the rule IDs relevant to the feature and the
   known deviations in §10. A known deviation is **never precedent** to copy.
4. Read every ADR in `docs/architecture/adr/` that applies to the feature (at minimum the ones
   governing boundaries, error handling and testing, if present).
5. Check `git status` and note any pre-existing modified or untracked files. They are not yours;
   don't change or overwrite them.

## 2. Explore

6. Explore the existing implementation the feature touches, across all layers, before changing
   anything. Identify existing patterns to follow and existing deviations to avoid.
7. Record a **baseline**: build the solution and run all tests, including
   `Invoicing.ArchitectureTests`. Note any tests that already fail, so they are not confused with
   failures you introduce.

## 3. Plan

8. For non-trivial changes, produce an implementation plan and **wait for approval** before
   implementing, unless the user has already approved a plan or explicitly asked to proceed.
   For each change give: project, file, responsibility, reason, dependency implications, tests.
9. In the plan, list:
   - ambiguities in the specification, each with a proposed default;
   - architectural decisions the change requires (see `architecture.md` §9 for when an ADR is needed);
   - any new dependency, and why it is needed.
10. If implementing without explicit answers to ambiguities, use the stated defaults and report
    them as assumptions.

## 4. Implement

11. Respect the layer boundaries and business-logic placement defined in `architecture.md`
    (§3–§7) and ADR-001. Place each rule in the innermost layer that can enforce it.
12. Implement **only** the requested specification. No unrelated refactors, fixes or
    clean-ups, even of known deviations. Mention those in the report instead.
13. Follow the conventions already established by ADRs (for example error handling and test tooling)
    rather than introducing alternatives.
14. If the change creates or alters an architectural decision, write or update the ADR and
    `architecture.md` (§9 links, §10 deviations) in the same change.

## 5. Test

15. Add automated tests at the layers the specification and `architecture.md` §8 call for, using
    the tooling in the test-tooling ADR. Business behaviour is tested in the innermost layer
    that owns it.

## 6. Verify

16. Build the solution: `dotnet build Invoicing.sln`. Report warnings as well as errors.
17. Run unit and integration tests: `dotnet test Invoicing.sln`.
18. Run the architecture guardrails explicitly: `dotnet test Invoicing.ArchitectureTests`.
19. Fix failures caused by your change and re-run. Compare against the baseline from step 7.

**Architecture guardrail failures:** never change production code, project references or
the guardrail tests just to make an architecture test pass without reporting it. If a guardrail
fails:
- because of your change, fix your change so it complies with the rule;
- because of a pre-existing violation, leave it and report it (test name, violating types,
  rule ID, whether `architecture.md` §10 already records it);
- because the rule itself seems wrong for this feature, stop and raise it as an architectural
  decision. Do not weaken or skip the test.

## 7. Report

Finish with a report containing:

- **Files changed:** grouped by project, marking each file new or modified, with a one-line purpose.
- **Tests added:** per test project, with what each group covers.
- **Build/test results:** build outcome and pass/fail counts per test project, including
  `Invoicing.ArchitectureTests`. Distinguish new failures from baseline failures.
- **Assumptions:** every ambiguity resolved without explicit user input.
- **Architectural decisions encountered:** decisions made, ADRs written or updated, guardrail
  violations found, and anything left for the user to decide.

Do not commit unless asked.
