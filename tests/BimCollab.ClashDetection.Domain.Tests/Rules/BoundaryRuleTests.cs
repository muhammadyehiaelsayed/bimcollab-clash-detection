using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Domain.Tests.Rules;

public class BoundaryRuleTests
{
    private readonly BoundaryRule _sut = new();

    private static SitePlan CreateSitePlan(double width = 1000, double length = 500) =>
        new() { Dimensions = new Dimensions(width, length) };

    private static Building CreateBuilding(
        string name = "Building A",
        BuildingType type = BuildingType.Office,
        double width = 100,
        double length = 100,
        double x = 0,
        double y = 0) =>
        new()
        {
            Name = name,
            Type = type,
            Position = new Position(x, y),
            Dimensions = new Dimensions(width, length)
        };

    [Fact]
    public void Evaluate_BuildingWithinBounds_ReturnsNoClashes()
    {
        var sitePlan = CreateSitePlan();
        var buildings = new[] { CreateBuilding(x: 50, y: 50) };

        var result = _sut.Evaluate(sitePlan, buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_BuildingAtExactBoundary_ReturnsNoClashes()
    {
        var sitePlan = CreateSitePlan(width: 200, length: 200);
        var buildings = new[] { CreateBuilding(width: 100, length: 100, x: 100, y: 100) };

        var result = _sut.Evaluate(sitePlan, buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_BuildingExceedsRightBoundary_ReturnsBoundaryViolation()
    {
        var sitePlan = CreateSitePlan(width: 200, length: 200);
        var buildings = new[] { CreateBuilding(width: 100, length: 100, x: 150, y: 0) };

        var result = _sut.Evaluate(sitePlan, buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.BoundaryViolation, result[0].Type);
        Assert.Equal(ClashSeverity.Critical, result[0].Severity);
        Assert.Contains("Building A", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_BuildingExceedsTopBoundary_ReturnsBoundaryViolation()
    {
        var sitePlan = CreateSitePlan(width: 200, length: 200);
        var buildings = new[] { CreateBuilding(width: 100, length: 100, x: 0, y: 150) };

        var result = _sut.Evaluate(sitePlan, buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.BoundaryViolation, result[0].Type);
        Assert.Equal(ClashSeverity.Critical, result[0].Severity);
    }

    [Fact]
    public void Evaluate_BuildingExceedsBothBoundaries_ReturnsSingleBoundaryViolation()
    {
        var sitePlan = CreateSitePlan(width: 200, length: 200);
        var buildings = new[] { CreateBuilding(width: 200, length: 200, x: 100, y: 100) };

        var result = _sut.Evaluate(sitePlan, buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.BoundaryViolation, result[0].Type);
        Assert.Equal(ClashSeverity.Critical, result[0].Severity);
    }

    [Fact]
    public void Evaluate_BuildingAtOriginWithinBounds_ReturnsNoClashes()
    {
        var sitePlan = CreateSitePlan(width: 100, length: 100);
        var buildings = new[] { CreateBuilding(width: 100, length: 100, x: 0, y: 0) };

        var result = _sut.Evaluate(sitePlan, buildings);

        Assert.Empty(result);
    }
}
