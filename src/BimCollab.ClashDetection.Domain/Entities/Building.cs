using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.ValueObjects;

namespace BimCollab.ClashDetection.Domain.Entities;

public class Building
{
    public required string Name { get; init; }
    public required BuildingType Type { get; init; }
    public required Position Position { get; init; }
    public required Dimensions Dimensions { get; init; }

    public double Right => Position.X + Dimensions.Width;
    public double Top => Position.Y + Dimensions.Length;
}
