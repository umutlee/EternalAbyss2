using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Core.Config;
using UnitAttributes = DeepAbyssHive.Units.Data.UnitAttributes;
using UnitAttributeType = DeepAbyssHive.Units.Enums.UnitAttributes;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 單位管理器配置部分
    /// 負責從ScriptableObject加載和管理單位配置
    /// </summary>
    public partial class UnitManager
    {
        [Header("配置集成")]
        [SerializeField] private bool useScriptableObjectConfig = true;
        
        // 配置緩存
        private Dictionary<UnitType, UnitTemplateSO> _unitTemplateCache;
        private bool _configInitialized = false;

        /// <summary>
        /// 初始化配置系統
        /// </summary>
        private void InitializeConfig()
        {
            if (_configInitialized) return;

            Debug.Log($"[{_managerName}] 初始化配置系統...");

            if (useScriptableObjectConfig)
            {
                InitializeFromScriptableObjects();
            }
            else
            {
                InitializeFromLegacyConfig();
            }

            _configInitialized = true;
            Debug.Log($"[{_managerName}] 配置系統初始化完成");
        }

        /// <summary>
        /// 從ScriptableObject初始化配置
        /// </summary>
        private void InitializeFromScriptableObjects()
        {
            // 確保ConfigManager已初始化
            if (!ConfigManager.Instance.IsInitialized)
            {
                ConfigManager.Instance.Initialize();
            }

            // 構建模板緩存
            _unitTemplateCache = new Dictionary<UnitType, UnitTemplateSO>();
            var allTemplates = ConfigManager.Instance.GetAllUnitTemplates();

            foreach (var template in allTemplates)
            {
                if (template != null)
                {
                    _unitTemplateCache[template.UnitType] = template;
                    Debug.Log($"[{_managerName}] 加載單位模板: {template.UnitName} ({template.UnitType})");
                }
            }

            Debug.Log($"[{_managerName}] 從ScriptableObject加載了 {_unitTemplateCache.Count} 個單位模板");
        }

        /// <summary>
        /// 從舊版配置初始化（向後兼容）
        /// </summary>
        private void InitializeFromLegacyConfig()
        {
            Debug.Log($"[{_managerName}] 使用舊版配置系統（向後兼容模式）");
            // 這裡保留原有的硬編碼配置邏輯
            // 在完全遷移到ScriptableObject後可以移除
        }

        /// <summary>
        /// 獲取單位模板
        /// </summary>
        /// <param name="unitType">單位類型</param>
        /// <returns>單位模板，如果不存在返回null</returns>
        public UnitTemplateSO GetUnitTemplate(UnitType unitType)
        {
            if (!_configInitialized)
            {
                InitializeConfig();
            }

            if (useScriptableObjectConfig && _unitTemplateCache != null)
            {
                _unitTemplateCache.TryGetValue(unitType, out UnitTemplateSO template);
                return template;
            }

            return null;
        }

        /// <summary>
        /// 獲取單位基礎屬性
        /// </summary>
        /// <param name="unitType">單位類型</param>
        /// <param name="level">等級</param>
        /// <returns>單位屬性</returns>
        public UnitAttributes GetUnitBaseAttributes(UnitType unitType, int level = 1)
        {
            var template = GetUnitTemplate(unitType);
            if (template != null)
            {
                return template.GetAttributesAtLevel(level);
            }

            // 回退到舊版邏輯
            return GetLegacyUnitAttributes(unitType, level);
        }

        /// <summary>
        /// 獲取舊版單位屬性（向後兼容）
        /// </summary>
        /// <param name="unitType">單位類型</param>
        /// <param name="level">等級</param>
        /// <returns>單位屬性</returns>
        private UnitAttributes GetLegacyUnitAttributes(UnitType unitType, int level)
        {
            // 這裡保留原有的硬編碼屬性邏輯
            // 作為ScriptableObject配置的回退方案
            return new UnitAttributes
            {
                MaxHealth = 100f,
                MaxEnergy = 50f,
                MoveSpeed = 5f,
                AttackDamage = 10f,
                AttackRange = 2f,
                AttackSpeed = 1f,
                Armor = 0f,
                SightRange = 8f,
                DetectionRange = 6f,
                HearingRange = 4f
            };
        }

        /// <summary>
        /// 獲取單位建造成本
        /// </summary>
        /// <param name="unitType">單位類型</param>
        /// <returns>建造成本</returns>
        public ResourceCost[] GetUnitBuildCost(UnitType unitType)
        {
            var template = GetUnitTemplate(unitType);
            if (template != null && template.BuildCost != null)
            {
                return template.BuildCost;
            }

            // 回退到默認成本
            return new ResourceCost[]
            {
                new ResourceCost { ResourceType = "BioMass", Amount = 50 },
                new ResourceCost { ResourceType = "Energy", Amount = 25 }
            };
        }

        /// <summary>
        /// 獲取單位建造時間
        /// </summary>
        /// <param name="unitType">單位類型</param>
        /// <returns>建造時間（秒）</returns>
        public float GetUnitBuildTime(UnitType unitType)
        {
            var template = GetUnitTemplate(unitType);
            if (template != null)
            {
                return template.BuildTime;
            }

            // 回退到默認時間
            return 10f;
        }

        /// <summary>
        /// 檢查配置是否有效
        /// </summary>
        /// <returns>配置是否有效</returns>
        public bool IsConfigValid()
        {
            if (!_configInitialized)
            {
                InitializeConfig();
            }

            if (useScriptableObjectConfig)
            {
                return _unitTemplateCache != null && _unitTemplateCache.Count > 0;
            }

            return true; // 舊版配置總是有效
        }
    }
}