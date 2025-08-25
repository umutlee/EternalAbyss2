using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Creep.Data;
// 使用别名解决枚举冲突
using DataNS = DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Enums;
using System;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器主文件
    /// 负责菌毯系统的整体管理和协调
    /// 实现ICreepManager接口的具体方法
    /// </summary>
    public partial class CreepManager : MonoBehaviour, ICreepManager
    {
        #region ICreepManager接口实现
        
        /// <summary>
        /// 菌毯扩张事件
        /// </summary>
        public event Action<Vector3, float> OnCreepExpanded;
        
        /// <summary>
        /// 菌毯移除事件
        /// </summary>
        public event Action<Vector3, float> OnCreepRemoved;
        
        /// <summary>
        /// 创建菌毯源点
        /// </summary>
        public bool CreateCreepSource(Vector3 position, float radius, int ownerId)
        {
            // 调用Sources模块的方法创建源点
            var sourceId = CreateCreepSourcePoint(position, radius, ownerId, DataNS.DeepAbyssHive.Creep.Compat.CreepSourceTypeCompat.Basic);
            return sourceId != -1;
        }
        
        /// <summary>
        /// 移除指定位置的菌毯
        /// </summary>
        public bool RemoveCreepAt(Vector3 position)
        {
            var gridPos = WorldToGridPosition(position);
            if (_creepTiles.ContainsKey(gridPos))
            {
                RemoveCreepTile(gridPos);
                OnCreepRemoved?.Invoke(position, _gridCellSize);
                return true;
            }
            return false;
        }
        
        /// <summary>
        /// 检查指定位置是否有菌毯
        /// </summary>
        public bool HasCreepAt(Vector3 position)
        {
            var gridPos = WorldToGridPosition(position);
            return HasCreepAt(gridPos);
        }
        
        /// <summary>
        /// 检查是否可以在指定位置放置建筑
        /// </summary>
        public bool CanPlaceBuildingAt(Vector3 position, BuildingType buildingType)
        {
            var gridPos = WorldToGridPosition(position);
            
            // 检查是否有菌毯覆盖
            if (!HasCreepAt(gridPos))
                return false;
                
            // 检查建筑类型的特殊要求
            return CheckBuildingPlacementRequirements(gridPos, buildingType);
        }
        
        /// <summary>
        /// 获取指定位置的菌毯密度
        /// </summary>
        public float GetCreepDensityAt(Vector3 position)
        {
            var gridPos = WorldToGridPosition(position);
            var tile = GetCreepTileAt(gridPos);
            
            if (tile == null || !tile.IsActive)
                return 0f;
                
            return tile.GrowthLevel / tile.MaxGrowthLevel;
        }
        
        /// <summary>
        /// 强制更新菌毯系统
        /// </summary>
        public void ForceUpdate()
        {
            if (!IsInitialized) return;
            
            // 强制更新所有菌毯瓦片
            foreach (var tile in _creepTiles.Values)
            {
                tile.NeedsUpdate = true;
            }
            
            // 立即处理更新
            ProcessCreepUpdates();
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 检查建筑放置要求
        /// </summary>
        private bool CheckBuildingPlacementRequirements(Vector2Int position, BuildingType buildingType)
        {
            // 根据建筑类型检查特殊要求
            switch (buildingType)
            {
                case BuildingType.SpawningPool:
                    // 孵化池需要周围有足够的菌毯覆盖
                    return IsAreaFullyCovered(position, 1);
                    
                case BuildingType.EvolutionChamber:
                    // 进化腔需要连接到主菌毯网络
                    return IsConnectedToMainNetwork(position);
                    
                case BuildingType.CreepNode:
                    // 菌毯节点可以放置在任何有菌毯的位置
                    return true;
                    
                default:
                    // 其他建筑的默认要求
                    return true;
            }
        }
        
        /// <summary>
        /// 检查位置是否连接到主菌毯网络
        /// </summary>
        private bool IsConnectedToMainNetwork(Vector2Int position)
        {
            var tile = GetCreepTileAt(position);
            if (tile == null) return false;
            
            // 简化实现：检查是否有连接的瓦片
            return tile.ConnectedTiles.Count > 0;
        }
        
        #endregion
    }
}
