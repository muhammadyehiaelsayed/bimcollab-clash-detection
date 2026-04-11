# API Contract: BIM Clash Detection

**Feature**: 001-clash-detection
**Date**: 2026-04-10
**Status**: Complete
**Base URL**: `http://localhost:5201`

---

## Endpoint

```
POST /api/clash-detection/detect
Content-Type: application/json
Accept: application/json
```

---

## Request Body Schema

```json
{
  "sitePlan": {
    "width": <number>,
    "length": <number>
  },
  "buildings": [
    {
      "name": <string>,
      "type": <string>,
      "width": <number>,
      "length": <number>,
      "x": <number>,
      "y": <number>
    }
  ]
}
```

### Field Definitions

| Field | Type | Required | Constraints | Description |
|-------|------|----------|-------------|-------------|
| `sitePlan` | object | Yes | -- | The rectangular site boundary |
| `sitePlan.width` | number | Yes | > 0 | Site plan horizontal extent |
| `sitePlan.length` | number | Yes | > 0 | Site plan vertical extent |
| `buildings` | array | Yes | -- | List of proposed buildings |
| `buildings[].name` | string | Yes | Non-empty, unique within request | Human-readable building identifier |
| `buildings[].type` | string | Yes | One of: `School`, `Nightclub`, `Stadium`, `ResidentialBuilding`, `Office` | Building classification |
| `buildings[].width` | number | Yes | > 0 | Building horizontal dimension |
| `buildings[].length` | number | Yes | > 0 | Building vertical dimension |
| `buildings[].x` | number | Yes | >= 0 | Horizontal position (bottom-left corner) |
| `buildings[].y` | number | Yes | >= 0 | Vertical position (bottom-left corner) |

### Coordinate System

- Origin (0, 0) is the bottom-left corner of the site plan.
- A building at position (x, y) with dimensions (width, length) occupies the rectangle `[x, x+width] x [y, y+length]`.
- The site plan occupies `[0, sitePlan.width] x [0, sitePlan.length]`.

---

## Success Response (HTTP 200)

Returned when the request is valid. The clashes array may be empty (no violations found) or contain one or more detected clashes.

### Schema

```json
{
  "clashes": [
    {
      "buildingNames": [<string>, ...],
      "type": <string>,
      "severity": <string>,
      "description": <string>
    }
  ]
}
```

### Field Definitions

| Field | Type | Description |
|-------|------|-------------|
| `clashes` | array | List of detected clashes (empty if none) |
| `clashes[].buildingNames` | string[] | Names of involved buildings (1 for boundary, 2 for pair-wise rules) |
| `clashes[].type` | string | One of: `BoundaryViolation`, `Overlap`, `InsufficientClearance`, `ZoningViolation` |
| `clashes[].severity` | string | One of: `Critical`, `Warning` |
| `clashes[].description` | string | Human-readable explanation of the violation |

### Severity Mapping

| Clash Type | Severity |
|------------|----------|
| `BoundaryViolation` | `Critical` |
| `Overlap` | `Critical` |
| `InsufficientClearance` | `Warning` |
| `ZoningViolation` | `Warning` |

---

## Validation Error Response (HTTP 400)

Returned when one or more validation rules fail. Follows RFC 9457 Problem Details format. No clash detection runs when validation fails.

### Schema

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "<propertyPath>": [<string>, ...]
  }
}
```

### Field Definitions

| Field | Type | Description |
|-------|------|-------------|
| `type` | string | URI reference identifying the problem type |
| `title` | string | Short human-readable summary |
| `status` | integer | HTTP status code (always 400) |
| `detail` | string | Human-readable explanation |
| `errors` | object | Dictionary of property paths to error message arrays |

### Error Property Path Convention

Paths use bracket notation for array indices:

- `SitePlan.Width` -- site plan field error
- `Buildings[0].Name` -- first building's name error
- `Buildings[1].Width` -- second building's width error
- `Buildings` -- collection-level error (e.g., duplicate names)

---

## Example: Valid Request with Clashes

### Request

```bash
curl -X POST http://localhost:5201/api/clash-detection/detect \
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
  }'
```

### Response (HTTP 200)

```json
{
  "clashes": [
    {
      "buildingNames": ["Pulse"],
      "type": "BoundaryViolation",
      "severity": "Critical",
      "description": "Building 'Pulse' extends beyond the site boundary on the Y-axis: occupies [500, 700] but site length is 500."
    },
    {
      "buildingNames": ["Centennial Park"],
      "type": "BoundaryViolation",
      "severity": "Critical",
      "description": "Building 'Centennial Park' extends beyond the site boundary on the X-axis: occupies [400, 1100] but site width is 1000."
    },
    {
      "buildingNames": ["Centennial Park"],
      "type": "BoundaryViolation",
      "severity": "Critical",
      "description": "Building 'Centennial Park' extends beyond the site boundary on the Y-axis: occupies [300, 600] but site length is 500."
    },
    {
      "buildingNames": ["Willow Residence", "Maple Plaza"],
      "type": "Overlap",
      "severity": "Critical",
      "description": "Buildings 'Willow Residence' and 'Maple Plaza' overlap."
    },
    {
      "buildingNames": ["Oakwood Academy", "Willow Residence"],
      "type": "InsufficientClearance",
      "severity": "Warning",
      "description": "Buildings 'Oakwood Academy' and 'Willow Residence' are 0 units apart, which is less than the required 10 units."
    },
    {
      "buildingNames": ["Oakwood Academy", "Maple Plaza"],
      "type": "InsufficientClearance",
      "severity": "Warning",
      "description": "Buildings 'Oakwood Academy' and 'Maple Plaza' are 0 units apart, which is less than the required 10 units."
    },
    {
      "buildingNames": ["Pulse", "Oakwood Academy"],
      "type": "ZoningViolation",
      "severity": "Warning",
      "description": "Nightclub 'Pulse' is 200 units from school 'Oakwood Academy', which meets the minimum 200-unit requirement."
    },
    {
      "buildingNames": ["Willow Residence", "Centennial Park"],
      "type": "ZoningViolation",
      "severity": "Warning",
      "description": "Residential building 'Willow Residence' is 100 units from stadium 'Centennial Park', which is less than the required 150 units."
    }
  ]
}
```

**Note**: The exact clashes produced depend on the geometric analysis of the example dataset. The descriptions above are illustrative. The actual implementation will compute precise distances and boundary violations.

---

## Example: Valid Request with No Clashes

### Request

```bash
curl -X POST http://localhost:5201/api/clash-detection/detect \
  -H "Content-Type: application/json" \
  -d '{
    "sitePlan": {
      "width": 1000,
      "length": 1000
    },
    "buildings": [
      {
        "name": "Building A",
        "type": "Office",
        "width": 100,
        "length": 100,
        "x": 0,
        "y": 0
      },
      {
        "name": "Building B",
        "type": "Office",
        "width": 100,
        "length": 100,
        "x": 200,
        "y": 0
      }
    ]
  }'
```

### Response (HTTP 200)

```json
{
  "clashes": []
}
```

---

## Example: Validation Error

### Request

```bash
curl -X POST http://localhost:5201/api/clash-detection/detect \
  -H "Content-Type: application/json" \
  -d '{
    "sitePlan": {
      "width": 1000,
      "length": 0
    },
    "buildings": [
      {
        "name": "",
        "type": "School",
        "width": -50,
        "length": 100,
        "x": 0,
        "y": -10
      },
      {
        "name": "Building A",
        "type": "InvalidType",
        "width": 100,
        "length": 100,
        "x": 0,
        "y": 0
      }
    ]
  }'
```

### Response (HTTP 400)

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Validation Failed",
  "status": 400,
  "detail": "One or more validation errors occurred.",
  "errors": {
    "SitePlan.Length": [
      "Site plan length must be greater than zero."
    ],
    "Buildings[0].Name": [
      "'Name' must not be empty."
    ],
    "Buildings[0].Width": [
      "Building width must be greater than zero."
    ],
    "Buildings[0].Y": [
      "Building Y position must be greater than or equal to zero."
    ],
    "Buildings[1].Type": [
      "Building type must be one of: School, Nightclub, Stadium, ResidentialBuilding, Office."
    ]
  }
}
```

---

## Error Handling Summary

| Scenario | HTTP Status | Response Format |
|----------|------------|-----------------|
| Valid input, clashes found | 200 | `ClashDetectionResponse` with populated clashes array |
| Valid input, no clashes | 200 | `ClashDetectionResponse` with empty clashes array |
| Invalid input (validation failure) | 400 | RFC 9457 `HttpValidationProblemDetails` |
| Malformed JSON / deserialization failure | 400 | RFC 9457 `ProblemDetails` (ASP.NET Core built-in) |
| Unhandled server error | 500 | RFC 9457 `ProblemDetails` (generic) |
