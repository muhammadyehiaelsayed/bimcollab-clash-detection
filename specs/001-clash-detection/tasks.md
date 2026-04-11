# Tasks: BIM Clash Detection API

**Input**: Design documents from `specs/001-clash-detection/`
**Prerequisites**: plan.md (required), spec.md (required), research.md, data-model.md, contracts/

**Tests**: BDD tests are MANDATORY per constitution (Principle III). Unit tests included for domain rules and validators.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2)
- Include exact file paths in descriptions

---

## Phase 1: Setup

**Purpose**: Clean up existing stub files to match the data model. Project structure already exists.

- [x] T001 [P] Update Position value object with X, Y properties in `src/BimCollab.ClashDetection.Domain/ValueObjects/Position.cs`
- [x] T002 [P] Update Dimensions value object with Width, Length properties in `src/BimCollab.ClashDetection.Domain/ValueObjects/Dimensions.cs`
- [x] T003 [P] Update BuildingType enum to match assessment types in `src/BimCollab.ClashDetection.Domain/Enums/BuildingType.cs`
- [x] T004 [P] Update ClashType enum (remove ValidationError, keep 4 types) in `src/BimCollab.ClashDetection.Domain/Enums/ClashType.cs`
- [x] T005 [P] Update ClashSeverity enum (remove Error, keep Critical + Warning) in `src/BimCollab.ClashDetection.Domain/Enums/ClashSeverity.cs`
- [x] T006 [P] Update SitePlan entity with Dimensions property in `src/BimCollab.ClashDetection.Domain/Entities/SitePlan.cs`
- [x] T007 [P] Update Building entity with Name, Type, Position, Dimensions in `src/BimCollab.ClashDetection.Domain/Entities/Building.cs`
- [x] T008 [P] Update Clash model with BuildingNames, Type, Severity, Description in `src/BimCollab.ClashDetection.Domain/Models/Clash.cs`
- [x] T009 Update IClashDetectionRule interface with Evaluate signature in `src/BimCollab.ClashDetection.Domain/Rules/IClashDetectionRule.cs`
- [x] T010 Verify solution builds with `dotnet build`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Application layer DTOs, API contracts, DI wiring, and BDD test infrastructure that MUST be complete before user stories

**CRITICAL**: No user story work can begin until this phase is complete

- [x] T011 Update DetectClashesCommand with SitePlanDto, BuildingDto, and DetectClashesResult in `src/BimCollab.ClashDetection.Application/ClashDetection/Commands/DetectClashesCommand.cs`
- [x] T012 [P] Update DetectClashesRequest contract with SitePlan and Buildings JSON models in `src/BimCollab.ClashDetection.Api/Contracts/DetectClashesRequest.cs`
- [x] T013 [P] Update ClashDetectionResponse contract with list of clash DTOs (buildingNames, type, severity, description) in `src/BimCollab.ClashDetection.Api/Contracts/ClashDetectionResponse.cs`
- [x] T014 Update DependencyInjection.cs to register MediatR, FluentValidation, and all IClashDetectionRule implementations in `src/BimCollab.ClashDetection.Application/DependencyInjection.cs`
- [x] T015 Update TestWebApplicationFactory for BDD and integration tests in `tests/BimCollab.ClashDetection.Specs/Support/TestWebApplicationFactory.cs`
- [x] T016 Verify solution builds and test infrastructure works with `dotnet build && dotnet test`

**Checkpoint**: Foundation ready -- user story implementation can begin

---

## Phase 3: User Story 1 - Detect All Clashes (Priority: P1) MVP

**Goal**: Submit site plan with buildings, receive all detected clashes with severity levels

**Independent Test**: POST the example dataset, verify correct clashes are returned with involved buildings, types, severity, and descriptions

### BDD Scenarios for User Story 1 (MANDATORY)

> **WRITE THESE FIRST. Verify they FAIL before implementation.**

- [x] T017 [US1] Write BDD feature file for clash detection scenarios (9 acceptance scenarios from spec US1; tag with @US1) in `tests/BimCollab.ClashDetection.Specs/Features/ClashDetection.feature`
- [x] T018 [P] [US1] Write BDD feature file for edge case scenarios (boundary conditions, single building, empty list; tag with @US1) in `tests/BimCollab.ClashDetection.Specs/Features/EdgeCases.feature`
- [x] T019 [US1] Write step definitions for ClashDetection.feature in `tests/BimCollab.ClashDetection.Specs/StepDefinitions/ClashDetectionStepDefinitions.cs`
- [x] T020 [P] [US1] Write step definitions for EdgeCases.feature in `tests/BimCollab.ClashDetection.Specs/StepDefinitions/EdgeCaseStepDefinitions.cs`
- [x] T021 [US1] Verify all BDD scenarios FAIL (red phase) with `dotnet test --filter "Category=US1"`

### Unit Tests for User Story 1 (MANDATORY)

- [x] T022 [P] [US1] Write unit tests for BoundaryRule in `tests/BimCollab.ClashDetection.Domain.Tests/Rules/BoundaryRuleTests.cs`
- [x] T023 [P] [US1] Write unit tests for OverlapRule in `tests/BimCollab.ClashDetection.Domain.Tests/Rules/OverlapRuleTests.cs`
- [x] T024 [P] [US1] Write unit tests for ClearanceRule (clearance detection only; overlap-subsumes-clearance is tested in T027) in `tests/BimCollab.ClashDetection.Domain.Tests/Rules/ClearanceRuleTests.cs`
- [x] T025 [P] [US1] Write unit tests for NightclubSchoolZoningRule in `tests/BimCollab.ClashDetection.Domain.Tests/Rules/NightclubSchoolZoningRuleTests.cs`
- [x] T026 [P] [US1] Write unit tests for ResidentialZoningRule in `tests/BimCollab.ClashDetection.Domain.Tests/Rules/ResidentialZoningRuleTests.cs`
- [x] T027 [US1] Write unit tests for DetectClashesCommandHandler (including overlap-subsumes-clearance filtering, DTO-to-domain mapping, all-rules-aggregation) in `tests/BimCollab.ClashDetection.Application.Tests/Commands/DetectClashesCommandHandlerTests.cs`

### Implementation for User Story 1

- [x] T028 [P] [US1] Implement BoundaryRule in `src/BimCollab.ClashDetection.Domain/Rules/BoundaryRule.cs`
- [x] T029 [P] [US1] Implement OverlapRule in `src/BimCollab.ClashDetection.Domain/Rules/OverlapRule.cs`
- [x] T030 [P] [US1] Implement ClearanceRule (check all non-adjacent pairs; does NOT handle overlap subsumption -- that is the handler's responsibility) in `src/BimCollab.ClashDetection.Domain/Rules/ClearanceRule.cs`
- [x] T031 [P] [US1] Implement NightclubSchoolZoningRule in `src/BimCollab.ClashDetection.Domain/Rules/NightclubSchoolZoningRule.cs`
- [x] T032 [P] [US1] Implement ResidentialZoningRule in `src/BimCollab.ClashDetection.Domain/Rules/ResidentialZoningRule.cs`
- [x] T033 [US1] Implement DetectClashesCommandHandler (map DTOs to domain, run all rules, apply overlap-subsumes-clearance) in `src/BimCollab.ClashDetection.Application/ClashDetection/Commands/DetectClashesCommandHandler.cs`
- [x] T034 [US1] Implement POST /api/clash-detection/detect endpoint in `src/BimCollab.ClashDetection.Api/Endpoints/ClashDetectionEndpoints.cs`
- [x] T035 [US1] Write integration tests for detect endpoint (happy path, no clashes, example dataset) in `tests/BimCollab.ClashDetection.Api.Tests/Endpoints/ClashDetectionEndpointTests.cs`
- [x] T036 [US1] Verify all unit tests pass for US1 rules with `dotnet test --filter "FullyQualifiedName~Domain.Tests.Rules"`
- [x] T037 [US1] Verify all BDD scenarios pass (green phase) with `dotnet test --filter "Category=US1"`

**Checkpoint**: User Story 1 fully functional. API accepts data and returns all detected clashes. MVP complete.

---

## Phase 4: User Story 2 - Input Validation (Priority: P2)

**Goal**: Invalid or incomplete data returns clear, structured validation errors with HTTP 400. No detection runs on invalid input.

**Independent Test**: Submit payloads with missing fields, zero dimensions, negative positions, duplicate names. Verify HTTP 400 with RFC 9457 problem details.

### BDD Scenarios for User Story 2 (MANDATORY)

> **WRITE THESE FIRST. Verify they FAIL before implementation.**

- [x] T038 [US2] Write BDD feature file for input validation scenarios (8 acceptance scenarios from spec US2 + site plan validation edge case: zero/negative dimensions) in `tests/BimCollab.ClashDetection.Specs/Features/InputValidation.feature`
- [x] T039 [US2] Write step definitions for InputValidation.feature in `tests/BimCollab.ClashDetection.Specs/StepDefinitions/InputValidationStepDefinitions.cs`
- [x] T040 [US2] Verify all BDD validation scenarios FAIL (red phase)

### Unit Tests for User Story 2 (MANDATORY)

- [x] T041 [US2] Write unit tests for DetectClashesCommandValidator (all validation rules: required fields, positive dimensions, non-negative positions, unique names) in `tests/BimCollab.ClashDetection.Application.Tests/Validators/DetectClashesCommandValidatorTests.cs`

### Implementation for User Story 2

- [x] T042 [US2] Implement DetectClashesCommandValidator with all FluentValidation rules (FR-002 through FR-006) in `src/BimCollab.ClashDetection.Application/ClashDetection/Validators/DetectClashesCommandValidator.cs`
- [x] T043 [US2] Implement ValidationBehavior to intercept validation failures and throw ValidationException in `src/BimCollab.ClashDetection.Application/Common/Behaviors/ValidationBehavior.cs`
- [x] T044 [US2] Create ValidationExceptionHandler middleware to map ValidationException to RFC 9457 Problem Details (HTTP 400) in `src/BimCollab.ClashDetection.Api/Middleware/ValidationExceptionHandler.cs`
- [x] T045 [US2] Register ValidationExceptionHandler in Program.cs in `src/BimCollab.ClashDetection.Api/Program.cs`
- [x] T046 [US2] Write integration tests for validation error responses (missing fields, bad dimensions, duplicate names) in `tests/BimCollab.ClashDetection.Api.Tests/Endpoints/ClashDetectionEndpointTests.cs`
- [x] T047 [US2] Verify all unit tests pass for validator with `dotnet test --filter "FullyQualifiedName~Application.Tests.Validators"`
- [x] T048 [US2] Verify all BDD validation scenarios pass (green phase)

**Checkpoint**: User Stories 1 AND 2 both work. Invalid input rejected with HTTP 400. Valid input returns clashes with HTTP 200.

---

## Phase 5: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, cleanup, and documentation

- [x] T049 Run full test suite and verify all tests pass with `dotnet test`
- [x] T050 Verify API starts and health endpoints respond with `dotnet run --project src/BimCollab.ClashDetection.Api`
- [x] T051 [P] Verify Aspire dashboard works with `dotnet run --project src/BimCollab.ClashDetection.AppHost --launch-profile http`
- [x] T052 [P] Test with example dataset via curl and verify response matches expected clashes
- [x] T053 Code cleanup: remove any unused template files, verify no compiler warnings

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies -- can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion -- BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Foundational phase completion
- **User Story 2 (Phase 4)**: Depends on Foundational phase completion. Can run in parallel with US1 but shares endpoint file.
- **Polish (Phase 5)**: Depends on both user stories being complete

### Within Each User Story

1. BDD feature files MUST be written FIRST
2. BDD step definitions written and verified to FAIL
3. Unit tests written (can parallel with BDD)
4. Implementation code (rules, handler, validator)
5. BDD scenarios verified to PASS
6. Story complete before moving to next priority

### Parallel Opportunities

**Phase 1** (all parallel):
```
T001 Position | T002 Dimensions | T003 BuildingType | T004 ClashType
T005 ClashSeverity | T006 SitePlan | T007 Building | T008 Clash
```

**Phase 3 -- BDD** (partial parallel):
```
T017 ClashDetection.feature | T018 EdgeCases.feature
T019 ClashDetection steps   | T020 EdgeCase steps
```

**Phase 3 -- Unit Tests** (all parallel):
```
T022 BoundaryRuleTests | T023 OverlapRuleTests | T024 ClearanceRuleTests
T025 NightclubSchoolZoningRuleTests | T026 ResidentialZoningRuleTests
```

**Phase 3 -- Rules** (all parallel):
```
T028 BoundaryRule | T029 OverlapRule | T030 ClearanceRule
T031 NightclubSchoolZoningRule | T032 ResidentialZoningRule
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001-T010)
2. Complete Phase 2: Foundational (T011-T016)
3. Complete Phase 3: User Story 1 (T017-T037)
4. **STOP and VALIDATE**: Test US1 independently
5. MVP ready -- API detects all clashes

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → MVP
3. Add User Story 2 → Test independently → Full feature
4. Polish → Final verification

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story
- BDD scenarios MUST be written and FAIL before implementation (Constitution Principle III)
- Overlap subsumes clearance handled in handler (T033), not individual rules
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
