using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Data;
// 使用别名解决枚举冲突
using DataNS = DeepAbyssHive.Creep.Data;
using EnumsNS = DeepAbyssHive.Creep.Enums;
using CreepSourceType = DeepAbyssHive.Creep.Data.CreepSourceType;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 源点管理模块
    /// 负责菌毯源点的增删、合并、分割和网络管理
    /// </summary>
    public partial class CreepManager
    {
        #region 源点数据结构
        
        private Dictionary<int, CreepSource> _creepSources = new Dictionary<int, CreepSource>();
        private Dictionary<int, List<CreepSource>> _sourceNetworks = new Dictionary<int, List<CreepSource>>();
        private int _nextSourceId = 1;
        private int _nextNetworkId = 1;
        
        #endregion
        
        #region 源点创建和删除
        
        /// <summary>
        /// 创建新的菌毯源点
        /// </summary>
        /// <param name="worldPosition">世界位置</param>
        /// <param name="radius">影响半径</param>
        /// <param name="ownerId">所有者ID</param>
        /// <param name="sourceType">源点类型</param>
        /// <returns>源点ID，失败返回-1</returns>
        public int CreateCreepSourcePoint(Vector3 worldPosition, float radius, int ownerId, DataNS.CreepSourceType sourceType = DataNS.CreepSourceType.CreepTumor)
        {
            var gridPos = WorldToGridPosition(worldPosition);
            
            // 检查位置是否已有源点
            if (HasSourceAt(gridPos))
            {
                DAHLog.Warning(LogCategory.MANAGER, $"[CreepManager] 位置 {worldPosition} 已存在菌毯源点");
                return -1;
            }
            
            // 创建源点数据
            var sourceId = _nextSourceId++;
            var source = new CreepSource
            {
                SourceId = sourceId,
                Position = worldPosition,
                Radius = radius,
                Type = sourceType,
                IsActive = true,
                CreationTime = UnityEngine.Time.time,
                NetworkId = ownerId, // 使用 ownerId 作为 NetworkId
                Strength = CalculateSourceStrength(sourceType)
            };
            
            _creepSources[sourceId] = source;
            
            // 创建对应的菌毯瓦片
            if (!_creepTiles.ContainsKey(gridPos))
            {
                var tile = CreateCreepTile(gridPos);
                if (tile != null)
                {
                    tile.IsNutritionSource = true;
                    tile.TileType = GetTileTypeForSource(sourceType);
                    tile.Health = tile.MaxHealth; // 源点瓦片满血
                    _creepTiles[gridPos] = tile;
                }
            }
            
            // 分配到网络
            AssignSourceToNetwork(source);
            
            // 初始扩张
            InitialSourceExpansion(source);
            
            DAHLog.Info(LogCategory.MANAGER, $"[CreepManager] 创建菌毯源点 {sourceId} 于位置 {worldPosition}，半径 {radius}");
            return sourceId;
        }
        
        /// <summary>
        /// 移除菌毯源点
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveCreepSource(int sourceId)
        {
            if (!_creepSources.TryGetValue(sourceId, out var source))
                return false;
            
            // 从网络中移除
            RemoveSourceFromNetwork(source);
            
            // 移除对应的菌毯瓦片
            var gridPos = WorldToGridPosition(source.Position);
            if (_creepTiles.TryGetValue(gridPos, out var tile))
            {
                tile.IsNutritionSource = false;
                
                // 如果没有其他源点支持，标记为衰减
                if (!HasNearbyNutritionSource(gridPos))
                {
                    tile.Status = DeepAbyssHive.Creep.Compat.CreepTileStatusCompat.Weakened;
                }
            }
            
            // 移除源点数据
            _creepSources.Remove(sourceId);
            
            // 检查网络分割
            CheckNetworkSplit(source.NetworkId);
            
            DAHLog.Info(LogCategory.MANAGER, $"[CreepManager] 移除菌毯源点 {sourceId}");
            return true;
        }
        
        /// <summary>
        /// 移除指定位置的所有源点
        /// </summary>
        /// <param name="worldPosition">世界位置</param>
        /// <param name="radius">影响半径</param>
        /// <returns>移除的源点数量</returns>
        public int RemoveSourcesInArea(Vector3 worldPosition, float radius)
        {
            var sourcesToRemove = _creepSources.Values
                .Where(source => Vector3.Distance(source.Position, worldPosition) <= radius)
                .ToList();
            
            int removedCount = 0;
            foreach (var source in sourcesToRemove)
            {
                if (RemoveCreepSource(source.SourceId))
                    removedCount++;
            }
            
            return removedCount;
        }
        
        #endregion
        
        #region 源点网络管理
        
        /// <summary>
        /// 分配源点到网络
        /// </summary>
        /// <param name="source">源点</param>
        private void AssignSourceToNetwork(CreepSource source)
        {
            // 查找附近的网络
            var nearbyNetworks = FindNearbyNetworks(source.Position, source.Radius * 2f);
            
            if (nearbyNetworks.Count == 0)
            {
                // 创建新网络
                var networkId = _nextNetworkId++;
                _sourceNetworks[networkId] = new List<CreepSource> { source };
                
                // 由于 CreepSource 是结构体，需要先获取副本，修改后再存回字典
                var updatedSource = source;
                updatedSource.NetworkId = networkId;
                _creepSources[source.SourceId] = updatedSource;
            }
            else if (nearbyNetworks.Count == 1)
            {
                // 加入现有网络
                var networkId = nearbyNetworks[0];
                _sourceNetworks[networkId].Add(source);
                
                // 由于 CreepSource 是结构体，需要先获取副本，修改后再存回字典
                var updatedSource = source;
                updatedSource.NetworkId = networkId;
                _creepSources[source.SourceId] = updatedSource;
            }
            else
            {
                // 合并多个网络
                MergeNetworks(nearbyNetworks, source);
            }
        }
        
        /// <summary>
        /// 从网络中移除源点
        /// </summary>
        /// <param name="source">源点</param>
        private void RemoveSourceFromNetwork(CreepSource source)
        {
            if (source.NetworkId == -1 || !_sourceNetworks.ContainsKey(source.NetworkId))
                return;
            
            var network = _sourceNetworks[source.NetworkId];
            network.Remove(source);
            
            // 如果网络为空，删除网络
            if (network.Count == 0)
            {
                _sourceNetworks.Remove(source.NetworkId);
            }
        }
        
        /// <summary>
        /// 合并多个网络
        /// </summary>
        /// <param name="networkIds">要合并的网络ID列表</param>
        /// <param name="newSource">新源点</param>
        private void MergeNetworks(List<int> networkIds, CreepSource newSource)
        {
            var primaryNetworkId = networkIds[0];
            var primaryNetwork = _sourceNetworks[primaryNetworkId];
            
            // 将新源点加入主网络
            primaryNetwork.Add(newSource);
            
            // 由于 CreepSource 是结构体，需要先获取副本，修改后再存回字典
            var updatedSource = newSource;
            updatedSource.NetworkId = primaryNetworkId;
            _creepSources[newSource.SourceId] = updatedSource;
            
            // 合并其他网络到主网络
            for (int i = 1; i < networkIds.Count; i++)
            {
                var networkId = networkIds[i];
                if (_sourceNetworks.TryGetValue(networkId, out var network))
                {
                    foreach (var source in network)
                    {
                        // 由于 CreepSource 是结构体，需要先获取副本，修改后再存回字典
                        var updatedNetworkSource = source;
                        updatedNetworkSource.NetworkId = primaryNetworkId;
                        _creepSources[source.SourceId] = updatedNetworkSource;
                        
                        primaryNetwork.Add(updatedNetworkSource);
                    }
                    _sourceNetworks.Remove(networkId);
                }
            }
            
            DAHLog.Info(LogCategory.MANAGER, $"[CreepManager] 合并 {networkIds.Count} 个网络到网络 {primaryNetworkId}");
        }
        
        /// <summary>
        /// 检查网络分割
        /// </summary>
        /// <param name="networkId">网络ID</param>
        private void CheckNetworkSplit(int networkId)
        {
            if (!_sourceNetworks.TryGetValue(networkId, out var network) || network.Count <= 1)
                return;
            
            // 使用连通性检查算法检测网络是否分割
            var connectedGroups = FindConnectedGroups(network);
            
            if (connectedGroups.Count > 1)
            {
                // 网络分割，创建新的子网络
                SplitNetwork(networkId, connectedGroups);
            }
        }
        
        /// <summary>
        /// 分割网络
        /// </summary>
        /// <param name="originalNetworkId">原网络ID</param>
        /// <param name="connectedGroups">连通组</param>
        private void SplitNetwork(int originalNetworkId, List<List<CreepSource>> connectedGroups)
        {
            // 保留最大的组在原网络中
            var largestGroup = connectedGroups.OrderByDescending(g => g.Count).First();
            _sourceNetworks[originalNetworkId] = largestGroup;
            
            // 为其他组创建新网络
            for (int i = 0; i < connectedGroups.Count; i++)
            {
                var group = connectedGroups[i];
                if (group == largestGroup) continue;
                
                var newNetworkId = _nextNetworkId++;
                _sourceNetworks[newNetworkId] = group;
                
                foreach (var source in group)
                {
                    // 由于 CreepSource 是结构体，需要先获取副本，修改后再存回字典
                    var updatedSource = source;
                    updatedSource.NetworkId = newNetworkId;
                    _creepSources[source.SourceId] = updatedSource;
                }
            }
            
            DAHLog.Info(LogCategory.MANAGER, $"[CreepManager] 网络 {originalNetworkId} 分割为 {connectedGroups.Count} 个子网络");
        }
        
        #endregion
        
        #region 源点查询和状态
        
        /// <summary>
        /// 检查指定位置是否有源点
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <returns>是否有源点</returns>
        public bool HasSourceAt(Vector2Int gridPosition)
        {
            var worldPos = GridToWorldPosition(gridPosition);
            return _creepSources.Values.Any(source => 
                Vector3.Distance(source.Position, worldPos) < 0.5f);
        }
        
        /// <summary>
        /// 获取指定位置的源点
        /// </summary>
        /// <param name="worldPosition">世界位置</param>
        /// <param name="searchRadius">搜索半径</param>
        /// <returns>源点，如果没有则返回null</returns>
        public CreepSource GetSourceAt(Vector3 worldPosition, float searchRadius = 1f)
        {
            return _creepSources.Values.FirstOrDefault(source => 
                Vector3.Distance(source.Position, worldPosition) <= searchRadius);
        }
        
        /// <summary>
        /// 获取所有活跃的源点
        /// </summary>
        /// <returns>活跃源点列表</returns>
        public List<CreepSource> GetActiveSources()
        {
            return _creepSources.Values.Where(source => source.IsActive).ToList();
        }
        
        /// <summary>
        /// 获取指定所有者的源点
        /// </summary>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>源点列表</returns>
        public List<CreepSource> GetSourcesByOwner(int ownerId)
        {
            return _creepSources.Values.Where(source => source.NetworkId == ownerId).ToList();
        }
        
        /// <summary>
        /// 获取网络信息
        /// </summary>
        /// <returns>网络统计信息</returns>
        public Dictionary<int, int> GetNetworkStatistics()
        {
            return _sourceNetworks.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 计算源点强度
        /// </summary>
        /// <param name="sourceType">源点类型</param>
        /// <returns>强度值</returns>
        private float CalculateSourceStrength(DataNS.CreepSourceType sourceType)
        {
            return sourceType switch
            {
                DataNS.CreepSourceType.MainHive => 100f,
                DataNS.CreepSourceType.SubHive => 75f,
                DataNS.CreepSourceType.CreepTumor => 50f,
                DataNS.CreepSourceType.CreepColony => 40f,
                DataNS.CreepSourceType.SpawningPool => 30f,
                _ => 10f
            };
        }
        
        /// <summary>
        /// 根据源点类型获取瓦片类型
        /// </summary>
        /// <param name="sourceType">源点类型</param>
        /// <returns>瓦片类型</returns>
        private EnumsNS.CreepTileType GetTileTypeForSource(DataNS.CreepSourceType sourceType)
        {
            return sourceType switch
            {
                DataNS.CreepSourceType.MainHive => EnumsNS.CreepTileType.Core,
                DataNS.CreepSourceType.SubHive => EnumsNS.CreepTileType.Core,
                DataNS.CreepSourceType.CreepTumor => EnumsNS.CreepTileType.Core,
                DataNS.CreepSourceType.SpawningPool => EnumsNS.CreepTileType.Core,
                DataNS.CreepSourceType.EvolutionChamber => EnumsNS.CreepTileType.Core,
                _ => EnumsNS.CreepTileType.Creep
            };
        }
        
        /// <summary>
        /// 源点初始扩张
        /// </summary>
        /// <param name="source">源点</param>
        private void InitialSourceExpansion(CreepSource source)
        {
            var centerPos = WorldToGridPosition(source.Position);
            var radius = Mathf.CeilToInt(source.Radius);
            
            // 在半径内创建菌毯瓦片
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    var pos = centerPos + new Vector2Int(x, y);
                    var distance = Vector2Int.Distance(centerPos, pos);
                    
                    if (distance <= source.Radius && CanExpandToPosition(pos))
                    {
                        TryExpandToPosition(pos, false); // 初始扩张不消耗资源
                    }
                }
            }
        }
        
        /// <summary>
        /// 查找附近的网络
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">搜索半径</param>
        /// <returns>网络ID列表</returns>
        private List<int> FindNearbyNetworks(Vector3 position, float radius)
        {
            var nearbyNetworks = new HashSet<int>();
            
            foreach (var source in _creepSources.Values)
            {
                if (source.NetworkId != -1 && 
                    Vector3.Distance(source.Position, position) <= radius)
                {
                    nearbyNetworks.Add(source.NetworkId);
                }
            }
            
            return nearbyNetworks.ToList();
        }
        
        /// <summary>
        /// 查找连通组
        /// </summary>
        /// <param name="sources">源点列表</param>
        /// <returns>连通组列表</returns>
        private List<List<CreepSource>> FindConnectedGroups(List<CreepSource> sources)
        {
            var visited = new HashSet<CreepSource>();
            var groups = new List<List<CreepSource>>();
            
            foreach (var source in sources)
            {
                if (!visited.Contains(source))
                {
                    var group = new List<CreepSource>();
                    DepthFirstSearch(source, sources, visited, group);
                    groups.Add(group);
                }
            }
            
            return groups;
        }
        
        /// <summary>
        /// 深度优先搜索连通性
        /// </summary>
        /// <param name="current">当前源点</param>
        /// <param name="allSources">所有源点</param>
        /// <param name="visited">已访问集合</param>
        /// <param name="group">当前组</param>
        private void DepthFirstSearch(CreepSource current, List<CreepSource> allSources, 
            HashSet<CreepSource> visited, List<CreepSource> group)
        {
            visited.Add(current);
            group.Add(current);
            
            // 查找连接的源点
            foreach (var other in allSources)
            {
                if (!visited.Contains(other) && AreSourcesConnected(current, other))
                {
                    DepthFirstSearch(other, allSources, visited, group);
                }
            }
        }
        
        /// <summary>
        /// 检查两个源点是否连通
        /// </summary>
        /// <param name="source1">源点1</param>
        /// <param name="source2">源点2</param>
        /// <returns>是否连通</returns>
        private bool AreSourcesConnected(CreepSource source1, CreepSource source2)
        {
            var distance = Vector3.Distance(source1.Position, source2.Position);
            var maxConnectionDistance = (source1.Radius + source2.Radius) * 1.5f;
            
            return distance <= maxConnectionDistance;
        }
        
        /// <summary>
        /// 检查附近是否有营养源
        /// </summary>
        /// <param name="gridPosition">网格位置</param>
        /// <returns>是否有营养源</returns>
        private bool HasNearbyNutritionSource(Vector2Int gridPosition)
        {
            var worldPos = GridToWorldPosition(gridPosition);
            return _creepSources.Values.Any(source => 
                source.IsActive && 
                Vector3.Distance(source.Position, worldPos) <= source.Radius);
        }
        
        #endregion
    }
}
