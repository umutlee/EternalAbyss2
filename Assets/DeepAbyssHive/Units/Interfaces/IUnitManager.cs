using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using Unity.Collections;

namespace DeepAbyssHive.Units.Interfaces
{
    /// <summary>
    /// 单位管理器接口，负责管理所有单位
    /// </summary>
    public interface IUnitManager : IManager
    {
        /// <summary>
        /// 创建单位
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>单位ID</returns>
        int CreateUnit(UnitType type, Vector3 position, int ownerId);
        
        /// <summary>
        /// 销毁单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        void DestroyUnit(int unitId);
        
        /// <summary>
        /// 移动单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetPosition">目标位置</param>
        void MoveUnit(int unitId, Vector3 targetPosition);
        
        /// <summary>
        /// 攻击目标
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetId">目标ID</param>
        void AttackTarget(int unitId, int targetId);
        
        /// <summary>
        /// 获取单位热数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位热数据</returns>
        UnitHotData GetUnitHotData(int unitId);
        
        /// <summary>
        /// 获取单位冷数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位冷数据，如果不存在则返回null</returns>
        UnitColdData? GetUnitColdData(int unitId);
        
        /// <summary>
        /// 获取范围内的单位
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>单位ID数组</returns>
        NativeArray<int> GetUnitsInRange(Vector3 position, float radius);
        
        /// <summary>
        /// 获取指定类型和所有者的单位
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>单位ID数组</returns>
        NativeArray<int> GetUnitsOfType(UnitType type, int ownerId);
        
        /// <summary>
        /// 进化单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="evolutionPath">进化路径ID</param>
        /// <returns>是否成功</returns>
        bool EvolveUnit(int unitId, string evolutionPath);
        
        /// <summary>
        /// 使单位适应环境
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="environmentType">环境类型</param>
        void AdaptToEnvironment(int unitId, string environmentType);
    }
}