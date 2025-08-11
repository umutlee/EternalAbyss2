using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 菌毯管理器接口
    /// 定义菌毯系统对外提供的所有功能
    /// </summary>
    public interface ICreepManager
    {
        #region 事件
        
        /// <summary>
        /// 菌毯扩张事件
        /// </summary>
        event Action<Vector2Int, CreepTile> OnCreepExpanded;
        
        /// <summary>
        /// 菌毯移除事件
        /// </summary>
        event Action<Vector2Int> OnCreepRemoved;
        
        /// <summary>
        /// 菌毯瓦片状态变化事件
        /// </summary>
        event Action<CreepTile> OnCreepTileStatusChanged;
        
        #endregion
        
        #region 基础功能
        
        /// <summary>
        /// 在指定位置创建菌毯源点
        /// </summary>
        bool CreateCreepSource(Vector3 worldPosition, float radius, int ownerId);
        
        /// <summary>
        /// 移除指定位置的菌毯
        /// </summary>
        bool RemoveCreepAt(Vector3 worldPosition);
        
        /// <summary>
        /// 检查指定位置是否有菌毯
        /// </summary>
        bool HasCreepAt(Vector3 worldPosition);
        
        /// <summary>
        /// 获取菌毯密度
        /// </summary>
        float GetCreepDensityAt(Vector3 worldPosition);
        
        #endregion
        
        #region 建筑相关
        
        /// <summary>
        /// 检查位置是否可以放置建筑
        /// </summary>
        bool CanPlaceBuildingAt(Vector3 worldPosition, BuildingType buildingType);
        
        /// <summary>
        /// 检查建筑是否在菌毯上
        /// </summary>
        bool IsBuildingOnCreep(BuildingData building);
        
        /// <summary>
        /// 扩张菌毯到建筑周围
        /// </summary>
        bool ExpandAroundBuilding(BuildingData building, float radius = 2f);
        
        /// <summary>
        /// 获取建筑周围的菌毯瓦片
        /// </summary>
        List<CreepTile> GetCreepAroundBuilding(BuildingData building, float radius = 2f);
        
        #endregion
        
        #region 查询功能
        
        /// <summary>
        /// 获取指定区域内的菌毯瓦片
        /// </summary>
        List<CreepTile> GetCreepTilesInArea(Vector2Int center, int radius);
        
        /// <summary>
        /// 获取菌毯瓦片总数
        /// </summary>
        int GetCreepTileCount();
        
        /// <summary>
        /// 获取菌毯覆盖的总面积
        /// </summary>
        float GetTotalCreepArea();
        
        /// <summary>
        /// 检查区域是否完全被菌毯覆盖
        /// </summary>
        bool IsAreaFullyCovered(Vector2Int center, int radius);
        
        #endregion
        
        #region 扩张控制
        
        /// <summary>
        /// 手动扩张菌毯到指定位置
        /// </summary>
        bool RequestExpansion(Vector2Int targetPosition, bool ignoreResourceCost = false);
        
        /// <summary>
        /// 批量扩张菌毯到多个位置
        /// </summary>
        int RequestBatchExpansion(List<Vector2Int> targetPositions, bool ignoreResourceCost = false);
        
        #endregion
        
        #region 系统控制
        
        /// <summary>
        /// 强制更新菌毯系统
        /// </summary>
        void ForceUpdate();
        
        #endregion
    }
}