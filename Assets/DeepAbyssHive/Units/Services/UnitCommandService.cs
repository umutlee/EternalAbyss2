using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Services;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.Units.Config;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 单位命令服务实现
    /// 提供所有单位相关的修改操作功能
    /// </summary>
    public class UnitCommandService : IUnitCommandService, ICommandService, IService
    {
        #region 私有字段
        private readonly Dictionary<int, UnitHotData> _unitHotData;
        private readonly Dictionary<int, UnitColdData> _unitColdData;
        private readonly Dictionary<int, GameObject> _unitGameObjects;
        private readonly Dictionary<int, SpatialNode> _unitSpatialNodes;
        private readonly ISpatialIndexService _spatialIndex;
        private readonly Dictionary<UnitType, string> _unitPrefabPaths;
        private readonly UnitConfigSO _config;
        private int _nextUnitId;
        private readonly string _serviceName = "UnitCommandService";
        
        // 進化和環境適應相關
        private readonly Dictionary<string, EvolutionPath> _evolutionPaths;
        private readonly Dictionary<string, EnvironmentAdaptation> _environmentAdaptations;
        #endregion

        #region IService属性实现
        public string ServiceName => _serviceName;
        public bool IsInitialized { get; private set; }
        public bool IsCommandAvailable => IsInitialized;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public UnitCommandService(
            Dictionary<int, UnitHotData> unitHotData,
            Dictionary<int, UnitColdData> unitColdData,
            Dictionary<int, GameObject> unitGameObjects,
            Dictionary<int, SpatialNode> unitSpatialNodes,
            ISpatialIndexService spatialIndex,
            Dictionary<UnitType, string> unitPrefabPaths,
            Dictionary<string, EvolutionPath> evolutionPaths,
            Dictionary<string, EnvironmentAdaptation> environmentAdaptations,
            UnitConfigSO config,
            int nextUnitId)
        {
            _unitHotData = unitHotData ?? throw new System.ArgumentNullException(nameof(unitHotData));
            _unitColdData = unitColdData ?? throw new System.ArgumentNullException(nameof(unitColdData));
            _unitGameObjects = unitGameObjects ?? throw new System.ArgumentNullException(nameof(unitGameObjects));
            _unitSpatialNodes = unitSpatialNodes ?? throw new System.ArgumentNullException(nameof(unitSpatialNodes));
            _spatialIndex = spatialIndex;
            _unitPrefabPaths = unitPrefabPaths ?? throw new System.ArgumentNullException(nameof(unitPrefabPaths));
            _evolutionPaths = evolutionPaths ?? throw new System.ArgumentNullException(nameof(evolutionPaths));
            _environmentAdaptations = environmentAdaptations ?? throw new System.ArgumentNullException(nameof(environmentAdaptations));
            _config = config;
            _nextUnitId = nextUnitId;
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
                
            Debug.Log($"[{_serviceName}] 初始化单位命令服务");
            IsInitialized = true;
        }

        /// <summary>
        /// 清理服务
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[{_serviceName}] 清理单位命令服务");
            IsInitialized = false;
        }
        #endregion

        #region IUnitCommandService接口实现
        /// <summary>
        /// 创建单位
        /// </summary>
        /// <param name="unitType">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="rotation">旋转（可选）</param>
        /// <returns>创建的单位ID，失败返回-1</returns>
        public int CreateUnit(UnitType unitType, Vector3 position, int playerId, Quaternion? rotation = null)
        {
            try
            {
                int unitId = _nextUnitId++;
                
                // 获取单位属性
                UnitAttributes baseAttributes = GetBaseAttributesForType(unitType);
                
                // 创建冷数据
                UnitColdData coldData = new UnitColdData
                {
                    Id = unitId,
                    Type = unitType,
                    OwnerId = playerId,
                    BaseAttributes = baseAttributes,
                    Attributes = baseAttributes, // 初始时相同
                    Evolution = new EvolutionInfo { Level = 0, PathId = "", UnlockedAbilities = new string[0] },
                    AdaptiveTraits = new AdaptiveTrait[0]
                };
                
                // 创建热数据
                UnitHotData hotData = new UnitHotData
                {
                    Id = unitId,
                    Position = position,
                    Rotation = rotation ?? Quaternion.identity,
                    Velocity = Vector3.zero,
                    State = UnitState.Idle,
                    Health = baseAttributes.MaxHealth,
                    Energy = baseAttributes.MaxEnergy,
                    TargetId = -1,
                    StateTimer = 0f,
                    MovementPath = new List<Vector3>()
                };
                
                // 创建游戏对象
                GameObject unitGameObject = CreateUnitGameObject(unitType, position, rotation ?? Quaternion.identity);
                if (unitGameObject == null)
                {
                    Debug.LogError($"[{_serviceName}] 创建单位游戏对象失败: {unitType}");
                    return -1;
                }
                
                // 创建空间节点
                Bounds bounds = new Bounds(position, new Vector3(baseAttributes.SightRange, baseAttributes.SightRange, baseAttributes.SightRange));
                SpatialNode spatialNode = new SpatialNode(unitId, unitGameObject, position, bounds);
                
                // 添加到字典
                _unitColdData[unitId] = coldData;
                _unitHotData[unitId] = hotData;
                _unitGameObjects[unitId] = unitGameObject;
                _unitSpatialNodes[unitId] = spatialNode;
                
                // 添加到空间索引
                if (_spatialIndex != null)
                {
                    _spatialIndex.AddObject(unitId, position, bounds, SpatialObjectType.Unit);
                }
                
                Debug.Log($"[{_serviceName}] 创建单位成功: ID={unitId}, 类型={unitType}, 位置={position}");
                return unitId;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{_serviceName}] 创建单位失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 销毁单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>是否成功</returns>
        public bool DestroyUnit(int unitId)
        {
            try
            {
                // 从空间索引移除
                if (_spatialIndex != null && _unitSpatialNodes.TryGetValue(unitId, out var spatialNode))
                {
                    _spatialIndex.RemoveObject(unitId);
                }
                
                // 销毁游戏对象
                if (_unitGameObjects.TryGetValue(unitId, out var gameObject))
                {
                    if (gameObject != null)
                        Object.Destroy(gameObject);
                }
                
                // 从字典移除
                _unitHotData.Remove(unitId);
                _unitColdData.Remove(unitId);
                _unitGameObjects.Remove(unitId);
                _unitSpatialNodes.Remove(unitId);
                
                Debug.Log($"[{_serviceName}] 销毁单位成功: ID={unitId}");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[{_serviceName}] 销毁单位失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 移动单位到指定位置
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否成功</returns>
        public bool MoveUnit(int unitId, Vector3 targetPosition)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData) || 
                !_unitColdData.TryGetValue(unitId, out var coldData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试移动不存在的单位: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.State = UnitState.Moving;
            updatedHotData.TargetId = -1;
            updatedHotData.StateTimer = 0f;
            
            // 计算移动方向
            Vector3 direction = (targetPosition - updatedHotData.Position).normalized;
            
            // 设置速度
            updatedHotData.Velocity = direction * coldData.Attributes.MoveSpeed;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                updatedHotData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 回設到字典
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 移动单位: ID={unitId}, 目标位置={targetPosition}");
            return true;
        }

        /// <summary>
        /// 单位攻击目标
        /// </summary>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="targetId">目标ID</param>
        /// <returns>是否成功</returns>
        public bool AttackTarget(int attackerId, int targetId)
        {
            if (!_unitHotData.TryGetValue(attackerId, out var attackerHotData) ||
                !_unitColdData.TryGetValue(attackerId, out var attackerColdData))
            {
                Debug.LogWarning($"[{_serviceName}] 攻击者不存在: {attackerId}");
                return false;
            }
            
            if (!_unitHotData.TryGetValue(targetId, out var targetHotData))
            {
                Debug.LogWarning($"[{_serviceName}] 攻击目标不存在: {targetId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedAttackerData = attackerHotData;
            updatedAttackerData.State = UnitState.Attacking;
            updatedAttackerData.TargetId = targetId;
            updatedAttackerData.StateTimer = 0f;
            
            // 计算方向
            Vector3 direction = (targetHotData.Position - updatedAttackerData.Position).normalized;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                updatedAttackerData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 停止移动
            updatedAttackerData.Velocity = Vector3.zero;
            
            // 回設到字典
            _unitHotData[attackerId] = updatedAttackerData;
            
            Debug.Log($"[{_serviceName}] 攻击目标: 攻击者={attackerId}, 目标={targetId}");
            return true;
        }

        /// <summary>
        /// 单位攻击位置
        /// </summary>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="targetPosition">目标位置</param>
        /// <returns>是否成功</returns>
        public bool AttackPosition(int attackerId, Vector3 targetPosition)
        {
            if (!_unitHotData.TryGetValue(attackerId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 攻击者不存在: {attackerId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.State = UnitState.Attacking;
            updatedHotData.TargetId = -1; // 攻击位置时没有目标ID
            updatedHotData.StateTimer = 0f;
            
            // 计算方向
            Vector3 direction = (targetPosition - updatedHotData.Position).normalized;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                updatedHotData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 停止移动
            updatedHotData.Velocity = Vector3.zero;
            
            // 回設到字典
            _unitHotData[attackerId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 攻击位置: 攻击者={attackerId}, 目标位置={targetPosition}");
            return true;
        }

        /// <summary>
        /// 停止单位行动
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>是否成功</returns>
        public bool StopUnit(int unitId)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试停止不存在的单位: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.State = UnitState.Idle;
            updatedHotData.TargetId = -1;
            updatedHotData.Velocity = Vector3.zero;
            updatedHotData.StateTimer = 0f;
            
            // 回設到字典
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 停止单位: ID={unitId}");
            return true;
        }

        /// <summary>
        /// 单位进化
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetType">目标类型</param>
        /// <returns>是否成功</returns>
        public bool EvolveUnit(int unitId, UnitType targetType)
        {
            // 简化实现，实际应该根据进化路径来处理
            if (!_unitColdData.TryGetValue(unitId, out var coldData) ||
                !_unitHotData.TryGetValue(unitId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试进化不存在的单位: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.State = UnitState.Evolving;
            updatedHotData.StateTimer = 5f; // 简化的进化时间
            
            var updatedColdData = coldData;
            updatedColdData.Type = targetType;
            var evolutionInfo = (EvolutionInfo)updatedColdData.Evolution;
            evolutionInfo.Level++;
            updatedColdData.Evolution = evolutionInfo;
            
            // 回設到字典
            _unitHotData[unitId] = updatedHotData;
            _unitColdData[unitId] = updatedColdData;
            
            Debug.Log($"[{_serviceName}] 单位进化: ID={unitId}, 目标类型={targetType}");
            return true;
        }

        /// <summary>
        /// 设置单位状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="state">新状态</param>
        /// <returns>是否成功</returns>
        public bool SetUnitState(int unitId, UnitState state)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试设置不存在单位的状态: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.State = state;
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 设置单位状态: ID={unitId}, 状态={state}");
            return true;
        }

        /// <summary>
        /// 修改单位属性
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="attributeType">属性类型</param>
        /// <param name="value">新值</param>
        /// <returns>是否成功</returns>
        public bool ModifyUnitAttribute(int unitId, UnitAttributeType attributeType, float value)
        {
            if (!_unitColdData.TryGetValue(unitId, out var coldData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试修改不存在单位的属性: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedColdData = coldData;
            var updatedAttributes = updatedColdData.Attributes;
            
            // 根据属性类型修改对应属性
            switch (attributeType)
            {
                case UnitAttributeType.MaxHealth:
                    updatedAttributes.MaxHealth = value;
                    break;
                case UnitAttributeType.Attack:
                    updatedAttributes.AttackDamage = value;
                    break;
                case UnitAttributeType.Defense:
                    // 假设防御影响生命值
                    updatedAttributes.MaxHealth *= (1 + value * 0.1f);
                    break;
                case UnitAttributeType.Speed:
                    updatedAttributes.MoveSpeed = value;
                    break;
                case UnitAttributeType.AttackRange:
                    updatedAttributes.AttackRange = value;
                    break;
                case UnitAttributeType.AttackSpeed:
                    updatedAttributes.AttackSpeed = value;
                    break;
            }
            
            updatedColdData.Attributes = updatedAttributes;
            _unitColdData[unitId] = updatedColdData;
            
            Debug.Log($"[{_serviceName}] 修改单位属性: ID={unitId}, 属性={attributeType}, 值={value}");
            return true;
        }

        /// <summary>
        /// 治疗单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="healAmount">治疗量</param>
        /// <returns>是否成功</returns>
        public bool HealUnit(int unitId, float healAmount)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData) ||
                !_unitColdData.TryGetValue(unitId, out var coldData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试治疗不存在的单位: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.Health = Mathf.Min(updatedHotData.Health + healAmount, coldData.Attributes.MaxHealth);
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 治疗单位: ID={unitId}, 治疗量={healAmount}, 当前生命={hotData.Health}");
            return true;
        }

        /// <summary>
        /// 对单位造成伤害
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="damage">伤害量</param>
        /// <param name="damageType">伤害类型</param>
        /// <returns>是否成功</returns>
        public bool DamageUnit(int unitId, float damage, DamageType damageType = DamageType.Physical)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试伤害不存在的单位: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.Health = Mathf.Max(0, updatedHotData.Health - damage);
            
            // 检查单位是否死亡
            if (updatedHotData.Health <= 0)
            {
                updatedHotData.State = UnitState.Dead;
            }
            
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 伤害单位: ID={unitId}, 伤害={damage}, 类型={damageType}, 剩余生命={hotData.Health}");
            return true;
        }

        /// <summary>
        /// 设置单位AI行为
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="behaviorType">行为类型</param>
        /// <returns>是否成功</returns>
        public bool SetUnitBehavior(int unitId, UnitBehaviorType behaviorType)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试设置不存在单位的行为: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            
            // 根据行为类型设置相应状态
            switch (behaviorType)
            {
                case UnitBehaviorType.Idle:
                    updatedHotData.State = UnitState.Idle;
                    break;
                case UnitBehaviorType.Aggressive:
                    // 可以设置特殊标记或状态
                    break;
                case UnitBehaviorType.Defensive:
                    // 可以设置特殊标记或状态
                    break;
            }
            
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 设置单位行为: ID={unitId}, 行为={behaviorType}");
            return true;
        }

        /// <summary>
        /// 单位适应环境
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="environmentType">环境类型</param>
        /// <returns>是否成功</returns>
        public bool AdaptToEnvironment(int unitId, EnvironmentType environmentType)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData))
            {
                Debug.LogWarning($"[{_serviceName}] 尝试适应不存在的单位: {unitId}");
                return false;
            }
            
            // 取值→修改→回設模式
            var updatedHotData = hotData;
            updatedHotData.State = UnitState.Adapting;
            updatedHotData.StateTimer = 8f; // 简化的适应时间
            
            _unitHotData[unitId] = updatedHotData;
            
            Debug.Log($"[{_serviceName}] 单位适应环境: ID={unitId}, 环境={environmentType}");
            return true;
        }

        /// <summary>
        /// 批量移动单位
        /// </summary>
        /// <param name="unitIds">单位ID数组</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="formation">编队类型</param>
        /// <returns>成功移动的单位数量</returns>
        public int MoveUnitsInFormation(int[] unitIds, Vector3 targetPosition, FormationType formation = FormationType.None)
        {
            int successCount = 0;
            
            for (int i = 0; i < unitIds.Length; i++)
            {
                Vector3 unitTargetPosition = CalculateFormationPosition(targetPosition, i, unitIds.Length, formation);
                if (MoveUnit(unitIds[i], unitTargetPosition))
                {
                    successCount++;
                }
            }
            
            Debug.Log($"[{_serviceName}] 批量移动单位: 成功={successCount}/{unitIds.Length}, 编队={formation}");
            return successCount;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 获取单位类型的基础属性
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <returns>单位属性</returns>
        private UnitAttributes GetBaseAttributesForType(UnitType type)
        {
            UnitAttributes attributes = new UnitAttributes();
            
            switch (type)
            {
                case UnitType.Worker:
                    attributes.MaxHealth = 50f;
                    attributes.MoveSpeed = 3f;
                    attributes.AttackDamage = 5f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 1f;
                    attributes.SightRange = 10f;
                    attributes.ResourceGatherRate = 1f;
                    attributes.BuildSpeed = 1f;
                    attributes.MaxEnergy = 100f;
                    break;
                    
                case UnitType.Warrior:
                    attributes.MaxHealth = 100f;
                    attributes.MoveSpeed = 3.5f;
                    attributes.AttackDamage = 15f;
                    attributes.AttackSpeed = 1.2f;
                    attributes.AttackRange = 1.5f;
                    attributes.SightRange = 12f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    attributes.MaxEnergy = 80f;
                    break;
                    
                // 其他单位类型...
                default:
                    attributes.MaxHealth = 50f;
                    attributes.MoveSpeed = 3f;
                    attributes.AttackDamage = 10f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 1f;
                    attributes.SightRange = 10f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    attributes.MaxEnergy = 100f;
                    break;
            }
            
            return attributes;
        }

        /// <summary>
        /// 创建单位游戏对象
        /// </summary>
        /// <param name="unitType">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="rotation">旋转</param>
        /// <returns>游戏对象</returns>
        private GameObject CreateUnitGameObject(UnitType unitType, Vector3 position, Quaternion rotation)
        {
            if (_unitPrefabPaths.TryGetValue(unitType, out string prefabPath))
            {
                GameObject prefab = Resources.Load<GameObject>(prefabPath);
                if (prefab != null)
                {
                    return Object.Instantiate(prefab, position, rotation);
                }
            }
            
            // 创建默认游戏对象
            GameObject defaultUnit = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            defaultUnit.transform.position = position;
            defaultUnit.transform.rotation = rotation;
            defaultUnit.name = $"Unit_{unitType}";
            
            return defaultUnit;
        }

        /// <summary>
        /// 计算编队位置
        /// </summary>
        /// <param name="centerPosition">中心位置</param>
        /// <param name="unitIndex">单位索引</param>
        /// <param name="totalUnits">总单位数</param>
        /// <param name="formation">编队类型</param>
        /// <returns>单位目标位置</returns>
        private Vector3 CalculateFormationPosition(Vector3 centerPosition, int unitIndex, int totalUnits, FormationType formation)
        {
            switch (formation)
            {
                case FormationType.Line:
                    return centerPosition + Vector3.right * (unitIndex - totalUnits / 2f) * 2f;
                    
                case FormationType.Column:
                    return centerPosition + Vector3.forward * (unitIndex - totalUnits / 2f) * 2f;
                    
                case FormationType.Circle:
                    float angle = (float)unitIndex / totalUnits * 2f * Mathf.PI;
                    float radius = Mathf.Max(2f, totalUnits * 0.5f);
                    return centerPosition + new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
                    
                default:
                    return centerPosition + Random.insideUnitSphere * 3f;
            }
        }
        #endregion

        #region 内部类
        /// <summary>
        /// 进化路径类
        /// </summary>
        public class EvolutionPath
        {
            public string PathId;
            public UnitType RequiredUnitType;
            public int MaxLevel;
            public float EvolutionTime;
            public Dictionary<int, AttributeModifier[]> AttributeModifiersByLevel = new Dictionary<int, AttributeModifier[]>();
            public Dictionary<int, string[]> UnlockedAbilitiesByLevel = new Dictionary<int, string[]>();
        }
        
        /// <summary>
        /// 环境适应类
        /// </summary>
        public class EnvironmentAdaptation
        {
            public string TraitId;
            public string EnvironmentType;
            public int MaxLevel;
            public float AdaptationTime;
            public Dictionary<int, AttributeModifier[]> ModifiersByLevel = new Dictionary<int, AttributeModifier[]>();
        }
        #endregion
    }
}