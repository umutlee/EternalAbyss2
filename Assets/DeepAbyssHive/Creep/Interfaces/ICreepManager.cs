using System;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Core.Interfaces;

namespace DeepAbyssHive.Creep.Interfaces
{
    /// <summary>
    /// 菌毯管理器接口
    /// </summary>
    public interface ICreepManager : IManager
    {
        /// <summary>
        /// 菌毯扩张事件
        /// </summary>
        event Action<Vector3, float> OnCreepExpanded;
        
        /// <summary>
        /// 菌毯移除事件
        /// </summary>
        event Action<Vector3, float> OnCreepRemoved;
        
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
        /// 检查位置是否可以放置建筑
        /// </summary>
        bool CanPlaceBuildingAt(Vector3 worldPosition, BuildingType buildingType);
        
        /// <summary>
        /// 获取菌毯密度
        /// </summary>
        float GetCreepDensityAt(Vector3 worldPosition);
        
        /// <summary>
        /// 强制更新菌毯系统
        /// </summary>
        void ForceUpdate();
    }
}
