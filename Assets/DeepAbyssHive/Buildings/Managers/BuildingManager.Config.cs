using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Buildings.Utils;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// 建築管理器配置部分
    /// 負責從ScriptableObject加載和管理建築配置
    /// </summary>
    public partial class BuildingManager
    {
        [Header("配置集成")]
        [SerializeField] private bool useScriptableObjectConfig = true;
        
        // 配置緩存
        private Dictionary<BuildingType, BuildingTemplateSO> _buildingTemplateCache;
        private Dictionary<string, ResearchTemplateSO> _researchTemplateCache;
        private bool _configInitialized = false;

        /// <summary>
        /// 初始化配置系統
        /// </summary>
        private void InitializeConfigSystem()
        {
            if (_configInitialized) return;

            DAHLog.Info(LogCategory.BUILDING, $"[{_managerName}] 初始化配置系統...");

            if (useScriptableObjectConfig)
            {
                InitializeFromScriptableObjects();
            }
            else
            {
                InitializeFromLegacyConfig();
            }

            _configInitialized = true;
            DAHLog.Info(LogCategory.BUILDING, $"[{_managerName}] 配置系統初始化完成");
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

            // 構建建築模板緩存
            _buildingTemplateCache = new Dictionary<BuildingType, BuildingTemplateSO>();
            var allBuildingTemplates = ConfigManager.Instance.GetAllBuildingTemplates();

            foreach (var template in allBuildingTemplates)
            {
                if (template != null)
                {
                    _buildingTemplateCache[template.BuildingType] = template;
                    DAHLog.Info(LogCategory.BUILDING, $"[{_managerName}] 加載建築模板: {template.BuildingName} ({template.BuildingType})");
                }
            }

            // 構建研究模板緩存
            _researchTemplateCache = new Dictionary<string, ResearchTemplateSO>();
            var allResearchTemplates = ConfigManager.Instance.GetAllResearchTemplates();

            foreach (var template in allResearchTemplates)
            {
                if (template != null)
                {
                    _researchTemplateCache[template.Id] = template;
                    DAHLog.Info(LogCategory.BUILDING, $"[{_managerName}] 加載研究模板: {template.ResearchName} ({template.Id})");
                }
            }

            DAHLog.Info(LogCategory.BUILDING, $"[{_managerName}] 從ScriptableObject加載了 {_buildingTemplateCache.Count} 個建築模板和 {_researchTemplateCache.Count} 個研究模板");
        }

        /// <summary>
        /// 從舊版配置初始化（向後兼容）
        /// </summary>
        private void InitializeFromLegacyConfig()
        {
            DAHLog.Info(LogCategory.BUILDING, $"[{_managerName}] 使用舊版配置系統（向後兼容模式）");
            // 這裡保留原有的硬編碼配置邏輯
            // 在完全遷移到ScriptableObject後可以移除
        }

        /// <summary>
        /// 獲取建築模板
        /// </summary>
        /// <param name="buildingType">建築類型</param>
        /// <returns>建築模板，如果不存在返回null</returns>
        public BuildingTemplateSO GetBuildingTemplate(BuildingType buildingType)
        {
            if (!_configInitialized)
            {
                InitializeConfigSystem();
            }

            if (useScriptableObjectConfig && _buildingTemplateCache != null)
            {
                _buildingTemplateCache.TryGetValue(buildingType, out BuildingTemplateSO template);
                return template;
            }

            return null;
        }

        /// <summary>
        /// 獲取研究模板
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <returns>研究模板，如果不存在返回null</returns>
        public ResearchTemplateSO GetResearchTemplate(string researchId)
        {
            if (!_configInitialized)
            {
                InitializeConfigSystem();
            }

            if (useScriptableObjectConfig && _researchTemplateCache != null)
            {
                _researchTemplateCache.TryGetValue(researchId, out ResearchTemplateSO template);
                return template;
            }

            return null;
        }

        /// <summary>
        /// 獲取建築基礎屬性
        /// </summary>
        /// <param name="buildingType">建築類型</param>
        /// <param name="level">等級</param>
        /// <returns>建築屬性</returns>
        public BuildingAttributes GetBuildingBaseAttributes(BuildingType buildingType, int level = 1)
        {
            var template = GetBuildingTemplate(buildingType);
            if (template != null)
            {
                return template.GetAttributesAtLevel(level);
            }

            // 回退到舊版邏輯
            return GetLegacyBuildingAttributes(buildingType, level);
        }

        /// <summary>
        /// 獲取舊版建築屬性（向後兼容）
        /// </summary>
        /// <param name="buildingType">建築類型</param>
        /// <param name="level">等級</param>
        /// <returns>建築屬性</returns>
        private BuildingAttributes GetLegacyBuildingAttributes(BuildingType buildingType, int level)
        {
            // 這裡保留原有的硬編碼屬性邏輯
            // 作為ScriptableObject配置的回退方案
            return new BuildingAttributes
            {
                MaxHealth = 500f,
                MaxShield = 100f,
                Armor = 2f,
                AttackDamage = 25f,
                AttackRange = 8f,
                AttackSpeed = 0.5f,
                BioEnergyGeneration = buildingType == BuildingType.BioEnergyCore ? 50f : 0f,
                BioEnergyConsumption = 10f,
                ProductionSpeedMultiplier = 1f
            };
        }

        /// <summary>
        /// 獲取建築建造成本
        /// </summary>
        /// <param name="buildingType">建築類型</param>
        /// <returns>建造成本</returns>
        public ResourceCost[] GetBuildingBuildCost(BuildingType buildingType)
        {
            var template = GetBuildingTemplate(buildingType);
            if (template != null && template.BuildCost != null)
            {
                return template.BuildCost;
            }

            // 回退到默認成本
            return new ResourceCost[]
            {
                new ResourceCost { ResourceType = "BioMass", Amount = 100 },
                new ResourceCost { ResourceType = "Energy", Amount = 50 }
            };
        }

        /// <summary>
        /// 獲取建築建造時間
        /// </summary>
        /// <param name="buildingType">建築類型</param>
        /// <returns>建造時間（秒）</returns>
        public float GetBuildingBuildTime(BuildingType buildingType)
        {
            var template = GetBuildingTemplate(buildingType);
            if (template != null)
            {
                return template.BuildTime;
            }

            // 回退到默認時間
            return 30f;
        }

        /// <summary>
        /// 獲取研究成本
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <returns>研究成本</returns>
        public ResourceCost[] GetResearchCost(string researchId)
        {
            var template = GetResearchTemplate(researchId);
            if (template != null && template.ResearchCost != null)
            {
                return template.ResearchCost;
            }

            // 回退到默認成本
            return new ResourceCost[]
            {
                new ResourceCost { ResourceType = "Knowledge", Amount = 100 },
                new ResourceCost { ResourceType = "Energy", Amount = 75 }
            };
        }

        /// <summary>
        /// 獲取研究時間
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <returns>研究時間（秒）</returns>
        public float GetResearchTime(string researchId)
        {
            var template = GetResearchTemplate(researchId);
            if (template != null)
            {
                return template.ResearchTime;
            }

            // 回退到默認時間
            return 60f;
        }

        /// <summary>
        /// 檢查配置是否有效
        /// </summary>
        /// <returns>配置是否有效</returns>
        public bool IsBuildingConfigValid()
        {
            if (!_configInitialized)
            {
                InitializeConfigSystem();
            }

            if (useScriptableObjectConfig)
            {
                return _buildingTemplateCache != null && _buildingTemplateCache.Count > 0 &&
                       _researchTemplateCache != null && _researchTemplateCache.Count > 0;
            }

            return true; // 舊版配置總是有效
        }

        /// <summary>
        /// 重新加載配置
        /// </summary>
        public void ReloadBuildingConfig()
        {
            _configInitialized = false;
            InitializeConfigSystem();
        }
    }
}