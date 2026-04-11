@US1
Feature: Clash Detection Edge Cases
    As a BIM system consumer
    I want edge cases in clash detection to be handled correctly
    So that boundary conditions are accurately resolved

    Scenario: Empty buildings list returns empty clashes
        Given a site plan with width 1000 and length 500
        And the following buildings exist on the site plan:
            | Name | Type | Width | Length | X | Y |
        When I run clash detection
        Then no clashes should be detected

    Scenario: Single building within bounds produces no clashes
        Given a site plan with width 1000 and length 500
        And the following buildings exist on the site plan:
            | Name       | Type   | Width | Length | X  | Y  |
            | Building A | Office | 100   | 100    | 50 | 50 |
        When I run clash detection
        Then no clashes should be detected

    Scenario: Single building outside bounds produces boundary violation
        Given a site plan with width 100 and length 100
        And the following buildings exist on the site plan:
            | Name       | Type   | Width | Length | X  | Y  |
            | Building A | Office | 50    | 50     | 80 | 80 |
        When I run clash detection
        Then a clash of type "BoundaryViolation" with severity "Critical" should be detected for building "Building A"

    Scenario: Buildings exactly touching are not overlapping but violate clearance
        Given a site plan with width 1000 and length 500
        And the following buildings exist on the site plan:
            | Name       | Type   | Width | Length | X   | Y |
            | Building A | Office | 100   | 100    | 0   | 0 |
            | Building B | Office | 100   | 100    | 100 | 0 |
        When I run clash detection
        Then no clash of type "Overlap" should be detected for buildings "Building A" and "Building B"
        And a clash of type "InsufficientClearance" with severity "Warning" should be detected for buildings "Building A" and "Building B"

    Scenario: Buildings exactly 10 units apart have no clearance violation
        Given a site plan with width 1000 and length 500
        And the following buildings exist on the site plan:
            | Name       | Type   | Width | Length | X   | Y |
            | Building A | Office | 100   | 100    | 0   | 0 |
            | Building B | Office | 100   | 100    | 110 | 0 |
        When I run clash detection
        Then no clashes should be detected

    Scenario: Nightclub exactly 200 units from school has no zoning violation
        Given a site plan with width 1000 and length 500
        And the following buildings exist on the site plan:
            | Name        | Type      | Width | Length | X   | Y |
            | School A    | School    | 100   | 100    | 0   | 0 |
            | Nightclub A | Nightclub | 100   | 100    | 300 | 0 |
        When I run clash detection
        Then no clashes should be detected
