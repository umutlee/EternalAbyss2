using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Logging;
using ICreepManager = DeepAbyssHive.Creep.Interfaces.ICreepManager;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 查询/放置校验模块
    /// 负责建筑查询、放置验证和菌毯集成逻辑
    /// </summary>
    public partial class BuildingManager
    {
        #region 菌毯系统依赖
        
        private ICreepManager _creepManager;
        
        /// <summary>
        /// 设置菌毯管理器依赖
        /// </summary>
        public void SetCreepManager(ICreepManager creepManager)
        {
            _creepManager = creepManager;
            
            // 订阅菌毯事件
            if (_creepManager != null)
            {
                _creepManager.OnCreepExpanded += OnCreepExpanded;
                _creepManager.OnCreepRemoved += OnCreepRemoved;
            }
        }
        
        #endregion
        
        #region 建筑放置验证
        
        /// <summary>
        /// 检查位置是否可以放置建筑（契约方法）
        /// </summary>
        public bool IsValidPlacement(Vector3 position, Vector2Int size, bool requiresCreep)
        {
            // 基础地形检查
            if (!IsValidTerrain(position, size))
                return false;
                
            // 检查是否与现有建筑冲突
            if (HasBuildingConflict(position, size))
                return false;
                
            // 菌毯依赖检查
            if (requiresCreep && !HasCreepSupport(position, size))
                return false;
                
            return true;
        }
        
        /// <summary>
        /// 检查是否有菌毯支持
        /// </summary>
        private bool HasCreepSupport(Vector3 position, Vector2Int size)
        {
            if (_creepManager == null)
                return !RequiresCreepByDefault(); // 如果没有菌毯系统，根据默认设置决定
                
            // 检查建筑占用区域是否完全被菌毯覆盖
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    Vector3 checkPos = position + new Vector3(x, 0, y);
                    if (!_creepManager.HasCreepAt(checkPos))
                        return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// 检查建筑类型是否需要菌毯支持
        /// </summary>
        private bool RequiresCreepSupport(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Hatchery:
                    return false;        // 孵化场不需要菌毯
                case BuildingType.Extractor:
                    return false;       // 提取器不需要菌毯
                case BuildingType.CreepTumor:
                    return false;      // 菌毯肿瘤不需要菌毯
                case BuildingType.SpawningPool:
                    return true;     // 孵化池需要菌毯
                case BuildingType.EvolutionChamber:
                    return true; // 进化腔需要菌毯
                default:
                    return true;                               // 其他建筑都需要菌毯
            }
        }
        
        /// <summary>
        /// 默认是否需要菌毯支持
        /// </summary>
        private bool RequiresCreepByDefault()
        {
            return false; // 默认不需要菌毯支持，避免在菌毯系统未初始化时阻止建造
        }
        
        #endregion
        
        #region 菌毯扩张支持
        
        /// <summary>
        /// 获取菌毯扩张半径（契约方法）
        /// </summary>
        public float GetCreepExpansionRadius(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out var building))
                return 0f;
                
            return GetCreepExpansionRadius(building.BuildingType);
        }
        
        /// <summary>
        /// 根据建筑类型获取菌毯扩张半径
        /// </summary>
        private float GetCreepExpansionRadius(BuildingType buildingType)
        {
            switch (buildingType)
            {
                case BuildingType.Hatchery:
                    return 8f;           // 孵化场扩张半径8格
                case BuildingType.SpawningPool:
                    return 8f;           // 孵化池扩张半径8格
                case BuildingType.CreepTumor:
                    return 4f;           // 菌毯肿瘤扩张半径4格
                case BuildingType.EvolutionChamber:
                    return 2f;           // 进化腔扩张半径2格
                default:
                    return 0f;           // 其他建筑不扩张菌毯
            }
        }
        
        /// <summary>
        /// 检查建筑是否产生菌毯
        /// </summary>
        private bool IsCreepProducingBuilding(BuildingType buildingType)
        {
            return GetCreepExpansionRadius(buildingType) > 0f;
        }
        
        #endregion
        
        #region 菌毯事件处理
        
        /// <summary>
        /// 菌毯扩张事件处理
        /// </summary>
        private void OnCreepExpanded(Vector3 position, float radius)
        {
            // 检查新扩张的菌毯位置是否有等待菌毯的建筑
            var nearbyBuildings = GetBuildingsInArea(position, radius);
            
            foreach (var building in nearbyBuildings)
            {
                if (RequiresCreepSupport(building.BuildingType) && 
                    building.State == BuildingState.Paused) // 假设缺少菌毯时建筑暂停
                {
                    // 恢复建筑运行
                    building.State = BuildingState.Operational;
                    DAHLog.Info(LogCategory.BUILDINGS, $"[BuildingManager] 建筑 {building.BuildingId} 因菌毯扩张恢复运行");
                }
            }
        }
        
        /// <summary>
        /// 菌毯移除事件处理
        /// </summary>
        private void OnCreepRemoved(Vector3 position, float radius)
        {
            // 检查菌毯移除位置是否有依赖菌毯的建筑
            var nearbyBuildings = GetBuildingsInArea(position, radius);
            
            foreach (var building in nearbyBuildings)
            {
                if (RequiresCreepSupport(building.BuildingType) && 
                    building.State == BuildingState.Operational)
                {
                    // 暂停建筑运行
                    building.State = BuildingState.Paused;
                    DAHLog.Info(LogCategory.BUILDINGS, $"[BuildingManager] 建筑 {building.BuildingId} 因菌毯移除暂停运行");
                }
            }
        }
        
        #endregion
        
        #region 菌毯状态查询
        
        /// <summary>
        /// 检查建筑是否在菌毯上
        /// </summary>
        public bool IsBuildingOnCreep(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out var building))
                return false;
                
            return _creepManager?.HasCreepAt(building.Position) ?? false;
        }
        
        /// <summary>
        /// 获取建筑位置的菌毯密度
        /// </summary>
        public float GetCreepDensityAtBuilding(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out var building))
                return 0f;
                
            return _creepManager?.GetCreepDensityAt(building.Position) ?? 0f;
        }
        
        /// <summary>
        /// 获取需要菌毯但缺少菌毯的建筑
        /// </summary>
        public List<BuildingData> GetBuildingsLackingCreep()
        {
            var result = new List<BuildingData>();
            
            if (_creepManager == null)
                return result;
                
            foreach (var building in _buildings.Values)
            {
                if (RequiresCreepSupport(building.BuildingType) && 
                    !_creepManager.HasCreepAt(building.Position))
                {
                    result.Add(building);
                }
            }
            
            return result;
        }
        
        #endregion
        
        #region 基础查询方法
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
        /// 检查建筑是否重叠（Vector3版本）
        /// </summary>
        /// <param name="pos1">位置1</param>
        /// <param name="size1">大小1</param>
        /// <param name="pos2">位置2</param>
        /// <param name="size2">大小2</param>
        /// <returns>是否重叠</returns>
        private bool IsBuildingOverlappingVector3(Vector3 pos1, Vector3 size1, Vector3 pos2, Vector3 size2)
        {
            Bounds bounds1 = new Bounds(pos1, size1);
            Bounds bounds2 = new Bounds(pos2, size2);
            
            return bounds1.Intersects(bounds2);
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
                
                if (IsBuildingOverlappingVector3(position, buildingSize, existingBuilding.Position, existingSize))
                {
                    return false;
                }
            }
            
            return true;
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
        /// 获取区域内的建筑
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>建筑数据列表</returns>
        private List<BuildingData> GetBuildingsInArea(Vector3 center, float radius)
        {
            return _buildings.Values.Where(building => 
                Vector3.Distance(building.Position, center) <= radius).ToList();
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 检查地形是否有效
        /// </summary>
        private bool IsValidTerrain(Vector3 position, Vector2Int size)
        {
            // 简化实现，实际项目中需要完整的地形检查
            // 这里可以检查地形高度、坡度、材质等
            return true;
        }
        
        /// <summary>
        /// 检查是否与现有建筑冲突
        /// </summary>
        private bool HasBuildingConflict(Vector3 position, Vector2Int size)
        {
            // 检查指定区域是否与现有建筑重叠
            var nearbyBuildings = GetBuildingsInArea(position, Mathf.Max(size.x, size.y) + 1f);
            
            foreach (var building in nearbyBuildings)
            {
                if (IsBuildingOverlapping(position, size, building.Position, building.Size))
                    return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// 检查两个建筑是否重叠
        /// </summary>
        private bool IsBuildingOverlapping(Vector3 pos1, Vector2Int size1, Vector3 pos2, Vector2Int size2)
        {
            // 简化的矩形重叠检查
            float left1 = pos1.x;
            float right1 = pos1.x + size1.x;
            float bottom1 = pos1.z;
            float top1 = pos1.z + size1.y;
            
            float left2 = pos2.x;
            float right2 = pos2.x + size2.x;
            float bottom2 = pos2.z;
            float top2 = pos2.z + size2.y;
            
            return !(right1 <= left2 || right2 <= left1 || top1 <= bottom2 || top2 <= bottom1);
        }
        
        #endregion
    }
}
