using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位配置数据 - ScriptableObject形式的单位模板
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit Data", menuName = "Deep Abyss Hive/Unit Data")]
    public class UnitData : ScriptableObject
    {
        [Header("基础信息")]
        public string unitName = "未命名单位";
        public UnitType unitType = UnitType.Worker;
        public string description = "";
        
        [Header("属性")]
        public float maxHealth = 100f;
        public float maxEnergy = 100f;
        public float moveSpeed = 5f;
        public float attackDamage = 10f;
        public float attackRange = 2f;
        public float attackCooldown = 1f;
        public float armor = 0f;
        public float detectionRange = 10f;
        
        [Header("资源消耗")]
        public int biomasseCost = 50;
        public int energyCost = 0;
        public float buildTime = 5f;
        
        [Header("视觉")]
        public GameObject prefab;
        public Sprite icon;
        
        [Header("音效")]
        public AudioClip attackSound;
        public AudioClip deathSound;
        public AudioClip moveSound;
        
        [Header("能力")]
        public bool canAttack = true;
        public bool canMove = true;
        public bool canGather = false;
        public bool canBuild = false;
        
        [Header("进化")]
        public UnitData[] evolutionTargets;
        public int[] evolutionCosts;
        
        // 属性访问器，保持与Unit.cs的兼容性
        public float MaxHealth => maxHealth;
        public float MaxEnergy => maxEnergy;
        public float MoveSpeed => moveSpeed;
        public float AttackDamage => attackDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public float DetectionRange => detectionRange;
        public string UnitName => unitName;
        public UnitType UnitType => unitType;
        public AudioClip AttackSound => attackSound;
        public AudioClip DeathSound => deathSound;
        public UnitData[] EvolutionOptions => evolutionTargets;
        
        /// <summary>
        /// 创建单位热数据
        /// </summary>
        public UnitHotData CreateHotData(uint unitId, Vector3 position)
        {
            return new UnitHotData
            {
                unitId = unitId,
                position = position,
                rotation = Quaternion.identity,
                velocity = Vector3.zero,
                currentHealth = maxHealth,
                state = UnitState.Idle,
                targetPosition = position,
                targetUnitId = 0,
                lastUpdateTime = Time.time
            };
        }
        
        /// <summary>
        /// 创建单位冷数据
        /// </summary>
        public UnitColdData CreateColdData()
        {
            return new UnitColdData
            {
                unitType = unitType,
                maxHealth = maxHealth,
                moveSpeed = moveSpeed,
                attackDamage = attackDamage,
                attackRange = attackRange,
                attackCooldown = attackCooldown,
                armor = armor,
                canAttack = canAttack,
                canMove = canMove,
                canGather = canGather,
                canBuild = canBuild
            };
        }
    }

    /// <summary>
    /// 单位热数据 - 频繁更新的运行时数据
    /// </summary>
    [System.Serializable]
    public struct UnitHotData
    {
        public uint unitId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 velocity;
        public float currentHealth;
        public UnitState state;
        public Vector3 targetPosition;
        public uint targetUnitId;
        public float lastUpdateTime;
        
        // 添加UnitManager.cs中引用的属性
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public float Health;
        public int TargetId;
        public UnitState State;
        public float StateTimer;
    }

    /// <summary>
    /// 单位冷数据 - 不常变化的配置数据
    /// </summary>
    [System.Serializable]
    public struct UnitColdData
    {
        public UnitType unitType;
        public float maxHealth;
        public float moveSpeed;
        public float attackDamage;
        public float attackRange;
        public float attackCooldown;
        public float armor;
        public bool canAttack;
        public bool canMove;
        public bool canGather;
        public bool canBuild;
        
        // 添加UnitManager.cs中引用的属性
        public int UnitId;
        public UnitType Type;
        public int OwnerId;
        public UnitAttributes BaseAttributes;
        public EvolutionInfo Evolution;
        public AdaptiveTrait[] AdaptiveTraits;
        public string PrefabPath;
    }
}