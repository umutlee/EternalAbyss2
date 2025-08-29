using System.Collections.Generic;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 研究模板数据
    /// </summary>
    [System.Serializable]
    public class ResearchTemplate
    {
        /// <summary>
        /// 研究ID
        /// </summary>
        public string Id;
        
        /// <summary>
        /// 研究名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 研究描述
        /// </summary>
        public string Description;
        
        /// <summary>
        /// 研究时间
        /// </summary>
        public float ResearchTime;
        
        /// <summary>
        /// 研究成本
        /// </summary>
        public ResourceCost ResearchCost;
        
        /// <summary>
        /// 支持的建筑类型
        /// </summary>
        public string[] SupportedBuildingTypes;
        
        /// <summary>
        /// 前置条件
        /// </summary>
        public string[] Prerequisites;
        
        /// <summary>
        /// 研究效果
        /// </summary>
        public Dictionary<string, float> Effects = new Dictionary<string, float>();
        
        /// <summary>
        /// 解锁的建筑类型
        /// </summary>
        public string[] UnlockedBuildings;
        
        /// <summary>
        /// 解锁的单位类型（UnitType枚举数组）
        /// </summary>
        public UnitType[] UnlockedUnitTypes;
        
        /// <summary>
        /// 解锁的技术
        /// </summary>
        public string[] UnlockedTechnologies;
        
        /// <summary>
        /// 研究类别
        /// </summary>
        public ResearchCategory Category = ResearchCategory.General;
    }
    
    /// <summary>
    /// 研究类别
    /// </summary>
    public enum ResearchCategory
    {
        /// <summary>
        /// 通用研究
        /// </summary>
        General,
        
        /// <summary>
        /// 单位升级
        /// </summary>
        UnitUpgrade,
        
        /// <summary>
        /// 建筑升级
        /// </summary>
        BuildingUpgrade,
        
        /// <summary>
        /// 经济升级
        /// </summary>
        EconomicUpgrade,
        
        /// <summary>
        /// 军事升级
        /// </summary>
        MilitaryUpgrade,
        
        /// <summary>
        /// 特殊能力
        /// </summary>
        SpecialAbility
    }
}