@US1
Feature: Clash Detection
    As a BIM system consumer
    I want to detect clashes in building site plans
    So that design conflicts and zoning violations are identified early

    Background:
        Given a site plan with width 1000 and length 500

    Scenario: Example dataset produces correct clashes
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | School A        | School             | 100   | 100    | 0   | 0   |
            | Office B        | Office             | 100   | 100    | 200 | 0   |
            | Nightclub C     | Nightclub          | 100   | 100    | 50  | 50  |
            | Stadium D       | Stadium            | 200   | 200    | 800 | 350 |
            | Residence E     | ResidentialBuilding| 80    | 80     | 60  | 200 |
        When I run clash detection
        Then a clash of type "Overlap" with severity "Critical" should be detected for buildings "School A" and "Nightclub C"
        And a clash of type "BoundaryViolation" with severity "Critical" should be detected for building "Stadium D"
        And a clash of type "ZoningViolation" with severity "Warning" should be detected for buildings "School A" and "Nightclub C"
        And a clash of type "ZoningViolation" with severity "Warning" should be detected for buildings "Residence E" and "Nightclub C"
        And 4 clashes should be detected

    Scenario: No clashes for valid layout
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | Building A      | Office             | 100   | 100    | 0   | 0   |
            | Building B      | School             | 100   | 100    | 500 | 0   |
        When I run clash detection
        Then no clashes should be detected

    Scenario: Boundary violation detection
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | Building A      | Office             | 200   | 200    | 900 | 400 |
        When I run clash detection
        Then a clash of type "BoundaryViolation" with severity "Critical" should be detected for building "Building A"

    Scenario: Overlap detection with no separate clearance reported
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | Building A      | Office             | 200   | 200    | 0   | 0   |
            | Building B      | School             | 200   | 200    | 100 | 100 |
        When I run clash detection
        Then a clash of type "Overlap" with severity "Critical" should be detected for buildings "Building A" and "Building B"
        And no clash of type "InsufficientClearance" should be detected for buildings "Building A" and "Building B"

    Scenario: Insufficient clearance for non-overlapping buildings
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | Building A      | Office             | 100   | 100    | 0   | 0   |
            | Building B      | Office             | 100   | 100    | 105 | 0   |
        When I run clash detection
        Then a clash of type "InsufficientClearance" with severity "Warning" should be detected for buildings "Building A" and "Building B"

    Scenario: Nightclub-school zoning violation
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | School A        | School             | 100   | 100    | 0   | 0   |
            | Nightclub A     | Nightclub          | 100   | 100    | 150 | 0   |
        When I run clash detection
        Then a clash of type "ZoningViolation" with severity "Warning" should be detected for buildings "School A" and "Nightclub A"

    Scenario: Residential-stadium zoning violation
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | Stadium A       | Stadium            | 100   | 100    | 0   | 0   |
            | Residence A     | ResidentialBuilding| 100   | 100    | 120 | 0   |
        When I run clash detection
        Then a clash of type "ZoningViolation" with severity "Warning" should be detected for buildings "Stadium A" and "Residence A"

    Scenario: Residential-nightclub zoning violation
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | Nightclub A     | Nightclub          | 100   | 100    | 0   | 0   |
            | Residence A     | ResidentialBuilding| 100   | 100    | 120 | 0   |
        When I run clash detection
        Then a clash of type "ZoningViolation" with severity "Warning" should be detected for buildings "Nightclub A" and "Residence A"

    Scenario: Multiple violations for same building
        Given the following buildings exist on the site plan:
            | Name            | Type               | Width | Length | X   | Y   |
            | School A        | School             | 100   | 100    | 0   | 0   |
            | Nightclub A     | Nightclub          | 200   | 200    | 50  | 50  |
        When I run clash detection
        Then a clash of type "Overlap" with severity "Critical" should be detected for buildings "School A" and "Nightclub A"
        And a clash of type "ZoningViolation" with severity "Warning" should be detected for buildings "School A" and "Nightclub A"
        And 2 clashes should be detected
