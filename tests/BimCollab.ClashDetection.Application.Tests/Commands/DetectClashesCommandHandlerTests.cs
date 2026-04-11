using BimCollab.ClashDetection.Application.ClashDetection.Commands;
using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Application.Tests.Commands;

public class DetectClashesCommandHandlerTests
{
    private static DetectClashesCommand CreateCommand(
        double sitePlanWidth = 1000,
        double sitePlanLength = 500,
        params BuildingDto[] buildings) =>
        new()
        {
            SitePlan = new SitePlanDto(sitePlanWidth, sitePlanLength),
            Buildings = buildings.ToList()
        };

    [Fact]
    public async Task Handle_EmptyBuildings_ReturnsEmptyClashes()
    {
        var rules = new List<IClashDetectionRule> { new BoundaryRule(), new OverlapRule() };
        var handler = new DetectClashesCommandHandler(rules);
        var command = CreateCommand();

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Empty(result.Clashes);
    }

    [Fact]
    public async Task Handle_MapsDtosToDomainsCorrectly()
    {
        // Use BoundaryRule with a building that exceeds boundary to verify mapping
        var rules = new List<IClashDetectionRule> { new BoundaryRule() };
        var handler = new DetectClashesCommandHandler(rules);
        var command = CreateCommand(
            sitePlanWidth: 100,
            sitePlanLength: 100,
            new BuildingDto("Test Building", "Office", 50, 50, 80, 80));

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(result.Clashes);
        Assert.Contains("Test Building", result.Clashes[0].BuildingNames);
        Assert.Equal("BoundaryViolation", result.Clashes[0].Type);
        Assert.Equal("Critical", result.Clashes[0].Severity);
    }

    [Fact]
    public async Task Handle_RunsAllRulesAndAggregatesResults()
    {
        var rules = new List<IClashDetectionRule>
        {
            new BoundaryRule(),
            new OverlapRule(),
            new ClearanceRule(),
            new NightclubSchoolZoningRule()
        };
        var handler = new DetectClashesCommandHandler(rules);

        // School and Nightclub overlapping and also violating nightclub-school zoning
        var command = CreateCommand(
            sitePlanWidth: 1000,
            sitePlanLength: 500,
            new BuildingDto("School A", "School", 100, 100, 0, 0),
            new BuildingDto("Nightclub A", "Nightclub", 100, 100, 50, 50));

        var result = await handler.Handle(command, CancellationToken.None);

        // Should have Overlap + ZoningViolation (clearance subsumed by overlap)
        Assert.Contains(result.Clashes, c => c.Type == "Overlap");
        Assert.Contains(result.Clashes, c => c.Type == "ZoningViolation");
        Assert.DoesNotContain(result.Clashes, c => c.Type == "InsufficientClearance");
    }

    [Fact]
    public async Task Handle_OverlapSubsumesClearance()
    {
        var rules = new List<IClashDetectionRule>
        {
            new OverlapRule(),
            new ClearanceRule()
        };
        var handler = new DetectClashesCommandHandler(rules);

        var command = CreateCommand(
            buildings: new[]
            {
                new BuildingDto("A", "Office", 200, 200, 0, 0),
                new BuildingDto("B", "Office", 200, 200, 100, 100)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(result.Clashes);
        Assert.Equal("Overlap", result.Clashes[0].Type);
    }

    [Fact]
    public async Task Handle_NonOverlappingPair_ClearanceNotSubsumed()
    {
        var rules = new List<IClashDetectionRule>
        {
            new OverlapRule(),
            new ClearanceRule()
        };
        var handler = new DetectClashesCommandHandler(rules);

        // Buildings 5 units apart (< 10), not overlapping
        var command = CreateCommand(
            buildings: new[]
            {
                new BuildingDto("A", "Office", 100, 100, 0, 0),
                new BuildingDto("B", "Office", 100, 100, 105, 0)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(result.Clashes);
        Assert.Equal("InsufficientClearance", result.Clashes[0].Type);
    }

    [Fact]
    public async Task Handle_ConvertsClashToClashDtoCorrectly()
    {
        var rules = new List<IClashDetectionRule> { new BoundaryRule() };
        var handler = new DetectClashesCommandHandler(rules);
        var command = CreateCommand(
            sitePlanWidth: 50,
            sitePlanLength: 50,
            new BuildingDto("Building X", "Office", 100, 100, 0, 0));

        var result = await handler.Handle(command, CancellationToken.None);

        var clash = Assert.Single(result.Clashes);
        Assert.IsType<string>(clash.Type);
        Assert.IsType<string>(clash.Severity);
        Assert.Equal("BoundaryViolation", clash.Type);
        Assert.Equal("Critical", clash.Severity);
        Assert.Contains("Building X", clash.BuildingNames);
        Assert.NotEmpty(clash.Description);
    }

    [Fact]
    public async Task Handle_ParsesBuildingTypeCorrectly()
    {
        var rules = new List<IClashDetectionRule> { new ResidentialZoningRule() };
        var handler = new DetectClashesCommandHandler(rules);

        // ResidentialBuilding near Stadium should trigger zoning
        var command = CreateCommand(
            buildings: new[]
            {
                new BuildingDto("Residence", "ResidentialBuilding", 100, 100, 0, 0),
                new BuildingDto("Stadium", "Stadium", 100, 100, 120, 0)
            });

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.Single(result.Clashes);
        Assert.Equal("ZoningViolation", result.Clashes[0].Type);
    }
}
