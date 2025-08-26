using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位热数据 - 频繁更新的运行时数据
    /// 用于高频访问的数据，如位置、状态、生命值等
    /// </summary>
    [System.Serializable]
    public partial struct UnitHotData
    {
        [Header("基础信息")]
        public int UnitId;
        public UnitType UnitType;
        public UnitState CurrentState;
        public int PlayerId;
        
        [Header("位置与移动")]
        public Vector3 Position;
        public Vector3 Velocity;
        public Vector3 TargetPosition;
        public float MoveSpeed;
        public float RotationSpeed;
        public Quaternion Rotation;
        
        [Header("生命与能量")]
        public float Health;
        public float MaxHealth;
        public float Energy;
        public float MaxEnergy;
        public float Shield;
        public float MaxShield;
        
        [Header("战斗状态")]
        public int TargetUnitId;
        public float AttackCooldown;
        public float LastAttackTime;
        public bool IsInCombat;
        public float CombatRange;
        
        [Header("行为状态")]
        public bool IsSelected;
        public bool IsVisible;
        public bool IsMoving;
        public bool IsAttacking;
        public bool IsIdle;
        
        [Header("时间戳")]
        public float LastUpdateTime;
        public float StateChangeTime;
        public float CreationTime;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public UnitHotData(int unitId, UnitType unitType, Vector3 position, int playerId = 0)
        {
            UnitId = unitId;
            UnitType = unitType;
            CurrentState = UnitState.Idle;
            PlayerId = playerId;
            
            Position = position;
            Velocity = Vector3.zero;
            TargetPosition = position;
            MoveSpeed = 5f;
            RotationSpeed = 180f;
            Rotation = Quaternion.identity;
            
            Health = 100f;
            MaxHealth = 100f;
            Energy = 100f;
            MaxEnergy = 100f;
            Shield = 0f;
            MaxShield = 0f;
            
            TargetUnitId = -1;
            AttackCooldown = 0f;
            LastAttackTime = 0f;
            IsInCombat = false;
            CombatRange = 3f;
            
            IsSelected = false;
            IsVisible = true;
            IsMoving = false;
            IsAttacking = false;
            IsIdle = true;
            
            var currentTime = Time.time;
            LastUpdateTime = currentTime;
            StateChangeTime = currentTime;
            CreationTime = currentTime;
        }
        
        /// <summary>
        /// 更新位置
        /// </summary>
        public void UpdatePosition(Vector3 newPosition)
        {
            Position = newPosition;
            LastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 更新状态
        /// </summary>
        public void UpdateState(UnitState newState)
        {
            if (CurrentState != newState)
            {
                CurrentState = newState;
                StateChangeTime = Time.time;
                LastUpdateTime = Time.time;
            }
        }
        
        /// <summary>
        /// 更新生命值
        /// </summary>
        public void UpdateHealth(float newHealth)
        {
            Health = Mathf.Clamp(newHealth, 0f, MaxHealth);
            LastUpdateTime = Time.time;
            
            if (Health <= 0f)
            {
                UpdateState(UnitState.Dead);
            }
        }
        
        /// <summary>
        /// 检查是否存活
        /// </summary>
        public bool IsAlive => Health > 0f && CurrentState != UnitState.Dead;
        
        /// <summary>
        /// 检查是否可以移动
        /// </summary>
        public bool CanMove => IsAlive && CurrentState != UnitState.Dead && CurrentState != UnitState.Evolving;
        
        /// <summary>
        /// 检查是否可以攻击
        /// </summary>
        public bool CanAttack => IsAlive && AttackCooldown <= 0f && CurrentState != UnitState.Evolving;
    }
}