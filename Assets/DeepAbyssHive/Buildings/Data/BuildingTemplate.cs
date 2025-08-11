using UnityEngine;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 建筑模板数据
    /// </summary>
    [System.Serializable]
    public class BuildingTemplate
    {
        /// <summary>
        /// 建筑类型
        /// </summary>
        public BuildingType Type;
        
        /// <summary>
        /// 建筑名称
        /// </summary>
        public string Name;
        
        /// <summary>
        /// 基础生命值
        /// </summary>
        public float BaseHealth;
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHealth;
        
        /// <summary>
        /// 建筑大小
        /// </summary>
        public Vector2Int Size;
        
        /// <summary>
        /// 预制体路径
        /// </summary>
        public string PrefabPath;
        
        /// <summary>
        /// 基础存储容量
        /// </summary>
        public float BaseStorageCapacity;
        
        /// <summary>
        /// 基础菌毯扩张半径
        /// </summary>
        public float BaseCreepExpansionRadius;
        
        /// <summary>
        /// 基础特殊能力
        /// </summary>
        public string[] BaseSpecialAbilities;
        
        /// <summary>
        /// 建造时间
        /// </summary>
        public float ConstructionTime = 60f;
        
        /// <summary>
        /// 建造成本
        /// </summary>
        public ResourceCost ConstructionCost;
        
        /// <summary>
        /// 是否需要菌毯
        /// </summary>
        public bool RequiresCreep = true;
        
        /// <summary>
        /// 最大等级
        /// </summary>
        public int MaxLevel = 1;
        
        /// <summary>
        /// 生物能消耗
        /// </summary>
        public float BioEnergyConsumption = 0f;
        
        /// <summary>
        /// 生物能产出
        /// </summary>
        public float BioEnergyGeneration = 0f;
        
        /// <summary>
        /// 升级路径
        /// </summary>
        public UpgradePath[] UpgradePaths;
    }
    
    /// <summary>
    /// 资源成本
    /// </summary>
    [System.Serializable]
    public class ResourceCost
    {
        /// <summary>
        /// 矿物成本
        /// </summary>
        public int Minerals;
        
        /// <summary>
        /// 气体成本
        /// </summary>
        public int Gas;
        
        /// <summary>
        /// 人口成本
        /// </summary>
        public int Supply;
    }
}