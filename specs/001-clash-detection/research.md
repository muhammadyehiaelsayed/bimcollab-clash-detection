# Research: BIM Clash Detection API

**Feature**: 001-clash-detection
**Date**: 2026-04-10
**Status**: Complete
**Input**: spec.md, bimcollab-techscreening-v1.1_1.pdf, follow-up-questions.md

---

## 1. Rectangle Distance Calculation (Edge-to-Edge Minimum Distance)

### Problem

Given two axis-aligned rectangles defined by position (x, y) and dimensions (width, length), compute the minimum edge-to-edge distance between them. This distance is needed for the 10-unit clearance rule, the 200-unit nightclub-school zoning rule, and the 150-unit residential zoning rule.

### Decision

Use the separating-axis gap formula for axis-aligned rectangles:

```
Rectangle A: [x1, x1+w1] x [y1, y1+l1]
Rectangle B: [x2, x2+w2] x [y2, y2+l2]

gapX = max(0, max(x1, x2) - min(x1+w1, x2+w2))
gapY = max(0, max(y1, y2) - min(y1+l1, y2+l2))

distance = sqrt(gapX^2 + gapY^2)
```

When one gap is zero (rectangles overlap on that axis), the distance reduces to the other gap. When both gaps are zero, the rectangles overlap and distance is zero (or negative, handled separately by the overlap check).

### Rationale

- Mathematically exact for axis-aligned rectangles with no approximation error.
- O(1) computation per pair -- no iteration or search required.
- Handles all spatial relationships: separated on one axis, separated on both axes (corner-to-corner), touching, and overlapping.
- Well-established computational geometry result with no edge-case surprises.
- The formula naturally returns 0 when rectangles touch or overlap, which aligns with the rule that overlapping pairs skip the clearance check (overlap subsumes clearance).

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Center-to-center Euclidean distance | Incorrect for rectangular buildings; center distance ignores shape and orientation. A large building can be far center-to-center but touching edge-to-edge. |
| Minkowski sum approach | Correct but unnecessarily complex for axis-aligned rectangles. Adds implementation overhead without benefit. |
| Vertex enumeration (check all 16 vertex-to-edge combinations) | Correct but O(n) per pair with many branches. The gap formula achieves the same result in O(1) with cleaner code. |
| GJK algorithm | General-purpose convex shape distance algorithm. Massive overkill for axis-aligned rectangles. |

---

## 2. Rectangle Overlap Detection (Axis-Aligned Rectangle Intersection)

### Problem

Determine whether two axis-aligned rectangles share interior space. Per the spec, sharing only an edge (distance = 0, no interior overlap) does NOT count as overlap -- it triggers clearance violation instead.

### Decision

Use the separating-axis theorem for axis-aligned rectangles. Two rectangles do NOT overlap if and only if they are separated on at least one axis:

```
Rectangle A: [x1, x1+w1] x [y1, y1+l1]
Rectangle B: [x2, x2+w2] x [y2, y2+l2]

overlaps = (x1 < x2+w2) && (x2 < x1+w1) && (y1 < y2+l2) && (y2 < y1+l1)
```

Strict inequality (`<` not `<=`) ensures that edge-touching rectangles are NOT classified as overlapping. This directly satisfies the edge case: "Two buildings share exactly an edge (distance = 0) but do not share interior area -- they do not overlap."

### Rationale

- The separating-axis theorem is the standard approach for AABB (Axis-Aligned Bounding Box) overlap detection.
- Strict inequality is critical: the spec explicitly states that edge-touching is NOT overlap. Using `<=` would produce false positives.
- O(1) per pair, four comparisons, no branching beyond the boolean result.
- The result feeds directly into the overlap-subsumes-clearance logic: if `overlaps == true`, report Overlap and skip clearance for that pair.

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Intersection area calculation | More complex; calculates area when we only need a boolean. Also requires careful handling of the edge-touching case. |
| Polygon clipping (Sutherland-Hodgman) | General-purpose polygon intersection. Unnecessary for axis-aligned rectangles. |
| Pixel/grid rasterization | Approximate, memory-intensive, and slow. Not suitable for analytic geometry. |

---

## 3. MediatR Pipeline Behavior for FluentValidation Integration

### Problem

Validation must run before the command handler. If validation fails, the handler must NOT execute and the pipeline must return structured validation errors. The spec requires: "If validation fails, no detection runs" (FR-006).

### Decision

Implement a generic `ValidationBehavior<TRequest, TResponse>` as an `IPipelineBehavior<TRequest, TResponse>`. The behavior:

1. Accepts all registered `IValidator<TRequest>` instances via constructor injection.
2. Runs all validators against the request before calling `next()`.
3. If any validation errors exist, throws a custom `ValidationException` containing the list of failures.
4. If validation passes, calls `next()` to proceed to the handler.

The `ValidationException` is caught by a global exception handler (or middleware) in the API layer and mapped to an RFC 9457 Problem Details response with HTTP 400.

```
Pipeline: Request -> ValidationBehavior -> Handler -> Response
                          |
                    (validation fails)
                          |
                  throw ValidationException
                          |
                  caught by exception handler
                          |
                  HTTP 400 Problem Details
```

Registration in DI (already scaffolded in `DependencyInjection.cs`):

```csharp
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
services.AddValidatorsFromAssembly(assembly);
```

### Rationale

- MediatR's `IPipelineBehavior` is the idiomatic cross-cutting concern pattern. It acts as middleware around every command, exactly like ASP.NET Core middleware but at the CQRS level.
- FluentValidation's `IValidator<T>` is auto-discovered by `AddValidatorsFromAssembly`, which the DI class already calls.
- Throwing a custom exception keeps the pipeline behavior clean (single responsibility: validate and fail or proceed). The API layer handles HTTP mapping.
- The existing `DependencyInjection.cs` already registers `ValidationBehavior<,>` and validators from assembly. Only the behavior implementation body needs to be filled in.

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Validate inside the handler | Violates single responsibility. Every handler would need validation boilerplate. Cross-cutting concerns belong in pipeline behaviors. |
| Return a result object with errors (no exception) | Requires every command result type to carry an errors collection. Adds complexity to all result types. Exception-based flow is cleaner when validation failure is an exceptional/rejected path. |
| ASP.NET Core model validation (DataAnnotations) | Runs at the API layer before MediatR. Couples validation to the API contract instead of the command. Harder to test in isolation. FluentValidation is more expressive for complex rules. |
| MediatR `IRequestPreProcessor` | Cannot short-circuit the pipeline. Pre-processors always run and cannot prevent the handler from executing. |

---

## 4. RFC 9457 Problem Details for .NET

### Problem

Validation errors must return structured, machine-readable error responses (FR-013). The spec calls for RFC 9457 Problem Details with HTTP 400.

### Decision

Use ASP.NET Core's built-in `ProblemDetails` and `HttpValidationProblemDetails` types, which natively implement RFC 9457. Map `ValidationException` to a 400 response in a global exception handler.

Response structure:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "Buildings[0].Name": ["'Name' must not be empty."],
    "Buildings[1].Width": ["'Width' must be greater than '0'."]
  }
}
```

Implementation approach:

- Register a global exception handler using `IExceptionHandler` (introduced in .NET 8, available in .NET 10).
- Catch `ValidationException` and map FluentValidation failures to the `errors` dictionary keyed by property name.
- Return `TypedResults.ValidationProblem(errors)` or write `HttpValidationProblemDetails` directly.
- For other unhandled exceptions, return a generic 500 Problem Details response.

### Rationale

- `HttpValidationProblemDetails` is built into ASP.NET Core -- no additional packages needed.
- The `errors` dictionary format matches what API consumers expect from .NET APIs (same format as DataAnnotations validation).
- `IExceptionHandler` is the modern .NET approach for global exception handling, replacing the older `UseExceptionHandler` middleware lambda pattern.
- RFC 9457 is the current standard (supersedes RFC 7807). ASP.NET Core's implementation is compliant.

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Custom error response format | Non-standard. Every API consumer must learn a bespoke format. RFC 9457 is the industry standard. |
| Return errors in the 200 response body | Violates HTTP semantics. 400 signals client error; mixing errors into 200 responses confuses API consumers and breaks standard HTTP client behavior. |
| Middleware-based exception handling (UseExceptionHandler lambda) | Works but `IExceptionHandler` is the newer, more testable, and more composable approach in .NET 8+. |
| Result pattern without exceptions | Would require the endpoint to inspect a result object and branch on success/failure. Adds coupling between the API layer and the command result. The exception approach keeps the endpoint clean. |

---

## 5. Strategy Pattern Registration in .NET DI

### Problem

Clash detection rules must follow the Strategy pattern (`IClashDetectionRule` interface) so that each rule is independently testable, extensible, and registered via DI. The handler needs all rules injected to execute them against the site plan.

### Decision

Register all `IClashDetectionRule` implementations as `IEnumerable<IClashDetectionRule>` using assembly scanning. The handler receives the collection via constructor injection and iterates all rules.

```csharp
// In DependencyInjection.cs
services.AddTransient<IClashDetectionRule, BoundaryRule>();
services.AddTransient<IClashDetectionRule, OverlapRule>();
services.AddTransient<IClashDetectionRule, ClearanceRule>();
services.AddTransient<IClashDetectionRule, ZoningRule>();
```

The handler:

```csharp
internal sealed class DetectClashesCommandHandler(
    IEnumerable<IClashDetectionRule> rules)
    : IRequestHandler<DetectClashesCommand, DetectClashesResult>
{
    public Task<DetectClashesResult> Handle(...)
    {
        var clashes = new List<Clash>();
        foreach (var rule in rules)
            clashes.AddRange(rule.Evaluate(sitePlan, buildings));
        // Post-process: overlap subsumes clearance
        return Task.FromResult(new DetectClashesResult(clashes));
    }
}
```

The interface contract:

```csharp
public interface IClashDetectionRule
{
    IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings);
}
```

### Rationale

- .NET DI natively supports `IEnumerable<T>` injection. Registering multiple implementations of the same interface automatically provides a collection to the consumer.
- Each rule is a self-contained class with no dependencies on other rules. This satisfies the assessment's emphasis on testability and extensibility.
- Adding a new rule requires only: (1) implement `IClashDetectionRule`, (2) register in DI. No handler modifications needed (Open/Closed Principle).
- Explicit registration (not assembly scanning) keeps the registration intentional and debuggable. With only 4 rules, assembly scanning adds complexity without benefit.
- The overlap-subsumes-clearance post-processing happens in the handler after all rules have run. This keeps individual rules simple and unaware of each other.

### Alternatives Considered

| Alternative | Rejected Because |
|-------------|-----------------|
| Assembly scanning for rules (`Scrutor` or reflection) | Adds a dependency for 4 classes. Explicit registration is clearer and more debuggable. Assembly scanning is justified for validators (FluentValidation provides it) but not for 4 known rules. |
| Chain of Responsibility pattern | Implies rules run sequentially and can short-circuit. Our rules are independent and all must run. Chain of Responsibility adds ordering complexity without benefit. |
| Rules as static methods | Not injectable, not testable in isolation, not extensible. Violates every principle the assessment evaluates. |
| Visitor pattern | Useful when operations vary by element type, but our rules operate on the entire site plan, not individual buildings. Adds unnecessary indirection. |
| Composite pattern (rules containing sub-rules) | The zoning rule could theoretically contain sub-rules per building type pair, but the number of zoning checks is small (2 pairs). A single ZoningRule with internal logic is simpler than a composite hierarchy. |
