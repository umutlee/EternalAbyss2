using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Units.Interfaces;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器，负责管理所有单位
    /// </summary>
    public class UnitManager : IUnitManager
    {
        #region 私有字段
        private Dictionary<int, UnitHotData> _unitHotData = new Dictionary<int, UnitHotData>();
        private Dictionary<int, UnitColdData> _unitColdData = new Dictionary<int, UnitColdData>();
        private Dictionary<int, GameObject> _unitGameObjects = new Dictionary<int, GameObject>();
        private Dictionary<int, SpatialNode> _unitSpatialNodes = new Dictionary<int, SpatialNode>();
        private ISpatialIndex<SpatialNode> _spatialIndex;
        private int _nextUnitId = 1;
        private bool _isInitialized = false;
        private bool _isPaused = false;
        private string _managerName = "UnitManager";
        private Dictionary<UnitType, string> _unitPrefabPaths = new Dictionary<UnitType, string>();
        private Dictionary<string, EvolutionPath> _evolutionPaths = new Dictionary<string, EvolutionPath>();
        private Dictionary<string, EnvironmentAdaptation> _environmentAdaptations = new Dictionary<string, EnvironmentAdaptation>();
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="spatialIndex">空间索引系统</param>
        public UnitManager(ISpatialIndex<SpatialNode> spatialIndex)
        {
            _spatialIndex = spatialIndex;
        }
        #endregion

        #region IUnitManager接口实现
        /// <summary>
        /// 创建单位
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>单位ID</returns>
        public int CreateUnit(UnitType type, Vector3 position, int ownerId)
        {
            int unitId = _nextUnitId++;
            
            // 创建单位冷数据
            UnitColdData coldData = new UnitColdData
            {
                UnitId = unitId,
                Type = type,
                OwnerId = ownerId,
                BaseAttributes = GetBaseAttributesForType(type),
                Evolution = new EvolutionInfo
                {
                    Level = 0,
                    PathId = "",
                    UnlockedAbilities = new string[0]
                },
                AdaptiveTraits = new AdaptiveTrait[0],
                PrefabPath = GetPrefabPathForType(type)
            };
            
            // 创建单位热数据
            UnitHotData hotData = new UnitHotData
            {
                Position = position,
                Rotation = Quaternion.identity,
                Velocity = Vector3.zero,
                Health = coldData.BaseAttributes.MaxHealth,
                TargetId = -1,
                State = UnitState.Idle,
                StateTimer = 0f
            };
            
            // 存储数据
            _unitColdData[unitId] = coldData;
            _unitHotData[unitId] = hotData;
            
            // 实例化单位游戏对象
            GameObject unitObject = InstantiateUnitObject(coldData, hotData);
            if (unitObject != null)
            {
                _unitGameObjects[unitId] = unitObject;
            }
            
            // 创建空间节点并添加到空间索引
            if (_spatialIndex != null)
            {
                SpatialNode spatialNode = new SpatialNode(
                    unitId, 
                    unitObject, 
                    position, 
                    new Bounds(position, Vector3.one * coldData.BaseAttributes.SightRange),
                    "Unit",
                    0,
                    false
                );
                _unitSpatialNodes[unitId] = spatialNode;
                _spatialIndex.Insert(spatialNode, position, Vector3.one * coldData.BaseAttributes.SightRange);
            }
            
            Debug.Log($"[{_managerName}] 创建单位: ID={unitId}, 类型={type}, 所有者={ownerId}, 位置={position}");
            
            return unitId;
        }

        /// <summary>
        /// 销毁单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        public void DestroyUnit(int unitId)
        {
            if (!_unitColdData.ContainsKey(unitId))
            {
                Debug.LogWarning($"[{_managerName}] 尝试销毁不存在的单位: {unitId}");
                return;
            }
            
            // 从空间索引中移除
            if (_spatialIndex != null && _unitSpatialNodes.TryGetValue(unitId, out SpatialNode spatialNode))
            {
                UnitColdData coldData = _unitColdData[unitId];
                UnitHotData hotData = _unitHotData[unitId];
                _spatialIndex.Remove(spatialNode, hotData.Position, Vector3.one * coldData.BaseAttributes.SightRange);
                _unitSpatialNodes.Remove(unitId);
            }
            
            // 销毁游戏对象
            if (_unitGameObjects.TryGetValue(unitId, out GameObject unitObject) && unitObject != null)
            {
                GameObject.Destroy(unitObject);
                _unitGameObjects.Remove(unitId);
            }
            
            // 移除数据
            _unitColdData.Remove(unitId);
            _unitHotData.Remove(unitId);
            
            Debug.Log($"[{_managerName}] 销毁单位: ID={unitId}");
        }

        /// <summary>
        /// 移动单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetPosition">目标位置</param>
        public void MoveUnit(int unitId, Vector3 targetPosition)
        {
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试移动不存在的单位: {unitId}");
                return;
            }
            
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                return;
            }
            
            // 更新单位状态
            hotData.State = UnitState.Moving;
            hotData.TargetId = -1;
            hotData.StateTimer = 0f;
            
            // 计算移动方向
            Vector3 direction = (targetPosition - hotData.Position).normalized;
            
            // 设置速度
            hotData.Velocity = direction * coldData.BaseAttributes.MoveSpeed;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                hotData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 更新数据
            _unitHotData[unitId] = hotData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            Debug.Log($"[{_managerName}] 移动单位: ID={unitId}, 目标位置={targetPosition}");
        }

        /// <summary>
        /// 攻击目标
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="targetId">目标ID</param>
        public void AttackTarget(int unitId, int targetId)
        {
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试攻击不存在的单位: {unitId}");
                return;
            }
            
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                return;
            }
            
            if (!_unitHotData.TryGetValue(targetId, out UnitHotData targetHotData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试攻击不存在的目标: {targetId}");
                return;
            }
            
            // 更新单位状态
            hotData.State = UnitState.Attacking;
            hotData.TargetId = targetId;
            hotData.StateTimer = 0f;
            
            // 计算方向
            Vector3 direction = (targetHotData.Position - hotData.Position).normalized;
            
            // 更新旋转
            if (direction != Vector3.zero)
            {
                hotData.Rotation = Quaternion.LookRotation(direction);
            }
            
            // 停止移动
            hotData.Velocity = Vector3.zero;
            
            // 更新数据
            _unitHotData[unitId] = hotData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            Debug.Log($"[{_managerName}] 攻击目标: ID={unitId}, 目标ID={targetId}");
        }

        /// <summary>
        /// 获取单位热数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位热数据</returns>
        public UnitHotData GetUnitHotData(int unitId)
        {
            if (_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                return hotData;
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的单位热数据: {unitId}");
            return new UnitHotData();
        }

        /// <summary>
        /// 获取单位冷数据
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位冷数据</returns>
        public UnitColdData? GetUnitColdData(int unitId)
        {
            if (_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                return coldData;
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的单位冷数据: {unitId}");
            return null;
        }

        /// <summary>
        /// 获取范围内的单位
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>单位ID数组</returns>
        public NativeArray<int> GetUnitsInRange(Vector3 position, float radius)
        {
            if (_spatialIndex != null)
            {
                // 使用空间索引查询
                List<SpatialNode> spatialResults = _spatialIndex.QueryRange(position, Vector3.one * radius * 2);
                
                // 过滤距离并转换为单位ID
                List<int> unitsInRange = new List<int>();
                foreach (var spatialNode in spatialResults)
                {
                    if (Vector3.Distance(spatialNode.Position, position) <= radius)
                    {
                        unitsInRange.Add(spatialNode.Id);
                    }
                }
                
                // 转换为NativeArray
                NativeArray<int> result = new NativeArray<int>(unitsInRange.Count, Allocator.Temp);
                for (int i = 0; i < unitsInRange.Count; i++)
                {
                    result[i] = unitsInRange[i];
                }
                
                return result;
            }
            
            // 如果没有空间索引，使用暴力搜索
            List<int> unitsInRangeFallback = new List<int>();
            foreach (var pair in _unitHotData)
            {
                int unitId = pair.Key;
                UnitHotData hotData = pair.Value;
                
                if (Vector3.Distance(hotData.Position, position) <= radius)
                {
                    unitsInRangeFallback.Add(unitId);
                }
            }
            
            // 转换为NativeArray
            NativeArray<int> resultFallback = new NativeArray<int>(unitsInRangeFallback.Count, Allocator.Temp);
            for (int i = 0; i < unitsInRangeFallback.Count; i++)
            {
                resultFallback[i] = unitsInRangeFallback[i];
            }
            
            return resultFallback;
        }

        /// <summary>
        /// 获取指定类型和所有者的单位
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>单位ID数组</returns>
        public NativeArray<int> GetUnitsOfType(UnitType type, int ownerId)
        {
            List<int> units = new List<int>();
            
            foreach (var pair in _unitColdData)
            {
                int unitId = pair.Key;
                UnitColdData coldData = pair.Value;
                
                if (coldData.Type == type && coldData.OwnerId == ownerId)
                {
                    units.Add(unitId);
                }
            }
            
            // 转换为NativeArray
            NativeArray<int> result = new NativeArray<int>(units.Count, Allocator.Temp);
            for (int i = 0; i < units.Count; i++)
            {
                result[i] = units[i];
            }
            
            return result;
        }

        /// <summary>
        /// 进化单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="evolutionPath">进化路径ID</param>
        /// <returns>是否成功</returns>
        public bool EvolveUnit(int unitId, string evolutionPath)
        {
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试进化不存在的单位: {unitId}");
                return false;
            }
            
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                return false;
            }
            
            // 检查进化路径是否存在
            if (!_evolutionPaths.TryGetValue(evolutionPath, out EvolutionPath path))
            {
                Debug.LogWarning($"[{_managerName}] 尝试使用不存在的进化路径: {evolutionPath}");
                return false;
            }
            
            // 检查单位类型是否匹配
            if (path.RequiredUnitType != coldData.Type)
            {
                Debug.LogWarning($"[{_managerName}] 单位类型不匹配进化路径: {coldData.Type} != {path.RequiredUnitType}");
                return false;
            }
            
            // 检查进化等级
            int nextLevel = coldData.Evolution.Level + 1;
            if (nextLevel > path.MaxLevel)
            {
                Debug.LogWarning($"[{_managerName}] 单位已达到最大进化等级: {coldData.Evolution.Level}");
                return false;
            }
            
            // 更新单位状态
            hotData.State = UnitState.Evolving;
            hotData.StateTimer = path.EvolutionTime;
            _unitHotData[unitId] = hotData;
            
            // 更新进化信息
            coldData.Evolution.PathId = evolutionPath;
            coldData.Evolution.Level = nextLevel;
            
            // 解锁新能力
            if (path.UnlockedAbilitiesByLevel.TryGetValue(nextLevel, out string[] abilities))
            {
                coldData.Evolution.UnlockedAbilities = abilities;
            }
            
            // 应用属性修改
            if (path.AttributeModifiersByLevel.TryGetValue(nextLevel, out AttributeModifier[] modifiers))
            {
                ApplyAttributeModifiers(ref coldData.BaseAttributes, modifiers);
            }
            
            // 更新单位冷数据
            _unitColdData[unitId] = coldData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            Debug.Log($"[{_managerName}] 单位进化: ID={unitId}, 路径={evolutionPath}, 等级={nextLevel}");
            
            return true;
        }

        /// <summary>
        /// 使单位适应环境
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="environmentType">环境类型</param>
        public void AdaptToEnvironment(int unitId, string environmentType)
        {
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试适应不存在的单位: {unitId}");
                return;
            }
            
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                return;
            }
            
            // 检查环境适应是否存在
            if (!_environmentAdaptations.TryGetValue(environmentType, out EnvironmentAdaptation adaptation))
            {
                Debug.LogWarning($"[{_managerName}] 尝试适应不存在的环境类型: {environmentType}");
                return;
            }
            
            // 检查单位是否已经适应该环境
            bool hasAdaptation = false;
            int adaptationIndex = -1;
            
            for (int i = 0; i < coldData.AdaptiveTraits.Length; i++)
            {
                if (coldData.AdaptiveTraits[i].EnvironmentType == environmentType)
                {
                    hasAdaptation = true;
                    adaptationIndex = i;
                    break;
                }
            }
            
            // 更新单位状态
            hotData.State = UnitState.Adapting;
            hotData.StateTimer = adaptation.AdaptationTime;
            _unitHotData[unitId] = hotData;
            
            // 如果已经有适应性特征，提升等级
            if (hasAdaptation)
            {
                AdaptiveTrait trait = coldData.AdaptiveTraits[adaptationIndex];
                int nextLevel = trait.Level + 1;
                
                if (nextLevel <= adaptation.MaxLevel)
                {
                    trait.Level = nextLevel;
                    
                    // 更新修改器
                    if (adaptation.ModifiersByLevel.TryGetValue(nextLevel, out AttributeModifier[] modifiers))
                    {
                        trait.Modifiers = modifiers;
                    }
                    
                    coldData.AdaptiveTraits[adaptationIndex] = trait;
                }
            }
            else
            {
                // 创建新的适应性特征
                AdaptiveTrait newTrait = new AdaptiveTrait
                {
                    TraitId = adaptation.TraitId,
                    Level = 1,
                    EnvironmentType = environmentType,
                    Modifiers = adaptation.ModifiersByLevel.ContainsKey(1) ? adaptation.ModifiersByLevel[1] : new AttributeModifier[0]
                };
                
                // 添加到特征列表
                AdaptiveTrait[] newTraits = new AdaptiveTrait[coldData.AdaptiveTraits.Length + 1];
                Array.Copy(coldData.AdaptiveTraits, newTraits, coldData.AdaptiveTraits.Length);
                newTraits[coldData.AdaptiveTraits.Length] = newTrait;
                coldData.AdaptiveTraits = newTraits;
            }
            
            // 更新单位冷数据
            _unitColdData[unitId] = coldData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            Debug.Log($"[{_managerName}] 单位适应环境: ID={unitId}, 环境={environmentType}");
        }
        #endregion

        #region IManager接口实现
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            Debug.Log($"[{_managerName}] 初始化单位管理器");
            
            // 初始化单位预制体路径
            InitializeUnitPrefabPaths();
            
            // 初始化进化路径
            InitializeEvolutionPaths();
            
            // 初始化环境适应
            InitializeEnvironmentAdaptations();
            
            _isInitialized = true;
            Debug.Log($"[{_managerName}] 单位管理器初始化完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        public void Update(float deltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 更新所有单位
            List<int> unitIds = new List<int>(_unitHotData.Keys);
            foreach (int unitId in unitIds)
            {
                UpdateUnit(unitId, deltaTime);
            }
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        public void FixedUpdate(float fixedDeltaTime)
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 在这里可以添加物理相关的更新逻辑
        }

        /// <summary>
        /// 后更新管理器
        /// </summary>
        public void LateUpdate()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 在这里可以添加后更新逻辑
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            Debug.Log($"[{_managerName}] 清理单位管理器");
            
            // 销毁所有单位
            List<int> unitIds = new List<int>(_unitColdData.Keys);
            foreach (int unitId in unitIds)
            {
                DestroyUnit(unitId);
            }
            
            _unitHotData.Clear();
            _unitColdData.Clear();
            _unitGameObjects.Clear();
            _unitSpatialNodes.Clear();
            _isInitialized = false;
            
            Debug.Log($"[{_managerName}] 单位管理器清理完成");
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            if (_isPaused)
                return;
                
            _isPaused = true;
            Debug.Log($"[{_managerName}] 单位管理器已暂停");
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            if (!_isPaused)
                return;
                
            _isPaused = false;
            Debug.Log($"[{_managerName}] 单位管理器已恢复");
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        public string GetManagerName()
        {
            return _managerName;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 更新单位
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateUnit(int unitId, float deltaTime)
        {
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
                return;
                
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
                return;
                
            SpatialNode spatialNode;

            // 更新状态计时器
            if (hotData.StateTimer > 0)
            {
                hotData.StateTimer -= deltaTime;
            }
            
            // 根据状态更新单位
            switch (hotData.State)
            {
                case UnitState.Idle:
                    // 空闲状态，不需要特殊处理
                    break;
                    
                case UnitState.Moving:
                    // 更新位置
                    hotData.Position += hotData.Velocity * deltaTime;
                    
                            // 更新空间索引
                            if (_spatialIndex != null && _unitSpatialNodes.TryGetValue(unitId, out spatialNode))
                            {
                                Vector3 oldPosition = hotData.Position - hotData.Velocity * deltaTime;
                                _spatialIndex.Update(spatialNode, oldPosition, hotData.Position, Vector3.one * coldData.BaseAttributes.SightRange);
                            }
                    break;
                    
                case UnitState.Attacking:
                    // 检查目标是否存在
                    if (!_unitHotData.TryGetValue(hotData.TargetId, out UnitHotData targetHotData))
                    {
                        // 目标不存在，回到空闲状态
                        hotData.State = UnitState.Idle;
                        hotData.TargetId = -1;
                        break;
                    }
                    
                    // 检查是否在攻击范围内
                    float distanceToTarget = Vector3.Distance(hotData.Position, targetHotData.Position);
                    if (distanceToTarget > coldData.BaseAttributes.AttackRange)
                    {
                        // 目标不在攻击范围内，移动向目标
                        Vector3 direction = (targetHotData.Position - hotData.Position).normalized;
                        hotData.Velocity = direction * coldData.BaseAttributes.MoveSpeed;
                        hotData.Position += hotData.Velocity * deltaTime;
                        
                        // 更新旋转
                        if (direction != Vector3.zero)
                        {
                            hotData.Rotation = Quaternion.LookRotation(direction);
                        }
                        
                        // 更新空间索引
                        if (_spatialIndex != null && _unitSpatialNodes.TryGetValue(unitId, out spatialNode))
                        {
                            Vector3 oldPosition = hotData.Position - hotData.Velocity * deltaTime;
                            _spatialIndex.Update(spatialNode, oldPosition, hotData.Position, Vector3.one * coldData.BaseAttributes.SightRange);
                        }
                    }
                    else
                    {
                        // 目标在攻击范围内，停止移动
                        hotData.Velocity = Vector3.zero;
                        
                        // 更新旋转，面向目标
                        Vector3 direction = (targetHotData.Position - hotData.Position).normalized;
                        if (direction != Vector3.zero)
                        {
                            hotData.Rotation = Quaternion.LookRotation(direction);
                        }
                        
                        // 检查攻击冷却
                        if (hotData.StateTimer <= 0)
                        {
                            // 执行攻击
                            PerformAttack(unitId, hotData.TargetId);
                            
                            // 重置攻击冷却
                            hotData.StateTimer = 1f / coldData.BaseAttributes.AttackSpeed;
                        }
                    }
                    break;
                    
                case UnitState.Gathering:
                    // 资源收集逻辑
                    UpdateGatheringState(unitId, hotData, coldData, deltaTime);
                    break;
                    
                case UnitState.Building:
                    // 建造逻辑
                    UpdateBuildingState(unitId, hotData, coldData, deltaTime);
                    break;
                    
                case UnitState.Dead:
                    // 死亡状态，不需要更新
                    break;
                    
                case UnitState.Evolving:
                    // 进化状态
                    if (hotData.StateTimer <= 0)
                    {
                        // 进化完成，回到空闲状态
                        hotData.State = UnitState.Idle;
                        
                        // 更新单位外观
                        UpdateUnitAppearance(unitId);
                    }
                    break;
                    
                case UnitState.Adapting:
                    // 适应状态
                    if (hotData.StateTimer <= 0)
                    {
                        // 适应完成，回到空闲状态
                        hotData.State = UnitState.Idle;
                        
                        // 更新单位外观
                        UpdateUnitAppearance(unitId);
                    }
                    break;
            }
            
            // 更新单位热数据
            _unitHotData[unitId] = hotData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
        }

        /// <summary>
        /// 执行攻击
        /// </summary>
        /// <param name="attackerId">攻击者ID</param>
        /// <param name="targetId">目标ID</param>
        private void PerformAttack(int attackerId, int targetId)
        {
            if (!_unitColdData.TryGetValue(attackerId, out UnitColdData attackerData))
                return;
                
            if (!_unitHotData.TryGetValue(targetId, out UnitHotData targetHotData))
                return;
                
            // 计算伤害
            float damage = attackerData.BaseAttributes.AttackDamage;
            
            // 应用伤害
            targetHotData.Health -= damage;
            
            // 检查目标是否死亡
            if (targetHotData.Health <= 0)
            {
                targetHotData.Health = 0;
                targetHotData.State = UnitState.Dead;
                
                // 更新目标热数据
                _unitHotData[targetId] = targetHotData;
                
                // 更新目标游戏对象
                UpdateUnitGameObject(targetId, targetHotData);
                
                // 延迟销毁目标
                // 在实际实现中，可能需要使用协程或定时器
                // 这里简化处理，直接销毁
                DestroyUnit(targetId);
            }
            else
            {
                // 更新目标热数据
                _unitHotData[targetId] = targetHotData;
                
                // 更新目标游戏对象
                UpdateUnitGameObject(targetId, targetHotData);
            }
            
            Debug.Log($"[{_managerName}] 单位攻击: 攻击者={attackerId}, 目标={targetId}, 伤害={damage}, 目标剩余生命={targetHotData.Health}");
        }

        /// <summary>
        /// 实例化单位游戏对象
        /// </summary>
        /// <param name="coldData">单位冷数据</param>
        /// <param name="hotData">单位热数据</param>
        /// <returns>游戏对象</returns>
        private GameObject InstantiateUnitObject(UnitColdData coldData, UnitHotData hotData)
        {
            // 加载预制体
            GameObject prefab = Resources.Load<GameObject>(coldData.PrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[{_managerName}] 无法加载单位预制体: {coldData.PrefabPath}");
                return null;
            }
            
            // 实例化游戏对象
            GameObject unitObject = GameObject.Instantiate(prefab, hotData.Position, hotData.Rotation);
            
            // 设置名称
            unitObject.name = $"{coldData.Type}_{coldData.UnitId}";
            
            // 设置标签和层
            unitObject.tag = "Unit";
            unitObject.layer = LayerMask.NameToLayer("Units");
            
            // 添加单位组件
            // 在实际实现中，应该添加一个UnitComponent组件来管理单位的游戏对象
            // 这里简化处理，不添加额外组件
            
            Debug.Log($"[{_managerName}] 实例化单位游戏对象: {unitObject.name}");
            
            return unitObject;
        }

        /// <summary>
        /// 更新单位游戏对象
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">单位热数据</param>
        private void UpdateUnitGameObject(int unitId, UnitHotData hotData)
        {
            if (!_unitGameObjects.TryGetValue(unitId, out GameObject unitObject) || unitObject == null)
                return;
                
            // 更新位置和旋转
            unitObject.transform.position = hotData.Position;
            unitObject.transform.rotation = hotData.Rotation;
            
            // 在实际实现中，应该更新UnitComponent组件的状态
            // 这里简化处理，不更新额外组件
        }

        /// <summary>
        /// 更新单位外观
        /// </summary>
        /// <param name="unitId">单位ID</param>
        private void UpdateUnitAppearance(int unitId)
        {
            if (!_unitGameObjects.TryGetValue(unitId, out GameObject unitObject) || unitObject == null)
                return;
                
            if (!_unitColdData.TryGetValue(unitId, out UnitColdData coldData))
                return;
                
            // 在实际实现中，应该根据进化等级和适应性特征更新单位的外观
            // 这里简化处理，不更新外观
            
            Debug.Log($"[{_managerName}] 更新单位外观: ID={unitId}, 进化等级={coldData.Evolution.Level}, 适应性特征数量={coldData.AdaptiveTraits.Length}");
        }

        /// <summary>
        /// 应用属性修改器
        /// </summary>
        /// <param name="attributes">单位属性</param>
        /// <param name="modifiers">属性修改器数组</param>
        private void ApplyAttributeModifiers(ref UnitAttributes attributes, AttributeModifier[] modifiers)
        {
            if (modifiers == null || modifiers.Length == 0)
                return;
                
            foreach (var modifier in modifiers)
            {
                // 根据属性名称应用修改
                switch (modifier.AttributeName)
                {
                    case "MaxHealth":
                        ApplyModifier(ref attributes.MaxHealth, modifier);
                        break;
                        
                    case "MoveSpeed":
                        ApplyModifier(ref attributes.MoveSpeed, modifier);
                        break;
                        
                    case "AttackDamage":
                        ApplyModifier(ref attributes.AttackDamage, modifier);
                        break;
                        
                    case "AttackSpeed":
                        ApplyModifier(ref attributes.AttackSpeed, modifier);
                        break;
                        
                    case "AttackRange":
                        ApplyModifier(ref attributes.AttackRange, modifier);
                        break;
                        
                    case "SightRange":
                        ApplyModifier(ref attributes.SightRange, modifier);
                        break;
                        
                    case "ResourceGatherRate":
                        ApplyModifier(ref attributes.ResourceGatherRate, modifier);
                        break;
                        
                    case "BuildSpeed":
                        ApplyModifier(ref attributes.BuildSpeed, modifier);
                        break;
                }
            }
        }

        /// <summary>
        /// 应用单个属性修改器
        /// </summary>
        /// <param name="value">属性值</param>
        /// <param name="modifier">属性修改器</param>
        private void ApplyModifier(ref float value, AttributeModifier modifier)
        {
            switch (modifier.Type)
            {
                case AttributeModifier.ModifierType.Add:
                    value += modifier.Value;
                    break;
                    
                case AttributeModifier.ModifierType.Multiply:
                    value *= modifier.Value;
                    break;
                    
                case AttributeModifier.ModifierType.Set:
                    value = modifier.Value;
                    break;
            }
        }

        /// <summary>
        /// 获取单位类型的基础属性
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <returns>单位属性</returns>
        private UnitAttributes GetBaseAttributesForType(UnitType type)
        {
            // 在实际实现中，应该从配置文件或数据库中加载单位属性
            // 这里简化处理，直接返回硬编码的属性
            
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
                    break;
                    
                case UnitType.AcidSprayer:
                    attributes.MaxHealth = 60f;
                    attributes.MoveSpeed = 3f;
                    attributes.AttackDamage = 12f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 8f;
                    attributes.SightRange = 15f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Tank:
                    attributes.MaxHealth = 200f;
                    attributes.MoveSpeed = 2f;
                    attributes.AttackDamage = 20f;
                    attributes.AttackSpeed = 0.8f;
                    attributes.AttackRange = 2f;
                    attributes.SightRange = 10f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Scout:
                    attributes.MaxHealth = 40f;
                    attributes.MoveSpeed = 5f;
                    attributes.AttackDamage = 8f;
                    attributes.AttackSpeed = 1.5f;
                    attributes.AttackRange = 1f;
                    attributes.SightRange = 20f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Flyer:
                    attributes.MaxHealth = 70f;
                    attributes.MoveSpeed = 4f;
                    attributes.AttackDamage = 10f;
                    attributes.AttackSpeed = 1.2f;
                    attributes.AttackRange = 1.5f;
                    attributes.SightRange = 18f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 0f;
                    break;
                    
                case UnitType.Queen:
                    attributes.MaxHealth = 500f;
                    attributes.MoveSpeed = 2.5f;
                    attributes.AttackDamage = 30f;
                    attributes.AttackSpeed = 1f;
                    attributes.AttackRange = 3f;
                    attributes.SightRange = 15f;
                    attributes.ResourceGatherRate = 0f;
                    attributes.BuildSpeed = 2f;
                    break;
            }
            
            return attributes;
        }

        /// <summary>
        /// 获取单位类型的预制体路径
        /// </summary>
        /// <param name="type">单位类型</param>
        /// <returns>预制体路径</returns>
        private string GetPrefabPathForType(UnitType type)
        {
            if (_unitPrefabPaths.TryGetValue(type, out string path))
            {
                return path;
            }
            
            // 默认路径
            return $"Prefabs/Units/{type}";
        }

        /// <summary>
        /// 初始化单位预制体路径
        /// </summary>
        private void InitializeUnitPrefabPaths()
        {
            // 在实际实现中，应该从配置文件或数据库中加载预制体路径
            // 这里简化处理，直接设置硬编码的路径
            
            _unitPrefabPaths.Clear();
            
            _unitPrefabPaths[UnitType.Worker] = "Prefabs/Units/Worker";
            _unitPrefabPaths[UnitType.Warrior] = "Prefabs/Units/Warrior";
            _unitPrefabPaths[UnitType.AcidSprayer] = "Prefabs/Units/AcidSprayer";
            _unitPrefabPaths[UnitType.Tank] = "Prefabs/Units/Tank";
            _unitPrefabPaths[UnitType.Scout] = "Prefabs/Units/Scout";
            _unitPrefabPaths[UnitType.Flyer] = "Prefabs/Units/Flyer";
            _unitPrefabPaths[UnitType.Queen] = "Prefabs/Units/Queen";
        }

        /// <summary>
        /// 初始化进化路径
        /// </summary>
        private void InitializeEvolutionPaths()
        {
            // 在实际实现中，应该从配置文件或数据库中加载进化路径
            // 这里简化处理，直接创建硬编码的进化路径
            
            _evolutionPaths.Clear();
            
            // 工蚁进化路径
            EvolutionPath workerPath = new EvolutionPath
            {
                PathId = "worker_efficiency",
                RequiredUnitType = UnitType.Worker,
                MaxLevel = 3,
                EvolutionTime = 10f
            };
            
            // 设置工蚁进化路径的属性修改器
            workerPath.AttributeModifiersByLevel[1] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "ResourceGatherRate", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "MoveSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            workerPath.AttributeModifiersByLevel[2] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "ResourceGatherRate", Type = AttributeModifier.ModifierType.Multiply, Value = 1.3f },
                new AttributeModifier { AttributeName = "BuildSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f }
            };
            
            workerPath.AttributeModifiersByLevel[3] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "ResourceGatherRate", Type = AttributeModifier.ModifierType.Multiply, Value = 1.5f },
                new AttributeModifier { AttributeName = "BuildSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.5f },
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f }
            };
            
            // 设置工蚁进化路径的解锁能力
            workerPath.UnlockedAbilitiesByLevel[1] = new string[] { "fast_gather" };
            workerPath.UnlockedAbilitiesByLevel[2] = new string[] { "fast_gather", "efficient_build" };
            workerPath.UnlockedAbilitiesByLevel[3] = new string[] { "fast_gather", "efficient_build", "resource_sense" };
            
            _evolutionPaths["worker_efficiency"] = workerPath;
            
            // 战蚁进化路径
            EvolutionPath warriorPath = new EvolutionPath
            {
                PathId = "warrior_strength",
                RequiredUnitType = UnitType.Warrior,
                MaxLevel = 3,
                EvolutionTime = 15f
            };
            
            // 设置战蚁进化路径的属性修改器
            warriorPath.AttributeModifiersByLevel[1] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "AttackDamage", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            warriorPath.AttributeModifiersByLevel[2] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "AttackDamage", Type = AttributeModifier.ModifierType.Multiply, Value = 1.4f },
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "AttackSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            warriorPath.AttributeModifiersByLevel[3] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "AttackDamage", Type = AttributeModifier.ModifierType.Multiply, Value = 1.6f },
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.4f },
                new AttributeModifier { AttributeName = "AttackSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "AttackRange", Type = AttributeModifier.ModifierType.Add, Value = 0.5f }
            };
            
            // 设置战蚁进化路径的解锁能力
            warriorPath.UnlockedAbilitiesByLevel[1] = new string[] { "power_strike" };
            warriorPath.UnlockedAbilitiesByLevel[2] = new string[] { "power_strike", "tough_carapace" };
            warriorPath.UnlockedAbilitiesByLevel[3] = new string[] { "power_strike", "tough_carapace", "battle_frenzy" };
            
            _evolutionPaths["warrior_strength"] = warriorPath;
            
            // 添加更多进化路径...
        }

        /// <summary>
        /// 初始化环境适应
        /// </summary>
        private void InitializeEnvironmentAdaptations()
        {
            // 在实际实现中，应该从配置文件或数据库中加载环境适应
            // 这里简化处理，直接创建硬编码的环境适应
            
            _environmentAdaptations.Clear();
            
            // 酸性环境适应
            EnvironmentAdaptation acidAdaptation = new EnvironmentAdaptation
            {
                TraitId = "acid_resistance",
                EnvironmentType = "acid",
                MaxLevel = 3,
                AdaptationTime = 8f
            };
            
            // 设置酸性环境适应的属性修改器
            acidAdaptation.ModifiersByLevel[1] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            acidAdaptation.ModifiersByLevel[2] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "MoveSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            acidAdaptation.ModifiersByLevel[3] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "MaxHealth", Type = AttributeModifier.ModifierType.Multiply, Value = 1.3f },
                new AttributeModifier { AttributeName = "MoveSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "AttackDamage", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            _environmentAdaptations["acid"] = acidAdaptation;
            
            // 高温环境适应
            EnvironmentAdaptation heatAdaptation = new EnvironmentAdaptation
            {
                TraitId = "heat_resistance",
                EnvironmentType = "heat",
                MaxLevel = 3,
                AdaptationTime = 8f
            };
            
            // 设置高温环境适应的属性修改器
            heatAdaptation.ModifiersByLevel[1] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "MoveSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            heatAdaptation.ModifiersByLevel[2] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "MoveSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "AttackSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            heatAdaptation.ModifiersByLevel[3] = new AttributeModifier[]
            {
                new AttributeModifier { AttributeName = "MoveSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.3f },
                new AttributeModifier { AttributeName = "AttackSpeed", Type = AttributeModifier.ModifierType.Multiply, Value = 1.2f },
                new AttributeModifier { AttributeName = "SightRange", Type = AttributeModifier.ModifierType.Multiply, Value = 1.1f }
            };
            
            _environmentAdaptations["heat"] = heatAdaptation;
            
            // 添加更多环境适应...
        }

        /// <summary>
        /// 更新资源收集状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateGatheringState(int unitId, UnitHotData hotData, UnitColdData coldData, float deltaTime)
        {
            // 检查是否有目标资源点
            if (hotData.TargetId == -1)
            {
                // 没有目标，回到空闲状态
                hotData.State = UnitState.Idle;
                return;
            }
            
            // 简化的资源收集逻辑
            if (hotData.StateTimer <= 0)
            {
                // 收集资源
                float gatherAmount = coldData.BaseAttributes.ResourceGatherRate * deltaTime;
                
                // 重置收集计时器
                hotData.StateTimer = 1f / coldData.BaseAttributes.ResourceGatherRate;
                
                Debug.Log($"[{_managerName}] 单位收集资源: ID={unitId}, 数量={gatherAmount}");
            }
        }

        /// <summary>
        /// 更新建造状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="coldData">冷数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateBuildingState(int unitId, UnitHotData hotData, UnitColdData coldData, float deltaTime)
        {
            // 检查是否有目标建筑位置
            if (hotData.TargetId == -1)
            {
                // 没有目标，回到空闲状态
                hotData.State = UnitState.Idle;
                return;
            }
            
            // 简化的建造逻辑
            if (hotData.StateTimer <= 0)
            {
                // 建造进度
                float buildProgress = coldData.BaseAttributes.BuildSpeed * deltaTime;
                
                // 重置建造计时器
                hotData.StateTimer = 1f / coldData.BaseAttributes.BuildSpeed;
                
                Debug.Log($"[{_managerName}] 单位建造进度: ID={unitId}, 进度={buildProgress}");
            }
        }
        #endregion

        #region 内部类
        /// <summary>
        /// 进化路径类
        /// </summary>
        private class EvolutionPath
        {
            /// <summary>
            /// 路径ID
            /// </summary>
            public string PathId;
            
            /// <summary>
            /// 所需单位类型
            /// </summary>
            public UnitType RequiredUnitType;
            
            /// <summary>
            /// 最大等级
            /// </summary>
            public int MaxLevel;
            
            /// <summary>
            /// 进化时间
            /// </summary>
            public float EvolutionTime;
            
            /// <summary>
            /// 各等级的属性修改器
            /// </summary>
            public Dictionary<int, AttributeModifier[]> AttributeModifiersByLevel = new Dictionary<int, AttributeModifier[]>();
            
            /// <summary>
            /// 各等级解锁的能力
            /// </summary>
            public Dictionary<int, string[]> UnlockedAbilitiesByLevel = new Dictionary<int, string[]>();
        }
        
        /// <summary>
        /// 环境适应类
        /// </summary>
        private class EnvironmentAdaptation
        {
            /// <summary>
            /// 特征ID
            /// </summary>
            public string TraitId;
            
            /// <summary>
            /// 环境类型
            /// </summary>
            public string EnvironmentType;
            
            /// <summary>
            /// 最大等级
            /// </summary>
            public int MaxLevel;
            
            /// <summary>
            /// 适应时间
            /// </summary>
            public float AdaptationTime;
            
            /// <summary>
            /// 各等级的属性修改器
            /// </summary>
            public Dictionary<int, AttributeModifier[]> ModifiersByLevel = new Dictionary<int, AttributeModifier[]>();
        }
        #endregion
    }
}
