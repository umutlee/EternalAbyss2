using System;
using System.Reflection;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Utils
{
    public static class ResourceCostShim
    {
        public static ResourceType GetResourceType(object cost)
        {
            if (cost == null) return default;
            var t = cost.GetType();

            // Try Property "ResourceType" then "Type"
            var p = t.GetProperty("ResourceType") ?? t.GetProperty("Type");
            if (p != null) return (ResourceType)p.GetValue(cost, null);

            // Try Field "ResourceType" then "Type"
            var f = t.GetField("ResourceType") ?? t.GetField("Type");
            if (f != null) return (ResourceType)f.GetValue(cost);

            return default;
        }

        public static float GetAmount(object cost)
        {
            if (cost == null) return 0f;
            var t = cost.GetType();

            // Try Property "Amount" then "Value"
            var p = t.GetProperty("Amount") ?? t.GetProperty("Value");
            if (p != null) return Convert.ToSingle(p.GetValue(cost, null));

            // Try Field "Amount" then "Value"
            var f = t.GetField("Amount") ?? t.GetField("Value");
            if (f != null) return Convert.ToSingle(f.GetValue(cost));

            return 0f;
        }
    }
}