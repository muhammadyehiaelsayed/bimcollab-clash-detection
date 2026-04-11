using System.Globalization;
using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;
using BimCollab.ClashDetection.Domain.Utilities;

namespace BimCollab.ClashDetection.Domain.Rules;

public class ResidentialZoningRule : IClashDetectionRule
{
    private const double MinimumDistance = 150.0;

    public IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings)
    {
        var clashes = new List<Clash>();

        var residentials = buildings.Where(b => b.Type == BuildingType.ResidentialBuilding);
        var restricted = buildings.Where(b => b.Type == BuildingType.Stadium || b.Type == BuildingType.Nightclub);

        foreach (var residential in residentials)
        {
            foreach (var other in restricted)
            {
                double distance = GeometryHelper.CalculateEdgeToEdgeDistance(residential, other);

                if (distance < MinimumDistance)
                {
                    clashes.Add(new Clash
                    {
                        BuildingNames = [residential.Name, other.Name],
                        Type = ClashType.ZoningViolation,
                        Severity = ClashSeverity.Warning,
                        Description = string.Create(CultureInfo.InvariantCulture, $"Residential building '{residential.Name}' is too close to {other.Type.ToString().ToLowerInvariant()} '{other.Name}' ({distance:F1} units, minimum is {MinimumDistance} units).")
                    });
                }
            }
        }

        return clashes;
    }
}
