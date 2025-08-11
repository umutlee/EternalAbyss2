using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using System.Linq;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器主类
    /// 负责统一管理菌毯系统的所有功能模块
    /// </summary>
    public partial class CreepManager : MonoBehaviour, ICreepManager
    {
        #region 字段和属性
        
        [Header("菌毯基础设置")]
        [SerializeField] private float _gridSize = 1f;
        [SerializeField] private float _tileSize = 1f;
        [SerializeField] private float _maxDensity = 100f;
        [SerializeField] private LayerMask _terrainLayer = 1;
        [SerializeField] private LayerMask _obstacleLayer = 2;
        
        [Header("性能设置")]
        [SerializeField] private int _maxProcessingPerFrame = 100;
        [SerializeField] private bool _enableSpatialOptimization = true;
        [SerializeField] private bool _enableDebugVisualization = false;
        
        // 核心数据结构
        private Dictionary<Vector2Int, CreepData> _creepGrid = new Dictionary<Vector2Int, CreepData>();
        private Dictionary<Vector2Int, CreepTile> _creepTiles = new Dictionary<Vector2Int, CreepTile>();
        private HashSet<Vector2Int> _activeCreepCells = new HashSet<Vector2Int>();
        private ISpatialIndex _spatialIndex;
        
        // 系统依赖
        private IBuildingManager _buildingManager;
        
        // 事件
        public event Action<Vector2Int, CreepTile> OnCreepExpanded;
        public event Action<Vector2Int> OnCreepRemoved;
        public event Action<CreepTile> OnCreepTileStatusChanged;
        public event Action<CreepStatistics> OnStatisticsUpdated;
        
        #endregion
        
        #region Unity生命周期
        
        private void Awake()
        {
            InitializeCreepManager();
        }
        
        private void Start()
        {
            StartCreepSystem();
        }
        
        private void Update()
        {
            UpdateCreepSystem();
            ProcessAutoExpansion();
        }
        
        private void OnDestroy()
        {
            ShutdownCreepSystem();
        }
        
        #endregion
        
        #region 初始化和关闭
        
        /// <summary>
        /// 初始化菌毯管理器
        /// </summary>
        private void InitializeCreepManager()
        {
            // 初始化空间索引
            if (_enableSpatialOptimization)
            {
                _spatialIndex = new QuadTreeSpatialIndex();
            }
            
            // 获取系统依赖
            _buildingManager = FindObjectOfType<MonoBehaviour>() as IBuildingManager;
            
            Debug.Log("[CreepManager] 菌毯管理器初始化完成");
        }
        
        /// <summary>
        /// 启动菌毯系统
        /// </summary>
        private void StartCreepSystem()
        {
            // 注册建筑系统事件
            if (_buildingManager != null)
            {
                _buildingManager.OnBuildingPlaced += OnBuildingPlaced;
                _buildingManager.OnBuildingDestroyed += OnBuildingDestroyed;
            }
            
            Debug.Log("[CreepManager] 菌毯系统启动完成");
        }
        
        /// <summary>
        /// 关闭菌毯系统
        /// </summary>
        private void ShutdownCreepSystem()
        {
            // 取消注册事件
            if (_buildingManager != null)
            {
                _buildingManager.OnBuildingPlaced -= OnBuildingPlaced;
                _buildingManager.OnBuildingDestroyed -= OnBuildingDestroyed;
            }
            
            // 清理资源
            _creepGrid.Clear();
            _creepTiles.Clear();
            _activeCreepCells.Clear();
            
            Debug.Log("[CreepManager] 菌毯系统关闭完成");
        }
        
        #endregion
        
        #region 公共接口实现
        
        /// <summary>
        /// 在指定位置创建菌毯源点
        /// </summary>
        public bool CreateCreepSource(Vector3 worldPosition, float radius, int ownerId)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            
            if (_creepGrid.ContainsKey(gridPos))
                return false;
                
            // 创建菌毯源点
            var creepSource = new CreepData
            {
                Position = worldPosition,
                Density = _maxDensity,
                OwnerId = ownerId,
                IsSource = true,
                SourceRadius = radius,
                LastUpdateTime = Time.time,
                CreationTime = Time.time
            };
            
            _creepGrid[gridPos] = creepSource;
            _activeCreepCells.Add(gridPos);
            
            // 创建对应的菌毯瓦片
            var creepTile = CreateCreepTile(gridPos);
            if (creepTile != null)
            {
                creepTile.IsNutritionSource = true;
                creepTile.TileType = CreepTileType.Enhanced;
                _creepTiles[gridPos] = creepTile;
            }
            
            // 添加到空间索引
            if (_spatialIndex != null)
            {
                _spatialIndex.Insert(creepSource, worldPosition, Vector3.one * radius);
            }
            
            Debug.Log($"[CreepManager] 在位置 {worldPosition} 创建菌毯源点");
            return true;
        }
        
        /// <summary>
        /// 移除指定位置的菌毯
        /// </summary>
        public bool RemoveCreepAt(Vector3 worldPosition)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            return RemoveCreepTile(gridPos);
        }
        
        /// <summary>
        /// 检查指定位置是否有菌毯
        /// </summary>
        public bool HasCreepAt(Vector3 worldPosition)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            return _creepTiles.ContainsKey(gridPos) && _creepTiles[gridPos].IsActive;
        }
        
        /// <summary>
        /// 检查位置是否可以放置建筑
        /// </summary>
        public bool CanPlaceBuildingAt(Vector3 worldPosition, BuildingType buildingType)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            
            // 检查是否需要菌毯支持
            if (RequiresCreepSupport(buildingType))
            {
                return HasCreepAt(gridPos);
            }
            
            return true;
        }
        
        /// <summary>
        /// 获取菌毯密度
        /// </summary>
        public float GetCreepDensityAt(Vector3 worldPosition)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            
            if (_creepGrid.TryGetValue(gridPos, out var creepData))
            {
                return creepData.Density;
            }
            
            return 0f;
        }
        
        /// <summary>
        /// 强制更新菌毯系统
        /// </summary>
        public void ForceUpdate()
        {
            UpdateCreepSystem();
            ProcessCreepMaintenance();
        }
        
        #endregion
        
        #region 建筑系统集成
        
        /// <summary>
        /// 建筑放置事件处理
        /// </summary>
        private void OnBuildingPlaced(BuildingData building)
        {
            if (building == null) return;
            
            // 如果是菌毯生产建筑，自动扩张菌毯
            if (IsCreepProducingBuilding(building.BuildingType))
            {
                var gridPos = WorldToGridPosition(building.Position);
                CreateCreepSource(building.Position, GetBuildingCreepRadius(building.BuildingType), building.OwnerId);
                
                // 扩张到建筑周围
                ExpandAroundBuilding(building, GetBuildingCreepRadius(building.BuildingType));
            }
        }
        
        /// <summary>
        /// 建筑销毁事件处理
        /// </summary>
        private void OnBuildingDestroyed(BuildingData building)
        {
            if (building == null) return;
            
            // 如果是菌毯源建筑，移除对应的菌毯源
            if (IsCreepProducingBuilding(building.BuildingType))
            {
                var gridPos = WorldToGridPosition(building.Position);
                if (_creepGrid.TryGetValue(gridPos, out var creepData) && creepData.IsSource)
                {
                    RemoveCreepTile(gridPos);
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 创建菌毯瓦片
        /// </summary>
        private CreepTile CreateCreepTile(Vector2Int position)
        {
            var worldPos = GridToWorldPosition(position);
            
            var tile = new CreepTile
            {
                Position = position,
                WorldPosition = worldPos,
                TileType = CreepTileType.Basic,
                Status = CreepTileStatus.Growing,
                Health = 100f,
                MaxHealth = 100f,
                GrowthLevel = 0f,
                MaxGrowthLevel = 1f,
                GrowthRate = 0.1f,
                IsActive = true,
                IsNutritionSource = false,
                ConnectedTiles = new List<CreepTile>(),
                CreationTime = Time.time,
                LastUpdateTime = Time.time,
                NeedsUpdate = true,
                TotalResourcesGenerated = 0f
            };
            
            return tile;
        }
        
        /// <summary>
        /// 移除菌毯瓦片
        /// </summary>
        private bool RemoveCreepTile(Vector2Int position)
        {
            if (!_creepTiles.ContainsKey(position))
                return false;
                
            var tile = _creepTiles[position];
            
            // 断开与其他瓦片的连接
            foreach (var connectedTile in tile.ConnectedTiles)
            {
                connectedTile.ConnectedTiles.Remove(tile);
            }
            
            // 从数据结构中移除
            _creepTiles.Remove(position);
            _creepGrid.Remove(position);
            _activeCreepCells.Remove(position);
            
            // 从空间索引中移除
            if (_spatialIndex != null && _creepGrid.TryGetValue(position, out var creepData))
            {
                _spatialIndex.Remove(creepData, creepData.Position, Vector3.one * _gridSize);
            }
            
            // 触发事件
            OnCreepRemoved?.Invoke(position);
            
            return true;
        }
        
        /// <summary>
        /// 获取相邻位置
        /// </summary>
        private List<Vector2Int> GetNeighborPositions(Vector2Int position)
        {
            return new List<Vector2Int>
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
            var worldPos = GridToWorldPosition(position);
            return Physics.CheckSphere(worldPos, _gridSize * 0.4f, _terrainLayer);
        }
        
        /// <summary>
        /// 检查是否有障碍物
        /// </summary>
        private bool HasObstacle(Vector2Int position)
        {
            var worldPos = GridToWorldPosition(position);
            return Physics.CheckSphere(worldPos, _gridSize * 0.4f, _obstacleLayer);
        }
        
        /// <summary>
        /// 检查建筑类型是否需要菌毯支持
        /// </summary>
        private bool RequiresCreepSupport(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.Hatchery => false,
                BuildingType.Extractor => false,
                _ => true
            };
        }
        
        /// <summary>
        /// 检查建筑是否产生菌毯
        /// </summary>
        private bool IsCreepProducingBuilding(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.Hatchery => true,
                BuildingType.CreepTumor => true,
                _ => false
            };
        }
        
        /// <summary>
        /// 获取建筑的菌毯半径
        /// </summary>
        private float GetBuildingCreepRadius(BuildingType buildingType)
        {
            return buildingType switch
            {
                BuildingType.Hatchery => 8f,
                BuildingType.CreepTumor => 4f,
                _ => 2f
            };
        }
        
        /// <summary>
        /// 获取附近建筑
        /// </summary>
        private List<BuildingData> GetNearbyBuildings(Vector2Int position, float radius)
        {
            // 这里需要调用建筑管理器的查询方法
            // 暂时返回空列表
            return new List<BuildingData>();
        }
        
        /// <summary>
        /// 获取附近资源
        /// </summary>
        private List<object> GetNearbyResources(Vector2Int position, float radius)
        {
            // 这里需要调用资源管理器的查询方法
            // 暂时返回空列表
            return new List<object>();
        }
        
        /// <summary>
        /// 获取营养消费者
        /// </summary>
        private List<CreepTile> GetNutritionConsumers()
        {
            return _creepTiles.Values.Where(tile => 
                !tile.IsNutritionSource && tile.IsActive).ToList();
        }
        
        /// <summary>
        /// 计算营养流动
        /// </summary>
        private void CalculateNutritionFlow(List<CreepTile> sources, List<CreepTile> consumers)
        {
            // 实现营养分配算法
            // 这里可以实现复杂的营养流动计算
        }
        
        /// <summary>
        /// 检测孤立区域
        /// </summary>
        private List<List<CreepTile>> DetectIsolatedRegions()
        {
            return GetIsolatedRegions();
        }
        
        /// <summary>
        /// 尝试重新连接区域
        /// </summary>
        private void TryReconnectRegion(List<CreepTile> region)
        {
            // 实现区域重连逻辑
            // 寻找最近的主要菌毯网络并尝试连接
        }
        
        /// <summary>
        /// 更新菌毯统计信息
        /// </summary>
        private void UpdateCreepStatistics()
        {
            var stats = GetCreepStatistics();
            OnStatisticsUpdated?.Invoke(stats);
        }
        
        /// <summary>
        /// 优化内存使用
        /// </summary>
        private void OptimizeMemoryUsage()
        {
            // 执行垃圾回收优化
            if (Time.frameCount % 300 == 0) // 每5秒执行一次
            {
                System.GC.Collect();
            }
        }
        
        #endregion
        
        #region 调试和可视化
        
        private void OnDrawGizmos()
        {
            if (!_enableDebugVisualization) return;
            
            // 绘制菌毯网格
            Gizmos.color = Color.green;
            foreach (var position in _creepTiles.Keys)
            {
                var worldPos = GridToWorldPosition(position);
                var tile = _creepTiles[position];
                
                // 根据瓦片状态设置颜色
                Gizmos.color = tile.Status switch
                {
                    CreepTileStatus.Healthy => Color.green,
                    CreepTileStatus.Growing => Color.yellow,
                    CreepTileStatus.Starving => Color.orange,
                    CreepTileStatus.Dying => Color.red,
                    _ => Color.gray
                };
                
                Gizmos.DrawWireCube(worldPos, Vector3.one * _tileSize);
            }
        }
        
        #endregion
    }
}