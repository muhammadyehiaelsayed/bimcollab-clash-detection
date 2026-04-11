namespace BimCollab.ClashDetection.Api.Contracts;

public record DetectClashesRequest
{
    public required SitePlanRequest SitePlan { get; init; }
    public required List<BuildingRequest> Buildings { get; init; }
}

public record SitePlanRequest(double Width, double Length);

public record BuildingRequest(
    string Name,
    string Type,
    double Width,
    double Length,
    double X,
    double Y);
