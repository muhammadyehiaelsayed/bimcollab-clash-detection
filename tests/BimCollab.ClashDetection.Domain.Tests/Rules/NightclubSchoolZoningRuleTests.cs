using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Domain.Tests.Rules;

public class NightclubSchoolZoningRuleTests
{
    private readonly NightclubSchoolZoningRule _sut = new();

    private static SitePlan CreateSitePlan() =>
        new() { Dimensions = new Dimensions(1000, 500) };

    private static Building CreateBuilding(
        string name,
        BuildingType type,
        double x,
        double y,
        double width = 100,
        double length = 100) =>
        new()
        {
            Name = name,
            Type = type,
            Position = new Position(x, y),
            Dimensions = new Dimensions(width, length)
        };

    [Fact]
    public void Evaluate_NightclubMoreThan200FromSchool_ReturnsNoClashes()
    {
        // School: [0,100]x[0,100], Nightclub: [400,500]x[0,100]
        // Edge-to-edge gap X = 400-100 = 300 > 200
        var buildings = new[]
        {
            CreateBuilding("School A", BuildingType.School, x: 0, y: 0),
            CreateBuilding("Nightclub A", BuildingType.Nightclub, x: 400, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_NightclubLessThan200FromSchool_ReturnsZoningViolation()
    {
        // School: [0,100]x[0,100], Nightclub: [150,250]x[0,100]
        // Edge-to-edge gap X = 150-100 = 50 < 200
        var buildings = new[]
        {
            CreateBuilding("School A", BuildingType.School, x: 0, y: 0),
            CreateBuilding("Nightclub A", BuildingType.Nightclub, x: 150, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.ZoningViolation, result[0].Type);
        Assert.Equal(ClashSeverity.Warning, result[0].Severity);
        Assert.Contains("School A", result[0].BuildingNames);
        Assert.Contains("Nightclub A", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_NightclubExactly200FromSchool_ReturnsNoClashes()
    {
        // School: [0,100]x[0,100], Nightclub: [300,400]x[0,100]
        // Edge-to-edge gap X = 300-100 = 200 (exactly 200, not less than 200)
        var buildings = new[]
        {
            CreateBuilding("School A", BuildingType.School, x: 0, y: 0),
            CreateBuilding("Nightclub A", BuildingType.Nightclub, x: 300, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_NonNightclubNonSchoolPair_ReturnsNoClashes()
    {
        var buildings = new[]
        {
            CreateBuilding("Office A", BuildingType.Office, x: 0, y: 0),
            CreateBuilding("Stadium A", BuildingType.Stadium, x: 50, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_TwoSchoolsNearby_ReturnsNoClashes()
    {
        var buildings = new[]
        {
            CreateBuilding("School A", BuildingType.School, x: 0, y: 0),
            CreateBuilding("School B", BuildingType.School, x: 110, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_NightclubDiagonallyCloseToSchool_ReturnsZoningClash()
    {
        // School: [0,100]x[0,100], Nightclub: [240,340]x[240,340]
        // gapX = max(0, max(0,240) - min(100,340)) = max(0, 240-100) = 140
        // gapY = max(0, max(0,240) - min(100,340)) = max(0, 240-100) = 140
        // distance = sqrt(19600+19600) = 197.99... < 200
        var buildings = new[]
        {
            CreateBuilding("School A", BuildingType.School, x: 0, y: 0),
            CreateBuilding("Nightclub A", BuildingType.Nightclub, x: 240, y: 240)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.ZoningViolation, result[0].Type);
        Assert.Contains("School A", result[0].BuildingNames);
        Assert.Contains("Nightclub A", result[0].BuildingNames);
    }
}
