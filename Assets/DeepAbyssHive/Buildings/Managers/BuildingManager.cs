using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// 建筑管理器，负责管理所有建筑
    /// </summary>
    public partial class BuildingManager : IBuildingManager, IManager
    {
        #region 私有字段
        private Dictionary<int, BuildingData> _buildings = new Dictionary<int, BuildingData>();
        private Dictionary<int, GameObject> _buildingGameObjects = new Dictionary<int, GameObject>();
        private Dictionary<BuildingType, BuildingTemplate> _buildingTemplates = new Dictionary<BuildingType, BuildingTemplate>();
        private Dictionary<string, ResearchTemplate> _researchTemplates = new Dictionary<string, ResearchTemplate>();
        private Dictionary<int, List<string>> _playerResearch = new Dictionary<int, List<string>>();
        private ISpatialIndex<BuildingData> _spatialIndex;
        
        private int _nextBuildingId = 1;
        private bool _isInitialized = false;
        private bool _isPaused = false;
        private string _managerName = "BuildingManager";
        
        // 建筑配置
        private Dictionary<BuildingType, string> _buildingPrefabPaths = new Dictionary<BuildingType, string>();
        private float _buildingPlacementGridSize = 2.0f;
        
        // 性能优化
        private Queue<BuildingData> _buildingUpdateQueue = new Queue<BuildingData>();
        private int _maxBuildingUpdatesPerFrame = 20;
        private float _buildingUpdateTimer = 0f;
        private float _buildingUpdateInterval = 0.1f;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="spatialIndex">空间索引系统</param>
        public BuildingManager(ISpatialIndex<BuildingData> spatialIndex)
        {
            _spatialIndex = spatialIndex;
        }
        #endregion

        #region IBuildingManager接口实现
        /// <summary>
        /// 创建建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <returns>建筑ID</returns>
        public int CreateBuilding(BuildingData buildingData)
        {
            return CreateBuilding(buildingData.Type, buildingData.Position, buildingData.OwnerId);
        }

        /// <summary>
        /// 创建建筑（内部方法）
        /// </summary>
        /// <param name="type">建筑类型</param>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>建筑ID</returns>
        private int CreateBuilding(BuildingType type, Vector3 position, int ownerId)
        {
            // 检查建筑模板是否存在
            if (!_buildingTemplates.TryGetValue(type, out BuildingTemplate template))
            {
                Debug.LogError($"[{_managerName}] 建筑模板不存在: {type}");
                return -1;
            }
            
            // 检查位置是否可以建造
            if (!CanPlaceBuildingAt(position, template.Size))
            {
                Debug.LogWarning($"[{_managerName}] 位置不可建造: {position}");
                return -1;
            }
            
            int buildingId = _nextBuildingId++;
            
            // 创建建筑数据
            BuildingData buildingData = new BuildingData
            {
                BuildingId = buildingId,
                Type = type,
                Position = position,
                Rotation = Quaternion.identity,
                OwnerId = ownerId,
                State = BuildingState.UnderConstruction,
                Health = template.MaxHealth,
                MaxHealth = template.MaxHealth,
                ConstructionProgress = 0f,
                ConstructionTime = template.ConstructionTime,
                Size = template.Size,
                BioEnergyConsumption = template.BioEnergyConsumption,
                BioEnergyGeneration = template.BioEnergyGeneration,
                CreationTime = Time.time,
                LastUpdateTime = Time.time,
                Level = 1,
                Experience = 0f,
                PrefabPath = GetPrefabPathForType(type)
            };
            
            // 存储建筑数据
            _buildings[buildingId] = buildingData;
            
            // 实例化建筑游戏对象
            GameObject buildingObject = InstantiateBuildingObject(buildingData);
            if (buildingObject != null)
            {
                _buildingGameObjects[buildingId] = buildingObject;
            }
            
            // 添加到空间索引
            if (_spatialIndex != null)
            {
                Vector3 size = new Vector3(template.Size.x, template.Size.y, template.Size.x);
                _spatialIndex.Insert(buildingData, position, size);
            }
            
            // 添加到更新队列
            _buildingUpdateQueue.Enqueue(buildingData);
            
            Debug.Log($"[{_managerName}] 创建建筑: ID={buildingId}, 类型={type}, 所有者={ownerId}, 位置={position}");
            
            return buildingId;
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
            
            // 从空间索引中移除
            if (_spatialIndex != null)
            {
                Vector3 size = new Vector3(buildingData.Size.x, buildingData.Size.y, buildingData.Size.x);
                _spatialIndex.Remove(buildingData, buildingData.Position, size);
            }
            
            // 销毁游戏对象
            if (_buildingGameObjects.TryGetValue(buildingId, out GameObject buildingObject) && buildingObject != null)
            {
                GameObject.Destroy(buildingObject);
                _buildingGameObjects.Remove(buildingId);
            }
            
            // 移除建筑数据
            _buildings.Remove(buildingId);
            
            Debug.Log($"[{_managerName}] 销毁建筑: ID={buildingId}");
        }

        // 注意：研究相关方法已迁移到 BuildingManager.Research.cs
        // 包括：StartResearch, CancelResearch, IsResearchCompleted, GetCompletedResearch
        // 以及私有方法：UpdateResearch, InitializeResearchTemplates, CompleteResearch

        /// <summary>
        /// 更新建筑数据
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        public void UpdateBuilding(BuildingData buildingData)
        {
            if (!_buildings.ContainsKey(buildingData.BuildingId))
            {
                Debug.LogWarning($"[{_managerName}] 尝试更新不存在的建筑: {buildingData.BuildingId}");
                return;
            }
            
            _buildings[buildingData.BuildingId] = buildingData;
            UpdateBuildingGameObject(buildingData.BuildingId, buildingData);
        }

        /// <summary>
        /// 删除建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        public void RemoveBuilding(int buildingId)
        {
            DestroyBuilding(buildingId);
        }

        /// <summary>
        /// 获取建筑数据
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>建筑数据，如果不存在则返回null</returns>
        public BuildingData GetBuildingData(int buildingId)
        {
            if (_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                return buildingData;
            }
            
            return null;
        }

        // 注意：IsValidPlacement 和 GetCreepExpansionRadius 方法已迁移到 BuildingManager.Query.cs


        /// <summary>
        /// 开始建造建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        public void StartConstruction(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试开始建造不存在的建筑: {buildingId}");
                return;
            }
            
            buildingData.State = BuildingState.UnderConstruction;
            buildingData.ConstructionProgress = 0f;
            buildingData.LastUpdateTime = Time.time;
            _buildings[buildingId] = buildingData;
        }

        /// <summary>
        /// 开始升级建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="upgradePathId">升级路径ID</param>
        public void StartUpgrade(int buildingId, string upgradePathId)
        {
            UpgradeBuilding(buildingId);
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否成功开始升级</returns>
        public bool UpgradeBuilding(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试升级不存在的建筑: {buildingId}");
                return false;
            }
            
            if (buildingData.State != BuildingState.Operational)
            {
                Debug.LogWarning($"[{_managerName}] 建筑状态不允许升级: {buildingId}, 状态={buildingData.State}");
                return false;
            }
            
            // 开始升级
            buildingData.State = BuildingState.Upgrading;
            buildingData.ConstructionProgress = 0f;
            buildingData.LastUpdateTime = Time.time;
            
            // 更新建筑数据
            _buildings[buildingId] = buildingData;
            
            // 添加到更新队列
            _buildingUpdateQueue.Enqueue(buildingData);
            
            Debug.Log($"[{_managerName}] 开始升级建筑: ID={buildingId}, 当前等级={buildingData.Level}");
            
            return true;
        }

        /// <summary>
        /// 添加生产队列项
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="productionItem">生产队列项</param>
        public void AddProductionQueueItem(int buildingId, ProductionQueueItem productionItem)
        {
            // 简化实现，实际项目中需要完整的生产队列系统
            Debug.Log($"[{_managerName}] 添加生产队列项: 建筑={buildingId}, 项目={productionItem}");
        }

        /// <summary>
        /// 取消生产队列项
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="queueIndex">队列索引</param>
        public void CancelProductionQueueItem(int buildingId, int queueIndex)
        {
            // 简化实现，实际项目中需要完整的生产队列系统
            Debug.Log($"[{_managerName}] 取消生产队列项: 建筑={buildingId}, 索引={queueIndex}");
        }

        // 注意：StartResearch 和 CancelResearch 方法已迁移到 BuildingManager.Research.cs

        #endregion

        #region IManager接口实现
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            Debug.Log($"[{_managerName}] 初始化建筑管理器");
            
            // 初始化建筑模板
            InitializeBuildingTemplates();
            
            // 初始化研究模板
            InitializeResearchTemplates();
            
            // 初始化建筑预制体路径
            InitializeBuildingPrefabPaths();
            
            _isInitialized = true;
            Debug.Log($"[{_managerName}] 建筑管理器初始化完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        public void UpdateManager()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 更新建筑状态
            UpdateBuildings();
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[{_managerName}] 清理建筑管理器");
            
            // 销毁所有建筑
            List<int> buildingIds = new List<int>(_buildings.Keys);
            foreach (int buildingId in buildingIds)
            {
                DestroyBuilding(buildingId);
            }
            
            _buildings.Clear();
            _buildingGameObjects.Clear();
            _buildingUpdateQueue.Clear();
            _playerResearch.Clear();
            
            _isInitialized = false;
            
            Debug.Log($"[{_managerName}] 建筑管理器清理完成");
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        public string ManagerName => _managerName;

        /// <summary>
        /// 获取初始化状态
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 更新所有建筑
            List<int> buildingIds = new List<int>(_buildings.Keys);
            foreach (int buildingId in buildingIds)
            {
                UpdateBuilding(buildingId, deltaTime);
            }
            
            // 更新生产队列
            UpdateProductionQueues(deltaTime);
            
            // 更新研究
            UpdateResearch(deltaTime);
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 在这里可以添加物理相关的更新逻辑
        }

        /// <summary>
        /// 后更新管理器
        /// </summary>
        public void LateUpdate()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 在这里可以添加后更新逻辑
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            if (_isPaused)
                return;
                
            _isPaused = true;
            Debug.Log($"[{_managerName}] 建筑管理器已暂停");
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            if (!_isPaused)
                return;
                
            _isPaused = false;
            Debug.Log($"[{_managerName}] 建筑管理器已恢复");
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        public string GetManagerName()
        {
            return _managerName;
        }
        #endregion

        #region 私有方法
        /// <summary>
        // UpdateBuildings方法已迁移到BuildingManager.Updates.cs

        /// <summary>
        // 以下方法已迁移到BuildingManager.Updates.cs：
        // - UpdateBuilding(int, float)
        // - UpdateBuilding(BuildingData, float)  
        // - UpdateConstructionProgress(BuildingData, float)
        // - UpdateUpgradeProgress(BuildingData, float)
        // - UpdateRepairProgress(BuildingData, float)
        // - UpdateOperationalBuilding(BuildingData, float)
        // - UpdateProductionQueues(float)
        // - UpdateResearch(float)
        // - ApplyUpgradeEffects(BuildingData)
        // - NeedsContinuousUpdate(BuildingData)

        /// <summary>
        // 以下方法已迁移到对应的partial文件：
        // BuildingManager.Core.cs:
        // - InstantiateBuildingObject(BuildingData)
        // - UpdateBuildingGameObject(int, BuildingData)
        // - GetPrefabPathForType(BuildingType)
        // - InitializeBuildingTemplates()
        // - InitializeBuildingPrefabPaths()
        // - CheckUpgradeRequirements(int, UpgradePath)
        // 
        // BuildingManager.Research.cs:
        // - InitializeResearchTemplates()
        // - CompleteResearch(string, int)

        #endregion
    }
}
