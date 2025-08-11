using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 更新模块
    /// 负责菌毯的定期更新、维护和状态管理
    /// </summary>
    public partial class CreepManager
    {
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
        
        #endregion
    }
}