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
    public class BuildingManager : IBuildingManager, IManager
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
                PowerConsumption = template.PowerConsumption,
                PowerGeneration = template.PowerGeneration,
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

        /// <summary>
        /// 升级建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否成功</returns>
        public bool UpgradeBuilding(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试升级不存在的建筑: {buildingId}");
                return false;
            }
            
            if (!_buildingTemplates.TryGetValue(buildingData.Type, out BuildingTemplate template))
            {
                Debug.LogError($"[{_managerName}] 建筑模板不存在: {buildingData.Type}");
                return false;
            }
            
            // 检查是否可以升级
            if (buildingData.Level >= template.MaxLevel)
            {
                Debug.LogWarning($"[{_managerName}] 建筑已达到最大等级: {buildingId}, 等级={buildingData.Level}");
                return false;
            }
            
            if (buildingData.State != BuildingState.Operational)
            {
                Debug.LogWarning($"[{_managerName}] 建筑状态不允许升级: {buildingId}, 状态={buildingData.State}");
                return false;
            }
            
            // 检查升级路径
            if (template.UpgradePaths != null && template.UpgradePaths.Length > 0)
            {
                UpgradePath upgradePath = template.UpgradePaths[0]; // 简化处理，使用第一个升级路径
                
                // 检查升级条件
                if (!CheckUpgradeRequirements(buildingData.OwnerId, upgradePath))
                {
                    Debug.LogWarning($"[{_managerName}] 升级条件不满足: {buildingId}");
                    return false;
                }
                
                // 开始升级
                buildingData.State = BuildingState.Upgrading;
                buildingData.ConstructionProgress = 0f;
                buildingData.ConstructionTime = upgradePath.UpgradeTime;
                buildingData.LastUpdateTime = Time.time;
                
                // 更新建筑数据
                _buildings[buildingId] = buildingData;
                
                // 更新游戏对象
                UpdateBuildingGameObject(buildingId, buildingData);
                
                Debug.Log($"[{_managerName}] 开始升级建筑: ID={buildingId}, 等级={buildingData.Level} -> {buildingData.Level + 1}");
                
                return true;
            }
            
            Debug.LogWarning($"[{_managerName}] 建筑没有升级路径: {buildingData.Type}");
            return false;
        }

        /// <summary>
        /// 修理建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        public void RepairBuilding(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试修理不存在的建筑: {buildingId}");
                return;
            }
            
            if (buildingData.Health >= buildingData.MaxHealth)
            {
                Debug.LogWarning($"[{_managerName}] 建筑不需要修理: {buildingId}");
                return;
            }
            
            // 设置建筑状态为修理中
            buildingData.State = BuildingState.Repairing;
            buildingData.LastUpdateTime = Time.time;
            
            // 更新建筑数据
            _buildings[buildingId] = buildingData;
            
            // 更新游戏对象
            UpdateBuildingGameObject(buildingId, buildingData);
            
            Debug.Log($"[{_managerName}] 开始修理建筑: ID={buildingId}, 生命值={buildingData.Health}/{buildingData.MaxHealth}");
        }

        /// <summary>
        /// 获取建筑数据
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>建筑数据</returns>
        public BuildingData GetBuildingData(int buildingId)
        {
            if (_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                return buildingData;
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的建筑数据: {buildingId}");
            return null;
        }

        /// <summary>
        /// 获取范围内的建筑
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>建筑ID数组</returns>
        public int[] GetBuildingsInRange(Vector3 position, float radius)
        {
            List<int> buildingsInRange = new List<int>();
            
            foreach (var pair in _buildings)
            {
                int buildingId = pair.Key;
                BuildingData buildingData = pair.Value;
                
                if (Vector3.Distance(buildingData.Position, position) <= radius)
                {
                    buildingsInRange.Add(buildingId);
                }
            }
            
            return buildingsInRange.ToArray();
        }

        /// <summary>
        /// 获取指定类型和所有者的建筑
        /// </summary>
        /// <param name="type">建筑类型</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>建筑ID数组</returns>
        public int[] GetBuildingsOfType(BuildingType type, int ownerId)
        {
            List<int> buildings = new List<int>();
            
            foreach (var pair in _buildings)
            {
                int buildingId = pair.Key;
                BuildingData buildingData = pair.Value;
                
                if (buildingData.Type == type && buildingData.OwnerId == ownerId)
                {
                    buildings.Add(buildingId);
                }
            }
            
            return buildings.ToArray();
        }

        /// <summary>
        /// 检查位置是否可以建造
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">建筑大小</param>
        /// <returns>是否可以建造</returns>
        public bool CanPlaceBuildingAt(Vector3 position, Vector2Int size)
        {
            // 检查位置是否在网格上
            if (!IsPositionOnGrid(position))
            {
                return false;
            }
            
            // 检查是否与其他建筑重叠
            foreach (var pair in _buildings)
            {
                BuildingData existingBuilding = pair.Value;
                Vector3 buildingSize = new Vector3(size.x, size.y, size.x);
                Vector3 existingSize = new Vector3(existingBuilding.Size.x, existingBuilding.Size.y, existingBuilding.Size.x);
                
                if (IsBuildingOverlapping(position, buildingSize, existingBuilding.Position, existingSize))
                {
                    return false;
                }
            }
            
            return true;
        }

        /// <summary>
        /// 开始研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功</returns>
        public bool StartResearch(string researchId, int playerId)
        {
            if (!_researchTemplates.TryGetValue(researchId, out ResearchTemplate template))
            {
                Debug.LogError($"[{_managerName}] 研究模板不存在: {researchId}");
                return false;
            }
            
            // 检查是否已经研究过
            if (IsResearchCompleted(researchId, playerId))
            {
                Debug.LogWarning($"[{_managerName}] 研究已完成: {researchId}");
                return false;
            }
            
            // 检查前置研究
            if (template.Prerequisites != null && template.Prerequisites.Length > 0)
            {
                foreach (string prerequisite in template.Prerequisites)
                {
                    if (!IsResearchCompleted(prerequisite, playerId))
                    {
                        Debug.LogWarning($"[{_managerName}] 前置研究未完成: {prerequisite}");
                        return false;
                    }
                }
            }
            
            // 开始研究
            CompleteResearch(researchId, playerId);
            
            Debug.Log($"[{_managerName}] 开始研究: {researchId}, 玩家={playerId}");
            
            return true;
        }

        /// <summary>
        /// 检查研究是否完成
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否完成</returns>
        public bool IsResearchCompleted(string researchId, int playerId)
        {
            if (!_playerResearch.TryGetValue(playerId, out List<string> completedResearch))
            {
                return false;
            }
            
            return completedResearch.Contains(researchId);
        }

        /// <summary>
        /// 获取玩家已完成的研究
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>已完成的研究ID数组</returns>
        public string[] GetCompletedResearch(int playerId)
        {
            if (!_playerResearch.TryGetValue(playerId, out List<string> completedResearch))
            {
                return new string[0];
            }
            
            return completedResearch.ToArray();
        }

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
        /// 检查建筑放置是否有效
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        /// <param name="requiresCreep">是否需要菌毯</param>
        /// <returns>是否可以放置</returns>
        public bool IsValidPlacement(Vector3 position, Vector2Int size, bool requiresCreep)
        {
            return CanPlaceBuildingAt(position, size);
        }

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

        /// <summary>
        /// 开始研究
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="researchId">研究ID</param>
        public void StartResearch(int buildingId, string researchId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试在不存在的建筑中开始研究: {buildingId}");
                return;
            }
            
            StartResearch(researchId, buildingData.OwnerId);
        }

        /// <summary>
        /// 取消研究
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        public void CancelResearch(int buildingId)
        {
            // 简化实现，实际项目中需要完整的研究系统
            Debug.Log($"[{_managerName}] 取消研究: 建筑={buildingId}");
        }

        /// <summary>
        /// 获取建筑周围的菌毯扩张范围
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>菌毯扩张范围</returns>
        public float GetCreepExpansionRadius(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                return 0f;
            }
            
            // 根据建筑类型和等级返回菌毯扩张范围
            return buildingData.Level * 5.0f; // 简化计算
        }
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
        /// 更新建筑状态
        /// </summary>
        private void UpdateBuildings()
        {
            _buildingUpdateTimer += Time.deltaTime;
            
            if (_buildingUpdateTimer < _buildingUpdateInterval)
                return;
                
            _buildingUpdateTimer = 0f;
            
            int updatedCount = 0;
            while (_buildingUpdateQueue.Count > 0 && updatedCount < _maxBuildingUpdatesPerFrame)
            {
                BuildingData buildingData = _buildingUpdateQueue.Dequeue();
                
                if (_buildings.ContainsKey(buildingData.BuildingId))
                {
                    UpdateBuilding(buildingData, _buildingUpdateInterval);
                    
                    // 如果建筑仍需要更新，重新加入队列
                    if (NeedsContinuousUpdate(buildingData))
                    {
                        _buildingUpdateQueue.Enqueue(buildingData);
                    }
                }
                
                updatedCount++;
            }
        }

        /// <summary>
        /// 更新单个建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateBuilding(int buildingId, float deltaTime)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
                return;
                
            UpdateBuilding(buildingData, deltaTime);
        }

        /// <summary>
        /// 更新单个建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateBuilding(BuildingData buildingData, float deltaTime)
        {
            switch (buildingData.State)
            {
                case BuildingState.UnderConstruction:
                    UpdateConstructionProgress(buildingData, deltaTime);
                    break;
                    
                case BuildingState.Upgrading:
                    UpdateUpgradeProgress(buildingData, deltaTime);
                    break;
                    
                case BuildingState.Repairing:
                    UpdateRepairProgress(buildingData, deltaTime);
                    break;
                    
                case BuildingState.Operational:
                    UpdateOperationalBuilding(buildingData, deltaTime);
                    break;
            }
            
            // 更新建筑数据
            _buildings[buildingData.BuildingId] = buildingData;
            
            // 更新游戏对象
            UpdateBuildingGameObject(buildingData.BuildingId, buildingData);
        }

        /// <summary>
        /// 更新建筑建造进度
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateConstructionProgress(BuildingData buildingData, float deltaTime)
        {
            buildingData.ConstructionProgress += deltaTime / buildingData.ConstructionTime;
            
            if (buildingData.ConstructionProgress >= 1.0f)
            {
                // 建造完成
                buildingData.ConstructionProgress = 1.0f;
                buildingData.State = BuildingState.Operational;
                buildingData.Health = buildingData.MaxHealth;
                
                Debug.Log($"[{_managerName}] 建筑建造完成: ID={buildingData.BuildingId}");
            }
        }

        /// <summary>
        /// 更新建筑升级进度
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateUpgradeProgress(BuildingData buildingData, float deltaTime)
        {
            buildingData.ConstructionProgress += deltaTime / buildingData.ConstructionTime;
            
            if (buildingData.ConstructionProgress >= 1.0f)
            {
                // 升级完成
                buildingData.ConstructionProgress = 1.0f;
                buildingData.State = BuildingState.Operational;
                buildingData.Level++;
                
                // 应用升级效果
                ApplyUpgradeEffects(buildingData);
                
                Debug.Log($"[{_managerName}] 建筑升级完成: ID={buildingData.BuildingId}, 等级={buildingData.Level}");
            }
        }

        /// <summary>
        /// 更新建筑修理进度
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateRepairProgress(BuildingData buildingData, float deltaTime)
        {
            // 简化修理逻辑，每秒恢复10%最大生命值
            float repairRate = buildingData.MaxHealth * 0.1f;
            buildingData.Health += repairRate * deltaTime;
            
            if (buildingData.Health >= buildingData.MaxHealth)
            {
                // 修理完成
                buildingData.Health = buildingData.MaxHealth;
                buildingData.State = BuildingState.Operational;
                
                Debug.Log($"[{_managerName}] 建筑修理完成: ID={buildingData.BuildingId}");
            }
        }

        /// <summary>
        /// 更新运行中的建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateOperationalBuilding(BuildingData buildingData, float deltaTime)
        {
            // 更新建筑经验
            buildingData.Experience += deltaTime;
            
            // 检查建筑是否受损
            if (buildingData.Health < buildingData.MaxHealth * 0.5f)
            {
                buildingData.State = BuildingState.Damaged;
            }
        }

        /// <summary>
        /// 更新生产队列
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateProductionQueues(float deltaTime)
        {
            // 简化实现，实际项目中需要完整的生产队列系统
        }

        /// <summary>
        /// 更新研究
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateResearch(float deltaTime)
        {
            // 简化实现，实际项目中需要完整的研究系统
        }

        /// <summary>
        /// 应用升级效果
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        private void ApplyUpgradeEffects(BuildingData buildingData)
        {
            if (!_buildingTemplates.TryGetValue(buildingData.Type, out BuildingTemplate template))
                return;
                
            // 根据等级应用属性加成
            float levelMultiplier = 1.0f + (buildingData.Level - 1) * 0.2f; // 每级增加20%
            
            buildingData.MaxHealth = template.MaxHealth * levelMultiplier;
            buildingData.Health = buildingData.MaxHealth; // 升级后恢复满血
            buildingData.PowerGeneration = template.PowerGeneration * levelMultiplier;
        }

        /// <summary>
        /// 检查建筑是否需要持续更新
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <returns>是否需要持续更新</returns>
        private bool NeedsContinuousUpdate(BuildingData buildingData)
        {
            switch (buildingData.State)
            {
                case BuildingState.UnderConstruction:
                case BuildingState.Upgrading:
                case BuildingState.Repairing:
                case BuildingState.Operational:
                    return true;
                default:
                    return false;
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
        /// 初始化建筑模板
        /// </summary>
        private void InitializeBuildingTemplates()
        {
            // 从配置文件或资源中加载建筑模板
            // 这里使用简化的硬编码实现
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
                    PowerConsumption = 10f,
                    PowerGeneration = type == BuildingType.PowerPlant ? 50f : 0f
                };
                _buildingTemplates[type] = template;
            }
        }

        /// <summary>
        /// 初始化研究模板
        /// </summary>
        private void InitializeResearchTemplates()
        {
            // 从配置文件或资源中加载研究模板
            // 这里使用简化的硬编码实现
        }

        /// <summary>
        /// 初始化建筑预制体路径
        /// </summary>
        private void InitializeBuildingPrefabPaths()
        {
            // 初始化建筑预制体路径映射
            foreach (BuildingType type in System.Enum.GetValues(typeof(BuildingType)))
            {
                _buildingPrefabPaths[type] = $"Prefabs/Buildings/{type}";
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
        /// 完成研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        private void CompleteResearch(string researchId, int playerId)
        {
            if (!_playerResearch.ContainsKey(playerId))
            {
                _playerResearch[playerId] = new List<string>();
            }
            
            if (!_playerResearch[playerId].Contains(researchId))
            {
                _playerResearch[playerId].Add(researchId);
            }
        }

        /// <summary>
        /// 检查位置是否在网格上
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否在网格上</returns>
        private bool IsPositionOnGrid(Vector3 position)
        {
            float gridX = position.x / _buildingPlacementGridSize;
            float gridZ = position.z / _buildingPlacementGridSize;
            
            return Mathf.Approximately(gridX, Mathf.Round(gridX)) && 
                   Mathf.Approximately(gridZ, Mathf.Round(gridZ));
        }

        /// <summary>
        /// 检查建筑是否重叠
        /// </summary>
        /// <param name="pos1">位置1</param>
        /// <param name="size1">大小1</param>
        /// <param name="pos2">位置2</param>
        /// <param name="size2">大小2</param>
        /// <returns>是否重叠</returns>
        private bool IsBuildingOverlapping(Vector3 pos1, Vector3 size1, Vector3 pos2, Vector3 size2)
        {
            Bounds bounds1 = new Bounds(pos1, size1);
            Bounds bounds2 = new Bounds(pos2, size2);
            
            return bounds1.Intersects(bounds2);
        }
        #endregion
    }
}
