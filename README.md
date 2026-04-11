# BimCollab Clash Detection API

A proof-of-concept BIM clash detection API that validates building placements on construction site plans and detects regulatory, spatial, and zoning violations.

Built for the [BIMcollab Tech Screening Assessment v1.1](bimcollab-techscreening-v1.1_1.pdf).

## Table of Contents

- [Overview](#overview)
- [Tech Stack](#tech-stack)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
- [Running the API](#running-the-api)
- [Running Tests](#running-tests)
- [API Reference](#api-reference)
- [Business Rules](#business-rules)
- [Design Decisions](#design-decisions)
- [Project Structure](#project-structure)
- [Scalability](#scalability)

## Overview

The API accepts a site plan with proposed buildings and detects all clashes (violations) based on validation rules, spatial constraints, and zoning regulations. Each clash includes the involved buildings, violation type, severity level, and a human-readable description.

**Key capabilities:**
- Input validation with structured error responses (RFC 9457)
- Boundary violation detection (buildings outside site plan)
- Building overlap detection
- Minimum clearance enforcement (10 units between buildings)
- Zoning compliance (nightclub-school and residential-stadium/nightclub distance rules)
- Severity classification (Critical vs Warning)

## Tech Stack

| Technology | Version | Purpose |
|-----------|---------|---------|
| .NET | 10.0 | Runtime & SDK |
| ASP.NET Core | Minimal API | Web framework |
| .NET Aspire | 13.2.2 | Orchestration, OpenTelemetry, health checks |
| MediatR | 14.1 | CQRS command/query dispatch |
| FluentValidation | 12.1 | Declarative input validation |
| Scalar | 2.13 | Interactive API documentation |
| xUnit | - | Unit & integration testing |
| Reqnroll | 3.3 | BDD testing (Gherkin scenarios) |

## Architecture

The solution follows **Clean Architecture (Onion Architecture)** with **CQRS** via MediatR:

```
                    +------------------+
                    |      API         |  Endpoints, Contracts, Middleware
                    +--------+---------+
                             |
                    +--------+---------+
                    |   Application    |  Commands, Handlers, Validators, Behaviors
                    +--------+---------+
                             |
                    +--------+---------+
                    |     Domain       |  Entities, Rules, Value Objects, Enums
                    +------------------+
```

**Dependencies flow inward only.** The Domain layer has zero external dependencies.

### Key Patterns

- **Strategy Pattern** for clash detection rules (`IClashDetectionRule`): each rule is an independent, testable class. Adding a new rule requires zero changes to existing code.
- **MediatR Pipeline Behaviors**: validation runs as a cross-cutting concern before the handler executes.
- **BDD-First Development**: Reqnroll feature files define acceptance criteria; implementation makes them pass.

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build

```bash
dotnet build
```

### Verify

```bash
dotnet test
```

## Running the API

### Standalone (recommended for quick testing)

```bash
dotnet run --project src/BimCollab.ClashDetection.Api
```

The API starts at **http://localhost:5201**

| URL | Description |
|-----|-------------|
| http://localhost:5201/scalar/v1 | Interactive API documentation |
| http://localhost:5201/health | Health check |
| http://localhost:5201/alive | Liveness probe |
| http://localhost:5201/openapi/v1.json | OpenAPI specification |

### With Aspire Dashboard (telemetry, logs, traces)

```bash
dotnet run --project src/BimCollab.ClashDetection.AppHost --launch-profile http
```

The Aspire Dashboard URL (with login token) will be printed in the console output.

### Quick Test with curl

**Detect clashes (assessment example dataset):**

```bash
curl -X POST http://localhost:5201/api/clash-detection/detect \
  -H "Content-Type: application/json" \
  -d '{
    "sitePlan": { "width": 1000, "length": 500 },
    "buildings": [
      { "name": "Oakwood Academy", "type": "School", "width": 300, "length": 300, "x": 0, "y": 0 },
      { "name": "Pulse", "type": "Nightclub", "width": 200, "length": 200, "x": 0, "y": 500 },
      { "name": "Centennial Park", "type": "Stadium", "width": 700, "length": 300, "x": 400, "y": 300 },
      { "name": "Willow Residence", "type": "ResidentialBuilding", "width": 200, "length": 200, "x": 300, "y": 100 },
      { "name": "Maple Plaza", "type": "Office", "width": 150, "length": 150, "x": 250, "y": 150 }
    ]
  }'
```

**Trigger validation errors:**

```bash
curl -X POST http://localhost:5201/api/clash-detection/detect \
  -H "Content-Type: application/json" \
  -d '{
    "sitePlan": { "width": 1000, "length": 500 },
    "buildings": [
      { "name": "", "type": "InvalidType", "width": 0, "length": -1, "x": -5, "y": 0 }
    ]
  }'
```

## Running Tests

```bash
# All tests (104 total)
dotnet test

# Domain rule tests only
dotnet test --filter "FullyQualifiedName~Domain.Tests"

# Application tests (handler + validator)
dotnet test --filter "FullyQualifiedName~Application.Tests"

# API integration tests
dotnet test --filter "FullyQualifiedName~Api.Tests"

# BDD scenarios only
dotnet test --filter "FullyQualifiedName~Specs"
```

### Test Distribution

| Project | Tests | Coverage |
|---------|-------|----------|
| Domain.Tests | 32 | All 5 detection rules + boundary conditions + diagonal distances + combinatorial cases |
| Application.Tests | 36 | Command handler (mapping, aggregation, overlap-subsumes-clearance) + validator (all rules, case sensitivity, building count limit) |
| Api.Tests | 11 | Integration tests via WebApplicationFactory (happy path, validation errors, malformed JSON) |
| Specs (BDD) | 25 | 9 clash detection scenarios + 6 edge cases + 10 validation scenarios |
| **Total** | **104** | |

## API Reference

### `POST /api/clash-detection/detect`

Accepts a site plan with buildings, validates input, runs all clash detection rules, and returns detected violations.

#### Request Body

```json
{
  "sitePlan": {
    "width": 1000,
    "length": 500
  },
  "buildings": [
    {
      "name": "Building A",
      "type": "School",
      "width": 100,
      "length": 100,
      "x": 0,
      "y": 0
    }
  ]
}
```

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `sitePlan.width` | number | Yes | > 0 |
| `sitePlan.length` | number | Yes | > 0 |
| `buildings[].name` | string | Yes | Non-empty, unique within request |
| `buildings[].type` | string | Yes | `School`, `Nightclub`, `Stadium`, `ResidentialBuilding`, or `Office` |
| `buildings[].width` | number | Yes | > 0 |
| `buildings[].length` | number | Yes | > 0 |
| `buildings[].x` | number | Yes | >= 0 |
| `buildings[].y` | number | Yes | >= 0 |

Building types are **case-sensitive**. Maximum **500 buildings** per request.

#### Success Response (HTTP 200)

```json
{
  "clashes": [
    {
      "buildingNames": ["Pulse"],
      "type": "BoundaryViolation",
      "severity": "Critical",
      "description": "Building 'Pulse' extends beyond site plan boundaries."
    },
    {
      "buildingNames": ["Oakwood Academy", "Maple Plaza"],
      "type": "Overlap",
      "severity": "Critical",
      "description": "Buildings 'Oakwood Academy' and 'Maple Plaza' overlap."
    }
  ]
}
```

#### Clash Types and Severity

| Type | Severity | Description |
|------|----------|-------------|
| `BoundaryViolation` | `Critical` | Building extends beyond site plan boundaries |
| `Overlap` | `Critical` | Two buildings share interior space |
| `InsufficientClearance` | `Warning` | Buildings are closer than 10 units (edge-to-edge) |
| `ZoningViolation` | `Warning` | Zoning distance requirement not met |

#### Validation Error Response (HTTP 400)

Follows [RFC 9457](https://www.rfc-editor.org/rfc/rfc9457) Problem Details format:

```json
{
  "type": "https://tools.ietf.org/html/rfc9457",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Buildings[0].Name": ["Building Name must not be empty."],
    "Buildings[0].Width": ["Building Width must be greater than 0."]
  }
}
```

If **any** building fails validation, the entire request is rejected. No clash detection runs.

## Business Rules

### Validation Rules

- All building attributes (name, type, width, length, x, y) must be provided
- Building dimensions (width, length) must be positive numbers (> 0)
- Building positions (x, y) must be non-negative (>= 0)
- Building names must be unique within a request
- Building type must be one of the five known types (case-sensitive)
- Site plan dimensions must be positive (> 0)

### General Rules

- **Boundary**: Buildings must be fully within the site plan. A building at (x, y) with dimensions (width, length) occupies `[x, x+width] x [y, y+length]`. The site plan occupies `[0, width] x [0, length]`.
- **Overlap**: Buildings cannot share interior space. Touching edges (distance = 0) is **not** overlap.
- **Clearance**: Each building must maintain a minimum 10-unit edge-to-edge distance from other buildings.

### Zoning Rules

- **Nightclubs** must be at least **200 units** (edge-to-edge) from any **school**
- **Residential buildings** must be at least **150 units** (edge-to-edge) from any **stadium** or **nightclub**

### Rule Interaction

- **Overlap subsumes clearance**: If two buildings overlap, only the Overlap clash is reported -- the redundant InsufficientClearance clash for that pair is suppressed.
- A building pair can trigger multiple independent violations (e.g., Overlap + ZoningViolation).
- Distance is measured as the minimum **Euclidean edge-to-edge distance** between axis-aligned rectangles.
- Threshold comparisons use strict inequality: distance >= N means the rule is satisfied; distance < N is a violation.

## Design Decisions

| Decision | Rationale |
|----------|-----------|
| **Clean Architecture** | Enforces separation of concerns. Domain has zero dependencies. Each layer is independently testable. |
| **CQRS via MediatR** | Clean command/handler separation. Pipeline behaviors enable cross-cutting validation without polluting business logic. |
| **Strategy Pattern for rules** | Each rule implements `IClashDetectionRule`. New rules are auto-discovered via assembly scanning -- zero configuration needed. Follows Open/Closed Principle. |
| **No Event Sourcing** | The operation is stateless (input in, clashes out). No state to track over time. |
| **No database** | Computation is pure and ephemeral. Each request is independent. |
| **FluentValidation pipeline** | Validation fires before the handler via `ValidationBehavior<TRequest, TResponse>`. Invalid requests never reach business logic. |
| **Overlap subsumes clearance** | Applied in the handler as post-processing, not in individual rules. Keeps rules independent and unaware of each other. |
| **Singleton rules** | Rules are stateless (no mutable fields). Singleton registration avoids per-request allocations. |
| **GeometryHelper utility** | Shared distance calculation and pair iteration extracted to avoid coupling between rules. |
| **RFC 9457 error responses** | Industry standard for API error reporting. Handles both validation errors and malformed JSON. |
| **BDD-first development** | Reqnroll feature files written before implementation. Ensures requirements coverage and enables regression testing. |

## Project Structure

```
src/
  BimCollab.ClashDetection.Domain/           # Entities, Value Objects, Enums, Rules (zero dependencies)
    Entities/                                # Building, SitePlan
    ValueObjects/                            # Position, Dimensions
    Enums/                                   # BuildingType, ClashType, ClashSeverity
    Models/                                  # Clash
    Rules/                                   # IClashDetectionRule + 5 implementations
    Utilities/                               # GeometryHelper (shared distance calculation)

  BimCollab.ClashDetection.Application/      # CQRS commands, handlers, validators (depends on Domain)
    ClashDetection/Commands/                 # DetectClashesCommand + Handler
    ClashDetection/Validators/               # FluentValidation rules
    Common/Behaviors/                        # MediatR pipeline (ValidationBehavior)

  BimCollab.ClashDetection.Api/              # Minimal API endpoints (depends on Application)
    Endpoints/                               # POST /api/clash-detection/detect
    Contracts/                               # Request/Response DTOs
    Middleware/                              # ValidationExceptionHandler (RFC 9457)

  BimCollab.ClashDetection.AppHost/          # .NET Aspire orchestrator
  BimCollab.ClashDetection.ServiceDefaults/  # OpenTelemetry, health checks, resilience

tests/
  BimCollab.ClashDetection.Domain.Tests/     # Unit tests for all 5 rules
  BimCollab.ClashDetection.Application.Tests/# Handler + validator tests
  BimCollab.ClashDetection.Api.Tests/        # Integration tests (WebApplicationFactory)
  BimCollab.ClashDetection.Specs/            # BDD scenarios (Reqnroll/Gherkin)
    Features/                                # ClashDetection, EdgeCases, InputValidation
    StepDefinitions/                         # HTTP-based step implementations
```

## Scalability

### Current Approach: Why O(n^2) Is the Right Choice Here

The clash detection rules (overlap, clearance, zoning) need to compare buildings against each other in pairs. With **n** buildings, there are **n(n-1)/2** unique pairs to check. This is O(n^2) complexity.

For the assessment's dataset of 5 buildings, that means:
- **10 pairs** per pairwise rule (5 choose 2)
- **3 pairwise rules** (overlap, clearance, zoning) = ~30 comparisons total
- Executes in **microseconds**

At this scale, adding complexity (spatial indexes, acceleration structures) would hurt readability without measurable performance benefit. The brute-force approach is the most maintainable and correct choice.

### What Happens as n Grows

| n (buildings) | Pairs per rule | Total comparisons (3 rules) | Approximate time |
|---------------|---------------|----------------------------|-----------------|
| 5 | 10 | 30 | < 1ms |
| 50 | 1,225 | 3,675 | < 10ms |
| 500 | 124,750 | 374,250 | ~100ms |
| 5,000 | 12,497,500 | 37,492,500 | ~10s |
| 50,000 | 1,249,975,000 | 3.7 billion | minutes |

The O(n^2) approach works well up to a few hundred buildings. Beyond that, the quadratic growth becomes the bottleneck.

### Scaling Roadmap

**Tier 1 (up to ~50 buildings): No changes needed**

Current implementation is sufficient. The readability and maintainability advantages of the simple approach outweigh any performance concerns.

**Tier 2 (50-500 buildings): Low-effort optimizations**

- **Pre-filter building types once**: Instead of each zoning rule calling `.Where(b => b.Type == ...)` per evaluation, build a `Dictionary<BuildingType, List<Building>>` once in the handler and pass pre-grouped buildings to rules. Eliminates redundant filtering.
- **Compare squared distances**: The `CalculateEdgeToEdgeDistance` method currently calls `Math.Sqrt(gapX^2 + gapY^2)`. Since we only compare against thresholds (`distance < 10`), we can compare `gapX^2 + gapY^2 < 100` instead -- avoiding the expensive square root for the majority of pairs that are not violations.

**Tier 3 (500-5,000 buildings): Spatial indexing**

Replace the brute-force pair iteration with a **spatial index** to avoid checking distant building pairs entirely:

- **Grid-based spatial hash**: Divide the site plan into cells sized to the largest distance threshold (200 units for nightclub-school zoning). Each building maps to one or more cells. Pairwise checks only happen between buildings in the same cell or neighboring cells. Average complexity drops from O(n^2) to O(n * k) where k is the average number of nearby buildings per cell.
- **R-tree**: A tree structure that groups nearby rectangles into hierarchical bounding boxes. Efficient for range queries ("find all buildings within 200 units of this nightclub"). Libraries like `RBush` provide ready-made implementations.

The key insight: most building pairs are far apart and can be skipped entirely. A spatial index eliminates these irrelevant comparisons.

**Tier 4 (5,000+ buildings): Advanced techniques**

- **Sweep-line algorithm** for overlap detection: Sort buildings by X coordinate, sweep a vertical line left-to-right, and maintain an active set of buildings the line currently intersects. Only check overlaps between active buildings. Reduces overlap detection from O(n^2) to O(n log n).
- **Parallel rule execution**: Since rules are pure functions with no shared state, run all 5 rules concurrently using `Task.WhenAll` or `Parallel.ForEach`. With spatial indexing, each rule's workload is already smaller, and parallelism provides an additional linear speedup.
- **Streaming results**: Instead of collecting all clashes into a list, yield results as they're found using `IAsyncEnumerable<Clash>`. Reduces memory footprint for datasets with many violations.

### Why the Architecture Supports This

The **Strategy pattern** and **GeometryHelper abstraction** make these optimizations straightforward:

1. **`IClashDetectionRule` interface**: Each rule can be independently optimized or replaced. Upgrading `ClearanceRule` to use spatial indexing doesn't require touching `OverlapRule` or any other rule.

2. **`GeometryHelper.GetPairs()`**: All pairwise rules call this single method to iterate building pairs. Replacing the brute-force nested loop with a spatial-index-backed pair generator is a **one-line change** -- all rules automatically benefit.

3. **`GeometryHelper.CalculateEdgeToEdgeDistance()`**: The distance formula is centralized. Switching to squared-distance comparison is a single method change that benefits all rules using distance thresholds.

4. **DI-registered rules**: Rules are discovered via assembly scanning and injected as `IEnumerable<IClashDetectionRule>`. The handler doesn't know or care how many rules exist or how they work internally. Adding a spatially-optimized rule variant is transparent.

This means the solution can scale from 5 to 50,000 buildings through **incremental, isolated changes** -- no architectural rewrites needed.
