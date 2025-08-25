using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.Buildings.Managers;
using DeepAbyssHive.Creep.Config;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Creep.Enums;
using ISpatialIndex = DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 核心模块
    /// 负责菌毯系统的初始化、清理和基础数据管理
    /// </summary>
    public partial class CreepManager : MonoBehaviour, IManager
    {
        #region IManager 实现
        
        public string ManagerName => _managerName;
        public bool IsInitialized { get; private set; }
        public bool IsPaused { get; private set; }

        public void Initialize()
        {
            if (IsInitialized) return;

            Debug.Log("[CreepManager] 初始化菌毯管理器");
            
            // 加载配置
            LoadConfiguration();
            
            // 初始化数据结构
            _creepGrid.Clear();
            _creepTiles.Clear();
            _activeCreepCells.Clear();
            
            // 获取其他管理器引用
            // TODO: 需要通过依赖注入或其他方式获取BuildingManager引用
            // _buildingManager = GameManager.Instance.GetManager<BuildingManager>();
            Debug.Log("[CreepManager] BuildingManager引用暂时禁用，等待依赖注入实现");
            
            IsInitialized = true;
            Debug.Log("[CreepManager] 菌毯管理器初始化完成");
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            Debug.Log("[CreepManager] 清理菌毯管理器");
            
            _creepGrid.Clear();
            _creepTiles.Clear();
            _activeCreepCells.Clear();
            
            IsInitialized = false;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        public void Update(float deltaTime)
        {
            if (!IsInitialized || IsPaused) return;
            
            // 更新菌毯逻辑
            UpdateCreepTiles(deltaTime);
        }

        public void FixedUpdate(float fixedDeltaTime)
        {
            // 固定更新逻辑
        }

        public void LateUpdate(float deltaTime)
        {
            // 延迟更新逻辑
        }

        public string GetManagerName()
        {
            return ManagerName;
        }

        #endregion

        #region Unity 生命周期

        private void Awake()
        {
            // Unity Awake
        }

        private void Start()
        {
            // Unity Start
        }

        private void Update()
        {
            // Unity Update - 直接执行更新逻辑
            if (IsInitialized && !IsPaused)
            {
                UpdateCreepTiles(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            // Unity FixedUpdate - 直接执行固定更新逻辑
            if (IsInitialized && !IsPaused)
            {
                // 固定更新逻辑在这里执行
            }
        }

        private void LateUpdate()
        {
            // Unity LateUpdate - 直接执行延迟更新逻辑
            if (IsInitialized && !IsPaused)
            {
                // 延迟更新逻辑在这里执行
            }
        }

        #endregion

        #region 私有字段定义
        
        // 私有字段定义
        private readonly Dictionary<Vector2Int, CreepData> _creepGrid = new Dictionary<Vector2Int, CreepData>();
        private readonly Dictionary<Vector2Int, CreepTile> _creepTiles = new Dictionary<Vector2Int, CreepTile>();
        private readonly HashSet<Vector2Int> _activeCreepCells = new HashSet<Vector2Int>();
        private DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex _spatialIndex;
        private BuildingManager _buildingManager;
        private CreepConfigSO _config;
        
        // 配置参数（从配置加载或使用默认值）
        private float _gridCellSize = 1f;
        private int _gridWidth = 100;
        private int _gridHeight = 100;
        private float _expansionRate = 1f;
        private float _decayRate = 0.05f;
        private float _expansionThreshold = 0.8f;
        private float _minDecayDensity = 0.1f;
        private int _batchSize = 100;
        private float _updateInterval = 0.1f;
        private float _networkCheckInterval = 2f;
        
        private string _managerName = "CreepManager";
        private float _lastUpdateTime = 0f;
        private float _lastNetworkCheckTime = 0f;
        
        // 事件定义
        public System.Action<CreepStatistics> OnStatisticsUpdated;
        // OnCreepExpanded 已移至 CreepManager.cs 中定义

        #endregion

        #region 配置加载

        /// <summary>
        /// 加载配置
        /// </summary>
        private void LoadConfiguration()
        {
            _config = ConfigManager.Instance.GetConfig<CreepConfigSO>();
            
            if (_config != null)
            {
                // 从配置加载参数
                _gridCellSize = _config.gridCellSize;
                _gridWidth = _config.gridWidth;
                _gridHeight = _config.gridHeight;
                _expansionRate = _config.expansionRate;
                _decayRate = _config.decayRate;
                _expansionThreshold = _config.expansionThreshold;
                _minDecayDensity = _config.minDecayDensity;
                _batchSize = _config.batchSize;
                _updateInterval = _config.updateInterval;
                _networkCheckInterval = _config.networkCheckInterval;
                
                Debug.Log($"[{_managerName}] 从配置加载菌毯参数：网格({_gridWidth}x{_gridHeight})，单元格大小({_gridCellSize})");
            }
            else
            {
                // 使用默认值
                _gridCellSize = 1f;
                _gridWidth = 100;
                _gridHeight = 100;
                _expansionRate = 1f;
                _decayRate = 0.05f;
                _expansionThreshold = 0.8f;
                _minDecayDensity = 0.1f;
                _batchSize = 100;
                _updateInterval = 0.1f;
                _networkCheckInterval = 2f;
                
                Debug.LogWarning($"[{_managerName}] 未找到CreepConfig配置，使用默认参数");
            }
        }

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新菌毯瓦片
        /// </summary>
        private void UpdateCreepTiles(float deltaTime)
        {
            // 检查更新间隔
            if (_updateInterval > 0f && Time.time - _lastUpdateTime < _updateInterval)
                return;
                
            _lastUpdateTime = Time.time;
            
            // 批量处理瓦片更新
            int processedCount = 0;
            foreach (var tile in _creepTiles.Values)
            {
                if (tile.IsActive)
                {
                    UpdateTileStatus(tile, deltaTime);
                    processedCount++;
                    
                    // 批量处理限制
                    if (processedCount >= _batchSize)
                        break;
                }
            }
            
            // 定期检查网络连接
            if (Time.time - _lastNetworkCheckTime >= _networkCheckInterval)
            {
                CheckNetworkConnections();
                _lastNetworkCheckTime = Time.time;
            }
        }

        /// <summary>
        /// 更新瓦片状态
        /// </summary>
        private void UpdateTileStatus(CreepTile tile, float deltaTime)
        {
            tile.LastUpdateTime = Time.time;
            
            // 根据状态更新瓦片
            switch (tile.Status)
            {
                case DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Healthy:
                    // 健康状态下不需要特殊处理
                    break;
                case DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Weakened:
                    UpdateWeakenedTile(tile, deltaTime);
                    break;
                case DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Collapsing:
                    UpdateCollapsingTile(tile, deltaTime);
                    break;
            }
        }

        /// <summary>
        /// 检查网络连接
        /// </summary>
        private void CheckNetworkConnections()
        {
            // 网络连接检查逻辑（使用配置参数）
            // 这里可以实现网络修复、分割检测等功能
        }

        /// <summary>
        /// 更新弱化的瓦片
        /// </summary>
        private void UpdateWeakenedTile(CreepTile tile, float deltaTime)
        {
            float starvationRate = _decayRate * 100f; // 使用配置的衰减速度
            tile.Health = Mathf.Max(0f, tile.Health - starvationRate * deltaTime);
            if (tile.Health <= _minDecayDensity * tile.MaxHealth)
            {
                tile.Status = DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Collapsing;
            }
        }

        /// <summary>
        /// 更新崩溃中的瓦片
        /// </summary>
        private void UpdateCollapsingTile(CreepTile tile, float deltaTime)
        {
            float deathRate = _decayRate * 400f; // 使用配置的衰减速度（加速死亡）
            tile.Health = Mathf.Max(0f, tile.Health - deathRate * deltaTime);
            if (tile.Health <= 0f)
            {
                tile.IsActive = false;
                _activeCreepCells.Remove(tile.Position);
            }
        }

        /// <summary>
        /// 网格位置转世界位置
        /// </summary>
        private Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * _gridCellSize, 0, gridPosition.y * _gridCellSize);
        }

        /// <summary>
        /// 世界位置转网格位置
        /// </summary>
        private Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / _gridCellSize),
                Mathf.RoundToInt(worldPosition.z / _gridCellSize)
            );
        }

        /// <summary>
        /// 获取相邻位置
        /// </summary>
        private Vector2Int[] GetNeighborPositions(Vector2Int position)
        {
            return new Vector2Int[]
            {
                position + Vector2Int.up,
                position + Vector2Int.down,
                position + Vector2Int.left,
                position + Vector2Int.right,
                position + new Vector2Int(1, 1),
                position + new Vector2Int(-1, 1),
                position + new Vector2Int(1, -1),
                position + new Vector2Int(-1, -1)
            };
        }

        /// <summary>
        /// 检查地形是否有效
        /// </summary>
        private bool IsValidTerrain(Vector2Int position)
        {
            // 简化实现，实际应该检查地形管理器
            return true;
        }

        /// <summary>
        /// 检查是否有障碍物
        /// </summary>
        private bool HasObstacle(Vector2Int position)
        {
            // 简化实现，实际应该检查建筑和单位
            return false;
        }

        /// <summary>
        /// 创建菌毯瓦片
        /// </summary>
        private CreepTile CreateCreepTile(Vector2Int position)
        {
            Vector3 worldPos = GridToWorldPosition(position);
            var tile = new CreepTile
            {
                Position = position,
                WorldPosition = worldPos,
                IsNutritionSource = false,
                TileType = CreepTileType.Creep,
                Status = DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Healthy,
                Health = 50f,
                MaxHealth = 100f,
                GrowthLevel = 0f,
                MaxGrowthLevel = 100f,
                GrowthRate = 1f,
                NeedsUpdate = true,
                IsActive = true,
                CreationTime = Time.time,
                LastUpdateTime = Time.time
            };
            return tile;
        }

        /// <summary>
        /// 移除菌毯瓦片
        /// </summary>
        private void RemoveCreepTile(Vector2Int position)
        {
            if (_creepTiles.TryGetValue(position, out CreepTile tile))
            {
                _creepTiles.Remove(position);
                _activeCreepCells.Remove(position);
                
                if (_spatialIndex != null && _creepGrid.TryGetValue(position, out CreepData creepData))
                {
                    _spatialIndex.Remove(creepData, creepData.Position, new Vector3(_gridCellSize, _gridCellSize, _gridCellSize));
                }
            }
        }

        // GetCreepStatistics() 方法已移至 CreepManager.Query.cs 中实现

        #endregion
    }

    #region 数据结构定义

    /// <summary>
    /// 菌毯统计信息
    /// </summary>
    [System.Serializable]
    public class CreepStatistics
    {
        public int TotalTiles;
        public int ActiveTiles;
        public float TotalCoverage;
        public float TotalArea;
        public float TotalHealth;
        public float AverageHealth;
        public float TotalResourcesGenerated;
        public int ConnectedRegions;
        
        // 按状态分类
        public int HealthyTiles;
        public int GrowingTiles;
        public int StarvingTiles;
        public int DyingTiles;
        
        // 按类型分类
        public int BasicTiles;
        public int EnhancedTiles;
        public int SpecializedTiles;
    }

    #endregion
}