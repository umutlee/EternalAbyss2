using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex;
using DeepAbyssHive.SpatialIndex.Enums;
using Unity.Collections;

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
            
            // 获取空间索引中的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象
            List<SpatialNode> result = new List<SpatialNode>(unitIds.Length);
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetUnitsOfType(UnitType unitType)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取所有单位ID
            NativeArray<int> allUnitIds = _spatialIndexService.QueryAllIds(SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选类型
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in allUnitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && unitData.UnitType == unitType)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (allUnitIds.IsCreated)
            {
                allUnitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetUnitsOfPlayer(int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取所有单位ID
            NativeArray<int> allUnitIds = _spatialIndexService.QueryAllIds(SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选玩家
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in allUnitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) == playerId)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (allUnitIds.IsCreated)
            {
                allUnitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetUnitsInRangeOfPlayer(Vector3 center, float radius, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选玩家
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) == playerId)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetUnitsInRangeOfType(Vector3 center, float radius, UnitType unitType, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选类型和玩家
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && 
                    unitData.UnitType == unitType && GetUnitPlayerId(unitData) == playerId)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public SpatialNode GetNearestUnit(Vector3 position)
        {
            if (!_isInitialized) return null;
            
            // 获取最近的单位ID
            NativeArray<int> nearestIds = _spatialIndexService.QueryNearestIds(position, 1, SpatialObjectType.Unit);
            
            // 转换为SpatialNode对象
            SpatialNode result = null;
            if (nearestIds.Length > 0)
            {
                result = _spatialIndexService.GetNodeById(nearestIds[0]);
            }
            
            // 释放NativeArray
            if (nearestIds.IsCreated)
            {
                nearestIds.Dispose();
            }
            
            return result;
        }

        public SpatialNode GetNearestUnitOfType(Vector3 position, UnitType unitType)
        {
            if (!_isInitialized) return null;
            
            // 获取所有单位ID
            NativeArray<int> allUnitIds = _spatialIndexService.QueryAllIds(SpatialObjectType.Unit);
            
            // 筛选类型并找出最近的
            SpatialNode nearest = null;
            float minDistance = float.MaxValue;
            
            foreach (int id in allUnitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && unitData.UnitType == unitType)
                {
                    float distance = Vector3.Distance(position, node.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearest = node;
                    }
                }
            }
            
            // 释放NativeArray
            if (allUnitIds.IsCreated)
            {
                allUnitIds.Dispose();
            }
            
            return nearest;
        }

        public List<SpatialNode> GetUnitsInRangeByType(Vector3 center, float radius, UnitType unitType, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选类型和玩家
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && 
                    unitData.UnitType == unitType && GetUnitPlayerId(unitData) == playerId)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetEnemyUnitsInRange(Vector3 center, float radius, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选敌方单位
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) != playerId)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetAlliedUnitsInRange(Vector3 center, float radius, int playerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选友方单位
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) == playerId)
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public bool HasUnitsInRange(Vector3 center, float radius)
        {
            if (!_isInitialized) return false;
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            bool result = unitIds.Length > 0;
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public bool HasUnitsOfTypeInRange(Vector3 center, float radius, UnitType unitType)
        {
            if (!_isInitialized) return false;
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 检查是否有指定类型的单位
            bool result = false;
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && unitData.UnitType == unitType)
                {
                    result = true;
                    break;
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public int CountUnitsInRange(Vector3 center, float radius)
        {
            if (!_isInitialized) return 0;
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            int count = unitIds.Length;
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return count;
        }

        public int CountUnitsOfTypeInRange(Vector3 center, float radius, UnitType unitType)
        {
            if (!_isInitialized) return 0;
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(center, radius, SpatialObjectType.Unit);
            
            // 计算指定类型的单位数量
            int count = 0;
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && unitData.UnitType == unitType)
                {
                    count++;
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return count;
        }

        public List<SpatialNode> GetUnitsWithinDistance(Vector3 position, float maxDistance)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            return GetUnitsInRange(position, maxDistance);
        }

        public List<SpatialNode> GetVisibleUnits(Vector3 viewerPosition, float viewRange, int viewerPlayerId)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(viewerPosition, viewRange, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选可见单位
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && node.Data is UnitColdData unitData && GetUnitPlayerId(unitData) != viewerPlayerId)
                {
                    // 这里可以添加视线检查逻辑
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        public List<SpatialNode> GetUnitsInArea(Bounds area)
        {
            if (!_isInitialized) return new List<SpatialNode>();
            
            // 使用区域中心和最大半径进行查询
            float radius = Mathf.Max(area.size.x, area.size.y, area.size.z) * 0.5f;
            
            // 获取范围内的单位ID
            NativeArray<int> unitIds = _spatialIndexService.QueryRangeIds(area.center, radius, SpatialObjectType.Unit);
            
            // 将ID转换为SpatialNode对象并筛选在区域内的单位
            List<SpatialNode> result = new List<SpatialNode>();
            foreach (int id in unitIds)
            {
                SpatialNode node = _spatialIndexService.GetNodeById(id);
                if (node != null && area.Contains(node.Position))
                {
                    result.Add(node);
                }
            }
            
            // 释放NativeArray
            if (unitIds.IsCreated)
            {
                unitIds.Dispose();
            }
            
            return result;
        }

        #endregion

        #region 私有方法

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