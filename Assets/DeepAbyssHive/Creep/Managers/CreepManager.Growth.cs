using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 生长模块
    /// 负责菌毯的自动生长、手动扩张和生长条件检查
    /// </summary>
    public partial class CreepManager
    {
        #region 扩张配置
        
        [Header("扩张设置")]
        [SerializeField] private float _expansionInterval = 2.0f;
        [SerializeField] private float _expansionRadius = 1.5f;
        [SerializeField] private int _maxExpansionPerTick = 3;
        [SerializeField] private float _expansionCostMultiplier = 1.0f;
        
        private float _lastExpansionTime;
        private Queue<Vector2Int> _expansionQueue = new Queue<Vector2Int>();
        
        #endregion
        
        #region 自动扩张
        
        /// <summary>
        /// 处理菌毯自动扩张
        /// </summary>
        private void ProcessAutoExpansion()
        {
            if (Time.time - _lastExpansionTime < _expansionInterval)
                return;
                
            if (_expansionQueue.Count == 0)
                GenerateExpansionTargets();
                
            int expansionsThisTick = 0;
            while (_expansionQueue.Count > 0 && expansionsThisTick < _maxExpansionPerTick)
            {
                Vector2Int target = _expansionQueue.Dequeue();
                if (TryExpandToPosition(target))
                {
                    expansionsThisTick++;
                }
            }
            
            _lastExpansionTime = Time.time;
        }
        
        /// <summary>
        /// 生成扩张目标位置
        /// </summary>
        private void GenerateExpansionTargets()
        {
            var expandablePositions = new List<Vector2Int>();
            
            // 从所有现有菌毯边缘寻找可扩张位置
            foreach (var creepPos in _creepTiles.Keys)
            {
                var neighbors = GetNeighborPositions(creepPos);
                foreach (var neighbor in neighbors)
                {
                    if (CanExpandToPosition(neighbor))
                    {
                        expandablePositions.Add(neighbor);
                    }
                }
            }
            
            // 按优先级排序扩张位置
            expandablePositions.Sort((a, b) => GetExpansionPriority(b).CompareTo(GetExpansionPriority(a)));
            
            // 添加到扩张队列
            foreach (var pos in expandablePositions)
            {
                _expansionQueue.Enqueue(pos);
            }
        }
        
        /// <summary>
        /// 获取位置的扩张优先级
        /// </summary>
        private float GetExpansionPriority(Vector2Int position)
        {
            float priority = 0f;
            
            // 连接现有菌毯网络的位置优先级更高
            var connectedCreepTiles = GetConnectedCreepTiles(position);
            priority += connectedCreepTiles * 2f;
            
            // 基础优先级
            priority += 1f;
            
            return priority;
        }
        
        #endregion
        
        #region 手动扩张
        
        /// <summary>
        /// 手动扩张菌毯到指定位置
        /// </summary>
        public bool RequestExpansion(Vector2Int targetPosition, bool ignoreResourceCost = false)
        {
            if (!CanExpandToPosition(targetPosition))
                return false;
                
            if (!ignoreResourceCost && !CanAffordExpansion(targetPosition))
                return false;
                
            return TryExpandToPosition(targetPosition, !ignoreResourceCost);
        }
        
        /// <summary>
        /// 批量扩张菌毯到多个位置
        /// </summary>
        public int RequestBatchExpansion(List<Vector2Int> targetPositions, bool ignoreResourceCost = false)
        {
            int successCount = 0;
            
            foreach (var position in targetPositions)
            {
                if (RequestExpansion(position, ignoreResourceCost))
                {
                    successCount++;
                }
            }
            
            return successCount;
        }
        
        /// <summary>
        /// 扩张菌毯到建筑周围
        /// </summary>
        public bool ExpandAroundBuilding(BuildingData building, float radius = 2f)
        {
            if (building == null)
                return false;
                
            var buildingGridPos = WorldToGridPosition(building.Position);
            var validPositions = new List<Vector2Int>();
            
            // 计算半径内的所有位置
            int intRadius = Mathf.CeilToInt(radius);
            for (int x = -intRadius; x <= intRadius; x++)
            {
                for (int y = -intRadius; y <= intRadius; y++)
                {
                    var pos = buildingGridPos + new Vector2Int(x, y);
                    if (Vector2.Distance(buildingGridPos, pos) <= radius && CanExpandToPosition(pos))
                    {
                        validPositions.Add(pos);
                    }
                }
            }
            
            return RequestBatchExpansion(validPositions) > 0;
        }
        
        #endregion
        
        #region 扩张条件检查
        
        /// <summary>
        /// 检查是否可以扩张到指定位置
        /// </summary>
        private bool CanExpandToPosition(Vector2Int position)
        {
            // 已有菌毯的位置不能重复扩张
            if (_creepTiles.ContainsKey(position))
                return false;
                
            // 检查地形是否适合
            if (!IsValidTerrain(position))
                return false;
                
            // 检查是否有阻挡物
            if (HasObstacle(position))
                return false;
                
            // 检查是否与现有菌毯相邻
            if (!IsAdjacentToCreep(position))
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 检查是否有足够资源进行扩张
        /// </summary>
        private bool CanAffordExpansion(Vector2Int position)
        {
            var cost = CalculateExpansionCost(position);
            // TODO: 实现资源管理器集成
            return true; // 暂时返回 true，待资源管理器实现后修复
        }
        
        /// <summary>
        /// 计算扩张到指定位置的资源消耗
        /// </summary>
        private Dictionary<string, float> CalculateExpansionCost(Vector2Int position)
        {
            var baseCost = new Dictionary<string, float>
            {
                ["Biomass"] = 10f * _expansionCostMultiplier,
                ["Energy"] = 5f * _expansionCostMultiplier
            };
            
            // 根据地形调整成本
            var terrainMultiplier = GetTerrainCostMultiplier(position);
            foreach (var key in baseCost.Keys.ToList())
            {
                baseCost[key] *= terrainMultiplier;
            }
            
            return baseCost;
        }
        
        /// <summary>
        /// 检查位置是否与现有菌毯相邻
        /// </summary>
        private bool IsAdjacentToCreep(Vector2Int position)
        {
            var neighbors = GetNeighborPositions(position);
            return neighbors.Any(neighbor => _creepTiles.ContainsKey(neighbor));
        }
        
        #endregion
        
        #region 扩张执行
        
        /// <summary>
        /// 尝试扩张到指定位置
        /// </summary>
        private bool TryExpandToPosition(Vector2Int position, bool consumeResources = true)
        {
            if (!CanExpandToPosition(position))
                return false;
                
            if (consumeResources)
            {
                var cost = CalculateExpansionCost(position);
                // TODO: 实现资源消耗逻辑
                // if (!_resourceManager?.ConsumeResources(cost) ?? false)
                //     return false;
            }
            
            // 创建新的菌毯瓦片
            var creepTile = CreateCreepTile(position);
            if (creepTile == null)
                return false;
                
            // 添加到菌毯网络
            _creepTiles[position] = creepTile;
            
            // 更新相邻菌毯的连接
            UpdateAdjacentConnections(position);
            
            // 触发扩张事件
            OnCreepExpanded?.Invoke(GridToWorldPosition(position), _gridCellSize);
            
            return true;
        }
        
        /// <summary>
        /// 更新相邻菌毯的连接关系
        /// </summary>
        private void UpdateAdjacentConnections(Vector2Int position)
        {
            var neighbors = GetNeighborPositions(position);
            var currentTile = _creepTiles[position];
            
            foreach (var neighbor in neighbors)
            {
                if (_creepTiles.TryGetValue(neighbor, out var neighborTile))
                {
                    // 建立双向连接
                    currentTile.ConnectedTiles.Add(neighborTile);
                    neighborTile.ConnectedTiles.Add(currentTile);
                }
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 获取连接到指定位置的菌毯瓦片数量
        /// </summary>
        private int GetConnectedCreepTiles(Vector2Int position)
        {
            var neighbors = GetNeighborPositions(position);
            return neighbors.Count(neighbor => _creepTiles.ContainsKey(neighbor));
        }
        
        /// <summary>
        /// 获取地形成本倍数
        /// </summary>
        private float GetTerrainCostMultiplier(Vector2Int position)
        {
            // 这里可以根据实际地形系统实现
            // 暂时返回基础倍数
            return 1.0f;
        }
        
        #endregion
    }
}