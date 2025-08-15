using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;
using IUnitManager = DeepAbyssHive.Units.Interfaces.IUnitManager;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器核心部分 - 数据结构、初始化/清理、生命周期管理
    /// </summary>
    public partial class UnitManager : IUnitManager, IManager
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

        #region IManager属性实现
        public string ManagerName => _managerName;
        public bool IsInitialized => _isInitialized;
        public bool IsPaused => _isPaused;
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
        /// 后更新管理器（带参数）
        /// </summary>
        public void LateUpdate(float deltaTime)
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

        #region 私有初始化方法
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
            _environmentAdaptations["heat"] = heatAdaptation;
            
            // 添加更多环境适应...
        }
        
        /// <summary>
        /// 更新单位
        /// </summary>
        private void UpdateUnit(int unitId, float deltaTime)
        {
            if (!_unitHotData.TryGetValue(unitId, out var hotData))
                return;
                
            // 更新单位状态
            switch (hotData.CurrentState)
            {
                case UnitState.Moving:
                    // 移动逻辑在Movement模块中处理
                    break;
                case UnitState.Attacking:
                    // 攻击逻辑在Combat模块中处理
                    break;
                case UnitState.Evolving:
                    // 进化逻辑在Evolution模块中处理
                    break;
            }
        }
        
        /// <summary>
        /// 销毁单位
        /// </summary>
        private void DestroyUnit(int unitId)
        {
            if (_unitGameObjects.TryGetValue(unitId, out var gameObject))
            {
                if (gameObject != null)
                    UnityEngine.Object.Destroy(gameObject);
                _unitGameObjects.Remove(unitId);
            }
            
            _unitHotData.Remove(unitId);
            _unitColdData.Remove(unitId);
            _unitSpatialNodes.Remove(unitId);
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