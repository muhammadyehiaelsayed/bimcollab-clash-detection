using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Domain.Tests.Rules;

public class ResidentialZoningRuleTests
{
    private readonly ResidentialZoningRule _sut = new();

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
    public void Evaluate_ResidentialMoreThan150FromStadium_ReturnsNoClashes()
    {
        // Residential: [0,100]x[0,100], Stadium: [300,400]x[0,100]
        // Edge-to-edge gap X = 300-100 = 200 > 150
        var buildings = new[]
        {
            CreateBuilding("Residence A", BuildingType.ResidentialBuilding, x: 0, y: 0),
            CreateBuilding("Stadium A", BuildingType.Stadium, x: 300, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_ResidentialLessThan150FromStadium_ReturnsZoningViolation()
    {
        // Residential: [0,100]x[0,100], Stadium: [120,220]x[0,100]
        // Edge-to-edge gap X = 120-100 = 20 < 150
        var buildings = new[]
        {
            CreateBuilding("Residence A", BuildingType.ResidentialBuilding, x: 0, y: 0),
            CreateBuilding("Stadium A", BuildingType.Stadium, x: 120, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.ZoningViolation, result[0].Type);
        Assert.Equal(ClashSeverity.Warning, result[0].Severity);
        Assert.Contains("Residence A", result[0].BuildingNames);
        Assert.Contains("Stadium A", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_ResidentialExactly150FromStadium_ReturnsNoClashes()
    {
        // Residential: [0,100]x[0,100], Stadium: [250,350]x[0,100]
        // Edge-to-edge gap X = 250-100 = 150 (exactly 150, not less)
        var buildings = new[]
        {
            CreateBuilding("Residence A", BuildingType.ResidentialBuilding, x: 0, y: 0),
            CreateBuilding("Stadium A", BuildingType.Stadium, x: 250, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_ResidentialLessThan150FromNightclub_ReturnsZoningViolation()
    {
        // Residential: [0,100]x[0,100], Nightclub: [120,220]x[0,100]
        // Edge-to-edge gap X = 120-100 = 20 < 150
        var buildings = new[]
        {
            CreateBuilding("Residence A", BuildingType.ResidentialBuilding, x: 0, y: 0),
            CreateBuilding("Nightclub A", BuildingType.Nightclub, x: 120, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.ZoningViolation, result[0].Type);
        Assert.Equal(ClashSeverity.Warning, result[0].Severity);
    }

    [Fact]
    public void Evaluate_NonResidentialNearStadium_ReturnsNoClashes()
    {
        var buildings = new[]
        {
            CreateBuilding("Office A", BuildingType.Office, x: 0, y: 0),
            CreateBuilding("Stadium A", BuildingType.Stadium, x: 110, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_ResidentialNearSchool_ReturnsNoClashes()
    {
        // Residential near school is fine - only stadiums and nightclubs matter
        var buildings = new[]
        {
            CreateBuilding("Residence A", BuildingType.ResidentialBuilding, x: 0, y: 0),
            CreateBuilding("School A", BuildingType.School, x: 110, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }
}
