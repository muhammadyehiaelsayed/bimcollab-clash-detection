# Implementation Plan: BIM Clash Detection API

**Branch**: `001-clash-detection` | **Date**: 2026-04-10 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `specs/001-clash-detection/spec.md`

## Summary

Build a stateless BIM clash detection API that accepts a site plan with buildings, validates input, runs 5 independent clash detection rules (boundary, overlap, clearance, nightclub-school zoning, residential zoning), and returns a structured JSON list of all detected clashes with severity levels. Clean Architecture with CQRS via MediatR, Strategy pattern for extensible rules, BDD-first development with Reqnroll.

## Technical Context

**Language/Version**: .NET 10 (net10.0), C# latest
**Primary Dependencies**: MediatR 14.1, FluentValidation 12.1, Aspire 13.2.2, Scalar.AspNetCore
**Storage**: N/A (stateless, no persistence)
**Testing**: xUnit, Reqnroll 3.3, Microsoft.AspNetCore.Mvc.Testing
**Target Platform**: Linux/Windows server (containerizable via Aspire)
**Project Type**: web-service (Minimal API)
**Performance Goals**: N/A (small dataset, correctness focus)
**Constraints**: Assessment scope (~2 hours), small dataset (~5 buildings)
**Scale/Scope**: Single POST endpoint, 5 detection rules, 4 validation rules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Clean Architecture | PASS | Domain (no deps) → Application (Domain only) → Api (Application + ServiceDefaults). Project references enforce inward-only dependency flow. |
| II. CQRS via MediatR | PASS | DetectClashesCommand + Handler. ValidationBehavior pipeline. Api interacts only via `IMediator.Send()`. |
| III. BDD-First (NON-NEGOTIABLE) | PASS | Reqnroll feature files for every rule, validator, and endpoint. Write BDD first, implement second. |
| IV. Extensible Rules (Strategy) | PASS | `IClashDetectionRule` interface. 5 implementations registered via DI. Handler receives `IEnumerable<IClashDetectionRule>`. |
| V. API-First Design | PASS | POST endpoint, JSON input/output, RFC 9457 validation errors, Scalar docs, severity in response. |
| VI. Simplicity | PASS | No database, no event sourcing, no message queues. Stateless. Fixed rules. |

No violations. No complexity justification needed.

## Project Structure

### Documentation (this feature)

```text
specs/001-clash-detection/
├── spec.md              # Feature specification (clarified)
├── plan.md              # This file
├── research.md          # Phase 0: technical research
├── data-model.md        # Phase 1: entity definitions
├── quickstart.md        # Phase 1: developer quickstart
├── contracts/
│   └── api-contract.md  # Phase 1: HTTP API contract
└── checklists/
    └── requirements.md  # Spec quality checklist
```

### Source Code (repository root)

```text
src/
├── BimCollab.ClashDetection.Domain/
│   ├── Entities/
│   │   ├── Building.cs           # Name, Type, Position, Dimensions
│   │   └── SitePlan.cs           # Dimensions
│   ├── ValueObjects/
│   │   ├── Position.cs           # X, Y (immutable record)
│   │   └── Dimensions.cs         # Width, Length (immutable record)
│   ├── Enums/
│   │   ├── BuildingType.cs       # School, Nightclub, Stadium, ResidentialBuilding, Office
│   │   ├── ClashType.cs          # BoundaryViolation, Overlap, InsufficientClearance, ZoningViolation
│   │   └── ClashSeverity.cs      # Critical, Warning
│   ├── Models/
│   │   └── Clash.cs              # BuildingNames, Type, Severity, Description
│   └── Rules/
│       ├── IClashDetectionRule.cs # Interface: Evaluate(SitePlan, IReadOnlyList<Building>) → IEnumerable<Clash>
│       ├── BoundaryRule.cs        # FR-007: buildings within site boundaries
│       ├── OverlapRule.cs         # FR-008: no overlapping buildings
│       ├── ClearanceRule.cs       # FR-009: min 10 units, skip if overlapping
│       ├── NightclubSchoolZoningRule.cs  # FR-010: nightclub >= 200 from school
│       └── ResidentialZoningRule.cs      # FR-011: residential >= 150 from stadium/nightclub
│
├── BimCollab.ClashDetection.Application/
│   ├── ClashDetection/
│   │   ├── Commands/
│   │   │   ├── DetectClashesCommand.cs       # SitePlanDto, List<BuildingDto>
│   │   │   └── DetectClashesCommandHandler.cs # Maps DTOs → Domain, runs all rules
│   │   └── Validators/
│   │       └── DetectClashesCommandValidator.cs  # FluentValidation: all FR-002..FR-006
│   ├── Common/
│   │   └── Behaviors/
│   │       └── ValidationBehavior.cs  # MediatR pipeline: validate → throw on failure
│   └── DependencyInjection.cs         # Register MediatR, FluentValidation, rules
│
├── BimCollab.ClashDetection.Api/
│   ├── Endpoints/
│   │   └── ClashDetectionEndpoints.cs  # POST /api/clash-detection/detect
│   ├── Contracts/
│   │   ├── DetectClashesRequest.cs     # JSON input model (SitePlan + Buildings)
│   │   └── ClashDetectionResponse.cs   # JSON output model (list of clashes)
│   ├── Middleware/
│   │   └── ValidationExceptionHandler.cs  # Maps ValidationException → RFC 9457
│   └── Program.cs                      # Composition root
│
├── BimCollab.ClashDetection.AppHost/
│   └── Program.cs                      # Aspire orchestrator
│
└── BimCollab.ClashDetection.ServiceDefaults/
    └── Extensions.cs                   # OpenTelemetry, health checks, resilience

tests/
├── BimCollab.ClashDetection.Domain.Tests/
│   └── Rules/
│       ├── BoundaryRuleTests.cs
│       ├── OverlapRuleTests.cs
│       ├── ClearanceRuleTests.cs
│       ├── NightclubSchoolZoningRuleTests.cs
│       └── ResidentialZoningRuleTests.cs
│
├── BimCollab.ClashDetection.Application.Tests/
│   ├── Commands/
│   │   └── DetectClashesCommandHandlerTests.cs
│   └── Validators/
│       └── DetectClashesCommandValidatorTests.cs
│
├── BimCollab.ClashDetection.Api.Tests/
│   └── Endpoints/
│       └── ClashDetectionEndpointTests.cs  # WebApplicationFactory integration tests
│
└── BimCollab.ClashDetection.Specs/
    ├── Features/
    │   ├── ClashDetection.feature        # US1: all detection scenarios
    │   ├── InputValidation.feature       # US2: all validation scenarios
    │   └── EdgeCases.feature             # Boundary conditions, special inputs
    ├── StepDefinitions/
    │   ├── ClashDetectionStepDefinitions.cs
    │   ├── InputValidationStepDefinitions.cs
    │   └── EdgeCaseStepDefinitions.cs
    └── Support/
        └── TestWebApplicationFactory.cs
```

**Structure Decision**: Single-project Clean Architecture. Domain/Application/Api separation enforced via project references. No Infrastructure layer needed (stateless, no external dependencies).

## Complexity Tracking

No Constitution Check violations. No complexity justification needed.

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| No Infrastructure layer | Accepted | No database, no external services. Rules are pure domain logic. |
| Rules in Domain layer | Accepted | Rules are core business logic with zero external dependencies. |
| No event sourcing | Accepted | Stateless operation. No state to track over time. |
