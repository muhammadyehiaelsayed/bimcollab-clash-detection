using BimCollab.ClashDetection.Domain.Enums;

namespace BimCollab.ClashDetection.Domain.Models;

public record Clash
{
    public required IReadOnlyList<string> BuildingNames { get; init; }
    public required ClashType Type { get; init; }
    public required ClashSeverity Severity { get; init; }
    public required string Description { get; init; }
}
