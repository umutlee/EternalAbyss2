using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 核心（字段、初始化、IManager生命周期）
    /// 说明：
    /// - 本文件为partial占位，不改变任何对外API与行为
    /// - 后续将把字段区、构造器、Initialize/Cleanup/Update等迁移至此
    /// </summary>
    public partial class BuildingManager
    {
        // 配置系统
        private BuildingConfigSO _config;
        
        // 私有字段定义
        private readonly Dictionary<int, BuildingData> _buildings = new Dictionary<int, BuildingData>();
        private readonly Dictionary<int, GameObject> _buildingGameObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<BuildingType, string> _buildingPrefabPaths = new Dictionary<BuildingType, string>();
        private readonly Dictionary<BuildingType, BuildingTemplate> _buildingTemplates = new Dictionary<BuildingType, BuildingTemplate>();
        private readonly Dictionary<string, ResearchTemplate> _researchTemplates = new Dictionary<string, ResearchTemplate>();
        private readonly Dictionary<int, HashSet<string>> _playerResearch = new Dictionary<int, HashSet<string>>();
        private readonly Queue<int> _buildingUpdateQueue = new Queue<int>();
        
        // 服务字段定义
        private IBuildingQueryService _queryService;
        private IBuildingConstructionService _constructionService;
        private IResearchService _researchService;
        
        // 配置参数（从配置文件加载，带默认值）
        private float _buildingUpdateTimer = 0f;
        private float _buildingUpdateInterval = 0.1f;
        private int _maxBuildingUpdatesPerFrame = 10;
        private float _buildingPlacementGridSize = 1f;
        private string _managerName = "BuildingManager";

        /// <summary>
        /// 初始化配置系统
        /// </summary>
        private void InitializeConfig()
        {
            _config = ConfigManager.Instance.GetConfig<BuildingConfigSO>();
            
            if (_config != null)
            {
                // 从配置加载参数
                _buildingUpdateInterval = _config.performanceConfig.updateInterval;
                _maxBuildingUpdatesPerFrame = _config.performanceConfig.maxUpdatesPerFrame;
                _buildingPlacementGridSize = _config.constructionConfig.placementCheckRadius;
                
                Debug.Log($"[{_managerName}] 配置加载成功: {_config.ConfigName}");
            }
            else
            {
                Debug.LogWarning($"[{_managerName}] 配置文件未找到，使用默认值");
            }
        }

        /// <summary>
        /// 从配置初始化建筑模板
        /// </summary>
        private void InitializeBuildingTemplatesFromConfig()
        {
            if (_config?.buildingTemplates != null)
            {
                foreach (var templateConfig in _config.buildingTemplates)
                {
                    var template = new BuildingTemplate
                    {
                        Type = templateConfig.buildingType,
                        Name = templateConfig.displayName,
                        MaxHealth = templateConfig.maxHealth,
                        ConstructionTime = templateConfig.buildTime,
                        Size = templateConfig.size,
                        MaxLevel = _config.upgradeConfig.maxUpgradeLevel,
                        BioEnergyConsumption = templateConfig.energyConsumption,
                        BioEnergyGeneration = templateConfig.buildingType == BuildingType.BioEnergyCore ? 50f : 0f
                    };
                    _buildingTemplates[templateConfig.buildingType] = template;
                }
                Debug.Log($"[{_managerName}] 从配置加载了 {_config.buildingTemplates.Length} 个建筑模板");
            }
            else
            {
                // 使用默认模板
                InitializeBuildingTemplates();
            }
        }

        /// <summary>
        /// 从配置初始化建筑预制体路径
        /// </summary>
        private void InitializeBuildingPrefabPathsFromConfig()
        {
            if (_config?.buildingTemplates != null)
            {
                foreach (var templateConfig in _config.buildingTemplates)
                {
                    _buildingPrefabPaths[templateConfig.buildingType] = templateConfig.prefabPath;
                }
                Debug.Log($"[{_managerName}] 从配置加载了 {_config.buildingTemplates.Length} 个预制体路径");
            }
            else
            {
                // 使用默认路径
                InitializeBuildingPrefabPaths();
            }
        }

        /// <summary>
        /// 从配置初始化研究模板
        /// </summary>
        private void InitializeResearchTemplatesFromConfig()
        {
            if (_config?.researchTemplates != null)
            {
                foreach (var researchConfig in _config.researchTemplates)
                {
                    var template = new ResearchTemplate
                    {
                        Id = researchConfig.researchId,
                        Name = researchConfig.displayName,
                        Description = researchConfig.description,
                        ResearchTime = researchConfig.researchTime,
                        RequiredBuilding = researchConfig.requiredBuilding,
                        Prerequisites = new List<string>(researchConfig.prerequisites ?? new string[0])
                    };
                    _researchTemplates[researchConfig.researchId] = template;
                }
                Debug.Log($"[{_managerName}] 从配置加载了 {_config.researchTemplates.Length} 个研究模板");
            }
        }

        /// <summary>
        /// 实例化建筑游戏对象
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <returns>建筑游戏对象</returns>
        private GameObject InstantiateBuildingObject(BuildingData buildingData)
        {
            GameObject buildingObject = new GameObject($"Building_{buildingData.BuildingId}");
            buildingObject.transform.position = buildingData.Position;
            buildingObject.transform.rotation = buildingData.Rotation;
            return buildingObject;
        }

        /// <summary>
        /// 更新建筑游戏对象
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="buildingData">建筑数据</param>
        private void UpdateBuildingGameObject(int buildingId, BuildingData buildingData)
        {
            if (_buildingGameObjects.TryGetValue(buildingId, out GameObject buildingObject) && buildingObject != null)
            {
                // 更新建筑对象的视觉状态
                buildingObject.transform.position = buildingData.Position;
                buildingObject.transform.rotation = buildingData.Rotation;
            }
        }

        /// <summary>
        /// 获取建筑类型的预制体路径
        /// </summary>
        /// <param name="type">建筑类型</param>
        /// <returns>预制体路径</returns>
        private string GetPrefabPathForType(BuildingType type)
        {
            if (_buildingPrefabPaths.TryGetValue(type, out string path))
            {
                return path;
            }
            return $"Buildings/{type}";
        }

        /// <summary>
        /// 初始化建筑模板（向后兼容版本）
        /// </summary>
        private void InitializeBuildingTemplates()
        {
            // 从配置文件或资源中加载建筑模板
            // 这里使用简化的硬编码实现（向后兼容）
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            {
                var template = new BuildingTemplate
                {
                    Type = type,
                    Name = type.ToString(),
                    MaxHealth = 100f,
                    ConstructionTime = 10f,
                    Size = new Vector2Int(2, 2),
                    MaxLevel = 3,
                    BioEnergyConsumption = 10f,
                    BioEnergyGeneration = type == BuildingType.BioEnergyCore ? 50f : 0f
                };
                _buildingTemplates[type] = template;
            }
        }

        /// <summary>
        /// 初始化建筑预制体路径（向后兼容版本）
        /// </summary>
        private void InitializeBuildingPrefabPaths()
        {
            // 初始化建筑预制体路径映射（向后兼容）
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            {
                _buildingPrefabPaths[type] = $"Prefabs/Buildings/{type}";
            }
        }

        /// <summary>
        /// 初始化所有配置和模板
        /// </summary>
        public void Initialize()
        {
            // 1. 首先初始化配置系统
            InitializeConfig();
            
            // 2. 从配置初始化各种模板和路径
            InitializeBuildingTemplatesFromConfig();
            InitializeBuildingPrefabPathsFromConfig();
            InitializeResearchTemplatesFromConfig();
            
            Debug.Log($"[{_managerName}] 初始化完成");
        }

        /// <summary>
        /// 清理资源
        /// </summary>
        public void Cleanup()
        {
            _buildings.Clear();
            _buildingGameObjects.Clear();
            _buildingTemplates.Clear();
            _researchTemplates.Clear();
            _playerResearch.Clear();
            _buildingUpdateQueue.Clear();
            
            Debug.Log($"[{_managerName}] 清理完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            _buildingUpdateTimer += deltaTime;
            
            if (_buildingUpdateTimer >= _buildingUpdateInterval)
            {
                ProcessBuildingUpdates();
                _buildingUpdateTimer = 0f;
            }
        }

        /// <summary>
        /// 处理建筑更新
        /// </summary>
        private void ProcessBuildingUpdates()
        {
            int updatesProcessed = 0;
            
            while (_buildingUpdateQueue.Count > 0 && updatesProcessed < _maxBuildingUpdatesPerFrame)
            {
                int buildingId = _buildingUpdateQueue.Dequeue();
                
                if (_buildings.TryGetValue(buildingId, out BuildingData buildingData))
                {
                    UpdateBuildingGameObject(buildingId, buildingData);
                    updatesProcessed++;
                }
            }
        }

        /// <summary>
        /// 检查升级需求
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="upgradePath">升级路径</param>
        /// <returns>是否满足需求</returns>
        private bool CheckUpgradeRequirements(int playerId, UpgradePath upgradePath)
        {
            // 检查升级需求的实现
            return true; // 简化实现
        }



        /// <summary>
        /// 获取建筑半径
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <returns>建筑半径</returns>
        private float GetBuildingRadius(BuildingType buildingType)
        {
            var template = GetBuildingTemplate(buildingType);
            if (template != null)
            {
                // 将Vector2Int转换为Vector2后再与float相乘
                Vector2 size = new Vector2(template.Size.x, template.Size.y);
                return Mathf.Max(size.x, size.y) * 0.5f;
            }
            return 1.0f; // 默认半径
        }

        /// <summary>
        /// 销毁建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        public void DestroyBuilding(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试销毁不存在的建筑: {buildingId}");
                return;
            }

            // 销毁游戏对象
            if (_buildingGameObjects.TryGetValue(buildingId, out GameObject buildingObject) && buildingObject != null)
            {
                UnityEngine.Object.Destroy(buildingObject);
                _buildingGameObjects.Remove(buildingId);
            }

            // 移除建筑数据
            _buildings.Remove(buildingId);

            Debug.Log($"[{_managerName}] 销毁建筑: ID={buildingId}");
        }
    }
}