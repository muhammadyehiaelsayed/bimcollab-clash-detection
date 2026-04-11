using BimCollab.ClashDetection.Domain.Models;
using MediatR;

namespace BimCollab.ClashDetection.Application.ClashDetection.Commands;

public record DetectClashesCommand : IRequest<DetectClashesResult>
{
    public required SitePlanDto SitePlan { get; init; }
    public required IReadOnlyList<BuildingDto> Buildings { get; init; }
}

public record SitePlanDto(double Width, double Length);

public record BuildingDto(
    string Name,
    string Type,
    double Width,
    double Length,
    double X,
    double Y);

public record DetectClashesResult(IReadOnlyList<ClashDto> Clashes);

public record ClashDto(
    IReadOnlyList<string> BuildingNames,
    string Type,
    string Severity,
    string Description);
