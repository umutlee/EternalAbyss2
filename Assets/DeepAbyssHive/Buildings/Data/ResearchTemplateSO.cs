using UnityEngine;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 研究模板（ScriptableObject）
    /// 用於定義科技研究的屬性和解鎖內容
    /// </summary>
    [CreateAssetMenu(fileName = "ResearchTemplate", menuName = "DeepAbyssHive/Research Template", order = 3)]
    public class ResearchTemplateSO : ScriptableObject
    {
        [Header("基本信息")]
        public string Id;
        public string ResearchName;
        [TextArea(2, 4)]
        public string Description;

        [Header("研究需求")]
        public ResourceCost[] ResearchCost;
        public float ResearchTime;
        public BuildingType RequiredBuilding;
        public string[] Prerequisites;

        [Header("解鎖內容")]
        public BuildingType[] UnlockedBuildings;
        public string[] UnlockedUnits;
        public string[] UnlockedAbilities;
        public string[] UnlockedUpgrades;

        [Header("屬性加成")]
        public AttributeBonus[] AttributeBonuses;

        [Header("特殊效果")]
        public string[] SpecialEffects;
        [TextArea(2, 3)]
        public string FlavorText;

        [Header("UI顯示")]
        public string IconPath;
        public Color ResearchColor = Color.white;
        public int DisplayOrder;

        /// <summary>
        /// 檢查研究前置條件是否滿足
        /// </summary>
        /// <param name="completedResearch">已完成的研究列表</param>
        /// <returns>是否滿足前置條件</returns>
        public bool CheckPrerequisites(string[] completedResearch)
        {
            if (Prerequisites == null || Prerequisites.Length == 0)
                return true;

            foreach (string prerequisite in Prerequisites)
            {
                bool found = false;
                foreach (string completed in completedResearch)
                {
                    if (completed == prerequisite)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 獲取研究的總價值（用於AI決策）
        /// </summary>
        /// <returns>研究價值評分</returns>
        public float GetResearchValue()
        {
            float value = 0f;
            
            // 解鎖建築的價值
            if (UnlockedBuildings != null)
                value += UnlockedBuildings.Length * 10f;
            
            // 解鎖單位的價值
            if (UnlockedUnits != null)
                value += UnlockedUnits.Length * 8f;
            
            // 解鎖能力的價值
            if (UnlockedAbilities != null)
                value += UnlockedAbilities.Length * 6f;
            
            // 屬性加成的價值
            if (AttributeBonuses != null)
                value += AttributeBonuses.Length * 5f;
            
            return value;
        }
    }

    /// <summary>
    /// 屬性加成配置
    /// </summary>
    [System.Serializable]
    public struct AttributeBonus
    {
        public string AttributeName;
        public float BonusValue;
        public bool IsPercentage;
        public string TargetType; // "Unit", "Building", "Global"
        public string TargetFilter; // 具體的單位類型或建築類型
    }
}