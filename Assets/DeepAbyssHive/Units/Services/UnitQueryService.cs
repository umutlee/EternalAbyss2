using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 单位查询服务实现
    /// 提供所有单位相关的只读查询功能
    /// </summary>
    public class UnitQueryService : IUnitQueryService, IQueryService, IService
    {
        #region 私有字段
        private readonly Dictionary<int, UnitHotData> _unitHotData;
        private readonly Dictionary<int, UnitColdData> _unitColdData;
        private readonly Dictionary<int, SpatialNode> _unitSpatialNodes;
        private readonly ISpatialIndex<SpatialNode> _spatialIndex;
        private readonly string _serviceName = "UnitQueryService";
        #endregion

        #region IService属性实现
        public string ServiceName => _serviceName;
        public bool IsInitialized { get; private set; }
        public bool IsQueryAvailable => IsInitialized;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="unitHotData">单位热数据字典</param>
        /// <param name="unitColdData">单位冷数据字典</param>
        /// <param name="unitSpatialNodes">单位空间节点字典</param>
        /// <param name="spatialIndex">空间索引</param>
        public UnitQueryService(
            Dictionary<int, UnitHotData> unitHotData,
            Dictionary<int, UnitColdData> unitColdData,
            Dictionary<int, SpatialNode> unitSpatialNodes,
            ISpatialIndex<SpatialNode> spatialIndex)
        {
            _unitHotData = unitHotData ?? throw new System.ArgumentNullException(nameof(unitHotData));
            _unitColdData = unitColdData ?? throw new System.ArgumentNullException(nameof(unitColdData));
            _unitSpatialNodes = unitSpatialNodes ?? throw new System.ArgumentNullException(nameof(unitSpatialNodes));
            _spatialIndex = spatialIndex;
            IsInitialized = true;
        }
        #endregion

        #region IService接口实现
        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized)
                return;
                
            Debug.Log($"[{_serviceName}] 初始化单位查询服务");
            IsInitialized = true;
        }

        /// <summary>
        /// 清理服务
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[{_serviceName}] 清理单位查询服务");
            IsInitialized = false;
        }
        #endregion

        #region IUnitQueryService接口实现
        /// <summary>
        /// 获取指定范围内的单位
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>单位数组（需要调用者Dispose）</returns>
        public NativeArray<UnitData> GetUnitsInRange(Vector3 center, float radius, int playerId = -1)
        {
            List<UnitData> unitsInRange = new List<UnitData>();

            if (_spatialIndex != null)
            {
                // 使用空间索引查询
                List<SpatialNode> spatialResults = _spatialIndex.QueryRange(center, Vector3.one * radius * 2);
                
                foreach (var spatialNode in spatialResults)
                {
                    if (Vector3.Distance(spatialNode.Position, center) <= radius)
                    {
                        int unitId = spatialNode.Id;
                        var unitData = GetUnitDataInternal(unitId);
                        if (unitData.HasValue && (playerId == -1 || unitData.Value.PlayerId == playerId))
                        {
                            unitsInRange.Add(unitData.Value);
                        }
                    }
                }
            }
            else
            {
                // 如果没有空间索引，使用暴力搜索
                foreach (var pair in _unitHotData)
                {
                    int unitId = pair.Key;
                    UnitHotData hotData = pair.Value;
                    
                    if (Vector3.Distance(hotData.Position, center) <= radius)
                    {
                        var unitData = GetUnitDataInternal(unitId);
                        if (unitData.HasValue && (playerId == -1 || unitData.Value.PlayerId == playerId))
                        {
                            unitsInRange.Add(unitData.Value);
                        }
                    }
                }
            }

            // 转换为NativeArray
            NativeArray<UnitData> result = new NativeArray<UnitData>(unitsInRange.Count, Allocator.Temp);
            for (int i = 0; i < unitsInRange.Count; i++)
            {
                result[i] = unitsInRange[i];
            }

            return result;
        }

        /// <summary>
        /// 获取指定类型的单位
        /// </summary>
        /// <param name="unitType">单位类型</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>单位数组（需要调用者Dispose）</returns>
        public NativeArray<UnitData> GetUnitsOfType(UnitType unitType, int playerId = -1)
        {
            List<UnitData> unitsOfType = new List<UnitData>();

            foreach (var pair in _unitColdData)
            {
                int unitId = pair.Key;
                UnitColdData coldData = pair.Value;

                if (coldData.Type == unitType && (playerId == -1 || coldData.OwnerId == playerId))
                {
                    var unitData = GetUnitDataInternal(unitId);
                    if (unitData.HasValue)
                    {
                        unitsOfType.Add(unitData.Value);
                    }
                }
            }

            // 转换为NativeArray
            NativeArray<UnitData> result = new NativeArray<UnitData>(unitsOfType.Count, Allocator.Temp);
            for (int i = 0; i < unitsOfType.Count; i++)
            {
                result[i] = unitsOfType[i];
            }

            return result;
        }

        /// <summary>
        /// 获取玩家的所有单位
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>单位数组（需要调用者Dispose）</returns>
        public NativeArray<UnitData> GetPlayerUnits(int playerId)
        {
            List<UnitData> playerUnits = new List<UnitData>();

            foreach (var pair in _unitColdData)
            {
                int unitId = pair.Key;
                UnitColdData coldData = pair.Value;

                if (coldData.OwnerId == playerId)
                {
                    var unitData = GetUnitDataInternal(unitId);
                    if (unitData.HasValue)
                    {
                        playerUnits.Add(unitData.Value);
                    }
                }
            }

            // 转换为NativeArray
            NativeArray<UnitData> result = new NativeArray<UnitData>(playerUnits.Count, Allocator.Temp);
            for (int i = 0; i < playerUnits.Count; i++)
            {
                result[i] = playerUnits[i];
            }

            return result;
        }

        /// <summary>
        /// 获取单位数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位数据，如果不存在返回null</returns>
        public UnitData? GetUnitData(int unitId)
        {
            return GetUnitDataInternal(unitId);
        }

        /// <summary>
        /// 检查单位是否存在
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>是否存在</returns>
        public bool UnitExists(int unitId)
        {
            return _unitHotData.ContainsKey(unitId) && _unitColdData.ContainsKey(unitId);
        }

        /// <summary>
        /// 获取单位数量统计
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>单位数量统计</returns>
        public Dictionary<UnitType, int> GetUnitCounts(int playerId)
        {
            Dictionary<UnitType, int> counts = new Dictionary<UnitType, int>();

            foreach (var pair in _unitColdData)
            {
                UnitColdData coldData = pair.Value;

                if (coldData.OwnerId == playerId)
                {
                    if (counts.ContainsKey(coldData.Type))
                    {
                        counts[coldData.Type]++;
                    }
                    else
                    {
                        counts[coldData.Type] = 1;
                    }
                }
            }

            return counts;
        }

        /// <summary>
        /// 获取最近的敌方单位
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的敌方单位ID，如果没有返回-1</returns>
        public int GetNearestEnemyUnit(Vector3 position, int playerId, float maxDistance = float.MaxValue)
        {
            int nearestUnitId = -1;
            float nearestDistance = maxDistance;

            foreach (var pair in _unitHotData)
            {
                int unitId = pair.Key;
                UnitHotData hotData = pair.Value;

                if (_unitColdData.TryGetValue(unitId, out var coldData) && coldData.OwnerId != playerId)
                {
                    float distance = Vector3.Distance(hotData.Position, position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestUnitId = unitId;
                    }
                }
            }

            return nearestUnitId;
        }

        /// <summary>
        /// 获取最近的友方单位
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的友方单位ID，如果没有返回-1</returns>
        public int GetNearestFriendlyUnit(Vector3 position, int playerId, float maxDistance = float.MaxValue)
        {
            int nearestUnitId = -1;
            float nearestDistance = maxDistance;

            foreach (var pair in _unitHotData)
            {
                int unitId = pair.Key;
                UnitHotData hotData = pair.Value;

                if (_unitColdData.TryGetValue(unitId, out var coldData) && coldData.OwnerId == playerId)
                {
                    float distance = Vector3.Distance(hotData.Position, position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestUnitId = unitId;
                    }
                }
            }

            return nearestUnitId;
        }

        /// <summary>
        /// 检查位置是否被单位占用
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">检查半径</param>
        /// <returns>是否被占用</returns>
        public bool IsPositionOccupied(Vector3 position, float radius = 1f)
        {
            foreach (var pair in _unitHotData)
            {
                UnitHotData hotData = pair.Value;
                if (Vector3.Distance(hotData.Position, position) <= radius)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 获取单位的移动路径
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>移动路径点列表</returns>
        public List<Vector3> GetUnitPath(int unitId)
        {
            if (_unitHotData.TryGetValue(unitId, out var hotData))
            {
                return hotData.MovementPath ?? new List<Vector3>();
            }
            return new List<Vector3>();
        }

        /// <summary>
        /// 获取单位的当前状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位状态</returns>
        public UnitState GetUnitState(int unitId)
        {
            if (_unitHotData.TryGetValue(unitId, out var hotData))
            {
                return hotData.State;
            }
            return UnitState.Idle;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 内部获取单位数据方法
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位数据</returns>
        private UnitData? GetUnitDataInternal(int unitId)
        {
            if (_unitHotData.TryGetValue(unitId, out var hotData) && 
                _unitColdData.TryGetValue(unitId, out var coldData))
            {
                return new UnitData
                {
                    Id = unitId,
                    Type = coldData.Type,
                    PlayerId = coldData.OwnerId,
                    Position = hotData.Position,
                    Rotation = hotData.Rotation,
                    State = hotData.State,
                    Health = hotData.Health,
                    MaxHealth = coldData.Attributes.MaxHealth,
                    Attributes = coldData.Attributes
                };
            }
            return null;
        }
        #endregion
    }
}