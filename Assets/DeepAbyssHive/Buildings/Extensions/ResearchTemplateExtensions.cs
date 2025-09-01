using System;
using System.Linq;
using System.Reflection;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Extensions
{
    public static class ResearchTemplateExtensions
    {
        // 兼容：不論實際欄位叫什麼，統一取出 BuildingType[]
        public static BuildingType[] GetRequiredBuildings(this ResearchTemplate t)
        {
            if (t == null) return Array.Empty<BuildingType>();
            var type = t.GetType();
            var prop = type.GetProperty("RequiredBuildings", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                      ?? type.GetProperty("RequiredBuildingTypes", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (prop != null && typeof(BuildingType[]).IsAssignableFrom(prop.PropertyType))
                return (prop.GetValue(t) as BuildingType[]) ?? Array.Empty<BuildingType>();
            return Array.Empty<BuildingType>();
        }
    }
}