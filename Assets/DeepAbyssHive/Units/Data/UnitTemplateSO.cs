using UnityEngine;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Creep.Enums;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 單位靜態屬性模板（ScriptableObject）
    /// 用於定義單位的基礎屬性和成長參數
    /// </summary>
    [CreateAssetMenu(fileName = "UnitTemplate", menuName = "DeepAbyssHive/Unit Template", order = 1)]
    public class UnitTemplateSO : ScriptableObject
    {
        [Header("基本信息")]
        public int Id;
        public UnitType UnitType;
        public string UnitName;
        [TextArea(2, 4)]
        public string Description;
        public int Level;

        [Header("基礎屬性")]
        public float BaseHealth;
        public float BaseEnergy;
        public float BaseShield;
        public float BaseMoveSpeed;
        public float BaseAttackDamage;
        public float BaseAttackRange;
        public float BaseAttackSpeed;
        public float BaseArmor;

        [Header("成長參數")]
        public float HealthGrowth;
        public float EnergyGrowth;
        public float DamageGrowth;
        public float ArmorGrowth;
        public float SpeedGrowth;

        [Header("環境適應")]
        public EnvironmentType PreferredEnvironment;
        public float EnvironmentAdaptationRate;

        [Header("視野和感知")]
        public float SightRange;
        public float DetectionRange;
        public float HearingRange;

        [Header("資源消耗")]
        public ResourceCost[] BuildCost;
        public float BuildTime;
        public float SupplyUsage;

        [Header("特殊能力")]
        public string[] AvailableAbilities;
        public string[] StartingAbilities;

        [Header("進化相關")]
        public string[] EvolutionPaths;
        public int MaxEvolutionLevel;

        [Header("預製體路徑")]
        public string PrefabPath;
        public string IconPath;

        /// <summary>
        /// 獲取指定等級的屬性值
        /// </summary>
        /// <param name="level">等級</param>
        /// <returns>計算後的屬性</returns>
        public UnitAttributes GetAttributesAtLevel(int level)
        {
            return new UnitAttributes
            {
                MaxHealth = BaseHealth + (HealthGrowth * (level - 1)),
                MaxEnergy = BaseEnergy + (EnergyGrowth * (level - 1)),
                MaxShield = BaseShield,
                MoveSpeed = BaseMoveSpeed + (SpeedGrowth * (level - 1)),
                AttackDamage = BaseAttackDamage + (DamageGrowth * (level - 1)),
                AttackRange = BaseAttackRange,
                AttackSpeed = BaseAttackSpeed,
                Armor = BaseArmor + (ArmorGrowth * (level - 1)),
                SightRange = SightRange,
                DetectionRange = DetectionRange,
                HearingRange = HearingRange
            };
        }
    }

    /// <summary>
    /// 資源消耗配置
    /// </summary>
    [System.Serializable]
    public struct ResourceCost
    {
        public string ResourceType;
        public int Amount;
    }
}