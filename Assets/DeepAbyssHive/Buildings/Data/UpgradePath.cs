using System.Collections.Generic;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 升级路径数据
    /// </summary>
    [System.Serializable]
    public class UpgradePath
    {
        /// <summary>
        /// 路径ID
        /// </summary>
        public string PathId;
        
        /// <summary>
        /// 路径名称
        /// </summary>
        public string PathName;
        
        /// <summary>
        /// 路径描述
        /// </summary>
        public string Description;
        
        /// <summary>
        /// 适用的建筑类型
        /// </summary>
        public BuildingType[] ApplicableBuildingTypes;
        
        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel;
        
        /// <summary>
        /// 基础升级时间
        /// </summary>
        public float UpgradeTime = 30f;
        
        /// <summary>
        /// 每级升级时间
        /// </summary>
        public Dictionary<int, float> UpgradeTimeByLevel = new Dictionary<int, float>();
        
        /// <summary>
        /// 每级升级成本
        /// </summary>
        public Dictionary<int, ResourceCost> UpgradeCostByLevel = new Dictionary<int, ResourceCost>();
        
        /// <summary>
        /// 每级生命值加成
        /// </summary>
        public Dictionary<int, float> HealthBonusByLevel = new Dictionary<int, float>();
        
        /// <summary>
        /// 每级存储容量加成
        /// </summary>
        public Dictionary<int, float> StorageBonusByLevel = new Dictionary<int, float>();
        
        /// <summary>
        /// 每级菌毯扩张范围加成
        /// </summary>
        public Dictionary<int, float> CreepExpansionBonusByLevel = new Dictionary<int, float>();
        
        /// <summary>
        /// 每级生产效率加成
        /// </summary>
        public Dictionary<int, float> ProductionBonusByLevel = new Dictionary<int, float>();
        
        /// <summary>
        /// 每级解锁的能力
        /// </summary>
        public Dictionary<int, string[]> UnlockedAbilitiesByLevel = new Dictionary<int, string[]>();
        
        /// <summary>
        /// 前置条件
        /// </summary>
        public string[] Prerequisites;
    }
}