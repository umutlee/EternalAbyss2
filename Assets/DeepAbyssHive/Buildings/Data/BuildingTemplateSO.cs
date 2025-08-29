using UnityEngine;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 建築靜態屬性模板（ScriptableObject）
    /// 用於定義建築的基礎屬性和功能參數
    /// </summary>
    [CreateAssetMenu(fileName = "BuildingTemplate", menuName = "DeepAbyssHive/Building Template", order = 2)]
    public class BuildingTemplateSO : ScriptableObject
    {
        [Header("基本信息")]
        public int Id;
        public BuildingType BuildingType;
        public string BuildingName;
        [TextArea(2, 4)]
        public string Description;

        [Header("基礎屬性")]
        public float MaxHealth;
        public float MaxShield;
        public float Armor;
        public Vector2Int Size;
        public int MaxLevel;

        [Header("建造需求")]
        public ResourceCost[] BuildCost;
        public float BuildTime;
        public string RequiredTech;
        public BuildingType RequiredBuilding;

        [Header("能源系統")]
        public float BioEnergyConsumption;
        public float BioEnergyGeneration;
        public float PowerRadius;

        [Header("生產功能")]
        public int ProductionCapacity;
        public string[] ProducibleUnits;
        public float ProductionSpeedMultiplier;

        [Header("防禦功能")]
        public float AttackDamage;
        public float AttackRange;
        public float AttackSpeed;
        public bool CanAttackAir;
        public bool CanAttackGround;

        [Header("特殊功能")]
        public string[] SpecialAbilities;
        public float InfluenceRadius;
        public bool RequiresCreepConnection;
        public float CreepGrowthRate;

        [Header("升級系統")]
        public BuildingUpgrade[] Upgrades;

        [Header("預製體路徑")]
        public string PrefabPath;
        public string IconPath;

        /// <summary>
        /// 獲取指定等級的屬性
        /// </summary>
        /// <param name="level">等級</param>
        /// <returns>計算後的屬性</returns>
        public BuildingAttributes GetAttributesAtLevel(int level)
        {
            float levelMultiplier = 1f + (0.2f * (level - 1)); // 每級20%提升
            
            return new BuildingAttributes
            {
                MaxHealth = MaxHealth * levelMultiplier,
                MaxShield = MaxShield * levelMultiplier,
                Armor = Armor + (level - 1),
                AttackDamage = AttackDamage * levelMultiplier,
                AttackRange = AttackRange,
                AttackSpeed = AttackSpeed,
                BioEnergyGeneration = BioEnergyGeneration * levelMultiplier,
                BioEnergyConsumption = BioEnergyConsumption,
                ProductionSpeedMultiplier = ProductionSpeedMultiplier * levelMultiplier
            };
        }
    }

    /// <summary>
    /// 建築升級配置
    /// </summary>
    [System.Serializable]
    public struct BuildingUpgrade
    {
        public int Level;
        public ResourceCost[] UpgradeCost;
        public float UpgradeTime;
        public string RequiredTech;
        [TextArea(1, 2)]
        public string Description;
    }

    /// <summary>
    /// 建築屬性結構
    /// </summary>
    [System.Serializable]
    public struct BuildingAttributes
    {
        public float MaxHealth;
        public float MaxShield;
        public float Armor;
        public float AttackDamage;
        public float AttackRange;
        public float AttackSpeed;
        public float BioEnergyGeneration;
        public float BioEnergyConsumption;
        public float ProductionSpeedMultiplier;
    }
}