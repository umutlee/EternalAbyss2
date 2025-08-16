using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 单位命令服务接口
    /// 提供所有单位相关的修改操作功能
    /// </summary>
    public interface IUnitCommandService : ICommandService
    {
        /// <summary>
        /// 创建单位
        /// </summary>
        /// <param name="unitType">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="rotation">旋转（可选）</param>
        /// <returns>创建的单位ID，失败返回-1</returns>
        int CreateUnit(UnitType unitType, Vector3 position, int playerId, Quaternion? rotation = null);

        /// <summary>
        /// 销毁单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>是否成功</returns>
        bool DestroyUnit(int unitId);

        /// <summary>
        /// 移动单位到指定位置
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否成功</returns>
        bool MoveUnit(int unitId, Vector3 targetPosition);

        /// <summary>
        /// 单位攻击目标
        /// </summary>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="targetId">目标ID</param>
        /// <returns>是否成功</returns>
        bool AttackTarget(int attackerId, int targetId);

        /// <summary>
        /// 单位攻击位置
        /// </summary>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否成功</returns>
        bool AttackPosition(int attackerId, Vector3 targetPosition);

        /// <summary>
        /// 停止单位行动
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>是否成功</returns>
        bool StopUnit(int unitId);

        /// <summary>
        /// 单位进化
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>是否成功</returns>
        bool EvolveUnit(int unitId, UnitType targetType);

        /// <summary>
        /// 设置单位状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="state">新状态</param>
        /// <returns>是否成功</returns>
        bool SetUnitState(int unitId, UnitState state);

        /// <summary>
        /// 修改单位属性
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="attributeType">属性类型</param>
        /// <param name="value">新值</param>
        /// <returns>是否成功</returns>
        bool ModifyUnitAttribute(int unitId, UnitAttributeType attributeType, float value);

        /// <summary>
        /// 治疗单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="healAmount">治疗量</param>
        /// <returns>是否成功</returns>
        bool HealUnit(int unitId, float healAmount);

        /// <summary>
        /// 对单位造成伤害
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="damage">伤害量</param>
        /// <param name="damageType">伤害类型</param>
        /// <returns>是否成功</returns>
        bool DamageUnit(int unitId, float damage, DamageType damageType = DamageType.Physical);

        /// <summary>
        /// 设置单位AI行为
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="behaviorType">行为类型</param>
        /// <returns>是否成功</returns>
        bool SetUnitBehavior(int unitId, UnitBehaviorType behaviorType);

        /// <summary>
        /// 单位适应环境
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="environmentType">环境类型</param>
        /// <returns>是否成功</returns>
        bool AdaptToEnvironment(int unitId, EnvironmentType environmentType);

        /// <summary>
        /// 批量移动单位
        /// </summary>
        /// <param name="unitIds">单位ID数组</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="formation">编队类型</param>
        /// <returns>成功移动的单位数量</returns>
        int MoveUnitsInFormation(int[] unitIds, Vector3 targetPosition, FormationType formation = FormationType.None);
    }

    /// <summary>
    /// 单位属性类型
    /// </summary>
    public enum UnitAttributeType
    {
        Health,
        MaxHealth,
        Attack,
        Defense,
        Speed,
        AttackRange,
        AttackSpeed,
        Energy,
        MaxEnergy
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageType
    {
        Physical,
        Energy,
        Poison,
        Fire,
        Ice,
        Lightning
    }

    /// <summary>
    /// 单位行为类型
    /// </summary>
    public enum UnitBehaviorType
    {
        Idle,
        Aggressive,
        Defensive,
        Patrol,
        Follow,
        Guard
    }

    /// <summary>
    /// 编队类型
    /// </summary>
    public enum FormationType
    {
        None,
        Line,
        Column,
        Wedge,
        Circle,
        Square
    }
}