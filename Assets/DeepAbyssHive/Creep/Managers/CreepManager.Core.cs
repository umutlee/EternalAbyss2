using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.Buildings.Managers;

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
            // Unity Update - 调用显式接口方法
            if (IsInitialized && !IsPaused)
            {
                ((IManager)this).Update(Time.deltaTime);
            }
        }

        private void FixedUpdate()
        {
            // Unity FixedUpdate - 调用显式接口方法
            if (IsInitialized && !IsPaused)
            {
                ((IManager)this).FixedUpdate(Time.fixedDeltaTime);
            }
        }

        private void LateUpdate()
        {
            // Unity LateUpdate - 调用显式接口方法
            if (IsInitialized && !IsPaused)
            {
                ((IManager)this).LateUpdate(Time.deltaTime);
            }
        }

        #endregion

        #region 私有字段定义
        
        // 私有字段定义
        private readonly Dictionary<Vector2Int, CreepData> _creepGrid = new Dictionary<Vector2Int, CreepData>();
        private readonly Dictionary<Vector2Int, CreepTile> _creepTiles = new Dictionary<Vector2Int, CreepTile>();
        private readonly HashSet<Vector2Int> _activeCreepCells = new HashSet<Vector2Int>();
        private ISpatialIndex<CreepData> _spatialIndex;
        private BuildingManager _buildingManager;
        
        private float _gridSize = 1f;
        private float _tileSize = 1f;
        private float _maxDensity = 1f;
        private string _managerName = "CreepManager";
        
        // 事件定义
        public System.Action<CreepStatistics> OnStatisticsUpdated;

        #endregion

        #region 核心方法

        /// <summary>
        /// 更新菌毯瓦片
        /// </summary>
        private void UpdateCreepTiles(float deltaTime)
        {
            foreach (var tile in _creepTiles.Values)
            {
                if (tile.IsActive)
                {
                    UpdateTileStatus(tile, deltaTime);
                }
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
                case CreepTileStatus.Growing:
                    UpdateGrowingTile(tile, deltaTime);
                    break;
                case CreepTileStatus.Starving:
                    UpdateStarvingTile(tile, deltaTime);
                    break;
                case CreepTileStatus.Dying:
                    UpdateDyingTile(tile, deltaTime);
                    break;
            }
        }

        /// <summary>
        /// 更新成长中的瓦片
        /// </summary>
        private void UpdateGrowingTile(CreepTile tile, float deltaTime)
        {
            tile.Health = Mathf.Min(tile.MaxHealth, tile.Health + 10f * deltaTime);
            if (tile.Health >= tile.MaxHealth)
            {
                tile.Status = CreepTileStatus.Healthy;
            }
        }

        /// <summary>
        /// 更新饥饿的瓦片
        /// </summary>
        private void UpdateStarvingTile(CreepTile tile, float deltaTime)
        {
            tile.Health = Mathf.Max(0f, tile.Health - 5f * deltaTime);
            if (tile.Health <= 0f)
            {
                tile.Status = CreepTileStatus.Dying;
            }
        }

        /// <summary>
        /// 更新死亡中的瓦片
        /// </summary>
        private void UpdateDyingTile(CreepTile tile, float deltaTime)
        {
            tile.Health = Mathf.Max(0f, tile.Health - 20f * deltaTime);
            if (tile.Health <= 0f)
            {
                tile.IsActive = false;
                _activeCreepCells.Remove(tile.Position);
            }
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
                TileType = CreepTileType.Basic,
                Status = CreepTileStatus.Growing,
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
                    _spatialIndex.Remove(creepData, creepData.Position, Vector3.one * _gridSize);
                }
            }
        }

        /// <summary>
        /// 网格位置转世界位置
        /// </summary>
        private Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(gridPosition.x * _gridSize, 0, gridPosition.y * _gridSize);
        }

        /// <summary>
        /// 世界位置转网格位置
        /// </summary>
        private Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / _gridSize),
                Mathf.RoundToInt(worldPosition.z / _gridSize)
            );
        }

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