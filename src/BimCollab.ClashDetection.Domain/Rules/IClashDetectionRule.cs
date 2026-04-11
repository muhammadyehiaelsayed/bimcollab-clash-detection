using BimCollab.ClashDetection.Domain.Entities;
using BimCollab.ClashDetection.Domain.Models;

namespace BimCollab.ClashDetection.Domain.Rules;

public interface IClashDetectionRule
{
    IReadOnlyList<Clash> Evaluate(SitePlan sitePlan, IReadOnlyList<Building> buildings);
}
