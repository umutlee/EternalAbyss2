using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建筑查询服务接口
    /// 提供所有建筑相关的只读查询功能
    /// </summary>
    public interface IBuildingQueryService : IQueryService
    {
        /// <summary>
        /// 获取指定范围内的建筑
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>建筑数据列表</returns>
        List<BuildingData> GetBuildingsInRange(Vector3 center, float radius, int playerId = -1);

        /// <summary>
        /// 获取指定类型的建筑
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>建筑数据列表</returns>
        List<BuildingData> GetBuildingsOfType(BuildingType buildingType, int playerId = -1);

        /// <summary>
        /// 获取玩家的所有建筑
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>建筑数据列表</returns>
        List<BuildingData> GetPlayerBuildings(int playerId);

        /// <summary>
        /// 获取建筑数据
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>建筑数据，如果不存在返回null</returns>
        BuildingData? GetBuildingData(int buildingId);

        /// <summary>
        /// 检查建筑是否存在
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否存在</returns>
        bool BuildingExists(int buildingId);

        /// <summary>
        /// 获取建筑数量统计
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>建筑数量统计</returns>
        Dictionary<BuildingType, int> GetBuildingCounts(int playerId);

        /// <summary>
        /// 检查位置是否可以放置建筑
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否可以放置</returns>
        bool CanPlaceBuilding(BuildingType buildingType, Vector3 position, int playerId);

        /// <summary>
        /// 获取建筑放置验证结果
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>验证结果</returns>
        PlacementValidationResult ValidateBuildingPlacement(BuildingType buildingType, Vector3 position, int playerId);

        /// <summary>
        /// 获取最近的指定类型建筑
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的建筑ID，如果没有返回-1</returns>
        int GetNearestBuilding(Vector3 position, BuildingType buildingType, int playerId, float maxDistance = float.MaxValue);

        /// <summary>
        /// 获取建筑的生产队列
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>生产队列</returns>
        List<ProductionItem> GetProductionQueue(int buildingId);

        /// <summary>
        /// 获取建筑的当前状态
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>建筑状态</returns>
        BuildingState GetBuildingState(int buildingId);

        /// <summary>
        /// 获取建筑模板
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <returns>建筑模板</returns>
        BuildingTemplate GetBuildingTemplate(BuildingType buildingType);

        /// <summary>
        /// 获取建筑的影响范围
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>影响范围半径</returns>
        float GetBuildingInfluenceRadius(int buildingId);
    }

    /// <summary>
    /// 建筑放置验证结果
    /// </summary>
    public struct PlacementValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public PlacementError ErrorType;
    }

    /// <summary>
    /// 放置错误类型
    /// </summary>
    public enum PlacementError
    {
        None,
        TerrainNotSuitable,
        ObstacleBlocking,
        InsufficientResources,
        NoCreepCoverage,
        TooCloseToEnemy,
        RequiredTechMissing,
        RequiredBuildingMissing,
        OutOfBounds
    }

    /// <summary>
    /// 生产项目
    /// </summary>
    public struct ProductionItem
    {
        public string ItemId;
        public string ItemName;
        public float Progress;
        public float TotalTime;
        public bool IsCompleted;
    }
}