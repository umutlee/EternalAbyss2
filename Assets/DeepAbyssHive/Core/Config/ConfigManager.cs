using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 配置管理器
    /// 負責加載和管理所有ScriptableObject配置數據
    /// </summary>
    public class ConfigManager : MonoBehaviour
    {
        private static ConfigManager _instance;
        public static ConfigManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ConfigManager>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ConfigManager");
                        _instance = go.AddComponent<ConfigManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        [Header("配置路徑")]
        [SerializeField] private string unitTemplatesPath = "Configs/Units";
        [SerializeField] private string buildingTemplatesPath = "Configs/Buildings";
        [SerializeField] private string researchTemplatesPath = "Configs/Research";

        [Header("已加載的配置")]
        [SerializeField] private List<UnitTemplateSO> unitTemplates = new List<UnitTemplateSO>();
        [SerializeField] private List<BuildingTemplateSO> buildingTemplates = new List<BuildingTemplateSO>();
        [SerializeField] private List<ResearchTemplateSO> researchTemplates = new List<ResearchTemplateSO>();

        // 快速查找字典
        private Dictionary<UnitType, UnitTemplateSO> _unitTemplateDict;
        private Dictionary<BuildingType, BuildingTemplateSO> _buildingTemplateDict;
        private Dictionary<string, ResearchTemplateSO> _researchTemplateDict;

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            DAHLog.Info(LogCategory.SYSTEM, "[ConfigManager] 開始初始化配置管理器...");

            LoadUnitTemplates();
            LoadBuildingTemplates();
            LoadResearchTemplates();

            BuildLookupDictionaries();

            IsInitialized = true;
            DAHLog.Info(LogCategory.SYSTEM, "[ConfigManager] 配置管理器初始化完成");
        }

        /// <summary>
        /// 加載單位模板
        /// </summary>
        private void LoadUnitTemplates()
        {
            unitTemplates.Clear();
            UnitTemplateSO[] templates = Resources.LoadAll<UnitTemplateSO>(unitTemplatesPath);
            
            foreach (var template in templates)
            {
                if (template != null)
                {
                    unitTemplates.Add(template);
                    DAHLog.Info(LogCategory.SYSTEM, $"[ConfigManager] 加載單位模板: {template.UnitName} ({template.UnitType})");
                }
            }

            DAHLog.Info(LogCategory.SYSTEM, $"[ConfigManager] 共加載 {unitTemplates.Count} 個單位模板");
        }

        /// <summary>
        /// 加載建築模板
        /// </summary>
        private void LoadBuildingTemplates()
        {
            buildingTemplates.Clear();
            BuildingTemplateSO[] templates = Resources.LoadAll<BuildingTemplateSO>(buildingTemplatesPath);
            
            foreach (var template in templates)
            {
                if (template != null)
                {
                    buildingTemplates.Add(template);
                    DAHLog.Info(LogCategory.SYSTEM, $"[ConfigManager] 加載建築模板: {template.BuildingName} ({template.BuildingType})");
                }
            }

            DAHLog.Info(LogCategory.SYSTEM, $"[ConfigManager] 共加載 {buildingTemplates.Count} 個建築模板");
        }

        /// <summary>
        /// 加載研究模板
        /// </summary>
        private void LoadResearchTemplates()
        {
            researchTemplates.Clear();
            ResearchTemplateSO[] templates = Resources.LoadAll<ResearchTemplateSO>(researchTemplatesPath);
            
            foreach (var template in templates)
            {
                if (template != null)
                {
                    researchTemplates.Add(template);
                    DAHLog.Info(LogCategory.SYSTEM, $"[ConfigManager] 加載研究模板: {template.ResearchName} ({template.Id})");
                }
            }

            DAHLog.Info(LogCategory.SYSTEM, $"[ConfigManager] 共加載 {researchTemplates.Count} 個研究模板");
        }

        /// <summary>
        /// 構建查找字典
        /// </summary>
        private void BuildLookupDictionaries()
        {
            // 構建單位模板字典
            _unitTemplateDict = new Dictionary<UnitType, UnitTemplateSO>();
            foreach (var template in unitTemplates)
            {
                if (!_unitTemplateDict.ContainsKey(template.UnitType))
                {
                    _unitTemplateDict[template.UnitType] = template;
                }
                else
                {
                    DAHLog.Warning(LogCategory.CONFIG, $"[ConfigManager] 重複的單位類型: {template.UnitType}");
                }
            }

            // 構建建築模板字典
            _buildingTemplateDict = new Dictionary<BuildingType, BuildingTemplateSO>();
            foreach (var template in buildingTemplates)
            {
                if (!_buildingTemplateDict.ContainsKey(template.BuildingType))
                {
                    _buildingTemplateDict[template.BuildingType] = template;
                }
                else
                {
                    DAHLog.Warning(LogCategory.CONFIG, $"[ConfigManager] 重複的建築類型: {template.BuildingType}");
                }
            }

            // 構建研究模板字典
            _researchTemplateDict = new Dictionary<string, ResearchTemplateSO>();
            foreach (var template in researchTemplates)
            {
                if (!_researchTemplateDict.ContainsKey(template.Id))
                {
                    _researchTemplateDict[template.Id] = template;
                }
                else
                {
                    DAHLog.Warning(LogCategory.CONFIG, $"[ConfigManager] 重複的研究ID: {template.Id}");
                }
            }
        }

        #region 公共API

        /// <summary>
        /// 獲取單位模板
        /// </summary>
        /// <param name="unitType">單位類型</param>
        /// <returns>單位模板，如果不存在返回null</returns>
        public UnitTemplateSO GetUnitTemplate(UnitType unitType)
        {
            if (_unitTemplateDict != null && _unitTemplateDict.TryGetValue(unitType, out UnitTemplateSO template))
            {
                return template;
            }
            return null;
        }

        /// <summary>
        /// 獲取建築模板
        /// </summary>
        /// <param name="buildingType">建築類型</param>
        /// <returns>建築模板，如果不存在返回null</returns>
        public BuildingTemplateSO GetBuildingTemplate(BuildingType buildingType)
        {
            if (_buildingTemplateDict != null && _buildingTemplateDict.TryGetValue(buildingType, out BuildingTemplateSO template))
            {
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
            if (_researchTemplateDict != null && _researchTemplateDict.TryGetValue(researchId, out ResearchTemplateSO template))
            {
                return template;
            }
            return null;
        }

        /// <summary>
        /// 獲取所有單位模板
        /// </summary>
        /// <returns>單位模板列表</returns>
        public List<UnitTemplateSO> GetAllUnitTemplates()
        {
            return new List<UnitTemplateSO>(unitTemplates);
        }

        /// <summary>
        /// 獲取所有建築模板
        /// </summary>
        /// <returns>建築模板列表</returns>
        public List<BuildingTemplateSO> GetAllBuildingTemplates()
        {
            return new List<BuildingTemplateSO>(buildingTemplates);
        }

        /// <summary>
        /// 獲取所有研究模板
        /// </summary>
        /// <returns>研究模板列表</returns>
        public List<ResearchTemplateSO> GetAllResearchTemplates()
        {
            return new List<ResearchTemplateSO>(researchTemplates);
        }

        /// <summary>
        /// 重新加載所有配置
        /// </summary>
        public void ReloadConfigs()
        {
            IsInitialized = false;
            Initialize();
        }

        #endregion
    }
}