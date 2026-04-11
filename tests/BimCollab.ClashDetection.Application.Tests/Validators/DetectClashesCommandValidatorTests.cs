using BimCollab.ClashDetection.Application.ClashDetection.Commands;
using BimCollab.ClashDetection.Application.ClashDetection.Validators;
using FluentValidation.TestHelper;

namespace BimCollab.ClashDetection.Application.Tests.Validators;

public class DetectClashesCommandValidatorTests
{
    private readonly DetectClashesCommandValidator _validator = new();

    private static DetectClashesCommand CreateValidCommand(
        double sitePlanWidth = 1000,
        double sitePlanLength = 500,
        List<BuildingDto>? buildings = null)
    {
        buildings ??=
        [
            new BuildingDto("Building A", "Office", 100, 100, 0, 0)
        ];

        return new DetectClashesCommand
        {
            SitePlan = new SitePlanDto(sitePlanWidth, sitePlanLength),
            Buildings = buildings
        };
    }

    [Fact]
    public async Task Validate_SitePlanWidthZero_HasError()
    {
        var command = CreateValidCommand(sitePlanWidth: 0);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SitePlan.Width);
    }

    [Fact]
    public async Task Validate_SitePlanWidthNegative_HasError()
    {
        var command = CreateValidCommand(sitePlanWidth: -10);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SitePlan.Width);
    }

    [Fact]
    public async Task Validate_SitePlanLengthZero_HasError()
    {
        var command = CreateValidCommand(sitePlanLength: 0);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SitePlan.Length);
    }

    [Fact]
    public async Task Validate_SitePlanLengthNegative_HasError()
    {
        var command = CreateValidCommand(sitePlanLength: -50);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.SitePlan.Length);
    }

    [Fact]
    public async Task Validate_BuildingNameEmpty_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("", "Office", 100, 100, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Name");
    }

    [Theory]
    [InlineData("")]
    [InlineData("InvalidType")]
    [InlineData("house")]
    public async Task Validate_BuildingTypeInvalid_HasError(string type)
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", type, 100, 100, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Type");
    }

    [Theory]
    [InlineData("School")]
    [InlineData("Nightclub")]
    [InlineData("Stadium")]
    [InlineData("ResidentialBuilding")]
    [InlineData("Office")]
    public async Task Validate_BuildingTypeValid_NoError(string type)
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", type, 100, 100, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor("Buildings[0].Type");
    }

    [Fact]
    public async Task Validate_BuildingWidthZero_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 0, 100, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Width");
    }

    [Fact]
    public async Task Validate_BuildingWidthNegative_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", -5, 100, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Width");
    }

    [Fact]
    public async Task Validate_BuildingLengthZero_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, 0, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Length");
    }

    [Fact]
    public async Task Validate_BuildingLengthNegative_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, -50, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Length");
    }

    [Fact]
    public async Task Validate_BuildingXNegative_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, 100, -10, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].X");
    }

    [Fact]
    public async Task Validate_BuildingYNegative_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, 100, 0, -5)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor("Buildings[0].Y");
    }

    [Fact]
    public async Task Validate_DuplicateBuildingNames_HasError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, 100, 0, 0),
            new BuildingDto("Building A", "School", 100, 100, 200, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldHaveValidationErrorFor(x => x.Buildings);
    }

    [Fact]
    public async Task Validate_UniqueBuildingNames_NoError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, 100, 0, 0),
            new BuildingDto("Building B", "School", 100, 100, 200, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Buildings);
    }

    [Fact]
    public async Task Validate_MultipleInvalidFields_ReportsAllErrors()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("", "InvalidType", -5, -10, -1, -1)
        ]);

        var result = await _validator.TestValidateAsync(command);

        Assert.True(result.Errors.Count >= 6,
            $"Expected at least 6 errors but got {result.Errors.Count}: {string.Join(", ", result.Errors.Select(e => e.PropertyName))}");
    }

    [Fact]
    public async Task Validate_ValidCommand_NoErrors()
    {
        var command = CreateValidCommand();

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_BuildingPositionAtZero_NoError()
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Building A", "Office", 100, 100, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);

        result.ShouldNotHaveValidationErrorFor("Buildings[0].X");
        result.ShouldNotHaveValidationErrorFor("Buildings[0].Y");
    }

    [Fact]
    public async Task Validate_TooManyBuildings_HasError()
    {
        var buildings = Enumerable.Range(1, 501)
            .Select(i => new BuildingDto($"Building {i}", "Office", 10, 10, i * 20.0, 0))
            .ToList();

        var command = new DetectClashesCommand
        {
            SitePlan = new SitePlanDto(20000, 20000),
            Buildings = buildings
        };

        var result = await _validator.TestValidateAsync(command);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("maximum of 500"));
    }

    [Fact]
    public async Task Validate_Exactly500Buildings_NoError()
    {
        var buildings = Enumerable.Range(1, 500)
            .Select(i => new BuildingDto($"Building {i}", "Office", 10, 10, i * 20.0, 0))
            .ToList();

        var command = new DetectClashesCommand
        {
            SitePlan = new SitePlanDto(20000, 20000),
            Buildings = buildings
        };

        var result = await _validator.TestValidateAsync(command);
        Assert.True(result.IsValid, $"Expected valid but got errors: {string.Join(", ", result.Errors.Select(e => e.ErrorMessage))}");
    }

    [Theory]
    [InlineData("school")]
    [InlineData("OFFICE")]
    [InlineData("nightClub")]
    public async Task Validate_BuildingTypeCaseSensitive_HasError(string type)
    {
        var command = CreateValidCommand(buildings:
        [
            new BuildingDto("Test", type, 10, 10, 0, 0)
        ]);

        var result = await _validator.TestValidateAsync(command);
        Assert.False(result.IsValid);
    }
}
