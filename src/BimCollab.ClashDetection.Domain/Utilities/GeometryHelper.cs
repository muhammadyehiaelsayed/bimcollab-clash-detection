using BimCollab.ClashDetection.Domain.Entities;

namespace BimCollab.ClashDetection.Domain.Utilities;

public static class GeometryHelper
{
    public static double CalculateEdgeToEdgeDistance(Building a, Building b)
    {
        double gapX = Math.Max(0, Math.Max(a.Position.X, b.Position.X) - Math.Min(a.Right, b.Right));
        double gapY = Math.Max(0, Math.Max(a.Position.Y, b.Position.Y) - Math.Min(a.Top, b.Top));
        return Math.Sqrt(gapX * gapX + gapY * gapY);
    }

    public static IEnumerable<(Building A, Building B)> GetPairs(IReadOnlyList<Building> buildings)
    {
        for (int i = 0; i < buildings.Count; i++)
            for (int j = i + 1; j < buildings.Count; j++)
                yield return (buildings[i], buildings[j]);
    }
}
