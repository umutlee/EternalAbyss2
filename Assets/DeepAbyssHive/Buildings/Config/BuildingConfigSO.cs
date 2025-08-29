using UnityEngine;
using System.Collections.Generic;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Config
{
    /// <summary>
    /// 建筑系统配置数据
    /// 包含建筑模板、研究配置、建造参数等所有配置
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingConfig", menuName = "DeepAbyssHive/Config/Building Config")]
    public class BuildingConfigSO : BaseConfigSO
    {
        [Header("建筑模板配置")]
        [Tooltip("建筑模板定义")]
        public BuildingTemplateConfig[] buildingTemplates = new BuildingTemplateConfig[]
        {
            new BuildingTemplateConfig
            {
                buildingType = BuildingType.Hatchery,
                prefabPath = "Prefabs/Buildings/Hatchery",
                displayName = "孵化场",
                description = "生产工蚁的基础建筑",
                buildCost = new ResourceCostConfig[]
                {
                    new ResourceCostConfig { resourceType = "Biomass", amount = 100 },
                    new ResourceCostConfig { resourceType = "Energy", amount = 50 }
                },
                buildTime = 30f,
                maxHealth = 500f,
                size = new Vector2Int(3, 3),
                requiredTech = "",
                energyConsumption = 10f,
                productionCapacity = 5
            },
            new BuildingTemplateConfig
            {
                buildingType = BuildingType.SpawningPool,
                prefabPath = "Prefabs/Buildings/SpawningPool",
                displayName = "孵化池",
                description = "生产战斗单位的建筑",
                buildCost = new ResourceCostConfig[]
                {
                    new ResourceCostConfig { resourceType = "Biomass", amount = 150 },
                    new ResourceCostConfig { resourceType = "Energy", amount = 75 }
                },
                buildTime = 45f,
                maxHealth = 600f,
                size = new Vector2Int(4, 4),
                requiredTech = "basic_evolution",
                energyConsumption = 15f,
                productionCapacity = 3
            },
            new BuildingTemplateConfig
            {
                buildingType = BuildingType.EvolutionChamber,
                prefabPath = "Prefabs/Buildings/EvolutionChamber",
                displayName = "进化腔",
                description = "研究进化科技的建筑",
                buildCost = new ResourceCostConfig[]
                {
                    new ResourceCostConfig { resourceType = "Biomass", amount = 200 },
                    new ResourceCostConfig { resourceType = "Energy", amount = 100 }
                },
                buildTime = 60f,
                maxHealth = 400f,
                size = new Vector2Int(2, 2),
                requiredTech = "",
                energyConsumption = 20f,
                productionCapacity = 0
            }
        };

        [Header("研究配置")]
        [Tooltip("研究项目定义")]
        public ResearchTemplateConfig[] researchTemplates = new ResearchTemplateConfig[]
        {
            new ResearchTemplateConfig
            {
                researchId = "basic_evolution",
                displayName = "基础进化",
                description = "解锁基础单位进化能力",
                researchCost = new ResourceCostConfig[]
                {
                    new ResourceCostConfig { resourceType = "Biomass", amount = 100 },
                    new ResourceCostConfig { resourceType = "Energy", amount = 50 }
                },
                researchTime = 60f,
                requiredBuilding = BuildingType.EvolutionChamber,
                prerequisites = new string[0],
                unlockedBuildings = new BuildingType[] { BuildingType.SpawningPool },
                unlockedUnits = new string[] { "Warrior" }
            },
            new ResearchTemplateConfig
            {
                researchId = "advanced_carapace",
                displayName = "高级甲壳",
                description = "提升单位防御力",
                researchCost = new ResourceCostConfig[]
                {
                    new ResourceCostConfig { resourceType = "Biomass", amount = 150 },
                    new ResourceCostConfig { resourceType = "Energy", amount = 100 }
                },
                researchTime = 90f,
                requiredBuilding = BuildingType.EvolutionChamber,
                prerequisites = new string[] { "basic_evolution" },
                unlockedBuildings = new BuildingType[0],
                unlockedUnits = new string[0]
            }
        };

        [Header("建造配置")]
        [Tooltip("建造系统参数")]
        public BuildConstructionConfig constructionConfig = new BuildConstructionConfig
        {
            maxConcurrentBuilds = 5,
            buildSpeedMultiplier = 1f,
            cancelRefundPercentage = 0.75f,
            placementCheckRadius = 1f,
            requiresCreepConnection = true,
            maxCreepDistance = 10f
        };

        [Header("升级配置")]
        [Tooltip("建筑升级参数")]
        public BuildUpgradeConfig upgradeConfig = new BuildUpgradeConfig
        {
            maxUpgradeLevel = 3,
            upgradeTimeMultiplier = 1.5f,
            upgradeCostMultiplier = 1.2f,
            healthIncreasePerLevel = 100f,
            efficiencyIncreasePerLevel = 0.1f
        };

        [Header("性能配置")]
        [Tooltip("性能优化参数")]
        public BuildPerformanceConfig performanceConfig = new BuildPerformanceConfig
        {
            updateBatchSize = 50,
            updateInterval = 0.1f,
            enableAsyncUpdates = true,
            maxUpdatesPerFrame = 10
        };

        [Header("放置验证配置")]
        [Tooltip("建筑放置验证参数")]
        public PlacementValidationConfig placementConfig = new PlacementValidationConfig
        {
            checkTerrain = true,
            checkObstacles = true,
            checkResources = true,
            checkCreepCoverage = true,
            minDistanceFromEnemies = 5f,
            allowOverlap = false
        };

        protected override void OnValidate()
        {
            base.OnValidate();
            
            // 验证建筑模板
            foreach (var template in buildingTemplates)
            {
                template.buildTime = Mathf.Max(1f, template.buildTime);
                template.maxHealth = Mathf.Max(1f, template.maxHealth);
                template.size.x = Mathf.Max(1, template.size.x);
                template.size.y = Mathf.Max(1, template.size.y);
                template.energyConsumption = Mathf.Max(0f, template.energyConsumption);
                template.productionCapacity = Mathf.Max(0, template.productionCapacity);
            }
            
            // 验证研究模板
            foreach (var research in researchTemplates)
            {
                research.researchTime = Mathf.Max(1f, research.researchTime);
            }
            
            // 验证建造配置
            constructionConfig.maxConcurrentBuilds = Mathf.Max(1, constructionConfig.maxConcurrentBuilds);
            constructionConfig.buildSpeedMultiplier = Mathf.Max(0.1f, constructionConfig.buildSpeedMultiplier);
            constructionConfig.cancelRefundPercentage = Mathf.Clamp01(constructionConfig.cancelRefundPercentage);
            constructionConfig.placementCheckRadius = Mathf.Max(0.1f, constructionConfig.placementCheckRadius);
            constructionConfig.maxCreepDistance = Mathf.Max(0f, constructionConfig.maxCreepDistance);
            
            // 验证升级配置
            upgradeConfig.maxUpgradeLevel = Mathf.Max(1, upgradeConfig.maxUpgradeLevel);
            upgradeConfig.upgradeTimeMultiplier = Mathf.Max(0.1f, upgradeConfig.upgradeTimeMultiplier);
            upgradeConfig.upgradeCostMultiplier = Mathf.Max(0.1f, upgradeConfig.upgradeCostMultiplier);
            upgradeConfig.healthIncreasePerLevel = Mathf.Max(0f, upgradeConfig.healthIncreasePerLevel);
            upgradeConfig.efficiencyIncreasePerLevel = Mathf.Max(0f, upgradeConfig.efficiencyIncreasePerLevel);
            
            // 验证性能配置
            performanceConfig.updateBatchSize = Mathf.Max(1, performanceConfig.updateBatchSize);
            performanceConfig.updateInterval = Mathf.Max(0f, performanceConfig.updateInterval);
            performanceConfig.maxUpdatesPerFrame = Mathf.Max(1, performanceConfig.maxUpdatesPerFrame);
            
            // 验证放置配置
            placementConfig.minDistanceFromEnemies = Mathf.Max(0f, placementConfig.minDistanceFromEnemies);
        }
    }

    // 配置数据结构
    [System.Serializable]
    public class BuildingTemplateConfig
    {
        public BuildingType buildingType;
        public string prefabPath;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public ResourceCostConfig[] buildCost;
        public float buildTime;
        public float maxHealth;
        public Vector2Int size;
        public string requiredTech;
        public float energyConsumption;
        public int productionCapacity;
    }

    [System.Serializable]
    public partial class ResearchTemplateConfig
    {
        public string Id;
        public string displayName;
        [TextArea(2, 4)]
        public string description;
        public ResourceCostConfig[] researchCost;
        public float researchTime;
        public BuildingType requiredBuilding;
        public string[] prerequisites;
        public BuildingType[] unlockedBuildings;
        public string[] unlockedUnits;
    }

    [System.Serializable]
    public class ResourceCostConfig
    {
        public string resourceType;
        public int amount;
    }

    [System.Serializable]
    public class BuildConstructionConfig
    {
        [Tooltip("最大同时建造数量")]
        public int maxConcurrentBuilds = 5;
        
        [Tooltip("建造速度倍数")]
        public float buildSpeedMultiplier = 1f;
        
        [Tooltip("取消建造的资源返还比例")]
        [Range(0f, 1f)]
        public float cancelRefundPercentage = 0.75f;
        
        [Tooltip("放置检查半径")]
        public float placementCheckRadius = 1f;
        
        [Tooltip("是否需要菌毯连接")]
        public bool requiresCreepConnection = true;
        
        [Tooltip("最大菌毯距离")]
        public float maxCreepDistance = 10f;
    }

    [System.Serializable]
    public class BuildUpgradeConfig
    {
        [Tooltip("最大升级等级")]
        public int maxUpgradeLevel = 3;
        
        [Tooltip("升级时间倍数")]
        public float upgradeTimeMultiplier = 1.5f;
        
        [Tooltip("升级成本倍数")]
        public float upgradeCostMultiplier = 1.2f;
        
        [Tooltip("每级生命值增加")]
        public float healthIncreasePerLevel = 100f;
        
        [Tooltip("每级效率增加")]
        public float efficiencyIncreasePerLevel = 0.1f;
    }

    [System.Serializable]
    public class BuildPerformanceConfig
    {
        [Tooltip("更新批处理大小")]
        public int updateBatchSize = 50;
        
        [Tooltip("更新间隔（秒）")]
        public float updateInterval = 0.1f;
        
        [Tooltip("启用异步更新")]
        public bool enableAsyncUpdates = true;
        
        [Tooltip("每帧最大更新数量")]
        public int maxUpdatesPerFrame = 10;
    }

    [System.Serializable]
    public class PlacementValidationConfig
    {
        [Tooltip("检查地形")]
        public bool checkTerrain = true;
        
        [Tooltip("检查障碍物")]
        public bool checkObstacles = true;
        
        [Tooltip("检查资源")]
        public bool checkResources = true;
        
        [Tooltip("检查菌毯覆盖")]
        public bool checkCreepCoverage = true;
        
        [Tooltip("与敌人的最小距离")]
        public float minDistanceFromEnemies = 5f;
        
        [Tooltip("允许重叠")]
        public bool allowOverlap = false;
    }
}