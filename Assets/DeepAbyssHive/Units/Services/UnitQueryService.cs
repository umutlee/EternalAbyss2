using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 单位查询服务实现
    /// 负责单位的空间查询和筛选操作
    /// </summary>
    public partial class UnitQueryService : IUnitQueryService, IQueryService, IService
    {
        #region 私有字段

        private ISpatialIndexService _spatialIndexService;
        private bool _isInitialized = false;

        #endregion

        #region 属性

        public string ServiceName => "UnitQueryService";
        public bool IsInitialized => _isInitialized;

        #endregion

        #region 构造函数

        public UnitQueryService(ISpatialIndexService spatialIndexService)
        {
            _spatialIndexService = spatialIndexService;
        }

        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (_isInitialized) return;
            
            _isInitialized = true;
        }

        public void Cleanup()
        {
            if (!_isInitialized) return;
            
            _isInitialized = false;
        }

        public void Pause()
        {
            // 查询服务无需暂停逻辑
        }

        public void Resume()
        {
            // 查询服务无需恢复逻辑
        }

        #endregion

        #region IUnitQueryService 实现

        public List<SpatialNode> GetUnitsInRange(Vector3 center, float radius)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            return _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
        }

        public List<SpatialNode> GetUnitsOfType(UnitType unitType)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var allNodes = _spatialIndexService.QueryAll().ToSpatialNodes();
            return allNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return unitData.UnitType == unitType;
                }
                return false;
            }).ToList();
        }

        public List<SpatialNode> GetUnitsOfPlayer(int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var allNodes = _spatialIndexService.QueryAll().ToSpatialNodes();
            return allNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return GetUnitPlayerId(unitData) == playerId;
                }
                return false;
            }).ToList();
        }

        public List<SpatialNode> GetUnitsInRangeOfPlayer(Vector3 center, float radius, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return GetUnitPlayerId(unitData) == playerId;
                }
                return false;
            }).ToList();
        }

        public List<SpatialNode> GetUnitsInRangeOfType(Vector3 center, float radius, UnitType unitType, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return GetUnitPlayerId(unitData) == playerId && unitData.UnitType == unitType;
                }
                return false;
            }).ToList();
        }

        public SpatialNode GetNearestUnit(Vector3 position)
        {
            if (!_isInitialized) return null;
            
            var nearestNodes = _spatialIndexService.QueryNearest(position, 1).ToSpatialNodes();
            return nearestNodes.FirstOrDefault();
        }

        public SpatialNode GetNearestUnitOfType(Vector3 position, UnitType unitType)
        {
            if (!_isInitialized) return null;
            
            var allNodes = _spatialIndexService.QueryAll().ToSpatialNodes();
            var unitsOfType = allNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return unitData.UnitType == unitType;
                }
                return false;
            });

            SpatialNode nearest = null;
            float minDistance = float.MaxValue;

            foreach (var node in unitsOfType)
            {
                float distance = Vector3.Distance(position, node.Position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = node;
                }
            }

            return nearest;
        }

        public List<SpatialNode> GetUnitsInRangeByType(Vector3 center, float radius, UnitType unitType, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData && unitData.UnitType == unitType && GetUnitPlayerId(unitData) == playerId)
                {
                    return true;
                }
                return false;
            }).ToList();
        }

        public List<SpatialNode> GetEnemyUnitsInRange(Vector3 center, float radius, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) != playerId)
                {
                    return true;
                }
                return false;
            }).ToList();
        }

        public List<SpatialNode> GetAlliedUnitsInRange(Vector3 center, float radius, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) == playerId)
                {
                    return true;
                }
                return false;
            }).ToList();
        }

        public bool HasUnitsInRange(Vector3 center, float radius)
        {
            if (!_isInitialized) return false;
            
            var nodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return nodes.Any();
        }

        public bool HasUnitsOfTypeInRange(Vector3 center, float radius, UnitType unitType)
        {
            if (!_isInitialized) return false;
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Any(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return unitData.UnitType == unitType;
                }
                return false;
            });
        }

        public int CountUnitsInRange(Vector3 center, float radius)
        {
            if (!_isInitialized) return 0;
            
            var nodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return nodes.Count;
        }

        public int CountUnitsOfTypeInRange(Vector3 center, float radius, UnitType unitType)
        {
            if (!_isInitialized) return 0;
            
            var spatialNodes = _spatialIndexService.QueryRange(center, radius).ToSpatialNodes();
            return spatialNodes.Count(node => 
            {
                if (node.Data is UnitColdData unitData)
                {
                    return unitData.UnitType == unitType;
                }
                return false;
            });
        }

        public List<SpatialNode> GetUnitsWithinDistance(Vector3 position, float maxDistance)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            return GetUnitsInRange(position, maxDistance);
        }

        public List<SpatialNode> GetVisibleUnits(Vector3 viewerPosition, float viewRange, int viewerPlayerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            var spatialNodes = _spatialIndexService.QueryRange(viewerPosition, viewRange).ToSpatialNodes();
            return spatialNodes.Where(node => 
            {
                if (node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) != viewerPlayerId)
                {
                    // 这里可以添加视线检查逻辑
                    return true;
                }
                return false;
            }).ToList();
        }

        public List<SpatialNode> GetUnitsInArea(Bounds area)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 使用区域中心和最大半径进行查询
            float radius = Mathf.Max(area.size.x, area.size.y, area.size.z) * 0.5f;
            var candidates = _spatialIndexService.QueryRange(area.center, radius).ToSpatialNodes();
            
            return candidates.Where(node => area.Contains(node.Position)).ToList();
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// <summary>
        /// 获取单位的玩家ID
        /// </summary>
        /// <param name="unitData">单位数据</param>
        /// <returns>玩家ID</returns>
        private int GetUnitPlayerId(UnitColdData unitData)
        {
            // UnitColdData中没有PlayerId属性，这里返回默认值
            // 实际项目中应该从其他地方获取玩家ID，比如从UnitHotData或其他组件
            return 0; // 默认玩家ID
        }

        #endregion
    }
}