using System.Globalization;
using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;
using BimCollab.ClashDetection.Domain.Utilities;

namespace BimCollab.ClashDetection.Domain.Rules;

public class ClearanceRule : IClashDetectionRule
{
    private const double MinimumClearance = 10.0;

    public IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings)
    {
        var clashes = new List<Clash>();

        foreach (var (a, b) in GeometryHelper.GetPairs(buildings))
        {
            double distance = GeometryHelper.CalculateEdgeToEdgeDistance(a, b);

            if (distance < MinimumClearance)
            {
                clashes.Add(new Clash
                {
                    BuildingNames = [a.Name, b.Name],
                    Type = ClashType.InsufficientClearance,
                    Severity = ClashSeverity.Warning,
                    Description = string.Create(CultureInfo.InvariantCulture, $"Buildings '{a.Name}' and '{b.Name}' have insufficient clearance ({distance:F1} units, minimum is {MinimumClearance} units).")
                });
            }
        }

        return clashes;
    }
}
