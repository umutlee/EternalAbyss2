using UnityEngine;
using System.Collections.Generic;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 单位冷数据 - 不频繁更新的配置和静态数据
    /// 用于低频访问的数据，如属性、技能、进化路径等
    /// </summary>
    [System.Serializable]
    public partial struct UnitColdData
    {
        [Header("基础属性")]
        public UnitType UnitType;
        public string UnitName;
        public string Description;
        public int Level;
        public float Experience;
        public float ExperienceToNextLevel;
        
        [Header("基础数值")]
        public float BaseHealth;
        public float BaseEnergy;
        public float BaseShield;
        public float BaseMoveSpeed;
        public float BaseAttackDamage;
        public float BaseAttackRange;
        public float BaseAttackSpeed;
        public float BaseArmor;
        
        [Header("成长属性")]
        public float HealthGrowth;
        public float EnergyGrowth;
        public float DamageGrowth;
        public float ArmorGrowth;
        public float SpeedGrowth;
        
        [Header("环境适应")]
        public Dictionary<EnvironmentType, float> EnvironmentAdaptation;
        public EnvironmentType PreferredEnvironment;
        public float EnvironmentAdaptationRate;
        
        [Header("进化信息")]
        public List<UnitType> EvolutionPath;
        public Dictionary<UnitType, float> EvolutionRequirements;
        public bool CanEvolve;
        public UnitType NextEvolution;
        
        [Header("技能信息")]
        public List<string> AvailableSkills;
        public Dictionary<string, float> SkillCooldowns;
        public Dictionary<string, float> SkillDamage;
        
        [Header("资源消耗")]
        public Dictionary<string, float> CreationCost;
        public Dictionary<string, float> MaintenanceCost;
        public float EvolutionCost;
        
        [Header("AI行为")]
        public float AggroRange;
        public float PatrolRadius;
        public float FleeHealthThreshold;
        public bool IsAggressive;
        public bool CanFlee;
        

        /// <summary>
        /// 构造函数
        /// </summary>
        public UnitColdData(UnitType unitType, string unitName = "")
        {
            UnitType = unitType;
            UnitName = string.IsNullOrEmpty(unitName) ? unitType.ToString() : unitName;
            Description = "";
            Level = 1;
            Experience = 0f;
            ExperienceToNextLevel = 100f;
            
            // 根据单位类型设置基础属性
            switch (unitType)
            {
                case UnitType.Drone:
                    BaseHealth = 50f;
                    BaseEnergy = 100f;
                    BaseShield = 0f;
                    BaseMoveSpeed = 4f;
                    BaseAttackDamage = 5f;
                    BaseAttackRange = 1f;
                    BaseAttackSpeed = 1f;
                    BaseArmor = 0f;
                    break;
                    
                case UnitType.Warrior:
                    BaseHealth = 120f;
                    BaseEnergy = 80f;
                    BaseShield = 20f;
                    BaseMoveSpeed = 5f;
                    BaseAttackDamage = 15f;
                    BaseAttackRange = 1.5f;
                    BaseAttackSpeed = 1.2f;
                    BaseArmor = 2f;
                    break;
                    
                case UnitType.Guardian:
                    BaseHealth = 200f;
                    BaseEnergy = 60f;
                    BaseShield = 50f;
                    BaseMoveSpeed = 3f;
                    BaseAttackDamage = 25f;
                    BaseAttackRange = 1f;
                    BaseAttackSpeed = 0.8f;
                    BaseArmor = 5f;
                    break;
                    
                default:
                    BaseHealth = 100f;
                    BaseEnergy = 100f;
                    BaseShield = 0f;
                    BaseMoveSpeed = 5f;
                    BaseAttackDamage = 10f;
                    BaseAttackRange = 2f;
                    BaseAttackSpeed = 1f;
                    BaseArmor = 1f;
                    break;
            }
            
            HealthGrowth = BaseHealth * 0.1f;
            EnergyGrowth = BaseEnergy * 0.05f;
            DamageGrowth = BaseAttackDamage * 0.15f;
            ArmorGrowth = BaseArmor * 0.2f;
            SpeedGrowth = BaseMoveSpeed * 0.02f;
            
            EnvironmentAdaptation = new Dictionary<EnvironmentType, float>();
            PreferredEnvironment = EnvironmentType.Normal;
            EnvironmentAdaptationRate = 0.1f;
            
            EvolutionPath = new List<UnitType>();
            EvolutionRequirements = new Dictionary<UnitType, float>();
            CanEvolve = false;
            NextEvolution = UnitType.Drone;
            
            AvailableSkills = new List<string>();
            SkillCooldowns = new Dictionary<string, float>();
            SkillDamage = new Dictionary<string, float>();
            
            CreationCost = new Dictionary<string, float>
            {
                ["Biomass"] = 50f,
                ["Energy"] = 25f
            };
            MaintenanceCost = new Dictionary<string, float>
            {
                ["Energy"] = 1f
            };
            EvolutionCost = 100f;
            
            AggroRange = 8f;
            PatrolRadius = 5f;
            FleeHealthThreshold = 0.2f;
            IsAggressive = true;
            CanFlee = true;
        }
        
        /// <summary>
        /// 获取当前等级的实际属性值
        /// </summary>
        public float GetCurrentHealth() => BaseHealth + (HealthGrowth * (Level - 1));
        public float GetCurrentEnergy() => BaseEnergy + (EnergyGrowth * (Level - 1));
        public float GetCurrentDamage() => BaseAttackDamage + (DamageGrowth * (Level - 1));
        public float GetCurrentArmor() => BaseArmor + (ArmorGrowth * (Level - 1));
        public float GetCurrentSpeed() => BaseMoveSpeed + (SpeedGrowth * (Level - 1));
        
        /// <summary>
        /// 检查是否可以进化到指定类型
        /// </summary>
        public bool CanEvolveTo(UnitType targetType)
        {
            return CanEvolve && EvolutionPath.Contains(targetType) && 
                   EvolutionRequirements.ContainsKey(targetType) && 
                   Experience >= EvolutionRequirements[targetType];
        }
        
        /// <summary>
        /// 获取环境适应度
        /// </summary>
        public float GetEnvironmentAdaptation(EnvironmentType environmentType)
        {
            return EnvironmentAdaptation.TryGetValue(environmentType, out float adaptation) ? adaptation : 1.0f;
        }
    }
}