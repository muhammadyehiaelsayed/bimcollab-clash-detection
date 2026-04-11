# Quickstart: BIM Clash Detection API

**Feature**: 001-clash-detection
**Date**: 2026-04-10

---

## Prerequisites

- **.NET 10 SDK** (net10.0) -- [download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- **Git** (to clone the repository)

Verify your installation:

```bash
dotnet --version
# Expected: 10.0.x
```

No additional tools, databases, or services are required. The API is fully self-contained.

---

## Build

From the repository root:

```bash
dotnet build BimCollab.ClashDetection.slnx
```

This builds all projects: Domain, Application, Api, AppHost, ServiceDefaults, and all test projects.

---

## Run the API (Standalone)

```bash
dotnet run --project src/BimCollab.ClashDetection.Api
```

The API starts on `http://localhost:5201`.

To verify it is running, open the Scalar API documentation in your browser:

```
http://localhost:5201/scalar/v1
```

Scalar provides an interactive API explorer where you can view endpoint schemas and send test requests directly from the browser.

---

## Run with Aspire Dashboard

The .NET Aspire AppHost provides a dashboard with distributed tracing, metrics, and structured logging.

```bash
dotnet run --project src/BimCollab.ClashDetection.AppHost --launch-profile http
```

The Aspire dashboard URL will be printed to the console on startup (typically `http://localhost:15888`). The dashboard shows:

- **Resources**: running services and their status
- **Console logs**: structured log output from the API
- **Traces**: distributed traces for each HTTP request
- **Metrics**: request counts, durations, error rates

The API itself is still available at `http://localhost:5201` when launched through the AppHost.

---

## Run Tests

Run all tests (unit, integration, BDD):

```bash
dotnet test
```

Run tests for a specific project:

```bash
# Domain unit tests
dotnet test tests/BimCollab.ClashDetection.Domain.Tests

# Application unit tests
dotnet test tests/BimCollab.ClashDetection.Application.Tests

# API integration tests
dotnet test tests/BimCollab.ClashDetection.Api.Tests

# BDD specs (Reqnroll)
dotnet test tests/BimCollab.ClashDetection.Specs
```

Run tests with detailed output:

```bash
dotnet test --verbosity normal
```

---

## Example curl Request

Submit the assessment's example dataset against the running API:

```bash
curl -s -X POST http://localhost:5201/api/clash-detection/detect \
  -H "Content-Type: application/json" \
  -d '{
    "sitePlan": {
      "width": 1000,
      "length": 500
    },
    "buildings": [
      {
        "name": "Oakwood Academy",
        "type": "School",
        "width": 300,
        "length": 300,
        "x": 0,
        "y": 0
      },
      {
        "name": "Pulse",
        "type": "Nightclub",
        "width": 200,
        "length": 200,
        "x": 0,
        "y": 500
      },
      {
        "name": "Centennial Park",
        "type": "Stadium",
        "width": 700,
        "length": 300,
        "x": 400,
        "y": 300
      },
      {
        "name": "Willow Residence",
        "type": "ResidentialBuilding",
        "width": 200,
        "length": 200,
        "x": 300,
        "y": 100
      },
      {
        "name": "Maple Plaza",
        "type": "Office",
        "width": 150,
        "length": 150,
        "x": 250,
        "y": 150
      }
    ]
  }' | python3 -m json.tool
```

Expected: HTTP 200 with a JSON response containing detected clashes (boundary violations, overlaps, clearance violations, zoning violations).

---

## Example Validation Error Request

Submit invalid data to see the validation error format:

```bash
curl -s -X POST http://localhost:5201/api/clash-detection/detect \
  -H "Content-Type: application/json" \
  -d '{
    "sitePlan": {
      "width": 1000,
      "length": -1
    },
    "buildings": [
      {
        "name": "",
        "type": "School",
        "width": 0,
        "length": 100,
        "x": 0,
        "y": 0
      }
    ]
  }' | python3 -m json.tool
```

Expected: HTTP 400 with RFC 9457 Problem Details listing each validation error by field path.

---

## Project Structure

```
BimCollab.ClashDetection.slnx          # Solution file
Directory.Build.props                   # Shared build properties (TreatWarningsAsErrors)

src/
  BimCollab.ClashDetection.Domain/      # Entities, value objects, enums, rules interface
  BimCollab.ClashDetection.Application/ # CQRS commands, handlers, validators, pipeline
  BimCollab.ClashDetection.Api/         # Minimal API endpoints, contracts, composition root
  BimCollab.ClashDetection.AppHost/     # Aspire orchestrator (dashboard, tracing)
  BimCollab.ClashDetection.ServiceDefaults/ # OpenTelemetry, health checks, resilience

tests/
  BimCollab.ClashDetection.Domain.Tests/      # Domain rule unit tests
  BimCollab.ClashDetection.Application.Tests/ # Handler and validator unit tests
  BimCollab.ClashDetection.Api.Tests/         # Integration tests (WebApplicationFactory)
  BimCollab.ClashDetection.Specs/             # BDD feature files (Reqnroll + xUnit)

specs/
  001-clash-detection/                  # Feature specification and plan artifacts
```

---

## Key URLs (Development)

| Resource | URL |
|----------|-----|
| API base | `http://localhost:5201` |
| Clash detection endpoint | `POST http://localhost:5201/api/clash-detection/detect` |
| Scalar API docs | `http://localhost:5201/scalar/v1` |
| OpenAPI spec | `http://localhost:5201/openapi/v1.json` |
| Aspire dashboard | Printed to console on AppHost startup |
