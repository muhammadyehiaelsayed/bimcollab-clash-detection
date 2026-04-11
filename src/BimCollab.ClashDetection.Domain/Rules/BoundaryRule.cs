using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Enums;
using BimCollab.ClashDetection.Domain.Models;

namespace BimCollab.ClashDetection.Domain.Rules;

public class BoundaryRule : IClashDetectionRule
{
    public IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings)
    {
        var clashes = new List<Clash>();

        foreach (var building in buildings)
        {
            bool exceedsRight = building.Right > sitePlan.Dimensions.Width;
            bool exceedsTop = building.Top > sitePlan.Dimensions.Length;

            if (exceedsRight || exceedsTop)
            {
                clashes.Add(new Clash
                {
                    BuildingNames = [building.Name],
                    Type = ClashType.BoundaryViolation,
                    Severity = ClashSeverity.Critical,
                    Description = $"Building '{building.Name}' extends beyond site plan boundaries."
                });
            }
        }

        return clashes;
    }
}
