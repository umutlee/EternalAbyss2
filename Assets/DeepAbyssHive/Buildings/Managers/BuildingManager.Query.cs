using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 查询功能 - 委托给 IBuildingQueryService
    /// 保持向后兼容的API，内部委托给查询服务处理
    /// </summary>
    public partial class BuildingManager
    {
        /// <summary>
        /// 获取指定类型的建筑（委托给查询服务）
        /// </summary>
        public List<BuildingData> GetBuildingsOfType(BuildingType buildingType, int playerId = -1)
        {
            return _queryService?.GetBuildingsOfType(buildingType, playerId) ?? new List<BuildingData>();
        }

        /// <summary>
        /// 获取玩家的所有建筑（委托给查询服务）
        /// </summary>
        public List<BuildingData> GetPlayerBuildings(int playerId)
        {
            return _queryService?.GetPlayerBuildings(playerId) ?? new List<BuildingData>();
        }

        /// <summary>
        /// 获取建筑数量统计（委托给查询服务）
        /// </summary>
        public Dictionary<BuildingType, int> GetBuildingCounts(int playerId)
        {
            return _queryService?.GetBuildingCounts(playerId) ?? new Dictionary<BuildingType, int>();
        }

        /// <summary>
        /// 检查位置是否可以放置建筑（委托给查询服务）
        /// </summary>
        public bool CanPlaceBuilding(BuildingType buildingType, Vector3 position, int playerId)
        {
            return _queryService?.CanPlaceBuilding(buildingType, position, playerId) ?? false;
        }

        /// <summary>
        /// 获取最近的指定类型建筑（委托给查询服务）
        /// </summary>
        public int GetNearestBuilding(Vector3 position, BuildingType buildingType, int playerId, float maxDistance = float.MaxValue)
        {
            return _queryService?.GetNearestBuilding(position, buildingType, playerId, maxDistance) ?? -1;
        }

        /// <summary>
        /// 获取建筑的影响范围（委托给查询服务）
        /// </summary>
        public float GetBuildingInfluenceRadius(int buildingId)
        {
            return _queryService?.GetBuildingInfluenceRadius(buildingId) ?? 0f;
        }

        /// <summary>

        /// <summary>
        /// 获取建筑状态（委托给查询服务）
        /// </summary>
        public BuildingState GetBuildingState(int buildingId)
        {
            return _queryService?.GetBuildingState(buildingId) ?? BuildingState.Destroyed;
        }
    }
}