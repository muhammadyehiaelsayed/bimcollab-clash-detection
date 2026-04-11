@US2
Feature: Input Validation
    As a BIM system consumer
    I want invalid input to be rejected before clash detection runs
    So that I receive clear error messages and no partial results

    Scenario: Missing building name returns validation error
        Given a site plan of width 1000 and length 500
        And a building with name "" type "Office" width 100 length 100 at position 0, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Name"

    Scenario: Missing building type returns validation error
        Given a site plan of width 1000 and length 500
        And a building with name "Building A" type "" width 100 length 100 at position 0, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Type"

    Scenario: Building width of zero returns validation error
        Given a site plan of width 1000 and length 500
        And a building with name "Building A" type "Office" width 0 length 100 at position 0, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Width"

    Scenario: Negative building length returns validation error
        Given a site plan of width 1000 and length 500
        And a building with name "Building A" type "Office" width 100 length -50 at position 0, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Length"

    Scenario: Negative X position returns validation error
        Given a site plan of width 1000 and length 500
        And a building with name "Building A" type "Office" width 100 length 100 at position -10, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "X"

    Scenario: Multiple invalid fields reports all errors
        Given a site plan of width 1000 and length 500
        And a building with name "" type "InvalidType" width -5 length -10 at position -1, -1
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Name"
        And the error should mention "Type"
        And the error should mention "Width"
        And the error should mention "Length"
        And the error should mention "X"
        And the error should mention "Y"

    Scenario: Mix of valid and invalid buildings rejects entire request
        Given a site plan of width 1000 and length 500
        And a building with name "Valid Building" type "Office" width 100 length 100 at position 0, 0
        And a building with name "" type "Office" width 100 length 100 at position 200, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Name"

    Scenario: Duplicate building names returns validation error
        Given a site plan of width 1000 and length 500
        And a building with name "Building A" type "Office" width 100 length 100 at position 0, 0
        And a building with name "Building A" type "School" width 100 length 100 at position 200, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "duplicate"

    Scenario: Site plan width of zero returns validation error
        Given a site plan of width 0 and length 500
        And a building with name "Building A" type "Office" width 100 length 100 at position 0, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Width"

    Scenario: Site plan negative length returns validation error
        Given a site plan of width 1000 and length -100
        And a building with name "Building A" type "Office" width 100 length 100 at position 0, 0
        When I submit the clash detection request
        Then I should receive a validation error
        And the error should mention "Length"
