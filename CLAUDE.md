# BimCollab Clash Detection - Project Instructions

## Project Overview
BIM clash detection API for a BIMCollab tech screening assessment. Detects and reports issues within building site plans (boundary violations, overlaps, clearance, zoning).

## Tech Stack
- .NET 10 (net10.0)
- .NET Aspire 13.2.2 (AppHost + ServiceDefaults)
- Minimal API (Scalar for API docs)
- MediatR 14.1 (CQRS)
- FluentValidation 12.1
- Reqnroll 3.3 (BDD testing)
- xUnit (unit + integration tests)

## Architecture
Clean Architecture / Onion Architecture with CQRS pattern:
- **Domain** (innermost): Entities, Value Objects, Enums, Models, Rules interface (Strategy pattern)
- **Application** (middle): CQRS commands/handlers, validators, pipeline behaviors. Depends only on Domain.
- **Api** (outermost): Minimal API endpoints, contracts, composition root. Depends on Application.
- **AppHost**: Aspire orchestrator
- **ServiceDefaults**: OpenTelemetry, health checks, resilience

## Running
- API standalone: `dotnet run --project src/BimCollab.ClashDetection.Api`
- With Aspire Dashboard: `dotnet run --project src/BimCollab.ClashDetection.AppHost --launch-profile http`
- Tests: `dotnet test`
- Scalar API docs (dev): http://localhost:5201/scalar/v1

## Key Design Decisions
- **CQRS via MediatR**: Command (DetectClashesCommand) -> Handler -> Result. Pipeline behaviors for cross-cutting concerns.
- **Strategy Pattern for rules**: `IClashDetectionRule` interface -- each rule is independently testable and extensible (per assessment Q2).
- **No Event Sourcing**: Operation is stateless (input -> clashes). No state to track over time.
- **BDD with Reqnroll**: Feature files cover all business rules from the assessment.
- **JSON output**: Clashes include involved buildings, violation type, description (per assessment Q3).

## Business Rules (from assessment)
### Validation Rules
- All building attributes (name, type, width, length, x, y) required
- Dimensions (width, length) must be positive (> 0)
- Positions (x, y) must be >= 0

### General Business Rules
- Buildings must be fully within site plan boundaries
- Buildings cannot overlap
- Minimum 10 units clearance between buildings

### Zoning Rules
- Nightclubs >= 200 units from any school
- Residential buildings >= 150 units from stadiums and nightclubs

## Solution Structure
```
src/
  BimCollab.ClashDetection.Domain/         # No dependencies
  BimCollab.ClashDetection.Application/    # -> Domain
  BimCollab.ClashDetection.Api/            # -> Application, ServiceDefaults
  BimCollab.ClashDetection.AppHost/        # -> Api (Aspire orchestrator)
  BimCollab.ClashDetection.ServiceDefaults/
tests/
  BimCollab.ClashDetection.Domain.Tests/
  BimCollab.ClashDetection.Application.Tests/
  BimCollab.ClashDetection.Api.Tests/
  BimCollab.ClashDetection.Specs/          # BDD (Reqnroll)
```

## Active Technologies
- .NET 10 (net10.0), C# lates + MediatR 14.1, FluentValidation 12.1, Aspire 13.2.2, Scalar.AspNetCore (HEAD)
- N/A (stateless, no persistence) (HEAD)

## Recent Changes
- HEAD: Added .NET 10 (net10.0), C# lates + MediatR 14.1, FluentValidation 12.1, Aspire 13.2.2, Scalar.AspNetCore
