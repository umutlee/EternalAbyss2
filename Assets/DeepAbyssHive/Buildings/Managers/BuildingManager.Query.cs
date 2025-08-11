using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 查询/放置校验
    /// 说明：
    /// - 本文件为partial占位，不改变任何对外API与行为
    /// - 后续将把范围/类型查询与放置校验等方法迁移至此：
    ///   - int[] GetBuildingsInRange(Vector3 position, float radius)
    ///   - int[] GetBuildingsOfType(BuildingType type, int ownerId)
    ///   - bool IsValidPlacement(Vector3 position, Vector2Int size, bool requiresCreep)
    ///   - bool CanPlaceBuildingAt(Vector3 position, Vector2Int size)
    ///   - float GetCreepExpansionRadius(int buildingId)
    ///   - 私有：IsPositionOnGrid / IsBuildingOverlapping
    /// </summary>
    public partial class BuildingManager
    {
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

    }
}