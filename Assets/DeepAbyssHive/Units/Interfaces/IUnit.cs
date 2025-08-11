using UnityEngine;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Interfaces
{
    /// <summary>
    /// 单位接口，定义所有单位的基本行为
    /// </summary>
    public interface IUnit
    {
        /// <summary>
        /// 单位ID
        /// </summary>
        int UnitId { get; }
        
        /// <summary>
        /// 单位名称
        /// </summary>
        string UnitName { get; }
        
        /// <summary>
        /// 单位类型
        /// </summary>
        UnitType UnitType { get; }
        
        /// <summary>
        /// 当前状态
        /// </summary>
        UnitState CurrentState { get; }
        
        /// <summary>
        /// 当前生命值
        /// </summary>
        float CurrentHealth { get; }
        
        /// <summary>
        /// 最大生命值
        /// </summary>
        float MaxHealth { get; }
        
        /// <summary>
        /// 当前能量
        /// </summary>
        float CurrentEnergy { get; }
        
        /// <summary>
        /// 最大能量
        /// </summary>
        float MaxEnergy { get; }
        
        /// <summary>
        /// 当前等级
        /// </summary>
        int CurrentLevel { get; }
        
        /// <summary>
        /// 当前经验值
        /// </summary>
        float CurrentExperience { get; }
        
        /// <summary>
        /// 是否存活
        /// </summary>
        bool IsAlive { get; }
        
        /// <summary>
        /// 是否可以进化
        /// </summary>
        bool CanEvolve { get; }
        
        /// <summary>
        /// 位置
        /// </summary>
        Vector3 Position { get; }
        
        /// <summary>
        /// 朝向
        /// </summary>
        Vector3 Forward { get; }
        
        /// <summary>
        /// 改变状态
        /// </summary>
        /// <param name="newState">新状态</param>
        void ChangeState(UnitState newState);
        
        /// <summary>
        /// 移动到指定位置
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        void MoveTo(Vector3 targetPosition);
        
        /// <summary>
        /// 设置攻击目标
        /// </summary>
        /// <param name="target">目标单位</param>
        void SetTarget(IUnit target);
        
        /// <summary>
        /// 攻击目标
        /// </summary>
        /// <param name="target">目标单位</param>
        void AttackTarget(IUnit target);
        
        /// <summary>
        /// 受到伤害
        /// </summary>
        /// <param name="damage">伤害值</param>
        void TakeDamage(float damage);
        
        /// <summary>
        /// 治疗
        /// </summary>
        /// <param name="amount">治疗量</param>
        void Heal(float amount);
        
        /// <summary>
        /// 死亡
        /// </summary>
        void Die();
        
        /// <summary>
        /// 获得经验
        /// </summary>
        /// <param name="experience">经验值</param>
        void GainExperience(float experience);
        
        /// <summary>
        /// 升级
        /// </summary>
        void LevelUp();
        
        /// <summary>
        /// 进化
        /// </summary>
        /// <param name="targetType">目标类型</param>
        void Evolve(UnitType targetType);
        
        /// <summary>
        /// 单位死亡事件
        /// </summary>
        event System.Action<IUnit> OnUnitDeath;
        
        /// <summary>
        /// 单位进化事件
        /// </summary>
        event System.Action<IUnit> OnUnitEvolution;
        
        /// <summary>
        /// 状态改变事件
        /// </summary>
        event System.Action<IUnit, UnitState> OnStateChanged;
        
        /// <summary>
        /// 生命值改变事件
        /// </summary>
        event System.Action<IUnit, float> OnHealthChanged;
        
        /// <summary>
        /// 升级事件
        /// </summary>
        event System.Action<IUnit, int> OnLevelUp;
    }
}