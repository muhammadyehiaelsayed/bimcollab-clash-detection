namespace BimCollab.ClashDetection.Api.Contracts;

public record ClashDetectionResponse(IReadOnlyList<ClashResponse> Clashes);

public record ClashResponse(
    IReadOnlyList<string> BuildingNames,
    string Type,
    string Severity,
    string Description);
