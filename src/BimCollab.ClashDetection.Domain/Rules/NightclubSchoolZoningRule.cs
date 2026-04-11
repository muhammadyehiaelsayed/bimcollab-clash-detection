using System.Globalization;
using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;
using BimCollab.ClashDetection.Domain.Utilities;

namespace BimCollab.ClashDetection.Domain.Rules;

public class NightclubSchoolZoningRule : IClashDetectionRule
{
    private const double MinimumDistance = 200.0;

    public IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings)
    {
        var clashes = new List<Clash>();

        var nightclubs = buildings.Where(b => b.Type == BuildingType.Nightclub);
        var schools = buildings.Where(b => b.Type == BuildingType.School);

        foreach (var nightclub in nightclubs)
        {
            foreach (var school in schools)
            {
                double distance = GeometryHelper.CalculateEdgeToEdgeDistance(nightclub, school);

                if (distance < MinimumDistance)
                {
                    clashes.Add(new Clash
                    {
                        BuildingNames = [nightclub.Name, school.Name],
                        Type = ClashType.ZoningViolation,
                        Severity = ClashSeverity.Warning,
                        Description = string.Create(CultureInfo.InvariantCulture, $"Nightclub '{nightclub.Name}' is too close to school '{school.Name}' ({distance:F1} units, minimum is {MinimumDistance} units).")
                    });
                }
            }
        }

        return clashes;
    }
}
