using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 批量更新/进度更新/性能节流
    /// 说明：
    /// - 本文件为partial占位，不改变任何对外API与行为
    /// - 后续将把生命周期更新与各类进度更新逻辑迁移至此：
    ///   - UpdateManager() / Update(float) / FixedUpdate(float) / LateUpdate()
    ///   - 私有批量更新：UpdateBuildings()
    ///   - 单体更新：UpdateBuilding(int,float) / UpdateBuilding(BuildingData,float)
    ///   - 进度：UpdateConstructionProgress / UpdateUpgradeProgress / UpdateRepairProgress
    ///   - 运行态：UpdateOperationalBuilding
    ///   - 生产队列：UpdateProductionQueues
    ///   - 私有辅助：NeedsContinuousUpdate / ApplyUpgradeEffects
    /// </summary>
    public partial class BuildingManager
    {
        /// <summary>
        /// 更新建筑状态
        /// </summary>
        private void UpdateBuildings()
        {
            _buildingUpdateTimer += UnityEngine.Time.deltaTime;
            
            if (_buildingUpdateTimer < _buildingUpdateInterval)
                return;
                
            _buildingUpdateTimer = 0f;
            
            int updatedCount = 0;
            while (_buildingUpdateQueue.Count > 0 && updatedCount < _maxBuildingUpdatesPerFrame)
            {
                int buildingId = _buildingUpdateQueue.Dequeue();
                
                if (_buildings.ContainsKey(buildingId) && _buildings.TryGetValue(buildingId, out BuildingData buildingData))
                {
                    UpdateBuilding(buildingData, _buildingUpdateInterval);
                    
                    // 如果建筑仍需要更新，重新加入队列
                    if (NeedsContinuousUpdate(buildingData))
                    {
                        _buildingUpdateQueue.Enqueue(buildingId);
                    }
                }
                
                updatedCount++;
            }
        }

        /// <summary>
        /// 更新单个建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateBuilding(int buildingId, float deltaTime)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
                return;
                
            UpdateBuilding(buildingData, deltaTime);
        }

        /// <summary>
        /// 更新单个建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateBuilding(BuildingData buildingData, float deltaTime)
        {
            switch (buildingData.State)
            {
                case BuildingState.UnderConstruction:
                    UpdateConstructionProgress(buildingData, deltaTime);
                    break;
                    
                case BuildingState.Upgrading:
                    UpdateUpgradeProgress(buildingData, deltaTime);
                    break;
                    
                case BuildingState.Repairing:
                    UpdateRepairProgress(buildingData, deltaTime);
                    break;
                    
                case BuildingState.Operational:
                    UpdateOperationalBuilding(buildingData, deltaTime);
                    break;
            }
            
            // 更新建筑数据
            _buildings[buildingData.BuildingId] = buildingData;
            
            // 更新游戏对象
            UpdateBuildingGameObject(buildingData.BuildingId, buildingData);
        }

        /// <summary>
        /// 更新建筑建造进度
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateConstructionProgress(BuildingData buildingData, float deltaTime)
        {
            buildingData.ConstructionProgress += deltaTime / buildingData.ConstructionTime;
            
            if (buildingData.ConstructionProgress >= 1.0f)
            {
                // 建造完成
                buildingData.ConstructionProgress = 1.0f;
                buildingData.State = BuildingState.Operational;
                buildingData.Health = buildingData.MaxHealth;
                
                DAHLog.Info(LogCategory.BUILDINGS, $"[{_managerName}] 建筑建造完成: ID={buildingData.BuildingId}");
            }
        }

        /// <summary>
        /// 更新建筑升级进度
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateUpgradeProgress(BuildingData buildingData, float deltaTime)
        {
            buildingData.ConstructionProgress += deltaTime / buildingData.ConstructionTime;
            
            if (buildingData.ConstructionProgress >= 1.0f)
            {
                // 升级完成
                buildingData.ConstructionProgress = 1.0f;
                buildingData.State = BuildingState.Operational;
                buildingData.Level++;
                
                // 应用升级效果
                ApplyUpgradeEffects(buildingData);
                
                DAHLog.Info(LogCategory.BUILDINGS, $"[{_managerName}] 建筑升级完成: ID={buildingData.BuildingId}, 等级={buildingData.Level}");
            }
        }

        /// <summary>
        /// 更新建筑修理进度
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateRepairProgress(BuildingData buildingData, float deltaTime)
        {
            // 简化修理逻辑，每秒恢复10%最大生命值
            float repairRate = buildingData.MaxHealth * 0.1f;
            buildingData.Health += repairRate * deltaTime;
            
            if (buildingData.Health >= buildingData.MaxHealth)
            {
                // 修理完成
                buildingData.Health = buildingData.MaxHealth;
                buildingData.State = BuildingState.Operational;
                
                DAHLog.Info(LogCategory.BUILDINGS, $"[{_managerName}] 建筑修理完成: ID={buildingData.BuildingId}");
            }
        }

        /// <summary>
        /// 更新运行中的建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateOperationalBuilding(BuildingData buildingData, float deltaTime)
        {
            // 更新建筑经验
            buildingData.Experience += deltaTime;
            
            // 检查建筑是否受损
            if (buildingData.Health < buildingData.MaxHealth * 0.5f)
            {
                buildingData.State = BuildingState.Damaged;
            }
        }

        /// <summary>
        /// 更新生产队列
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateProductionQueues(float deltaTime)
        {
            // 简化实现，实际项目中需要完整的生产队列系统
        }

        /// <summary>
        /// 应用升级效果
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        private void ApplyUpgradeEffects(BuildingData buildingData)
        {
            if (!_buildingTemplates.TryGetValue(buildingData.BuildingType, out BuildingTemplate template))
                return;
                
            // 根据等级应用属性加成
            float levelMultiplier = 1.0f + (buildingData.Level - 1) * 0.2f; // 每级增加20%
            
            buildingData.MaxHealth = template.MaxHealth * levelMultiplier;
            buildingData.Health = buildingData.MaxHealth; // 升级后恢复满血
            buildingData.BioEnergyGeneration = template.BioEnergyGeneration * levelMultiplier;
        }

        /// <summary>
        /// 检查建筑是否需要持续更新
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <returns>是否需要持续更新</returns>
        private bool NeedsContinuousUpdate(BuildingData buildingData)
        {
            switch (buildingData.State)
            {
                case BuildingState.UnderConstruction:
                case BuildingState.Upgrading:
                case BuildingState.Repairing:
                case BuildingState.Operational:
                    return true;
                default:
                    return false;
            }
        }
    }
}