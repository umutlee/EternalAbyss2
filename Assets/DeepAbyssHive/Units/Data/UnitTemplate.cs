using UnityEngine;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位模板数据 - 用于定义单位的基础属性和配置
    /// </summary>
    [CreateAssetMenu(fileName = "New Unit Template", menuName = "DeepAbyssHive/Templates/Unit Template")]
    public class UnitTemplateSO : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private string _unitName = "新单位";
        [SerializeField] private UnitType _unitType = UnitType.Worker;
        [SerializeField] private string _description = "";
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _prefab;

        [Header("生命值和能量")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxEnergy = 100f;
        [SerializeField] private float _healthRegenRate = 1f;
        [SerializeField] private float _energyRegenRate = 2f;

        [Header("移动属性")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 180f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _deceleration = 15f;

        [Header("战斗属性")]
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _detectionRange = 10f;
        [SerializeField] private float _armor = 0f;
        [SerializeField] private float _magicResistance = 0f;

        [Header("资源和建造")]
        [SerializeField] private float _gatherRate = 1f;
        [SerializeField] private float _buildSpeed = 1f;
        [SerializeField] private int _carryCapacity = 10;

        [Header("进化系统")]
        [SerializeField] private UnitType[] _evolutionOptions;
        [SerializeField] private int _evolutionLevel = 1;
        [SerializeField] private float _evolutionCost = 100f;

        [Header("音效")]
        [SerializeField] private AudioClip _spawnSound;
        [SerializeField] private AudioClip _moveSound;
        [SerializeField] private AudioClip _attackSound;
        [SerializeField] private AudioClip _deathSound;
        [SerializeField] private AudioClip _evolutionSound;

        [Header("视觉效果")]
        [SerializeField] private GameObject _spawnEffect;
        [SerializeField] private GameObject _deathEffect;
        [SerializeField] private GameObject _evolutionEffect;
        [SerializeField] private Material _material;

        [Header("AI行为")]
        [SerializeField] private bool _isAggressive = false;
        [SerializeField] private bool _canFly = false;
        [SerializeField] private bool _canSwim = false;
        [SerializeField] private float _aiUpdateInterval = 0.1f;

        // 属性访问器
        public string UnitName => _unitName;
        public UnitType UnitType => _unitType;
        public string Description => _description;
        public Sprite Icon => _icon;
        public GameObject Prefab => _prefab;

        public float MaxHealth => _maxHealth;
        public float MaxEnergy => _maxEnergy;
        public float HealthRegenRate => _healthRegenRate;
        public float EnergyRegenRate => _energyRegenRate;

        public float MoveSpeed => _moveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float Acceleration => _acceleration;
        public float Deceleration => _deceleration;

        public float AttackDamage => _attackDamage;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public float DetectionRange => _detectionRange;
        public float Armor => _armor;
        public float MagicResistance => _magicResistance;

        public float GatherRate => _gatherRate;
        public float BuildSpeed => _buildSpeed;
        public int CarryCapacity => _carryCapacity;

        public UnitType[] EvolutionOptions => _evolutionOptions;
        public int EvolutionLevel => _evolutionLevel;
        public float EvolutionCost => _evolutionCost;

        public AudioClip SpawnSound => _spawnSound;
        public AudioClip MoveSound => _moveSound;
        public AudioClip AttackSound => _attackSound;
        public AudioClip DeathSound => _deathSound;
        public AudioClip EvolutionSound => _evolutionSound;

        public GameObject SpawnEffect => _spawnEffect;
        public GameObject DeathEffect => _deathEffect;
        public GameObject EvolutionEffect => _evolutionEffect;
        public Material Material => _material;

        public bool IsAggressive => _isAggressive;
        public bool CanFly => _canFly;
        public bool CanSwim => _canSwim;
        public float AIUpdateInterval => _aiUpdateInterval;

        /// <summary>
        /// 验证单位数据的有效性
        /// </summary>
        public bool IsValid()
        {
            if (string.IsNullOrEmpty(_unitName)) return false;
            if (_maxHealth <= 0f) return false;
            if (_moveSpeed < 0f) return false;
            if (_attackDamage < 0f) return false;
            if (_attackRange < 0f) return false;
            if (_attackCooldown <= 0f) return false;
            
            return true;
        }

        /// <summary>
        /// 获取单位的总战斗力评分
        /// </summary>
        public float GetCombatRating()
        {
            float healthScore = _maxHealth * 0.1f;
            float damageScore = _attackDamage * 2f;
            float speedScore = _moveSpeed * 1f;
            float rangeScore = _attackRange * 1.5f;
            float cooldownScore = (2f - _attackCooldown) * 10f;
            
            return healthScore + damageScore + speedScore + rangeScore + cooldownScore;
        }

        /// <summary>
        /// 获取单位的经济价值评分
        /// </summary>
        public float GetEconomicRating()
        {
            float gatherScore = _gatherRate * 20f;
            float buildScore = _buildSpeed * 15f;
            float carryScore = _carryCapacity * 2f;
            
            return gatherScore + buildScore + carryScore;
        }

        /// <summary>
        /// 在编辑器中验证数据
        /// </summary>
        private void OnValidate()
        {
            // 确保数值在合理范围内
            _maxHealth = Mathf.Max(1f, _maxHealth);
            _maxEnergy = Mathf.Max(0f, _maxEnergy);
            _moveSpeed = Mathf.Max(0f, _moveSpeed);
            _attackDamage = Mathf.Max(0f, _attackDamage);
            _attackRange = Mathf.Max(0f, _attackRange);
            _attackCooldown = Mathf.Max(0.1f, _attackCooldown);
            _detectionRange = Mathf.Max(0f, _detectionRange);
            _gatherRate = Mathf.Max(0f, _gatherRate);
            _buildSpeed = Mathf.Max(0f, _buildSpeed);
            _carryCapacity = Mathf.Max(0, _carryCapacity);
        }
    }
}