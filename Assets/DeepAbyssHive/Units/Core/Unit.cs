using UnityEngine;
using System.Collections.Generic;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Core.Interfaces;

namespace DeepAbyssHive.Units.Core
{
    /// <summary>
    /// 单位基础类
    /// 实现所有单位的基本功能和行为
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(Collider))]
    public class Unit : MonoBehaviour, IUnit
    {
        [Header("单位基础信息")]
        [SerializeField] private UnitData _unitData;
        [SerializeField] private UnitType _unitType;
        [SerializeField] private int _unitId;
        [SerializeField] private string _unitName;

        [Header("单位状态")]
        [SerializeField] private UnitState _currentState = UnitState.Idle;
        [SerializeField] private float _currentHealth;
        [SerializeField] private float _currentEnergy;
        [SerializeField] private int _currentLevel = 1;
        [SerializeField] private float _currentExperience = 0f;

        [Header("移动系统")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _rotationSpeed = 180f;
        [SerializeField] private Vector3 _targetPosition;
        [SerializeField] private bool _isMoving = false;

        [Header("战斗系统")]
        [SerializeField] private float _attackDamage = 10f;
        [SerializeField] private float _attackRange = 3f;
        [SerializeField] private float _attackCooldown = 1f;
        [SerializeField] private float _lastAttackTime = 0f;
        [SerializeField] private Unit _currentTarget;

        [Header("进化系统")]
        [SerializeField] private List<UnitType> _evolutionOptions;
        [SerializeField] private Dictionary<string, float> _evolutionProgress;
        [SerializeField] private bool _canEvolve = false;

        [Header("AI系统")]
        [SerializeField] private float _detectionRange = 10f;
        [SerializeField] private LayerMask _enemyLayers = -1;
        [SerializeField] private LayerMask _allyLayers = -1;
        [SerializeField] private bool _isAIControlled = true;

        // 组件引用
        private Rigidbody _rigidbody;
        private Collider _collider;
        private Animator _animator;
        private AudioSource _audioSource;

        // 状态机
        private Dictionary<UnitState, System.Action> _stateMachine;
        private UnitState _previousState;

        // 事件
        public event System.Action<IUnit> OnUnitDeath;
        public event System.Action<IUnit> OnUnitEvolution;
        public event System.Action<IUnit, UnitState> OnStateChanged;
        public event System.Action<IUnit, float> OnHealthChanged;
        public event System.Action<IUnit, int> OnLevelUp;

        // 属性访问器
        public UnitData UnitData => _unitData;
        public UnitType UnitType => _unitType;
        public int UnitId => _unitId;
        public string UnitName => _unitName;
        public UnitState CurrentState => _currentState;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _unitData.MaxHealth;
        public float CurrentEnergy => _currentEnergy;
        public float MaxEnergy => 100f; // MaxEnergy字段在UnitData中不存在，使用默认值
        public int CurrentLevel => _currentLevel;
        public float CurrentExperience => _currentExperience;
        public bool IsAlive => _currentHealth > 0f;
        public bool CanEvolve => _canEvolve;
        public Vector3 Position => transform.position;
        public Vector3 Forward => transform.forward;

        /// <summary>
        /// Unity初始化
        /// </summary>
        private void Awake()
        {
            InitializeComponents();
            InitializeStateMachine();
            InitializeEvolutionSystem();
        }

        /// <summary>
        /// Unity开始
        /// </summary>
        private void Start()
        {
            InitializeUnit();
        }

        /// <summary>
        /// Unity更新
        /// </summary>
        private void Update()
        {
            if (!IsAlive) return;

            UpdateStateMachine();
            UpdateMovement();
            UpdateCombat();
            UpdateAI();
            UpdateEvolution();
        }

        /// <summary>
        /// Unity固定更新
        /// </summary>
        private void FixedUpdate()
        {
            if (!IsAlive) return;

            UpdatePhysics();
        }

        /// <summary>
        /// 初始化组件
        /// </summary>
        private void InitializeComponents()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();

            // 配置刚体
            if (_rigidbody != null)
            {
                _rigidbody.freezeRotation = true;
                _rigidbody.useGravity = true;
            }
        }

        /// <summary>
        /// 初始化状态机
        /// </summary>
        private void InitializeStateMachine()
        {
            _stateMachine = new Dictionary<UnitState, System.Action>
            {
                { UnitState.Idle, UpdateIdleState },
                { UnitState.Moving, UpdateMovingState },
                { UnitState.Attacking, UpdateAttackingState },
                { UnitState.Gathering, UpdateGatheringState },
                { UnitState.Building, UpdateBuildingState },
                { UnitState.Evolving, UpdateEvolvingState },
                { UnitState.Dead, UpdateDeadState }
            };
        }

        /// <summary>
        /// 初始化进化系统
        /// </summary>
        private void InitializeEvolutionSystem()
        {
            _evolutionProgress = new Dictionary<string, float>();
            _evolutionOptions = new List<UnitType>();

            if (!Equals(_unitData, default) && _unitData.EvolutionOptions != null)
            {
                // 从UnitData[]中提取UnitType
                if (_unitData.EvolutionOptions is UnitData[] evolutionArray)
                {
                    foreach (var evolutionOption in evolutionArray)
                    {
                        if (!evolutionOption.Equals(default(UnitData)))
                        {
                            _evolutionOptions.Add((UnitType)evolutionOption.UnitType);
                        }
                    }
                }
                
                foreach (var option in _evolutionOptions)
                {
                    _evolutionProgress[option.ToString()] = 0f;
                }
            }
        }

        /// <summary>
        /// 初始化单位
        /// </summary>
        private void InitializeUnit()
        {
            if (!Equals(_unitData, default))
            {
                _unitId = GetInstanceID();
                _unitName = $"Unit_{_unitData.UnitId}"; // 使用 UnitId 生成名稱
                _unitType = (UnitType)_unitData.UnitType;
                _currentHealth = _unitData.MaxHealth;
                _currentEnergy = _unitData.MaxEnergy;
                _moveSpeed = _unitData.Speed; // 使用 Speed 屬性
                _attackDamage = _unitData.AttackDamage;
                _attackRange = _unitData.AttackRange;
                _attackCooldown = 1.0f; // 使用預設值
                _detectionRange = _unitData.AttackRange * 1.2f; // 偵測範圍為攻擊範圍的 1.2 倍
            }

            ChangeState(UnitState.Idle);
        }

        /// <summary>
        /// 更新状态机
        /// </summary>
        private void UpdateStateMachine()
        {
            if (_stateMachine.ContainsKey(_currentState))
            {
                _stateMachine[_currentState]?.Invoke();
            }
        }

        /// <summary>
        /// 更新移动
        /// </summary>
        private void UpdateMovement()
        {
            if (!_isMoving) return;

            Vector3 direction = (_targetPosition - transform.position).normalized;
            float distance = Vector3.Distance(transform.position, _targetPosition);

            if (distance > 0.1f)
            {
                // 移动
                Vector3 movement = direction * _moveSpeed * UnityEngine.Time.deltaTime;
                transform.position += movement;

                // 旋转
                if (direction != Vector3.zero)
                {
                    Quaternion targetRotation = Quaternion.LookRotation(direction);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, 
                                                                 _rotationSpeed * UnityEngine.Time.deltaTime);
                }
            }
            else
            {
                _isMoving = false;
                if (_currentState == UnitState.Moving)
                {
                    ChangeState(UnitState.Idle);
                }
            }
        }

        /// <summary>
        /// 更新战斗
        /// </summary>
        private void UpdateCombat()
        {
            if (_currentTarget == null) return;

            float distanceToTarget = Vector3.Distance(transform.position, _currentTarget.transform.position);
            
            if (distanceToTarget <= _attackRange)
            {
                if (UnityEngine.Time.time >= _lastAttackTime + _attackCooldown)
                {
                    AttackTarget(_currentTarget);
                }
            }
            else if (_currentState == UnitState.Attacking)
            {
                // 目标超出攻击范围，移动到目标
                MoveTo(_currentTarget.transform.position);
            }
        }

        /// <summary>
        /// 更新AI
        /// </summary>
        private void UpdateAI()
        {
            if (!_isAIControlled) return;

            // 简单的AI逻辑
            if (_currentState == UnitState.Idle)
            {
                // 寻找敌人
                Unit nearestEnemy = FindNearestEnemy();
                if (nearestEnemy != null)
                {
                    SetTarget(nearestEnemy);
                    ChangeState(UnitState.Attacking);
                }
            }
        }

        /// <summary>
        /// 更新进化
        /// </summary>
        private void UpdateEvolution()
        {
            if (_evolutionOptions.Count == 0) return;

            // 检查进化条件
            CheckEvolutionConditions();
        }

        /// <summary>
        /// 更新物理
        /// </summary>
        private void UpdatePhysics()
        {
            // 物理相关更新
        }

        /// <summary>
        /// 改变状态
        /// </summary>
        public void ChangeState(UnitState newState)
        {
            if (_currentState == newState) return;

            _previousState = _currentState;
            _currentState = newState;

            OnStateChanged?.Invoke(this, newState);

            // 更新动画
            if (_animator != null)
            {
                _animator.SetInteger("State", (int)newState);
            }
        }

        /// <summary>
        /// 移动到指定位置
        /// </summary>
        public void MoveTo(Vector3 targetPosition)
        {
            _targetPosition = targetPosition;
            _isMoving = true;
            ChangeState(UnitState.Moving);
        }

        /// <summary>
        /// 设置攻击目标
        /// </summary>
        public void SetTarget(IUnit target)
        {
            _currentTarget = target as Unit;
        }

        /// <summary>
        /// 攻击目标
        /// </summary>
        public void AttackTarget(IUnit target)
        {
            Unit unitTarget = target as Unit;
            if (unitTarget == null || !unitTarget.IsAlive) return;

            _lastAttackTime = UnityEngine.Time.time;
            unitTarget.TakeDamage(_attackDamage);

            // 播放攻击动画和音效
            if (_animator != null)
            {
                _animator.SetTrigger("Attack");
            }

            // 使用AudioHelper播放攻擊音效
            AudioHelper.PlaySound("attackSoundPath");
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public void TakeDamage(float damage)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Max(0f, _currentHealth - damage);
            OnHealthChanged?.Invoke(this, _currentHealth);

            if (_currentHealth <= 0f)
            {
                Die();
            }
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public void Heal(float amount)
        {
            if (!IsAlive) return;

            _currentHealth = Mathf.Min(MaxHealth, _currentHealth + amount);
            OnHealthChanged?.Invoke(this, _currentHealth);
        }

        /// <summary>
        /// 死亡
        /// </summary>
        public void Die()
        {
            ChangeState(UnitState.Dead);
            OnUnitDeath?.Invoke(this);

            // 播放死亡动画和音效
            if (_animator != null)
            {
                _animator.SetTrigger("Die");
            }

            // 使用AudioHelper播放死亡音效
            AudioHelper.PlaySound("deathSoundPath");

            // 禁用碰撞器
            if (_collider != null)
            {
                _collider.enabled = false;
            }
        }

        /// <summary>
        /// 获得经验
        /// </summary>
        public void GainExperience(float experience)
        {
            _currentExperience += experience;

            // 检查升级
            float requiredExp = GetRequiredExperience(_currentLevel + 1);
            if (_currentExperience >= requiredExp)
            {
                LevelUp();
            }
        }

        /// <summary>
        /// 升级
        /// </summary>
        public void LevelUp()
        {
            _currentLevel++;
            _currentExperience = 0f;

            // 提升属性
            float healthIncrease = MaxHealth * 0.1f;
            float energyIncrease = MaxEnergy * 0.1f;
            
            _currentHealth += healthIncrease;
            _currentEnergy += energyIncrease;

            OnLevelUp?.Invoke(this, _currentLevel);
        }

        /// <summary>
        /// 进化
        /// </summary>
        public void Evolve(UnitType targetType)
        {
            if (!_canEvolve || !_evolutionOptions.Contains(targetType)) return;

            ChangeState(UnitState.Evolving);
            OnUnitEvolution?.Invoke(this);

            // 进化逻辑将在管理器中处理
        }

        /// <summary>
        /// 寻找最近的敌人
        /// </summary>
        private Unit FindNearestEnemy()
        {
            Collider[] enemies = Physics.OverlapSphere(transform.position, _detectionRange, _enemyLayers);
            Unit nearestEnemy = null;
            float nearestDistance = float.MaxValue;

            foreach (var enemy in enemies)
            {
                Unit enemyUnit = enemy.GetComponent<Unit>();
                if (enemyUnit != null && enemyUnit.IsAlive)
                {
                    float distance = Vector3.Distance(transform.position, enemy.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        nearestEnemy = enemyUnit;
                    }
                }
            }

            return nearestEnemy;
        }

        /// <summary>
        /// 检查进化条件
        /// </summary>
        private void CheckEvolutionConditions()
        {
            _canEvolve = _currentLevel >= 3 && _currentExperience >= GetRequiredExperience(_currentLevel);
        }

        /// <summary>
        /// 获取升级所需经验
        /// </summary>
        private float GetRequiredExperience(int level)
        {
            return level * 100f; // 简单的经验计算
        }

        // 状态更新方法
        private void UpdateIdleState()
        {
            // 空闲状态逻辑
        }

        private void UpdateMovingState()
        {
            // 移动状态逻辑
        }

        private void UpdateAttackingState()
        {
            // 攻击状态逻辑
        }

        private void UpdateGatheringState()
        {
            // 采集状态逻辑
        }

        private void UpdateBuildingState()
        {
            // 建造状态逻辑
        }

        private void UpdateEvolvingState()
        {
            // 进化状态逻辑
        }

        private void UpdateDeadState()
        {
            // 死亡状态逻辑
        }

        /// <summary>
        /// Unity销毁
        /// </summary>
        private void OnDestroy()
        {
            OnUnitDeath = null;
            OnUnitEvolution = null;
            OnStateChanged = null;
            OnHealthChanged = null;
            OnLevelUp = null;
        }

        /// <summary>
        /// Unity绘制Gizmos
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            // 绘制检测范围
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, _detectionRange);

            // 绘制攻击范围
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _attackRange);

            // 绘制移动目标
            if (_isMoving)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, _targetPosition);
                Gizmos.DrawWireSphere(_targetPosition, 0.5f);
            }
        }
    }
}