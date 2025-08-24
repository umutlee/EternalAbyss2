using UnityEngine;
using System.Collections.Generic;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Units.Config
{
    /// <summary>
    /// 单位系统配置数据
    /// 包含单位属性、进化路径、环境适应等所有配置
    /// </summary>
    [CreateAssetMenu(fileName = "UnitConfig", menuName = "DeepAbyssHive/Config/Unit Config")]
    public class UnitConfigSO : BaseConfigSO
    {
        [Header("单位预制体路径")]
        [Tooltip("各单位类型对应的预制体路径")]
        public UnitPrefabPath[] unitPrefabPaths = new UnitPrefabPath[]
        {
            new UnitPrefabPath { unitType = UnitType.Worker, prefabPath = "Prefabs/Units/Worker" },
            new UnitPrefabPath { unitType = UnitType.Warrior, prefabPath = "Prefabs/Units/Warrior" },
            new UnitPrefabPath { unitType = UnitType.AcidSprayer, prefabPath = "Prefabs/Units/AcidSprayer" },
            new UnitPrefabPath { unitType = UnitType.Hunter, prefabPath = "Prefabs/Units/Hunter" },
            new UnitPrefabPath { unitType = UnitType.Guardian, prefabPath = "Prefabs/Units/Guardian" },
            new UnitPrefabPath { unitType = UnitType.Scout, prefabPath = "Prefabs/Units/Scout" },
            new UnitPrefabPath { unitType = UnitType.Overlord, prefabPath = "Prefabs/Units/Overlord" },
            new UnitPrefabPath { unitType = UnitType.Queen, prefabPath = "Prefabs/Units/Queen" }
        };

        [Header("进化路径配置")]
        [Tooltip("单位进化路径定义")]
        public EvolutionPathConfig[] evolutionPaths = new EvolutionPathConfig[]
        {
            new EvolutionPathConfig
            {
                pathId = "worker_efficiency",
                requiredUnitType = UnitType.Worker,
                maxLevel = 3,
                evolutionTime = 10f,
                levelConfigs = new EvolutionLevelConfig[]
                {
                    new EvolutionLevelConfig
                    {
                        level = 1,
                        attributeModifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "ResourceGatherRate", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "MoveSpeed", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        },
                        unlockedAbilities = new string[] { "fast_gather" }
                    },
                    new EvolutionLevelConfig
                    {
                        level = 2,
                        attributeModifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "ResourceGatherRate", modifierType = AttributeModifierType.Multiply, value = 1.3f },
                            new AttributeModifierConfig { attributeName = "BuildSpeed", modifierType = AttributeModifierType.Multiply, value = 1.2f }
                        },
                        unlockedAbilities = new string[] { "fast_gather", "efficient_build" }
                    },
                    new EvolutionLevelConfig
                    {
                        level = 3,
                        attributeModifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "ResourceGatherRate", modifierType = AttributeModifierType.Multiply, value = 1.5f },
                            new AttributeModifierConfig { attributeName = "BuildSpeed", modifierType = AttributeModifierType.Multiply, value = 1.5f },
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.2f }
                        },
                        unlockedAbilities = new string[] { "fast_gather", "efficient_build", "resource_sense" }
                    }
                }
            },
            new EvolutionPathConfig
            {
                pathId = "warrior_strength",
                requiredUnitType = UnitType.Warrior,
                maxLevel = 3,
                evolutionTime = 15f,
                levelConfigs = new EvolutionLevelConfig[]
                {
                    new EvolutionLevelConfig
                    {
                        level = 1,
                        attributeModifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "AttackDamage", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        },
                        unlockedAbilities = new string[] { "power_strike" }
                    },
                    new EvolutionLevelConfig
                    {
                        level = 2,
                        attributeModifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "AttackDamage", modifierType = AttributeModifierType.Multiply, value = 1.4f },
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "AttackSpeed", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        },
                        unlockedAbilities = new string[] { "power_strike", "tough_carapace" }
                    },
                    new EvolutionLevelConfig
                    {
                        level = 3,
                        attributeModifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "AttackDamage", modifierType = AttributeModifierType.Multiply, value = 1.6f },
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.4f },
                            new AttributeModifierConfig { attributeName = "AttackSpeed", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "AttackRange", modifierType = AttributeModifierType.Add, value = 0.5f }
                        },
                        unlockedAbilities = new string[] { "power_strike", "tough_carapace", "battle_frenzy" }
                    }
                }
            }
        };

        [Header("环境适应配置")]
        [Tooltip("环境适应特征定义")]
        public EnvironmentAdaptationConfig[] environmentAdaptations = new EnvironmentAdaptationConfig[]
        {
            new EnvironmentAdaptationConfig
            {
                traitId = "acid_resistance",
                environmentType = "acid",
                maxLevel = 3,
                adaptationTime = 8f,
                levelConfigs = new AdaptationLevelConfig[]
                {
                    new AdaptationLevelConfig
                    {
                        level = 1,
                        modifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        }
                    },
                    new AdaptationLevelConfig
                    {
                        level = 2,
                        modifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "MoveSpeed", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        }
                    },
                    new AdaptationLevelConfig
                    {
                        level = 3,
                        modifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "MaxHealth", modifierType = AttributeModifierType.Multiply, value = 1.3f },
                            new AttributeModifierConfig { attributeName = "MoveSpeed", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "AttackDamage", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        }
                    }
                }
            },
            new EnvironmentAdaptationConfig
            {
                traitId = "heat_resistance",
                environmentType = "heat",
                maxLevel = 3,
                adaptationTime = 8f,
                levelConfigs = new AdaptationLevelConfig[]
                {
                    new AdaptationLevelConfig
                    {
                        level = 1,
                        modifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "MoveSpeed", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        }
                    },
                    new AdaptationLevelConfig
                    {
                        level = 2,
                        modifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "MoveSpeed", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "AttackSpeed", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        }
                    },
                    new AdaptationLevelConfig
                    {
                        level = 3,
                        modifiers = new AttributeModifierConfig[]
                        {
                            new AttributeModifierConfig { attributeName = "MoveSpeed", modifierType = AttributeModifierType.Multiply, value = 1.3f },
                            new AttributeModifierConfig { attributeName = "AttackSpeed", modifierType = AttributeModifierType.Multiply, value = 1.2f },
                            new AttributeModifierConfig { attributeName = "SightRange", modifierType = AttributeModifierType.Multiply, value = 1.1f }
                        }
                    }
                }
            }
        };

        protected override void OnValidate()
        {
            base.OnValidate();
            
            // 验证进化路径配置
            foreach (var evolutionPath in evolutionPaths)
            {
                evolutionPath.maxLevel = Mathf.Max(1, evolutionPath.maxLevel);
                evolutionPath.evolutionTime = Mathf.Max(0.1f, evolutionPath.evolutionTime);
            }
            
            // 验证环境适应配置
            foreach (var adaptation in environmentAdaptations)
            {
                adaptation.maxLevel = Mathf.Max(1, adaptation.maxLevel);
                adaptation.adaptationTime = Mathf.Max(0.1f, adaptation.adaptationTime);
            }
        }
    }

    // 配置数据结构
    [System.Serializable]
    public class UnitPrefabPath
    {
        public UnitType unitType;
        public string prefabPath;
    }

    [System.Serializable]
    public class EvolutionPathConfig
    {
        public string pathId;
        public UnitType requiredUnitType;
        public int maxLevel;
        public float evolutionTime;
        public EvolutionLevelConfig[] levelConfigs;
    }

    [System.Serializable]
    public class EvolutionLevelConfig
    {
        public int level;
        public AttributeModifierConfig[] attributeModifiers;
        public string[] unlockedAbilities;
    }

    [System.Serializable]
    public class EnvironmentAdaptationConfig
    {
        public string traitId;
        public string environmentType;
        public int maxLevel;
        public float adaptationTime;
        public AdaptationLevelConfig[] levelConfigs;
    }

    [System.Serializable]
    public class AdaptationLevelConfig
    {
        public int level;
        public AttributeModifierConfig[] modifiers;
    }

    [System.Serializable]
    public class AttributeModifierConfig
    {
        public string attributeName;
        public AttributeModifierType modifierType;
        public float value;
    }

    [System.Serializable]
    public enum AttributeModifierType
    {
        Add,
        Multiply
    }
}