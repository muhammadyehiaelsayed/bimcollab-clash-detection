# Feature Specification: BIM Clash Detection API

**Feature Branch**: `001-clash-detection`
**Created**: 2026-04-10
**Status**: Clarified
**Input**: BIMCollab tech screening assessment v1.1 + follow-up clarifications

## Clarifications

### Session 2026-04-10

- Q: When a request contains both valid and invalid buildings, should detection still run on the valid ones? → A: Reject entire request -- return only validation errors, no detection runs.
- Q: When two buildings overlap, should both Overlap and InsufficientClearance be reported? → A: Overlap subsumes clearance -- report only the Overlap clash, skip clearance for that pair.
- Q: Should each clash include a severity level? → A: Yes -- Critical (BoundaryViolation, Overlap), Warning (InsufficientClearance, ZoningViolation).
- Q: What happens if two buildings share the same name? → A: Validation error -- reject request if any building names are duplicated.
- Q: What HTTP status codes should the API use? → A: 200 for successful detection (with or without clashes), 400 for validation errors.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Detect All Clashes in a Site Plan (Priority: P1)

As a BIM system consumer, I submit a site plan with proposed buildings and receive a complete list of all detected clashes so that I can generate resolution tasks for each issue.

**Why this priority**: This is the core value proposition. Without clash detection, the entire system has no purpose. Every other story depends on this flow working end-to-end.

**Independent Test**: Submit the example dataset from the assessment. Verify that the response contains the correct number of clashes, each with involved buildings, violation type, severity, and description. Can be validated by comparing the response against a manually calculated list of expected clashes.

**Acceptance Scenarios**:

1. **Given** a site plan (1000x500) with 5 buildings from the example dataset, **When** I submit the data to the clash detection API, **Then** I receive HTTP 200 with a JSON response containing all detected clashes with involved buildings, violation types, severity levels, and descriptions.

2. **Given** a site plan with buildings that are all within boundaries, non-overlapping, properly spaced, and zoning-compliant, **When** I submit the data, **Then** I receive HTTP 200 with an empty clash list.

3. **Given** a building that extends beyond the site plan boundary, **When** I run clash detection, **Then** a boundary violation clash with severity Critical is reported naming the offending building and describing which boundary it exceeds.

4. **Given** two buildings whose rectangular areas share interior space, **When** I run clash detection, **Then** an overlap clash with severity Critical is reported naming both buildings, and no separate clearance clash is reported for that pair.

5. **Given** two non-overlapping buildings with less than 10 units of edge-to-edge clearance between them, **When** I run clash detection, **Then** an insufficient clearance clash with severity Warning is reported naming both buildings and the actual distance.

6. **Given** a nightclub positioned less than 200 units (edge-to-edge) from a school, **When** I run clash detection, **Then** a zoning violation clash with severity Warning is reported naming the nightclub and the school.

7. **Given** a residential building positioned less than 150 units (edge-to-edge) from a stadium, **When** I run clash detection, **Then** a zoning violation clash with severity Warning is reported naming both buildings.

8. **Given** a residential building positioned less than 150 units (edge-to-edge) from a nightclub, **When** I run clash detection, **Then** a zoning violation clash with severity Warning is reported naming both buildings.

9. **Given** a building that violates multiple rules simultaneously (e.g., outside boundary AND too close to another building), **When** I run clash detection, **Then** all applicable clashes are reported independently, except that overlap subsumes clearance for the same pair.

---

### User Story 2 - Input Validation (Priority: P2)

As a BIM system consumer, I receive clear, structured validation errors when I submit incomplete or invalid building data, so that I can correct the input before re-submitting.

**Why this priority**: Validation is a prerequisite for reliable clash detection. Bad data must be rejected early with actionable feedback rather than producing incorrect results or crashing.

**Independent Test**: Submit payloads with various invalid fields and verify that each returns HTTP 400 with a structured error response identifying exactly which fields are invalid and why. Confirm that no clash detection runs when validation fails.

**Acceptance Scenarios**:

1. **Given** a building with a missing name field, **When** I submit the data, **Then** I receive HTTP 400 with a validation error identifying the missing field.

2. **Given** a building with a missing type field, **When** I submit the data, **Then** I receive HTTP 400 with a validation error identifying the missing field.

3. **Given** a building with width of 0, **When** I submit the data, **Then** I receive HTTP 400 with a validation error stating dimensions must be greater than zero.

4. **Given** a building with a negative length, **When** I submit the data, **Then** I receive HTTP 400 with a validation error stating dimensions must be greater than zero.

5. **Given** a building with a negative x position, **When** I submit the data, **Then** I receive HTTP 400 with a validation error stating positions must be greater than or equal to zero.

6. **Given** a building with multiple invalid fields, **When** I submit the data, **Then** I receive HTTP 400 with all validation errors for that building in a single response.

7. **Given** a payload with multiple buildings where some are valid and some are invalid, **When** I submit the data, **Then** I receive HTTP 400 with validation errors for each invalid building. No clash detection runs.

8. **Given** a payload with two buildings sharing the same name, **When** I submit the data, **Then** I receive HTTP 400 with a validation error identifying the duplicate name.

---

### Edge Cases

- What happens when the buildings list is empty? The system accepts the request and returns HTTP 200 with an empty clash list (valid input with nothing to check).
- What happens when there is only one building? Only boundary checks apply; pair-wise rules (overlap, clearance, zoning) produce no clashes if the single building is within bounds.
- What happens when two buildings share exactly an edge (distance = 0) but do not share interior area? They do not overlap, but they violate the 10-unit minimum clearance rule. Only an InsufficientClearance clash is reported.
- What happens when two buildings overlap? Only an Overlap clash is reported. The InsufficientClearance rule is skipped for that pair (overlap subsumes clearance).
- What happens when two buildings are exactly 10 units apart? The clearance rule is satisfied (10 units meets "minimum clearance distance of 10 units"). No clash.
- What happens when a nightclub is exactly 200 units from a school? The zoning rule is satisfied (200 units meets "at least 200 units away"). No clash.
- What happens when a building type is not one of the known types (School, Nightclub, Stadium, ResidentialBuilding, Office)? The system returns HTTP 400 with a validation error listing the valid types. Unknown types are rejected at the validation layer.
- What happens when the site plan dimensions are zero or negative? The system returns HTTP 400 with a validation error for the site plan.
- What happens when two buildings have the same name? The system returns HTTP 400 with a validation error identifying the duplicate.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST accept a JSON payload containing a site plan (width, length) and a list of buildings (name, type, width, length, x, y) via an HTTP POST endpoint.
- **FR-002**: System MUST validate that all building attributes (name, type, width, length, x, y) are present.
- **FR-003**: System MUST validate that building dimensions (width, length) are positive numbers greater than zero.
- **FR-004**: System MUST validate that building positions (x, y) are numbers greater than or equal to zero.
- **FR-005**: System MUST validate that building names are unique within a request. Duplicate names MUST be rejected.
- **FR-006**: System MUST reject the entire request with HTTP 400 and structured validation errors if ANY building fails validation. No clash detection runs when validation fails.
- **FR-007**: System MUST detect when a building extends beyond the site plan boundaries. A building at position (x, y) with dimensions (width, length) occupies the area [x, x+width] x [y, y+length]. The site plan occupies [0, sitePlan.width] x [0, sitePlan.length].
- **FR-008**: System MUST detect when two buildings overlap (their rectangular areas share interior space).
- **FR-009**: System MUST detect when two non-overlapping buildings have less than 10 units of edge-to-edge clearance between them. If buildings overlap, the clearance check is skipped for that pair (overlap subsumes clearance).
- **FR-010**: System MUST detect when a nightclub is less than 200 units (edge-to-edge) from any school.
- **FR-011**: System MUST detect when a residential building is less than 150 units (edge-to-edge) from any stadium or nightclub.
- **FR-012**: System MUST return all detected clashes as a structured JSON list with HTTP 200. Each clash MUST include: the involved building name(s), the type of violation, the severity level (Critical or Warning), and a human-readable description.
- **FR-013**: System MUST return validation errors as structured problem details (RFC 9457) with HTTP 400, identifying the specific field(s) and reason(s) for failure.
- **FR-014**: System MUST detect ALL applicable clashes in a single request. A building pair may trigger multiple independent violations (e.g., boundary AND zoning), and each MUST be reported separately, except that overlap subsumes clearance for the same pair.

### Key Entities

- **Site Plan**: Defines the rectangular boundary of the construction site. Key attributes: width, length.
- **Building**: A proposed structure placed on the site plan. Key attributes: name (unique identifier), type (School, Nightclub, Stadium, ResidentialBuilding, Office), width, length, x position, y position.
- **Clash**: A detected issue in the site plan. Key attributes: involved building name(s), violation type (BoundaryViolation, Overlap, InsufficientClearance, ZoningViolation), severity (Critical or Warning), description (human-readable explanation of the issue).

### Severity Mapping

| Violation Type | Severity | Rationale |
|----------------|----------|-----------|
| BoundaryViolation | Critical | Building cannot physically exist outside the site |
| Overlap | Critical | Two buildings cannot occupy the same space |
| InsufficientClearance | Warning | Buildings are too close but physically possible |
| ZoningViolation | Warning | Regulatory non-compliance, may be resolvable |

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: All validation rules (required fields, positive dimensions, non-negative positions, unique names) correctly reject invalid input with field-specific error messages and HTTP 400. Zero valid inputs are incorrectly rejected.
- **SC-002**: All 5 business rules (boundary, overlap, clearance, nightclub-school zoning, residential zoning) correctly detect violations in the example dataset. Zero false negatives.
- **SC-003**: Valid building configurations that comply with all rules produce HTTP 200 with an empty clash list. Zero false positives in clash detection.
- **SC-004**: Each clash in the response contains enough information for a downstream system to generate a task: which buildings are involved, what type of violation, what severity, and what specifically is wrong.
- **SC-005**: The response is parseable by any standard JSON client without special configuration.
- **SC-006**: A building that violates multiple rules simultaneously produces multiple independent clash entries (one per violation), except that overlapping pairs do not also produce a clearance clash.
- **SC-007**: Boundary conditions (exactly 10 units clearance, exactly 200 units nightclub-school distance, building fitting exactly within boundaries) produce correct results -- no off-by-one errors.
- **SC-008**: When any building fails validation, the entire request is rejected with HTTP 400. No partial detection results are returned.

## Assumptions

- The site plan origin is at (0, 0). A building at position (x, y) occupies the rectangle [x, x+width] x [y, y+length].
- "Distance" between buildings means the minimum edge-to-edge distance between their rectangular areas (not center-to-center).
- The dataset is small (similar to the 5-building example). No performance optimization is needed.
- Building names are unique within a request. Duplicates are rejected as validation errors.
- The system is stateless -- each API call is independent with no persistence between requests.
- Building types are case-sensitive and match the exact values from the dataset: School, Nightclub, Stadium, ResidentialBuilding, Office.
- The API serves as a backend component. There is no frontend or user authentication.
- "At least N units away" means distance >= N (inclusive). Distance < N is a violation.
- Validation is a prerequisite for detection. If validation fails, no detection runs.
- Overlap subsumes clearance: overlapping building pairs are reported as Overlap only, not also as InsufficientClearance.
