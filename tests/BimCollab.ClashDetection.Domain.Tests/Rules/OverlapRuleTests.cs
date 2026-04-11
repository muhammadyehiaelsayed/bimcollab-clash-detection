using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Domain.Tests.Rules;

public class OverlapRuleTests
{
    private readonly OverlapRule _sut = new();

    private static SitePlan CreateSitePlan() =>
        new() { Dimensions = new Dimensions(1000, 500) };

    private static Building CreateBuilding(
        string name,
        double width = 100,
        double length = 100,
        double x = 0,
        double y = 0,
        BuildingType type = BuildingType.Office) =>
        new()
        {
            Name = name,
            Type = type,
            Position = new Position(x, y),
            Dimensions = new Dimensions(width, length)
        };

    [Fact]
    public void Evaluate_NonOverlappingBuildings_ReturnsNoClashes()
    {
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0),
            CreateBuilding("B", x: 200, y: 0)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_OverlappingBuildings_ReturnsOverlapClash()
    {
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0, width: 200, length: 200),
            CreateBuilding("B", x: 100, y: 100, width: 200, length: 200)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.Overlap, result[0].Type);
        Assert.Equal(ClashSeverity.Critical, result[0].Severity);
        Assert.Contains("A", result[0].BuildingNames);
        Assert.Contains("B", result[0].BuildingNames);
    }

    [Fact]
    public void Evaluate_TouchingBuildings_ReturnsNoOverlap()
    {
        // Touching edges: A ends at x=100, B starts at x=100
        // Strict inequality means this is NOT overlap
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0, width: 100, length: 100),
            CreateBuilding("B", x: 100, y: 0, width: 100, length: 100)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_CompletelyContainedBuilding_ReturnsOverlap()
    {
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0, width: 200, length: 200),
            CreateBuilding("B", x: 50, y: 50, width: 50, length: 50)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Single(result);
        Assert.Equal(ClashType.Overlap, result[0].Type);
    }

    [Fact]
    public void Evaluate_SingleBuilding_ReturnsNoClashes()
    {
        var buildings = new[] { CreateBuilding("A") };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Empty(result);
    }

    [Fact]
    public void Evaluate_ThreeMutuallyOverlappingBuildings_ReturnsThreeClashes()
    {
        // A: [0,200]x[0,200], B: [100,300]x[0,200], C: [50,250]x[50,250]
        // A-B overlap: A.x(0) < B.right(300) && B.x(100) < A.right(200) && same y => yes
        // A-C overlap: A.x(0) < C.right(250) && C.x(50) < A.right(200) && A.y(0) < C.top(250) && C.y(50) < A.top(200) => yes
        // B-C overlap: B.x(100) < C.right(250) && C.x(50) < B.right(300) && B.y(0) < C.top(250) && C.y(50) < B.top(200) => yes
        var buildings = new[]
        {
            CreateBuilding("A", x: 0, y: 0, width: 200, length: 200),
            CreateBuilding("B", x: 100, y: 0, width: 200, length: 200),
            CreateBuilding("C", x: 50, y: 50, width: 200, length: 200)
        };

        var result = _sut.Evaluate(CreateSitePlan(), buildings);

        Assert.Equal(3, result.Count);
        Assert.All(result, c => Assert.Equal(ClashType.Overlap, c.Type));
    }
}
