using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Config;
using IBuildingManagerCore = DeepAbyssHive.Core.Interfaces.IBuildingManager;
using IBuildingManager = DeepAbyssHive.Buildings.Interfaces.IBuildingManager;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Buildings.Services;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 核心 - 服务容器和API适配器
    /// 职责：
    /// - 作为服务容器，持有 IBuildingQueryService 和 IBuildingConstructionService
    /// - 提供向后兼容的公共API，内部委托给服务处理
    /// - 管理MonoBehaviour生命周期和IManager接口实现
    /// </summary>
    public partial class BuildingManager : MonoBehaviour, IBuildingManager, IManager
    {
        // 服务引用
        private IBuildingQueryService _queryService;
        private IBuildingConstructionService _constructionService;
        private IResearchService _researchService;
        
        // 配置系统
        private BuildingConfigSO _config;
        
        // 数据容器（由服务共享）
        private readonly Dictionary<int, BuildingData> _buildings = new Dictionary<int, BuildingData>();
        private readonly Dictionary<int, GameObject> _buildingGameObjects = new Dictionary<int, GameObject>();
        private readonly Dictionary<BuildingType, string> _buildingPrefabPaths = new Dictionary<BuildingType, string>();
        private readonly Dictionary<BuildingType, BuildingTemplate> _buildingTemplates = new Dictionary<BuildingType, BuildingTemplate>();
        private readonly Dictionary<string, ResearchTemplate> _researchTemplates = new Dictionary<string, ResearchTemplate>();
        private readonly Dictionary<int, HashSet<string>> _playerResearch = new Dictionary<int, HashSet<string>>();
        private readonly Queue<int> _buildingUpdateQueue = new Queue<int>();
        
        // 配置参数
        private float _buildingUpdateTimer = 0f;
        private float _buildingUpdateInterval = 0.1f;
        private int _maxBuildingUpdatesPerFrame = 10;
        private float _buildingPlacementGridSize = 1f;
        private string _managerName = "BuildingManager";
        private bool _isPaused = false;
        private int _nextBuildingId = 1;

        // 事件
        public System.Action<BuildingData> OnBuildingPlaced;

        /// <summary>
        /// 初始化配置系统
        /// </summary>
        private void InitializeConfig()
        {
            _config = ConfigManager.GetConfig<BuildingConfigSO>("BuildingConfig");
            
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
        /// 创建建筑
        /// </summary>
        public int CreateBuilding(BuildingData buildingData)
        {
            if (buildingData == null)
            {
                Debug.LogError("BuildingData 不能为空");
                return -1;
            }

            // 生成唯一ID
            buildingData.Id = GenerateUniqueId();
            
            // 添加到建筑字典
            _buildings[buildingData.Id] = buildingData;
            
            // 触发建筑放置事件
            OnBuildingPlaced?.Invoke(buildingData);
            
            Debug.Log($"建筑创建成功: {buildingData.BuildingType} at {buildingData.Position}");
            
            return buildingData.Id;
        }

        /// <summary>
        /// 生成唯一建筑ID
        /// </summary>
        private int GenerateUniqueId()
        {
            return _nextBuildingId++;
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
                        ConstructionTime = templateConfig.constructionTime,
                        Size = templateConfig.size,
                        MaxLevel = templateConfig.maxLevel,
                        BioEnergyConsumption = templateConfig.bioEnergyConsumption,
                        BioEnergyGeneration = templateConfig.bioEnergyGeneration
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
        /// 获取建筑模板
        /// </summary>
        public BuildingTemplate GetBuildingTemplate(BuildingType buildingType)
        {
            _buildingTemplates.TryGetValue(buildingType, out var template);
            return template;
        }

        /// <summary>
        /// 获取研究模板
        /// </summary>
        public ResearchTemplate GetResearchTemplate(ResearchType researchType)
        {
            _researchTemplates.TryGetValue(researchType, out var template);
            return template;
        }


        /// <summary>
        /// 获取所有建筑
        /// </summary>
        public List<Building> GetAllBuildings()
        {
            return new List<Building>(_buildings.Values);
        }

        /// <summary>
        /// 获取指定类型的建筑
        /// </summary>
        public List<Building> GetBuildingsOfType(BuildingType buildingType)
        {
            return _buildings.Values.Where(b => b.BuildingType == buildingType).ToList();
        }

        /// <summary>
        /// 获取范围内的建筑
        /// </summary>
        public List<Building> GetBuildingsInRange(Vector3 center, float radius)
        {
            float radiusSquared = radius * radius;
            return _buildings.Values.Where(b => 
                (b.Position - center).sqrMagnitude <= radiusSquared).ToList();
        }

        /// <summary>
        /// 检查位置是否可以放置建筑
        /// </summary>
        public bool CanPlaceBuilding(BuildingType buildingType, Vector3 position)
        {
            var template = GetBuildingTemplate(buildingType);
            if (template == null) return false;

            // 检查是否有足够空间
            float checkRadius = template.Size * 0.5f + 1f; // 添加一些缓冲
            var nearbyBuildings = GetBuildingsInRange(position, checkRadius);
            
            return nearbyBuildings.Count == 0;
        }

        /// <summary>
        /// 获取建筑统计信息
        /// </summary>
        public BuildingStats GetBuildingStats()
        {
            var stats = new BuildingStats();
            foreach (var building in _buildings.Values)
            {
                stats.TotalBuildings++;
                if (building.IsConstructed)
                    stats.ConstructedBuildings++;
                else
                    stats.UnderConstructionBuildings++;
            }
            return stats;
        }

        // IBuildingManager 接口实现

        public void UpdateBuilding(BuildingData buildingData)
        {
            if (_buildings.ContainsKey(buildingData.Id))
            {
                // 更新建筑数据
                var building = _buildings[buildingData.Id];
                building.SetConstructionProgress(buildingData.ConstructionProgress);
                building.SetHealth(buildingData.Health);
            }
        }

        public void RemoveBuilding(int buildingId)
        {
            _buildings.Remove(buildingId);
        }

        public bool IsValidPlacement(Vector3 position, Vector2Int size, bool checkResources = true)
        {
            // 简单的放置验证逻辑
            return true; // 实际实现需要检查地形、资源等
        }

        public void StartConstruction(int buildingId)
        {
            if (_buildings.ContainsKey(buildingId))
            {
                // 开始建造逻辑
                Debug.Log($"开始建造建筑 {buildingId}");
            }
        }

        public void StartUpgrade(int buildingId, string upgradeType)
        {
            if (_buildings.ContainsKey(buildingId))
            {
                // 开始升级逻辑
                Debug.Log($"开始升级建筑 {buildingId} 类型: {upgradeType}");
            }
        }

        public void AddProductionQueueItem(int buildingId, ProductionQueueItem item)
        {
            // 添加生产队列项目
            Debug.Log($"为建筑 {buildingId} 添加生产项目: {item}");
        }

        public void CancelProductionQueueItem(int buildingId, int itemIndex)
        {
            // 取消生产队列项目
            Debug.Log($"取消建筑 {buildingId} 的生产项目 {itemIndex}");
        }

        public void StartResearch(int buildingId, string researchType)
        {
            // 开始研究
            Debug.Log($"建筑 {buildingId} 开始研究: {researchType}");
        }

        public void CancelResearch(int buildingId)
        {
            // 取消研究
            Debug.Log($"取消建筑 {buildingId} 的研究");
        }

        public float GetCreepExpansionRadius(int buildingId)
        {
            // 获取菌毯扩张半径
            return 10f; // 默认值
        }

        // IManager 接口实现
        public void FixedUpdate(float fixedDeltaTime)
        {
            // 固定更新逻辑
        }

        public void LateUpdate(float deltaTime)
        {
            // 延迟更新逻辑
        }

        public void Pause()
        {
            _isPaused = true;
        }

        public void Resume()
        {
            _isPaused = false;
        }

        public string GetManagerName()
        {
            return "BuildingManager";
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
        /// 初始化服务和配置
        /// </summary>
        public void Initialize()
        {
            // 1. 首先初始化配置系统
            InitializeConfig();
            
            // 2. 从配置初始化各种模板和路径
            InitializeBuildingTemplatesFromConfig();
            InitializeBuildingPrefabPathsFromConfig();
            InitializeResearchTemplatesFromConfig();
            
            // 3. 初始化服务
            InitializeServices();
            
            Debug.Log($"[{_managerName}] 服务化初始化完成");
        }

        /// <summary>
        /// 初始化服务实例
        /// </summary>
        private void InitializeServices()
        {
            // 创建查询服务
            _queryService = new BuildingQueryService(
                _buildings,
                _buildingGameObjects,
                _buildingTemplates
            );

            // 创建建造服务
            _constructionService = new BuildingConstructionService(
                _buildings,
                _buildingGameObjects,
                _buildingTemplates,
                _buildingPrefabPaths
            );

            // 创建研究服务
            _researchService = new ResearchService();

            Debug.Log($"[{_managerName}] 服务初始化完成");
        }

        /// <summary>
        /// 清理资源和服务
        /// </summary>
        public void Cleanup()
        {
            // 清理数据
            _buildings.Clear();
            _buildingGameObjects.Clear();
            _buildingTemplates.Clear();
            _researchTemplates.Clear();
            _playerResearch.Clear();
            _buildingUpdateQueue.Clear();
            
            // 清理服务引用
            _queryService = null;
            _constructionService = null;
            _researchService = null;
            
            Debug.Log($"[{_managerName}] 服务化清理完成");
        }

        /// <summary>
        /// 获取服务实例
        /// </summary>
        /// <typeparam name="T">服务接口类型</typeparam>
        /// <returns>服务实例，如果不存在则返回null</returns>
        public T GetService<T>() where T : class
        {
            if (typeof(T) == typeof(IBuildingQueryService))
                return _queryService as T;
            if (typeof(T) == typeof(IBuildingConstructionService))
                return _constructionService as T;
            if (typeof(T) == typeof(IResearchService))
                return _researchService as T;
            
            return null;
        }

        /// <summary>
        /// 更新管理器 - 委托给服务处理
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            // 更新建造服务
            if (_constructionService is BuildingConstructionService constructionService)
            {
                constructionService.Update(deltaTime);
            }
            
            // 更新研究服务
            if (_researchService is ResearchService researchService)
            {
                researchService.UpdateResearchProgress(deltaTime);
            }
            
            // 处理建筑更新队列
            _buildingUpdateTimer += deltaTime;
            if (_buildingUpdateTimer >= _buildingUpdateInterval)
            {
                ProcessBuildingUpdates();
                _buildingUpdateTimer = 0f;
            }
        }

        /// <summary>
        /// 处理建筑更新队列
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

        // ===== 公共API适配器方法 =====
        // 这些方法保持向后兼容，内部委托给服务处理

        /// <summary>
        /// 获取建筑数据（委托给查询服务）
        /// </summary>
        public BuildingData? GetBuildingData(int buildingId)
        {
            return _queryService?.GetBuildingData(buildingId);
        }

        /// <summary>
        /// 检查建筑是否存在（委托给查询服务）
        /// </summary>
        public bool BuildingExists(int buildingId)
        {
            return _queryService?.BuildingExists(buildingId) ?? false;
        }

        /// <summary>
        /// 获取指定范围内的建筑（委托给查询服务）
        /// </summary>
        public List<BuildingData> GetBuildingsInRange(Vector3 center, float radius, int playerId = -1)
        {
            return _queryService?.GetBuildingsInRange(center, radius, playerId) ?? new List<BuildingData>();
        }

        /// <summary>
        /// 开始建造建筑（委托给建造服务）
        /// </summary>
        public int StartConstruction(BuildingType buildingType, Vector3 position, int playerId, Quaternion? rotation = null)
        {
            return _constructionService?.StartConstruction(buildingType, position, playerId, rotation) ?? -1;
        }

        /// <summary>
        /// 升级建筑（委托给建造服务）
        /// </summary>
        public bool UpgradeBuilding(int buildingId)
        {
            return _constructionService?.UpgradeBuilding(buildingId) ?? false;
        }

        /// <summary>
        /// 销毁建筑（委托给建造服务）
        /// </summary>
        public void DestroyBuilding(int buildingId)
        {
            // 委托给建造服务处理
            _constructionService?.DestroyBuilding(buildingId);
            
            // 移除建筑数据
            _buildings.Remove(buildingId);

            Debug.Log($"[{_managerName}] 销毁建筑: ID={buildingId}");
        }
    }
}
