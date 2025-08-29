using System.Collections.Generic;

namespace DeepAbyssHive.Buildings.Data
{
    public enum ResourceKind { Minerals, Gas, Supply }

    public static class ResourceCostExtensions
    {
        public static IEnumerable<(ResourceKind ResourceType, int Amount)> EnumerateCosts(this ResourceCost c)
        {
            if (c.Minerals > 0) yield return (ResourceKind.Minerals, c.Minerals);
            if (c.Gas      > 0) yield return (ResourceKind.Gas,      c.Gas);
            if (c.Supply   > 0) yield return (ResourceKind.Supply,   c.Supply);
        }
    }
}