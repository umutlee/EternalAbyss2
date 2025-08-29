#if false
// Disabled to avoid duplicate type with existing project definition.

using System;
using System.Collections.Generic;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 研究解鎖內容
    /// </summary>
    [Serializable]
    public struct ResearchUnlocks
    {
        /// <summary>
        /// 解鎖的建築類型
        /// </summary>
        public List<BuildingType> UnlockedBuildings;
        
        /// <summary>
        /// 解鎖的單位類型
        /// </summary>
        public List<UnitType> UnlockedUnitTypes;
        
        /// <summary>
        /// 解鎖的技術項目
        /// </summary>
        public List<string> UnlockedTechnologies;
        
        /// <summary>
        /// 解鎖的能力項目
        /// </summary>
        public List<string> UnlockedAbilities;

        /// <summary>
        /// 創建空的解鎖內容
        /// </summary>
        /// <returns>空的解鎖內容</returns>
        public static ResearchUnlocks Empty()
            => new ResearchUnlocks
            {
                UnlockedBuildings = new List<BuildingType>(),
                UnlockedUnitTypes = new List<UnitType>(),
                UnlockedTechnologies = new List<string>(),
                UnlockedAbilities = new List<string>()
            };
    }
}

#endif