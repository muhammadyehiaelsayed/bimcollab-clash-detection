# Data Model: BIM Clash Detection API

**Feature**: 001-clash-detection
**Date**: 2026-04-10
**Status**: Complete
**Input**: spec.md, research.md

---

## Layer Overview

```
Domain (innermost, no dependencies)
├── Entities:     SitePlan, Building
├── Value Objects: Position, Dimensions
├── Enums:        BuildingType, ClashType, ClashSeverity
├── Models:       Clash
└── Rules:        IClashDetectionRule

Application (depends on Domain)
├── Commands:     DetectClashesCommand, DetectClashesResult
├── Handlers:     DetectClashesCommandHandler
├── Validators:   DetectClashesCommandValidator
└── Behaviors:    ValidationBehavior<TRequest, TResponse>

Api (depends on Application)
├── Contracts:    DetectClashesRequest, ClashDetectionResponse
└── Endpoints:    ClashDetectionEndpoints
```

---

## Domain Layer

### Value Object: Position

Represents the (x, y) coordinate of a building's bottom-left corner on the site plan.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `X` | `double` | >= 0 | Horizontal position from site origin |
| `Y` | `double` | >= 0 | Vertical position from site origin |

```csharp
namespace BimCollab.ClashDetection.Domain.ValueObjects;

public record Position(double X, double Y);
```

**Notes**:
- Immutable record type (value semantics, structural equality).
- Validation (>= 0) is enforced at the Application layer by FluentValidation, not in the value object itself. The domain stays pure.
- `double` chosen over `decimal` because these are spatial coordinates, not financial values. Floating-point precision is acceptable.

---

### Value Object: Dimensions

Represents the width and length of a building or site plan.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Width` | `double` | > 0 | Horizontal extent |
| `Length` | `double` | > 0 | Vertical extent |

```csharp
namespace BimCollab.ClashDetection.Domain.ValueObjects;

public record Dimensions(double Width, double Length);
```

**Notes**:
- Same rationale as Position: immutable record, validation at Application layer.

---

### Enum: BuildingType

Enumerates the known building classifications. Values match the assessment dataset exactly.

| Value | Description |
|-------|-------------|
| `School` | Educational facility |
| `Nightclub` | Entertainment venue |
| `Stadium` | Sports/event venue |
| `ResidentialBuilding` | Housing |
| `Office` | Commercial workspace |

```csharp
namespace BimCollab.ClashDetection.Domain.Enums;

public enum BuildingType
{
    School,
    Nightclub,
    Stadium,
    ResidentialBuilding,
    Office
}
```

**Notes**:
- Case-sensitive string parsing in the API layer (e.g., `"ResidentialBuilding"` not `"residential_building"`).
- Unknown types in JSON will fail deserialization, which is handled as a validation error.

---

### Enum: ClashType

Enumerates the categories of detected clashes.

| Value | Description | Rule |
|-------|-------------|------|
| `BoundaryViolation` | Building extends beyond site plan | BoundaryRule |
| `Overlap` | Two buildings share interior space | OverlapRule |
| `InsufficientClearance` | Buildings closer than 10 units | ClearanceRule |
| `ZoningViolation` | Zoning distance rule violated | ZoningRule |

```csharp
namespace BimCollab.ClashDetection.Domain.Enums;

public enum ClashType
{
    BoundaryViolation,
    Overlap,
    InsufficientClearance,
    ZoningViolation
}
```

**Notes**:
- `ValidationError` has been removed from the existing enum. Validation errors are handled at the Application layer via FluentValidation and returned as RFC 9457 Problem Details, not as clashes. Mixing validation errors with detection results would conflate two distinct concerns.

---

### Enum: ClashSeverity

Severity level assigned to each clash type.

| Value | Applies To | Rationale |
|-------|-----------|-----------|
| `Critical` | BoundaryViolation, Overlap | Physical impossibility -- building cannot exist in that configuration |
| `Warning` | InsufficientClearance, ZoningViolation | Regulatory non-compliance -- physically possible but not permitted |

```csharp
namespace BimCollab.ClashDetection.Domain.Enums;

public enum ClashSeverity
{
    Warning,
    Critical
}
```

**Notes**:
- The existing `Error` value is removed. The spec defines only two severity levels: Critical and Warning. The `Error` level was not specified in any requirement or clarification.
- Severity is deterministic from ClashType. It is set by each rule when constructing a Clash instance.

---

### Entity: SitePlan

The rectangular boundary of the construction site. Origin is always (0, 0).

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Dimensions` | `Dimensions` | Width > 0, Length > 0 | Site plan size |

```csharp
namespace BimCollab.ClashDetection.Domain.Entities;

public class SitePlan
{
    public required Dimensions Dimensions { get; init; }
}
```

**Derived Properties** (used by BoundaryRule):
- Boundary: `[0, Width] x [0, Length]`

**Notes**:
- No `Position` field because the site plan origin is always (0, 0) per the spec assumptions.
- `required` modifier enforces that `Dimensions` is set at construction time.
- `init` makes the property immutable after construction.

---

### Entity: Building

A proposed structure placed on the site plan.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| `Name` | `string` | Required, unique within request | Human-readable identifier |
| `Type` | `BuildingType` | Required, valid enum value | Building classification |
| `Position` | `Position` | X >= 0, Y >= 0 | Bottom-left corner coordinate |
| `Dimensions` | `Dimensions` | Width > 0, Length > 0 | Building size |

```csharp
namespace BimCollab.ClashDetection.Domain.Entities;

public class Building
{
    public required string Name { get; init; }
    public required BuildingType Type { get; init; }
    public required Position Position { get; init; }
    public required Dimensions Dimensions { get; init; }
}
```

**Derived Properties** (used by rules):
- Right edge: `Position.X + Dimensions.Width`
- Top edge: `Position.Y + Dimensions.Length`
- Occupied area: `[X, X+Width] x [Y, Y+Length]`

**Notes**:
- The entity is a plain class (not a record) because it represents an identity-bearing domain object.
- `Name` serves as the logical identifier within a request (unique per FR-005).
- No database ID because the system is stateless -- no persistence.

---

### Model: Clash

A detected issue in the site plan. This is a model (not an entity or value object) because it is a computed result with no identity or lifecycle.

| Field | Type | Description |
|-------|------|-------------|
| `BuildingNames` | `IReadOnlyList<string>` | Names of involved buildings (1 for boundary, 2 for pair-wise) |
| `Type` | `ClashType` | Category of violation |
| `Severity` | `ClashSeverity` | Critical or Warning |
| `Description` | `string` | Human-readable explanation of the issue |

```csharp
namespace BimCollab.ClashDetection.Domain.Models;

public class Clash
{
    public required IReadOnlyList<string> BuildingNames { get; init; }
    public required ClashType Type { get; init; }
    public required ClashSeverity Severity { get; init; }
    public required string Description { get; init; }
}
```

**Notes**:
- `BuildingNames` uses a list rather than two nullable fields because boundary violations involve one building while pair-wise rules involve two. A list handles both uniformly.
- `Description` is generated by each rule and includes specifics (e.g., "Pulse extends beyond site boundary on the Y-axis: building occupies [0, 700] but site length is 500" or "Willow Residence and Maple Plaza overlap").
- Clash has no ID because it is ephemeral -- computed per request, never persisted.

---

## Rules Interface

### IClashDetectionRule (Strategy Pattern)

The contract that all clash detection rules implement. Each rule evaluates the entire site plan and returns zero or more clashes.

```csharp
namespace BimCollab.ClashDetection.Domain.Rules;

public interface IClashDetectionRule
{
    IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings);
}
```

**Contract**:
- Input: the site plan and all buildings (already validated by FluentValidation before this point).
- Output: a list of clashes detected by this rule. Empty list if no violations found.
- Rules must NOT throw exceptions for valid inputs. Any clash is reported as a `Clash` object, not an exception.
- Rules must be stateless. No side effects, no shared mutable state.

**Implementations** (5 rules):

| Rule Class | ClashType | Severity | Scope |
|-----------|-----------|----------|-------|
| `BoundaryRule` | BoundaryViolation | Critical | Each building vs. site plan |
| `OverlapRule` | Overlap | Critical | Each unique building pair |
| `ClearanceRule` | InsufficientClearance | Warning | Each building pair (handler filters overlapping pairs) |
| `NightclubSchoolZoningRule` | ZoningViolation | Warning | Each nightclub vs. each school |
| `ResidentialZoningRule` | ZoningViolation | Warning | Each residential vs. each stadium/nightclub |

**Rule Interaction -- Overlap Subsumes Clearance**:
- The `OverlapRule` and `ClearanceRule` are independent rules that both examine building pairs.
- The handler (not the rules) applies the subsumption logic after all rules have run: if a pair has both Overlap and InsufficientClearance, the InsufficientClearance is removed.
- This keeps individual rules simple and unaware of each other.

---

## Application Layer

### Command: DetectClashesCommand

The MediatR command that carries the input payload from the API to the handler.

```csharp
public record DetectClashesCommand : IRequest<DetectClashesResult>
{
    public required SitePlanDto SitePlan { get; init; }
    public required IReadOnlyList<BuildingDto> Buildings { get; init; }
}

public record SitePlanDto(double Width, double Length);

public record BuildingDto(
    string Name,
    string Type,
    double Width,
    double Length,
    double X,
    double Y);
```

**Notes**:
- DTOs use primitive types (`string`, `double`) because the command is the Application layer's representation of the input. Mapping to domain entities (with value objects and enums) happens in the handler.
- `BuildingDto.Type` is a `string` to allow FluentValidation to report "invalid building type" rather than failing silently during enum parsing.

### Result: DetectClashesResult

```csharp
public record DetectClashesResult(IReadOnlyList<Clash> Clashes);
```

### Validator: DetectClashesCommandValidator

FluentValidation rules applied before the handler executes.

| Target | Rule | Error Message |
|--------|------|---------------|
| `SitePlan.Width` | > 0 | Site plan width must be greater than zero |
| `SitePlan.Length` | > 0 | Site plan length must be greater than zero |
| `Buildings` | Not null | Buildings list is required |
| `Buildings[i].Name` | Not empty | Building name is required |
| `Buildings[i].Type` | Valid enum value | Building type must be one of: School, Nightclub, Stadium, ResidentialBuilding, Office |
| `Buildings[i].Width` | > 0 | Building width must be greater than zero |
| `Buildings[i].Length` | > 0 | Building length must be greater than zero |
| `Buildings[i].X` | >= 0 | Building X position must be greater than or equal to zero |
| `Buildings[i].Y` | >= 0 | Building Y position must be greater than or equal to zero |
| `Buildings` | Unique names | Building names must be unique. Duplicate: '{name}' |

### Behavior: ValidationBehavior

Generic pipeline behavior that intercepts all MediatR requests, runs FluentValidation, and throws `ValidationException` on failure. Already registered in `DependencyInjection.cs`.

---

## Mapping Summary

```
API Request (JSON)        -> DetectClashesRequest (Api contract)
DetectClashesRequest      -> DetectClashesCommand (Application DTO)
DetectClashesCommand      -> SitePlan + List<Building> (Domain entities, in handler)
List<Clash> (Domain)      -> DetectClashesResult (Application)
DetectClashesResult       -> ClashDetectionResponse (Api contract, JSON)
ValidationException       -> HttpValidationProblemDetails (RFC 9457, JSON)
```
