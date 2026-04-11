<!-- Sync Impact Report
  Version change: 1.0.0 → 1.1.0
  Modified principles:
    - "III. Testability First" → "III. BDD-First Development (NON-NEGOTIABLE)"
      Expanded: BDD scenarios now MUST be written before ANY implementation code.
      Every feature, rule, validator, and endpoint MUST have corresponding
      Reqnroll feature files. No code is considered complete without BDD coverage.
  Added sections: None
  Removed sections: None
  Templates requiring updates:
    - .specify/templates/plan-template.md ✅ (aligned - Constitution Check enforces BDD gate)
    - .specify/templates/spec-template.md ✅ (aligned - Given/When/Then scenarios mandatory)
    - .specify/templates/tasks-template.md ✅ (aligned - test tasks precede implementation)
  Follow-up TODOs: None
-->

# BimCollab Clash Detection Constitution

## Core Principles

### I. Clean Architecture (NON-NEGOTIABLE)

The solution MUST follow Clean Architecture (Onion Architecture) with strict dependency inversion:

- **Domain** (innermost): Entities, Value Objects, Enums, Models, Rule interfaces. MUST have zero external dependencies.
- **Application** (middle): CQRS commands/handlers, validators, pipeline behaviors. MUST depend only on Domain.
- **Api** (outermost): Minimal API endpoints, contracts, composition root. MUST depend on Application (never Domain directly for use cases).
- Dependencies MUST flow inward only. Inner layers MUST NOT reference outer layers.
- Each layer MUST be independently compilable and testable.

### II. CQRS via MediatR

All use cases MUST be expressed as MediatR commands or queries:

- Each operation has a dedicated Command/Query record and a corresponding Handler.
- Cross-cutting concerns (validation, logging) MUST be implemented as MediatR pipeline behaviors, not embedded in handlers.
- Handlers MUST be `internal sealed` to enforce encapsulation within the Application layer.
- The API layer MUST only interact with the Application layer through MediatR, never by calling services directly.

### III. BDD-First Development (NON-NEGOTIABLE)

Every piece of code MUST have corresponding BDD scenarios written BEFORE implementation:

- **BDD is mandatory for ALL code**: Every domain entity, value object, clash detection rule, validator, command handler, and API endpoint MUST have Reqnroll feature files with Gherkin scenarios.
- **Write BDD first, implement second**: Feature files and step definition stubs MUST be created and committed before any implementation code is written. Scenarios MUST fail (red) before implementation makes them pass (green).
- **Scenario coverage**: Gherkin scenarios MUST map 1:1 to every business rule, validation rule, and zoning rule from the assessment. Edge cases MUST also have explicit scenarios.
- **Unit tests complement BDD**: Domain logic and individual rule implementations MUST also have isolated xUnit tests. BDD covers behavior; unit tests cover edge cases and internal correctness.
- **Integration tests for API**: API endpoints MUST be verified through `WebApplicationFactory`-based integration tests in addition to BDD scenarios.
- Test projects MUST mirror source project structure: `Domain.Tests`, `Application.Tests`, `Api.Tests`, `Specs`.
- **No code without BDD**: Any implementation code submitted without corresponding BDD scenarios is considered incomplete and MUST NOT be merged or marked done.

### IV. Extensible Rules (Strategy Pattern)

Clash detection rules MUST be implemented using the Strategy pattern:

- Each rule MUST implement the `IClashDetectionRule` interface.
- Rules MUST be independently testable with no coupling to other rules.
- Adding a new rule MUST NOT require modification of existing rules or the handler (Open/Closed Principle).
- Rules MUST be registered via dependency injection, allowing the handler to receive all rules as an `IEnumerable<IClashDetectionRule>`.

### V. API-First Design

The API MUST be designed for consumption by other systems:

- JSON is the required output format. Responses MUST be structured, consistent, and machine-parseable.
- Each clash in the response MUST include: involved building(s), violation type, and a human-readable description.
- Input validation errors MUST return structured problem details (RFC 9457), not raw exceptions.
- The API MUST expose OpenAPI documentation via Scalar in development.

### VI. Simplicity

The solution MUST prioritize clarity and correctness over cleverness:

- YAGNI: Do not implement features, patterns, or abstractions not required by the assessment.
- No database, no event sourcing, no message queues. The operation is stateless: input in, clashes out.
- Prefer simple, readable code over complex abstractions. Three lines of clear code beats one line of clever code.
- No premature optimization. The dataset is small; focus on correctness.

## Technology Constraints

The technology stack is fixed for this assessment:

- **.NET 10** (net10.0) with C# latest
- **.NET Aspire 13.2.2** for orchestration, OpenTelemetry, health checks, and resilience
- **Minimal API** (no controllers) with Scalar for API documentation
- **MediatR 14.1** for CQRS command/query dispatch
- **FluentValidation 12.1** for declarative input validation
- **Reqnroll 3.3** for BDD test scenarios
- **xUnit** for unit and integration tests
- No additional frameworks or libraries without explicit justification

## Quality Gates

All changes MUST pass these gates before completion:

- **BDD Gate (NON-NEGOTIABLE)**: Every implementation task MUST have corresponding Reqnroll feature files. Code without BDD scenarios MUST NOT pass review.
- **Build**: `dotnet build` MUST succeed with zero warnings and zero errors (`TreatWarningsAsErrors` is enabled).
- **Tests**: `dotnet test` MUST pass all BDD, unit, and integration tests.
- **Architecture**: No layer dependency violations. Domain MUST remain dependency-free.
- **API Contract**: All endpoints MUST be documented in OpenAPI and return consistent JSON structures.
- **Code Review**: All decisions, trade-offs, and shortcuts MUST be explainable and documented where non-obvious.

## Governance

This constitution defines the non-negotiable standards for the BimCollab Clash Detection project. It supersedes any conflicting guidance.

- Amendments MUST be documented with rationale and version bump.
- Versioning follows SemVer: MAJOR (principle removal/redefinition), MINOR (new principle/expansion), PATCH (clarification/wording).
- All implementation work MUST verify compliance with these principles before marking tasks complete.
- The `CLAUDE.md` file at the project root serves as runtime development guidance and MUST stay aligned with this constitution.

**Version**: 1.1.0 | **Ratified**: 2026-04-10 | **Last Amended**: 2026-04-10
