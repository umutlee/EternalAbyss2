using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 建筑管理器接口
    /// 定义建筑系统对外提供的所有功能
    /// </summary>
    public interface IBuildingManager
    {
        #region 事件
        
        /// <summary>
        /// 建筑放置事件
        /// </summary>
        event Action<BuildingData> OnBuildingPlaced;
        
        /// <summary>
        /// 建筑销毁事件
        /// </summary>
        event Action<BuildingData> OnBuildingDestroyed;
        
        /// <summary>
        /// 建筑状态变化事件
        /// </summary>
        event Action<BuildingData> OnBuildingStatusChanged;
        
        /// <summary>
        /// 建筑升级完成事件
        /// </summary>
        event Action<BuildingData> OnBuildingUpgraded;
        
        #endregion
        
        #region 核心建筑操作（契约锁定）
        
        /// <summary>
        /// 创建建筑
        /// </summary>
        int CreateBuilding(BuildingData buildingData);
        
        /// <summary>
        /// 获取建筑数据
        /// </summary>
        BuildingData GetBuildingData(int buildingId);
        
        /// <summary>
        /// 更新建筑
        /// </summary>
        void UpdateBuilding(BuildingData buildingData);
        
        /// <summary>
        /// 移除建筑
        /// </summary>
        void RemoveBuilding(int buildingId);
        
        /// <summary>
        /// 检查位置是否可以放置建筑
        /// </summary>
        bool IsValidPlacement(Vector3 position, Vector2Int size, bool requiresCreep);
        
        #endregion
        
        #region 建筑生命周期（契约锁定）
        
        /// <summary>
        /// 开始建造
        /// </summary>
        void StartConstruction(int buildingId);
        
        /// <summary>
        /// 开始升级
        /// </summary>
        void StartUpgrade(int buildingId, string upgradePathId);
        
        #endregion
        
        #region 生产系统（契约锁定）
        
        /// <summary>
        /// 添加生产队列项
        /// </summary>
        void AddProductionQueueItem(int buildingId, ProductionQueueItem productionItem);
        
        /// <summary>
        /// 取消生产队列项
        /// </summary>
        void CancelProductionQueueItem(int buildingId, int queueIndex);
        
        #endregion
        
        #region 研究系统（契约锁定）
        
        /// <summary>
        /// 开始研究
        /// </summary>
        void StartResearch(int buildingId, string researchId);
        
        /// <summary>
        /// 取消研究
        /// </summary>
        void CancelResearch(int buildingId);
        
        #endregion
        
        #region 菌毯相关（契约锁定）
        
        /// <summary>
        /// 获取菌毯扩张半径
        /// </summary>
        float GetCreepExpansionRadius(int buildingId);
        
        #endregion
        
        #region 扩展查询功能
        
        /// <summary>
        /// 放置建筑
        /// </summary>
        bool PlaceBuilding(BuildingType buildingType, Vector3 position, int ownerId);
        
        /// <summary>
        /// 销毁建筑
        /// </summary>
        bool DestroyBuilding(int buildingId);
        
        /// <summary>
        /// 升级建筑
        /// </summary>
        bool UpgradeBuilding(int buildingId);
        
        /// <summary>
        /// 检查是否可以放置建筑
        /// </summary>
        bool CanPlaceBuilding(BuildingType buildingType, Vector3 position);
        
        /// <summary>
        /// 获取指定位置的建筑
        /// </summary>
        BuildingData GetBuildingAt(Vector3 position);
        
        /// <summary>
        /// 获取指定区域内的建筑
        /// </summary>
        List<BuildingData> GetBuildingsInArea(Vector3 center, float radius);
        
        /// <summary>
        /// 获取指定类型的所有建筑
        /// </summary>
        List<BuildingData> GetBuildingsByType(BuildingType buildingType);
        
        /// <summary>
        /// 获取玩家的所有建筑
        /// </summary>
        List<BuildingData> GetPlayerBuildings(int playerId);
        
        #endregion
    }
}