using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 单位查询服务接口
    /// 提供所有单位相关的只读查询功能
    /// </summary>
    public interface IUnitQueryService : IQueryService
    {
        /// <summary>
        /// 获取指定范围内的单位
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>单位数组（需要调用者Dispose）</returns>
        NativeArray<UnitData> GetUnitsInRange(Vector3 center, float radius, int playerId = -1);

        /// <summary>
        /// 获取指定类型的单位
        /// </summary>
        /// <param name="unitType">单位类型</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>单位数组（需要调用者Dispose）</returns>
        NativeArray<UnitData> GetUnitsOfType(UnitType unitType, int playerId = -1);

        /// <summary>
        /// 获取玩家的所有单位
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>单位数组（需要调用者Dispose）</returns>
        NativeArray<UnitData> GetPlayerUnits(int playerId);

        /// <summary>
        /// 获取单位数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位数据，如果不存在返回null</returns>
        UnitData? GetUnitData(int unitId);

        /// <summary>
        /// 检查单位是否存在
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>是否存在</returns>
        bool UnitExists(int unitId);

        /// <summary>
        /// 获取单位数量统计
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>单位数量统计</returns>
        Dictionary<UnitType, int> GetUnitCounts(int playerId);

        /// <summary>
        /// 获取最近的敌方单位
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的敌方单位ID，如果没有返回-1</returns>
        int GetNearestEnemyUnit(Vector3 position, int playerId, float maxDistance = float.MaxValue);

        /// <summary>
        /// 获取最近的友方单位
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="maxDistance">最大距离</param>
        /// <returns>最近的友方单位ID，如果没有返回-1</returns>
        int GetNearestFriendlyUnit(Vector3 position, int playerId, float maxDistance = float.MaxValue);

        /// <summary>
        /// 检查位置是否被单位占用
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">检查半径</param>
        /// <returns>是否被占用</returns>
        bool IsPositionOccupied(Vector3 position, float radius = 1f);

        /// <summary>
        /// 获取单位的移动路径
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>移动路径点列表</returns>
        List<Vector3> GetUnitPath(int unitId);

        /// <summary>
        /// 获取单位的当前状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位状态</returns>
        UnitState GetUnitState(int unitId);
    }
}