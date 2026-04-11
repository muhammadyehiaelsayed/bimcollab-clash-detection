using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Domain.Tests.Rules;

public class ClearanceRuleTests
{
    private readonly ClearanceRule _sut = new();

    private static SitePlan CreateSitePlan() =>
        new() { Dimensions = new Dimensions(1000, 500) };

    private static Building CreateBuilding(
        string name,
        double width = 100,
        double length = 100,
        double x = 0,
        double y = 0) =>
        new()
        {
            Name = name,
            Type = BuildingType.Office,
            Position = new Position(x, y),
            Dimensions = new Dimensions(width, length)
        };

    [Fact]
    public void Evaluate_BuildingsMoreThan10UnitsApart_ReturnsNoClashes()
    {
        // Gap = 200 - 100 = 100 > 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 200, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_BuildingsLessThan10UnitsApart_ReturnsClearanceClash()
    {
        // Gap = 105 - 100 = 5 < 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 105, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.InsufficientClearance, result[0].Type);
        Assert.Equal(ClashSeverity.Warning, result[0].Severity);
        Assert.Contains("A", result[0].BuildingNames);
        Assert.Contains("B", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_BuildingsExactly10UnitsApart_ReturnsNoClashes()
    {
        // Gap = 110 - 100 = 10 (exactly 10, not less than 10)
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 110, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_TouchingBuildings_ReturnsClearanceClash()
    {
        // Distance = 0 (touching), which is < 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 100, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.InsufficientClearance, result[0].Type);
        Assert.Equal(ClashSeverity.Warning, result[0].Severity);
    }

    [Fact]
    public void Evaluate_OverlappingBuildings_StillReturnsClearanceClash()
    {
        // ClearanceRule does NOT know about overlap subsumption - that is handler's job
        // Overlapping buildings have distance 0, which is < 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0, width: 200, length: 200),
            CreateBuilding("B", x: 100, y: 100, width: 200, length: 200)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.InsufficientClearance, result[0].Type);
    }

    [Fact]
    public void Evaluate_BuildingsDiagonallySeparated_LessThan10Units_ReturnsClash()
    {
        // A: [0,100]x[0,100], B: [107,207]x[107,207]
        // gapX = max(0, max(0,107) - min(100,207)) = max(0, 107-100) = 7
        // gapY = max(0, max(0,107) - min(100,207)) = max(0, 107-100) = 7
        // distance = sqrt(49+49) = 9.899... < 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 107, y: 107)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.InsufficientClearance, result[0].Type);
        Assert.Contains("A", result[0].BuildingNames);
        Assert.Contains("B", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_BuildingsSeparatedOnYAxisOnly_LessThan10Units_ReturnsClash()
    {
        // A: [0,100]x[0,100], B: [0,100]x[105,205]
        // gapX = max(0, max(0,0) - min(100,100)) = max(0, 0-100) = 0
        // gapY = max(0, max(0,105) - min(100,205)) = max(0, 105-100) = 5
        // distance = sqrt(0+25) = 5 < 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 0, y: 105)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.InsufficientClearance, result[0].Type);
        Assert.Contains("A", result[0].BuildingNames);
        Assert.Contains("B", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_ThreeBuildingsAllTooClose_ReturnsThreeClashes()
    {
        // A: [0,100]x[0,100], B: [105,205]x[0,100], C: [0,100]x[105,205]
        // A-B: gapX=5, gapY=0, distance=5 < 10
        // A-C: gapX=0, gapY=5, distance=5 < 10
        // B-C: gapX=5, gapY=5, distance=sqrt(50)=7.07 < 10
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 105, y: 0),
            CreateBuilding("C", x: 0, y: 105)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Equal(3, result.Count);
        Assert.All(result, c => Assert.Equal(ClashType.InsufficientClearance, c.Type));
    }
}
