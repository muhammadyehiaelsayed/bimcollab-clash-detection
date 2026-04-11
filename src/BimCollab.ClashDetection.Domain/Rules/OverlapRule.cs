using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;
using BimCollab.ClashDetection.Domain.Utilities;

namespace BimCollab.ClashDetection.Domain.Rules;

public class OverlapRule : IClashDetectionRule
{
    public IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings)
    {
        var clashes = new List<Clash>();

        foreach (var (a, b) in GeometryHelper.GetPairs(buildings))
        {
            // Strict inequality: touching edges are NOT overlap
            if (a.Position.X < b.Right &&
                b.Position.X < a.Right &&
                a.Position.Y < b.Top &&
                b.Position.Y < a.Top)
            {
                clashes.Add(new Clash
                {
                    BuildingNames = [a.Name, b.Name],
                    Type = ClashType.Overlap,
                    Severity = ClashSeverity.Critical,
                    Description = $"Buildings '{a.Name}' and '{b.Name}' overlap."
                });
            }
        }

        return clashes;
    }
}
