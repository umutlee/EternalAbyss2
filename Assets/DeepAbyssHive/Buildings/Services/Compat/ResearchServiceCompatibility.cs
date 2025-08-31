using System;
using System.Linq;
using System.Collections.Generic;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Buildings.Services.Compat
{
    /// <summary>
    /// 研究服務相容性輔助：把各種舊型別統一轉成 string[]。
    /// </summary>
    public static class ResearchServiceCompatibility
    {
        public static string[] AsStringArray(this List<string> src)
            => src?.ToArray() ?? Array.Empty<string>();

        public static string[] AsStringArray(this List<BuildingType> src)
            => src?.Select(b => b.ToString()).ToArray() ?? Array.Empty<string>();

        public static string[] AsStringArray(this UnitType[] src)
            => src?.Select(u => u.ToString()).ToArray() ?? Array.Empty<string>();
    }
}