using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;
using BimCollab.ClashDetection.Domain.Rules;
using BimCollab.ClashDetection.Domain.ValueObjects;
using MediatR;

namespace BimCollab.ClashDetection.Application.ClashDetection.Commands;

internal sealed class DetectClashesCommandHandler(
    IEnumerable<IClashDetectionRule> rules) : IRequestHandler<DetectClashesCommand, DetectClashesResult>
{
    public Task<DetectClashesResult> Handle(DetectClashesCommand request, CancellationToken cancellationToken)
    {
        var sitePlan = MapToSitePlan(request.SitePlan);
        var buildings = MapToBuildings(request.Buildings);

        var allClashes = new List<Clash>();
        foreach (var rule in rules)
        {
            allClashes.AddRange(rule.Evaluate(sitePlan, buildings));
        }

        var processedClashes = ApplyOverlapSubsumesClearance(allClashes);

        var clashDtos = processedClashes
            .Select(c => new ClashDto(
                c.BuildingNames,
                c.Type.ToString(),
                c.Severity.ToString(),
                c.Description))
            .ToList();

        return Task.FromResult(new DetectClashesResult(clashDtos));
    }

    private static SitePlan MapToSitePlan(SitePlanDto dto) =>
        new() { Dimensions = new Dimensions(dto.Width, dto.Length) };

    private static IReadOnlyList<Building> MapToBuildings(IReadOnlyList<BuildingDto> dtos) =>
        dtos.Select(dto =>
        {
            if (!Enum.TryParse<BuildingType>(dto.Type, out var buildingType))
                throw new InvalidOperationException($"Invalid building type '{dto.Type}'. This should have been caught by validation.");

            return new Building
            {
                Name = dto.Name,
                Type = buildingType,
                Position = new Position(dto.X, dto.Y),
                Dimensions = new Dimensions(dto.Width, dto.Length)
            };
        }).ToList();

    private static List<Clash> ApplyOverlapSubsumesClearance(List<Clash> clashes)
    {
        var overlapPairs = clashes
            .Where(c => c.Type == ClashType.Overlap)
            .Select(c => CreatePairKey(c.BuildingNames))
            .ToHashSet();

        return clashes
            .Where(c => !(c.Type == ClashType.InsufficientClearance && overlapPairs.Contains(CreatePairKey(c.BuildingNames))))
            .ToList();
    }

    private static (string, string) CreatePairKey(IReadOnlyList<string> names)
    {
        if (names.Count < 2) return (names[0], string.Empty);

        return StringComparer.OrdinalIgnoreCase.Compare(names[0], names[1]) <= 0
            ? (names[0], names[1])
            : (names[1], names[0]);
    }
}
