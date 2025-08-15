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
            return _resourceManager?.CanAfford(cost) ?? true;
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
                if (!_resourceManager?.ConsumeResources(cost) ?? false)
                    return false;
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
            OnCreepExpanded?.Invoke(GridToWorldPosition(position), _gridSize);
            
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
        
        #region 更新配置
        
        [Header("更新设置")]
        [SerializeField] private float _updateInterval = 1.0f;
        [SerializeField] private float _maintenanceInterval = 5.0f;
        [SerializeField] private float _cleanupInterval = 10.0f;
        [SerializeField] private int _maxUpdatesPerFrame = 50;
        
        private float _lastUpdateTime;
        private float _lastMaintenanceTime;
        private float _lastCleanupTime;
        private Queue<CreepTile> _updateQueue = new Queue<CreepTile>();
        
        #endregion
        
        #region 主更新循环
        
        /// <summary>
        /// 菌毯系统主更新方法
        /// </summary>
        private void UpdateCreepSystem()
        {
            // 定期更新菌毯瓦片
            if (Time.time - _lastUpdateTime >= _updateInterval)
            {
                ProcessCreepUpdates();
                _lastUpdateTime = Time.time;
            }
            
            // 定期维护菌毯网络
            if (Time.time - _lastMaintenanceTime >= _maintenanceInterval)
            {
                ProcessCreepMaintenance();
                _lastMaintenanceTime = Time.time;
            }
            
            // 定期清理无效菌毯
            if (Time.time - _lastCleanupTime >= _cleanupInterval)
            {
                ProcessCreepCleanup();
                _lastCleanupTime = Time.time;
            }
        }
        
        #endregion
        
        #region 菌毯瓦片更新
        
        /// <summary>
        /// 处理菌毯瓦片更新
        /// </summary>
        private void ProcessCreepUpdates()
        {
            // 将所有需要更新的瓦片加入队列
            if (_updateQueue.Count == 0)
            {
                foreach (var tile in _creepTiles.Values)
                {
                    if (tile.NeedsUpdate)
                    {
                        _updateQueue.Enqueue(tile);
                    }
                }
            }
            
            // 分帧处理更新，避免卡顿
            int updatesThisFrame = 0;
            while (_updateQueue.Count > 0 && updatesThisFrame < _maxUpdatesPerFrame)
            {
                var tile = _updateQueue.Dequeue();
                UpdateCreepTile(tile);
                updatesThisFrame++;
            }
        }
        
        /// <summary>
        /// 更新单个菌毯瓦片
        /// </summary>
        private void UpdateCreepTile(CreepTile tile)
        {
            if (tile == null || !tile.IsActive)
                return;
                
            // 更新瓦片状态
            UpdateTileStatus(tile);
            
            // 更新瓦片效果
            UpdateTileEffects(tile);
            
            // 更新瓦片连接
            UpdateTileConnections(tile);
            
            // 更新瓦片资源生产
            UpdateTileResourceGeneration(tile);
            
            // 标记更新完成
            tile.NeedsUpdate = false;
            tile.LastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 更新瓦片状态
        /// </summary>
        private void UpdateTileStatus(CreepTile tile)
        {
            // 检查瓦片健康状态
            if (tile.Health <= 0)
            {
                tile.Status = CreepTileStatus.Dying;
                return;
            }
            
            // 检查是否有足够的营养供应
            if (!HasAdequateNutrition(tile))
            {
                tile.Status = CreepTileStatus.Starving;
                tile.Health -= Time.deltaTime * 5f; // 饥饿时缓慢失血
                return;
            }
            
            // 检查是否在成长
            if (tile.GrowthLevel < tile.MaxGrowthLevel)
            {
                tile.Status = CreepTileStatus.Growing;
                tile.GrowthLevel += Time.deltaTime * tile.GrowthRate;
                return;
            }
            
            // 正常状态
            tile.Status = CreepTileStatus.Healthy;
        }
        
        /// <summary>
        /// 更新瓦片效果
        /// </summary>
        private void UpdateTileEffects(CreepTile tile)
        {
            // 更新视觉效果
            UpdateTileVisuals(tile);
            
            // 更新音效
            UpdateTileAudio(tile);
            
            // 更新粒子效果
            UpdateTileParticles(tile);
        }
        
        /// <summary>
        /// 更新瓦片连接关系
        /// </summary>
        private void UpdateTileConnections(CreepTile tile)
        {
            var neighbors = GetNeighborPositions(tile.Position);
            var currentConnections = new HashSet<CreepTile>(tile.ConnectedTiles);
            
            // 检查新的连接
            foreach (var neighborPos in neighbors)
            {
                if (_creepTiles.TryGetValue(neighborPos, out var neighborTile))
                {
                    if (!currentConnections.Contains(neighborTile))
                    {
                        // 建立新连接
                        tile.ConnectedTiles.Add(neighborTile);
                        neighborTile.ConnectedTiles.Add(tile);
                    }
                }
            }
            
            // 移除无效连接
            var validConnections = new List<CreepTile>();
            foreach (var connectedTile in tile.ConnectedTiles)
            {
                if (connectedTile != null && connectedTile.IsActive && 
                    neighbors.Contains(connectedTile.Position))
                {
                    validConnections.Add(connectedTile);
                }
            }
            tile.ConnectedTiles = validConnections;
        }
        
        /// <summary>
        /// 更新瓦片资源生产
        /// </summary>
        private void UpdateTileResourceGeneration(CreepTile tile)
        {
            if (tile.Status != CreepTileStatus.Healthy)
                return;
                
            // 计算资源生产量
            var resourceGeneration = CalculateTileResourceGeneration(tile);
            
            // 生产资源
            foreach (var resource in resourceGeneration)
            {
                _resourceManager?.AddResource(resource.Key, resource.Value * Time.deltaTime);
            }
            
            // 更新累计生产统计
            tile.TotalResourcesGenerated += resourceGeneration.Values.Sum() * Time.deltaTime;
        }
        
        #endregion
        
        #region 菌毯维护
        
        /// <summary>
        /// 处理菌毯维护
        /// </summary>
        private void ProcessCreepMaintenance()
        {
            // 修复受损的菌毯
            RepairDamagedCreep();
            
            // 优化菌毯网络连接
            OptimizeCreepNetwork();
            
            // 平衡菌毯营养分配
            BalanceNutritionDistribution();
            
            // 更新菌毯统计信息
            UpdateCreepStatistics();
        }
        
        /// <summary>
        /// 修复受损的菌毯
        /// </summary>
        private void RepairDamagedCreep()
        {
            var damagedTiles = _creepTiles.Values.Where(tile => 
                tile.Health < tile.MaxHealth && 
                tile.Status != CreepTileStatus.Dying).ToList();
                
            foreach (var tile in damagedTiles)
            {
                // 自然恢复
                var healAmount = tile.MaxHealth * 0.1f * Time.deltaTime;
                tile.Health = Mathf.Min(tile.MaxHealth, tile.Health + healAmount);
                
                // 如果完全恢复，标记需要更新
                if (tile.Health >= tile.MaxHealth)
                {
                    tile.NeedsUpdate = true;
                }
            }
        }
        
        /// <summary>
        /// 优化菌毯网络连接
        /// </summary>
        private void OptimizeCreepNetwork()
        {
            // 检测孤立的菌毯区域
            var isolatedRegions = DetectIsolatedRegions();
            
            // 尝试重新连接孤立区域
            foreach (var region in isolatedRegions)
            {
                TryReconnectRegion(region);
            }
        }
        
        /// <summary>
        /// 平衡营养分配
        /// </summary>
        private void BalanceNutritionDistribution()
        {
            var nutritionSources = GetNutritionSources();
            var nutritionConsumers = GetNutritionConsumers();
            
            // 计算营养流动
            CalculateNutritionFlow(nutritionSources, nutritionConsumers);
        }
        
        #endregion
        
        #region 菌毯清理
        
        /// <summary>
        /// 处理菌毯清理
        /// </summary>
        private void ProcessCreepCleanup()
        {
            // 移除死亡的菌毯瓦片
            RemoveDeadCreepTiles();
            
            // 清理无效引用
            CleanupInvalidReferences();
            
            // 垃圾回收优化
            OptimizeMemoryUsage();
        }
        
        /// <summary>
        /// 移除死亡的菌毯瓦片
        /// </summary>
        private void RemoveDeadCreepTiles()
        {
            var deadTiles = _creepTiles.Where(kvp => 
                kvp.Value.Status == CreepTileStatus.Dying || 
                kvp.Value.Health <= 0).ToList();
                
            foreach (var deadTile in deadTiles)
            {
                RemoveCreepTile(deadTile.Key);
            }
        }
        
        /// <summary>
        /// 清理无效引用
        /// </summary>
        private void CleanupInvalidReferences()
        {
            foreach (var tile in _creepTiles.Values)
            {
                // 清理无效的连接引用
                tile.ConnectedTiles.RemoveAll(connectedTile => 
                    connectedTile == null || !connectedTile.IsActive);
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
        
        /// <summary>
        /// 检查瓦片是否有足够营养
        /// </summary>
        private bool HasAdequateNutrition(CreepTile tile)
        {
            // 检查是否连接到营养源
            return tile.ConnectedTiles.Any(connectedTile => 
                connectedTile.IsNutritionSource || 
                connectedTile.ConnectedTiles.Any(t => t.IsNutritionSource));
        }
        
        /// <summary>
        /// 计算瓦片资源生产量
        /// </summary>
        private Dictionary<string, float> CalculateTileResourceGeneration(CreepTile tile)
        {
            var baseGeneration = new Dictionary<string, float>
            {
                ["Biomass"] = 1f * tile.GrowthLevel,
                ["Energy"] = 0.5f * tile.GrowthLevel
            };
            
            // 根据瓦片类型调整生产量
            var typeMultiplier = GetTileTypeMultiplier(tile.TileType);
            foreach (var key in baseGeneration.Keys.ToList())
            {
                baseGeneration[key] *= typeMultiplier;
            }
            
            return baseGeneration;
        }
        
        /// <summary>
        /// 获取瓦片类型倍数
        /// </summary>
        private float GetTileTypeMultiplier(CreepTileType tileType)
        {
            return tileType switch
            {
                CreepTileType.Basic => 1.0f,
                CreepTileType.Enhanced => 1.5f,
                CreepTileType.Specialized => 2.0f,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// 更新瓦片视觉效果
        /// </summary>
        private void UpdateTileVisuals(CreepTile tile)
        {
            // 根据瓦片状态更新视觉
            // 这里可以调用具体的渲染系统
        }
        
        /// <summary>
        /// 更新瓦片音效
        /// </summary>
        private void UpdateTileAudio(CreepTile tile)
        {
            // 根据瓦片状态播放音效
            // 这里可以调用音频系统
        }
        
        /// <summary>
        /// 更新瓦片粒子效果
        /// </summary>
        private void UpdateTileParticles(CreepTile tile)
        {
            // 根据瓦片状态更新粒子效果
            // 这里可以调用粒子系统
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
            // 尝试找到最近的主网络连接点
            // 这里可以实现重连逻辑
        }
        
        /// <summary>
        /// 获取营养源
        /// </summary>
        private List<CreepTile> GetNutritionSources()
        {
            return _creepTiles.Values.Where(tile => tile.IsNutritionSource && tile.IsActive).ToList();
        }
        
        /// <summary>
        /// 获取营养消费者
        /// </summary>
        private List<CreepTile> GetNutritionConsumers()
        {
            return _creepTiles.Values.Where(tile => !tile.IsNutritionSource && tile.IsActive).ToList();
        }
        
        /// <summary>
        /// 计算营养流动
        /// </summary>
        private void CalculateNutritionFlow(List<CreepTile> sources, List<CreepTile> consumers)
        {
            // 实现营养分配算法
            // 这里可以使用流网络算法来优化营养分配
        }
        
        /// <summary>
        /// 优化内存使用
        /// </summary>
        private void OptimizeMemoryUsage()
        {
            // 清理无用的引用
            System.GC.Collect();
        }
        
        /// <summary>
        /// 更新菌毯统计信息
        /// </summary>
        private void UpdateCreepStatistics()
        {
            var stats = GetCreepStatistics();
            OnStatisticsUpdated?.Invoke(stats);
        }
        
        #endregion
    }
}