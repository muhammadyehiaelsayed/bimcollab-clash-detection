# BIM Clash Detection API

Tech screening for BIMcollab. The API takes a site plan and a list of
buildings, runs validation and five rules, and returns the clashes.

Run: `dotnet run --project src/BimCollab.ClashDetection.Api`, then open
http://localhost:5201/scalar/v1.
Tests: `dotnet test` (104 tests).

Below is what the reviewer asked for: my approach, the key decisions,
the trade-offs, and what I'd change for production.


## How I worked on this

I used **spec-driven development with BDD**. Specs first, code second.
That's my default way of working as an architect, and I kept it here.

**1. Read the brief and asked questions before any code.**
I sent the reviewer three questions up front (see
`follow-up-questions.md`): how big can the dataset be, are the rules
fixed or configurable, and what output format is expected. The answers
shaped everything. Small dataset means brute force is fine. Fixed rules
with extensibility valued means Strategy pattern is worth the
structure. JSON output means clean DTOs.

**2. Listed the rules on paper.**
Validation, boundary, overlap, clearance, two zoning rules. After
validation, every rule has the same shape: take a site plan and
buildings, return clashes. That's where the Strategy pattern came from.
Validation is different because it returns errors, so it goes before
the rules, not inside them.

**3. Wrote the specs as Reqnroll (Gherkin) feature files.**
Happy paths, edge cases (touching edges, distance exactly at the
threshold, empty list, single building), validation cases. All written
before any production code. The feature files in
`tests/.../Specs/Features` are the contract.

**4. Designed the architecture on paper.**
Clean Architecture, Domain with zero dependencies, Application with
CQRS via MediatR, one POST endpoint in a Minimal API.

**5. Brainstormed with AI, then had it implement.**
I walked through the layers with AI, corrected anything that didn't
match my design, then had AI generate the scaffolding. I wrote the
parts I wanted to own myself: the geometry math and the rule merging
in the handler.

**6. Ran the specs.**
If something failed, I fixed the code, not the spec.

AI did a lot of the typing. The specs, the architecture, and the
trade-offs are mine.


## Key decisions

**Clean Architecture.**
Domain depends on nothing. Application depends on Domain. API depends
on Application. It's more structure than 5 buildings need, but the
brief asks about extensibility. If the transport changes (HTTP to a
queue consumer), the rules don't move.

**Strategy pattern for the rules.**
Each rule implements `IClashDetectionRule`. DI picks them up by
assembly scanning. Adding a rule is one new class, nothing else
changes. This is the piece I care about most, because "what if another
rule shows up" is the main question the brief is testing.

**CQRS with MediatR.**
One command today, so MediatR is more than I strictly need. I kept it
because the pipeline behaviour for validation is cleaner than any
alternative, and future queries come free.

**BDD with Reqnroll.**
The brief already reads like Gherkin. Feature files mean a
non-developer can check "did he build what we asked?" without reading
C#. It also forced me to write the edge cases upfront.

**FluentValidation in a pipeline, not inside the handler.**
`ValidationBehavior<TRequest, TResponse>` runs FluentValidation before
every handler automatically. Field checks, enum checks (case-sensitive),
unique names, max 500 buildings. If anything fails, the whole request
is rejected before any rule runs. No half-valid responses.

**Overlap hides clearance, handled outside the rules.**
Two buildings that overlap are also closer than 10 units, so both
rules fire. I suppress the clearance clash in the handler after all
rules have run. I kept this out of `ClearanceRule` on purpose. Rules
shouldn't know about each other.

**.NET 10, Minimal API, Scalar, Aspire.**
Newest .NET on purpose. Minimal API because controllers are heavy for
one endpoint. Scalar for cleaner docs. Aspire gives me OpenTelemetry,
health checks, and a dashboard without wiring anything. Not required
by the brief. It's the observability baseline I'd bring to any real
service.


## Trade-offs I considered

**One project vs. Clean Architecture.**
One project with a few files would ship in 30 minutes. Clean
Architecture wins because the brief is about extensibility.

**Plain service class vs. MediatR.**
A simple `Detect(request)` method works and needs no library. MediatR
wins because the validation pipeline is cleaner and future queries are
free.

**DataAnnotations vs. FluentValidation.**
DataAnnotations are fine for simple fields. They break on "names must
be unique" and "type is one of five, case-sensitive". FluentValidation
handles all of it in one class.

**Reqnroll vs. xUnit only.**
Reqnroll adds a project and a build step. Pure xUnit with theory data
covers the same ground. I kept Reqnroll because the brief already
reads like scenarios and feature files are readable by non-developers.

**Inline rules vs. Strategy pattern.**
A 60-line method with 5 `if` blocks is shorter to read. Strategy wins
because each rule is independently testable and adding one is a
one-file change.

**O(n²) brute force vs. spatial index.**
Brute force for 5 buildings is microseconds. A spatial index costs
readability. I picked brute force and documented the scaling path
below. `GeometryHelper.GetPairs` is the one place that changes later
if we ever need to switch.


## Shortcuts (what I'd do differently for production)

I already asked three questions before writing any code. For a
production version I'd ask a lot more, because "production" isn't one
thing. An internal tool and a public API need very different
architectures.

### Questions I'd ask first

**Business and product.** Standalone service, or embedded in a bigger
product? Who's the actual end user, and what does success look like for
them? What's on the roadmap: new rule types, customer-specific rules,
AI-assisted detection?

**Consumers and integration.** Who calls this: client apps, a UI, or
other backend services? Sync (user waits) or async (batch job)?
Realistic payload size today, and in two years?

**Scale.** Expected load and peak? Latency targets? Single region or
global? Any data-residency constraints?

**Security.** Is the geometry data sensitive? Tenant model: single,
per-customer keys, or SSO? Audit trail? Any compliance standards
(GDPR, ISO 27001, SOC 2)?

**Cost and operations.** Budget per request or per tenant? Who operates
this at 3am? Preferred cloud, or existing infrastructure we need to
fit into?

### How the answers change the tech

**Auth.** Internal behind a VPN: Entra ID, one app, done. Public API
with third-party consumers: OAuth per customer, tenant isolation in
every handler, audit logs.

**Thresholds.** The 10, 150, 200 are `const double` in the rules. Fine
if the rule is global. For per-region variants, move them to
`IOptions<ZoningOptions>` from config. For customer-authored rules,
that becomes a separate service with a UI.

**Persistence.** API is stateless today. If clash reports are used
later (disputes, task tracking, analytics), I'd store request and
result per tenant in Postgres.

**Sync vs async.** Request/response works for small plans. For a
5,000-building masterplan, HTTP times out before the algorithm
finishes. Past some size I'd return `202 Accepted` with a job id, run
detection with a spatial index, and deliver via webhook or polling.

**Rate limiting.** Internal: not needed. Public: depends on pricing.
Flat per-seat gets an RPM cap in the gateway. Metered pricing needs
rate limiting and usage counting in Redis, not in-memory.

**Observability.** Aspire dashboard is fine on my laptop. Production
exports to Application Insights or similar. Multi-tenant: tenant id on
every trace so "slow for customer X" is a filter, not an
investigation.

**API versioning.** URL-based (`/v1/clashes`) before a second
consumer. Deprecation headers with sunset dates. And agreement with
product on what "breaking" means, because renaming an enum breaks a
strongly-typed C# client even if the JSON still parses.

**Errors.** English only today. If errors end up in a user-facing
tool, they need localisation, with stable error codes separated from
the human-readable messages.

**Benchmarks.** Add a BenchmarkDotNet project with p99 latency gates in
CI. Plus JSON contract tests, so enum renames don't silently break
consumers.


## Project layout

```
src/
  Domain/          Entities, rules, geometry. Zero dependencies.
  Application/     CQRS command, handler, validator, MediatR pipeline.
  Api/             Minimal API, problem-details middleware, Scalar.
  AppHost/         Aspire orchestrator.
  ServiceDefaults/ OpenTelemetry, health checks.

tests/
  Domain.Tests/        32 tests, one per rule.
  Application.Tests/   36 tests, handler + validator.
  Api.Tests/           11 tests, integration via WebApplicationFactory.
  Specs/               25 Reqnroll scenarios.
```

Total: 104 tests.


## Business rules (from the brief)

- Validation: all fields required, width/length > 0, x/y ≥ 0, unique
  names, max 500 buildings.
- Boundary: buildings fit fully inside the site plan.
- Overlap: no shared interior. Touching edges is not overlap.
- Clearance: ≥ 10 units edge-to-edge between any two buildings.
- Nightclub vs School: ≥ 200 units apart.
- Residential vs Stadium or Nightclub: ≥ 150 units apart.

If two buildings overlap, only the overlap is reported for that pair.


## Scaling past 5 buildings

The 3 pairwise rules are O(n²).

- 5 buildings: 10 pairs, microseconds.
- 500 buildings: ~125k pairs, ~100ms.
- 5,000 buildings: ~12M pairs, ~10 seconds.

Path if it ever matters:

- Up to ~50: no change needed.
- 50 to 500: pre-group by type once, compare squared distances (skip
  the `Math.Sqrt`).
- 500 to 5,000: swap `GeometryHelper.GetPairs` from a nested loop to a
  spatial index (grid hash or R-tree via RBush). One-line change.
- 5,000+: sweep-line for overlap detection (O(n log n)), parallel rule
  execution via `Task.WhenAll`, stream results with
  `IAsyncEnumerable<Clash>` if memory matters.

This works because `GeometryHelper.GetPairs` is the single place every
pairwise rule iterates pairs, and `CalculateEdgeToEdgeDistance` is the
single place the distance math lives. Swap either, every rule
benefits.
