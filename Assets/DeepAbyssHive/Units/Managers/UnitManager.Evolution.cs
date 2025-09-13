using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;
using UnitAttributes = DeepAbyssHive.Units.Data.UnitAttributes;
using UnitAttributeType = DeepAbyssHive.Units.Enums.UnitAttributes;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器进化部分 - EvolveUnit和AdaptToEnvironment
    /// </summary>
    public partial class UnitManager
    {
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
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 尝试进化不存在的单位: {unitId}");
                return false;
            }
            
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                return false;
            }
            
            // 检查进化路径是否存在
            if (!_evolutionPaths.TryGetValue(evolutionPath, out EvolutionPath path))
            {
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 尝试使用不存在的进化路径: {evolutionPath}");
                return false;
            }
            
            // 检查单位类型是否匹配
            if (path.RequiredUnitType != coldData.Type)
            {
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 单位类型不匹配进化路径: {coldData.Type} != {path.RequiredUnitType}");
                return false;
            }
            
            // 检查进化等级
            int nextLevel = ((EvolutionInfo)coldData.Evolution).Level + 1;
            if (nextLevel > path.MaxLevel)
            {
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 单位已达到最大进化等级: {((EvolutionInfo)coldData.Evolution).Level}");
                return false;
            }
            
            // 更新单位状态
            hotData.State = UnitState.Evolving;
            hotData.StateTimer = path.EvolutionTime;
            _unitHotData[unitId] = hotData;
            
            // 更新进化信息
            var evolutionInfo = (EvolutionInfo)coldData.Evolution;
            evolutionInfo.PathId = evolutionPath;
            evolutionInfo.Level = nextLevel;
            coldData.Evolution = evolutionInfo;
            
            // 解锁新能力
            if (path.UnlockedAbilitiesByLevel.TryGetValue(nextLevel, out string[] abilities))
            {
                // 使用反射來設置 UnlockedAbilities 屬性，避免類型轉換問題
                var evolutionObj = coldData.Evolution;
                if (evolutionObj != null)
                {
                    var abilitiesList = new List<string>();
                    foreach (var a in abilities)
                    {
                        if (!abilitiesList.Contains(a))
                        {
                            abilitiesList.Add(a);
                        }
                    }
                    
                    // 使用反射設置屬性
                    var evolutionType = evolutionObj.GetType();
                    var unlockedAbilitiesProp = evolutionType.GetProperty("UnlockedAbilities");
                    if (unlockedAbilitiesProp != null && unlockedAbilitiesProp.CanWrite)
                    {
                        unlockedAbilitiesProp.SetValue(evolutionObj, abilitiesList.ToArray());
                    }
                }
            }
            
            // 应用属性修改
            if (path.AttributeModifiersByLevel.TryGetValue(nextLevel, out AttributeModifier[] modifiers))
            {
                // CS0206：屬性/索引子不能用作 ref/out。改用區域變數，呼叫後再回填。
                var __attributes = coldData.BaseAttributes;
                ApplyAttributeModifiers(ref __attributes, modifiers);
                coldData.BaseAttributes = __attributes;
            }
            
            // 更新单位冷数据
            _unitColdData[unitId] = coldData;
            
            // 更新游戏对象
            UpdateUnitGameObject(unitId, hotData);
            
            DAHLog.Info(LogCategory.UNITS, $"[{_managerName}] 单位进化: ID={unitId}, 路径={evolutionPath}, 等级={nextLevel}");
            
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
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 尝试适应不存在的单位: {unitId}");
                return;
            }
            
            if (!_unitHotData.TryGetValue(unitId, out UnitHotData hotData))
            {
                return;
            }
            
            // 检查环境适应是否存在
            if (!_environmentAdaptations.TryGetValue(environmentType, out EnvironmentAdaptation adaptation))
            {
                DAHLog.Warning(LogCategory.UNITS, $"[{_managerName}] 尝试适应不存在的环境类型: {environmentType}");
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
            
            DAHLog.Info(LogCategory.UNITS, $"[{_managerName}] 单位适应环境: ID={unitId}, 环境={environmentType}");
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
        /// 更新进化状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateEvolvingState(int unitId, ref UnitHotData hotData, float deltaTime)
        {
            if (hotData.StateTimer <= 0)
            {
                // 进化完成，回到空闲状态
                hotData.State = UnitState.Idle;
                
                // 更新单位外观
                UpdateUnitAppearance(unitId);
            }
        }

        /// <summary>
        /// 更新适应状态
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <param name="hotData">热数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateAdaptingState(int unitId, ref UnitHotData hotData, float deltaTime)
        {
            if (hotData.StateTimer <= 0)
            {
                // 适应完成，回到空闲状态
                hotData.State = UnitState.Idle;
                
                // 更新单位外观
                UpdateUnitAppearance(unitId);
            }
        }
    }
}