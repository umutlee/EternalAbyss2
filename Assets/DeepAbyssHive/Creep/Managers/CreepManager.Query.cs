using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器 - 查询模块
    /// 提供各种菌毯查询和检索功能
    /// </summary>
    public partial class CreepManager
    {
        #region 基础查询
        
        /// <summary>
        /// 检查指定位置是否有菌毯
        /// </summary>
        public bool HasCreepAt(Vector2Int position)
        {
            return _creepTiles.ContainsKey(position) && _creepTiles[position].IsActive;
        }
        
        /// <summary>
        /// 获取指定位置的菌毯瓦片
        /// </summary>
        public CreepTile GetCreepTileAt(Vector2Int position)
        {
            return _creepTiles.TryGetValue(position, out var tile) ? tile : null;
        }
        
        /// <summary>
        /// 获取所有菌毯瓦片
        /// </summary>
        public IReadOnlyCollection<CreepTile> GetAllCreepTiles()
        {
            return _creepTiles.Values.Where(tile => tile.IsActive).ToList().AsReadOnly();
        }
        
        /// <summary>
        /// 获取菌毯瓦片总数
        /// </summary>
        public int GetCreepTileCount()
        {
            return _creepTiles.Count(kvp => kvp.Value.IsActive);
        }
        
        /// <summary>
        /// 获取菌毯覆盖的总面积
        /// </summary>
        public float GetTotalCreepArea()
        {
            return GetCreepTileCount() * _tileSize * _tileSize;
        }
        
        #endregion
        
        #region 区域查询
        
        /// <summary>
        /// 获取指定区域内的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetCreepTilesInArea(Vector2Int center, int radius)
        {
            var tiles = new List<CreepTile>();
            
            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    var position = new Vector2Int(x, y);
                    if (Vector2Int.Distance(center, position) <= radius)
                    {
                        var tile = GetCreepTileAt(position);
                        if (tile != null && tile.IsActive)
                        {
                            tiles.Add(tile);
                        }
                    }
                }
            }
            
            return tiles;
        }
        
        /// <summary>
        /// 获取菌毯边缘位置
        /// </summary>
        public List<Vector2Int> GetCreepEdgePositions()
        {
            var edgePositions = new List<Vector2Int>();
            
            foreach (var position in _creepTiles.Keys)
            {
                if (IsEdgePosition(position))
                {
                    edgePositions.Add(position);
                }
            }
            
            return edgePositions;
        }
        
        #endregion
        
        #region 状态查询
        
        /// <summary>
        /// 获取指定状态的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetCreepTilesByStatus(CreepTileStatus status)
        {
            return _creepTiles.Values.Where(tile => tile.Status == status && tile.IsActive).ToList();
        }
        
        /// <summary>
        /// 获取健康的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetHealthyCreepTiles()
        {
            return GetCreepTilesByStatus(CreepTileStatus.Healthy);
        }
        
        /// <summary>
        /// 获取受损的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetDamagedCreepTiles()
        {
            return _creepTiles.Values.Where(tile => 
                tile.IsActive && tile.Health < tile.MaxHealth).ToList();
        }
        
        /// <summary>
        /// 获取正在成长的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetGrowingCreepTiles()
        {
            return GetCreepTilesByStatus(CreepTileStatus.Growing);
        }
        
        #endregion
        
        #region 类型查询
        
        /// <summary>
        /// 获取指定类型的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetCreepTilesByType(CreepTileType tileType)
        {
            return _creepTiles.Values.Where(tile => 
                tile.TileType == tileType && tile.IsActive).ToList();
        }
        
        /// <summary>
        /// 获取特化菌毯瓦片
        /// </summary>
        public List<CreepTile> GetSpecializedCreepTiles()
        {
            return GetCreepTilesByType(CreepTileType.Specialized);
        }
        
        #endregion
        
        #region 连接查询
        
        /// <summary>
        /// 检查两个位置是否通过菌毯连接
        /// </summary>
        public bool ArePositionsConnected(Vector2Int pos1, Vector2Int pos2)
        {
            var tile1 = GetCreepTileAt(pos1);
            var tile2 = GetCreepTileAt(pos2);
            
            if (tile1 == null || tile2 == null)
                return false;
                
            return FindPath(tile1, tile2) != null;
        }
        
        /// <summary>
        /// 获取菌毯网络中的所有连通区域
        /// </summary>
        public List<List<CreepTile>> GetConnectedRegions()
        {
            var regions = new List<List<CreepTile>>();
            var visited = new HashSet<CreepTile>();
            
            foreach (var tile in _creepTiles.Values)
            {
                if (!visited.Contains(tile) && tile.IsActive)
                {
                    var region = GetConnectedRegion(tile, visited);
                    if (region.Count > 0)
                    {
                        regions.Add(region);
                    }
                }
            }
            
            return regions;
        }
        
        /// <summary>
        /// 获取孤立的菌毯区域
        /// </summary>
        public List<List<CreepTile>> GetIsolatedRegions()
        {
            var allRegions = GetConnectedRegions();
            var mainRegion = allRegions.OrderByDescending(r => r.Count).FirstOrDefault();
            
            return allRegions.Where(region => region != mainRegion).ToList();
        }
        
        #endregion
        
        #region 建筑相关查询
        
        /// <summary>
        /// 获取建筑周围的菌毯瓦片
        /// </summary>
        public List<CreepTile> GetCreepAroundBuilding(BuildingData building, float radius = 2f)
        {
            if (building == null)
                return new List<CreepTile>();
                
            var buildingGridPos = WorldToGridPosition(building.Position);
            return GetCreepTilesInArea(buildingGridPos, Mathf.CeilToInt(radius));
        }
        
        /// <summary>
        /// 检查建筑是否在菌毯上
        /// </summary>
        public bool IsBuildingOnCreep(BuildingData building)
        {
            if (building == null)
                return false;
                
            var gridPos = WorldToGridPosition(building.Position);
            return HasCreepAt(gridPos);
        }
        
        /// <summary>
        /// 获取需要菌毯支持的建筑位置
        /// </summary>
        public List<Vector2Int> GetRequiredCreepPositionsForBuilding(BuildingData building)
        {
            var positions = new List<Vector2Int>();
            
            if (building == null)
                return positions;
                
            var centerPos = WorldToGridPosition(building.Position);
            var requiredRadius = GetBuildingCreepRequirement(building.BuildingType);
            
            return GetPositionsInRadius(centerPos, (float)requiredRadius);
        }
        
        #endregion
        
        #region 统计查询
        
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 检查位置是否为边缘位置
        /// </summary>
        private bool IsEdgePosition(Vector2Int position)
        {
            var neighbors = GetNeighborPositions(position);
            return neighbors.Any(neighbor => !HasCreepAt(neighbor));
        }
        
        /// <summary>
        /// 获取连通区域
        /// </summary>
        private List<CreepTile> GetConnectedRegion(CreepTile startTile, HashSet<CreepTile> visited)
        {
            var region = new List<CreepTile>();
            var queue = new Queue<CreepTile>();
            
            queue.Enqueue(startTile);
            visited.Add(startTile);
            
            while (queue.Count > 0)
            {
                var currentTile = queue.Dequeue();
                region.Add(currentTile);
                
                foreach (var connectedTile in currentTile.ConnectedTiles)
                {
                    if (!visited.Contains(connectedTile) && connectedTile.IsActive)
                    {
                        visited.Add(connectedTile);
                        queue.Enqueue(connectedTile);
                    }
                }
            }
            
            return region;
        }
        
        /// <summary>
        /// 寻找两个瓦片之间的路径
        /// </summary>
        private List<CreepTile> FindPath(CreepTile start, CreepTile end)
        {
            // 简单的BFS路径查找
            var queue = new Queue<CreepTile>();
            var visited = new HashSet<CreepTile>();
            var parent = new Dictionary<CreepTile, CreepTile>();
            
            queue.Enqueue(start);
            visited.Add(start);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                
                if (current == end)
                {
                    // 重建路径
                    var path = new List<CreepTile>();
                    var node = end;
                    
                    while (node != null)
                    {
                        path.Add(node);
                        parent.TryGetValue(node, out node);
                    }
                    
                    path.Reverse();
                    return path;
                }
                
                foreach (var neighbor in current.ConnectedTiles)
                {
                    if (!visited.Contains(neighbor) && neighbor.IsActive)
                    {
                        visited.Add(neighbor);
                        parent[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
            
            return null; // 无路径
        }
        
        /// <summary>
        /// 获取建筑的菌毯需求半径
        /// </summary>
        private int GetBuildingCreepRequirement(BuildingType buildingType)
        {
            // 根据建筑类型返回所需的菌毯覆盖半径
            return buildingType switch
            {
                BuildingType.SpawningPool => 2,        // 孵化池需求半径2 (包含Hatchery别名)
                BuildingType.EvolutionChamber => 1,    // 进化腔需求半径1
                BuildingType.ResourceProcessor => 0,   // 资源处理器需求半径0 (包含Extractor别名)
                BuildingType.CreepNode => 0,           // 菌毯节点需求半径0 (包含CreepTumor别名)
                _ => 1
            };
        }
        
        /// <summary>
        /// 获取指定半径内的所有位置
        /// </summary>
        private List<Vector2Int> GetPositionsInRadius(Vector2Int center, float radius)
        {
            var positions = new List<Vector2Int>();
            int intRadius = Mathf.CeilToInt(radius);
            
            for (int x = -intRadius; x <= intRadius; x++)
            {
                for (int y = -intRadius; y <= intRadius; y++)
                {
                    var pos = center + new Vector2Int(x, y);
                    if (Vector2.Distance(center, pos) <= radius)
                    {
                        positions.Add(pos);
                    }
                }
            }
            
            return positions;
        }
        
        /// <summary>
        /// 获取附近的建筑
        /// </summary>
        private List<BuildingData> GetNearbyBuildings(Vector2Int position, float radius)
        {
            var buildings = new List<BuildingData>();
            
            if (_buildingManager != null)
            {
                var worldPos = GridToWorldPosition(position);
                // 这里需要调用建筑管理器的方法来获取附近建筑
                // buildings = _buildingManager.GetBuildingsInRadius(worldPos, radius);
            }
            
            return buildings;
        }
        
        /// <summary>
        /// 获取附近的资源点
        /// </summary>
        private List<Vector3> GetNearbyResources(Vector2Int position, float radius)
        {
            var resources = new List<Vector3>();
            
            // TODO: 实现资源管理器集成
            // 暂时返回空列表，待资源管理器实现后修复
            var worldPos = GridToWorldPosition(position);
            // resources = ResourceManager.Instance?.GetResourcesInRadius(worldPos, radius) ?? new List<Vector3>();
            
            return resources;
        }
        
        #endregion
    }
    
}