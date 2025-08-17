using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Buildings.Enums;
using System;
using System.Collections.Generic;

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
        /// 菌毯擴張事件
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
            if (!IsInitialized) return false;
            
            // 委托给源点服务
            var sourceId = _sourceService.CreateCreepSource(position, ownerId, CreepSourceType.Building, radius);
            return sourceId != -1;
        }
        
        /// <summary>
        /// 移除指定位置的菌毯
        /// </summary>
        public bool RemoveCreepAt(Vector3 position)
        {
            if (!IsInitialized) return false;
            
            // 委托给模拟服务
            bool success = _simulationService.ClearCreep(position, 0.5f);
            if (success)
            {
                OnCreepRemoved?.Invoke(position, _gridService.GridCellSize);
            }
            return success;
        }
        
        /// <summary>
        /// 检查指定位置是否有菌毯
        /// </summary>
        public bool HasCreepAt(Vector3 position)
        {
            if (!IsInitialized) return false;
            
            // 委托给网格服务
            var gridPos = _gridService.WorldToGridPosition(position);
            return _gridService.HasCreepAt(gridPos);
        }
        
        /// <summary>
        /// 检查是否可以在指定位置放置建筑
        /// </summary>
        public bool CanPlaceBuildingAt(Vector3 position, BuildingType buildingType)
        {
            if (!IsInitialized) return false;
            
            // 委托给查询服务
            var gridPos = _gridService.WorldToGridPosition(position);
            
            // 检查是否有菌毯覆盖
            if (!_gridService.HasCreepAt(gridPos))
                return false;
                
            // 检查建筑类型的特殊要求
            return CheckBuildingPlacementRequirements(gridPos, buildingType);
        }
        
        /// <summary>
        /// 获取指定位置的菌毯密度
        /// </summary>
        public float GetCreepDensityAt(Vector3 position)
        {
            if (!IsInitialized) return 0f;
            
            // 委托给查询服务
            var gridPos = _gridService.WorldToGridPosition(position);
            
            if (!_gridService.HasCreepAt(gridPos))
                return 0f;
                
            var data = _gridService.GetGridCell(gridPos);
            return data.Strength;
        }
        
        /// <summary>
        /// 强制更新菌毯系统
        /// </summary>
        public void ForceUpdate()
        {
            if (!IsInitialized) return;
            
            // 委托给各个服务
            UpdateServices(Time.deltaTime);
            
            // 强制优化网络结构
            foreach (var playerId in GetActivePlayers())
            {
                _networkService.OptimizeNetworkStructure(playerId);
            }
        }
        
        #endregion
        
        #region 辅助方法
        
        /// <summary>
        /// 检查建筑放置要求
        /// </summary>
        private bool CheckBuildingPlacementRequirements(Vector2Int position, BuildingType buildingType)
        {
            if (!IsInitialized) return false;
            
            // 委托给查询服务
            switch (buildingType)
            {
                case BuildingType.SpawningPool:
                    // 孵化池需要周围有足够的菌毯覆盖
                    return _queryService.IsAreaFullyCovered(position, 1);
                    
                case BuildingType.EvolutionChamber:
                    // 进化腔需要连接到主菌毯网络
                    return _queryService.IsConnectedToMainNetwork(position);
                    
                case BuildingType.CreepNode:
                    // 菌毯节点可以放置在任何有菌毯的位置
                    return true;
                    
                default:
                    // 其他建筑的默认要求
                    return true;
            }
        }
        
        /// <summary>
        /// 获取活跃玩家ID列表
        /// </summary>
        private System.Collections.Generic.List<int> GetActivePlayers()
        {
            if (!IsInitialized) return new System.Collections.Generic.List<int>();
            
            // 委托给网络服务
            return _networkService.GetActivePlayerIds();
        }
        
        
        /// <summary>
        /// 获取指定位置的菌毯强度
        /// </summary>
        public float GetCreepStrengthAt(Vector3 position)
        {
            if (!IsInitialized) return 0f;
            
            // 委托给查询服务
            return _queryService.GetCreepStrength(position);
        }
        
        /// <summary>
        /// 获取指定区域内的菌毯覆盖率
        /// </summary>
        public float GetCreepCoverageInArea(Vector3 center, float radius)
        {
            if (!IsInitialized) return 0f;
            
            // 委托给查询服务
            return _queryService.GetCreepCoverageInRange(center, radius);
        }
        
        /// <summary>
        /// 获取菌毯网络的连通性信息
        /// </summary>
        public CreepNetworkInfo GetCreepNetworkInfo(int ownerId)
        {
            if (!IsInitialized) return new CreepNetworkInfo();
            
            // 委托给网络服务
            return _networkService.GetNetworkInfo(ownerId);
        }
        
        #endregion
    }
}
