using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 建造/升级/修理 逻辑
    /// 说明：
    /// - 本文件为partial占位，不改变任何对外API与行为
    /// - 后续将把 StartConstruction/StartUpgrade/RepairBuilding/
    ///   UpdateConstructionProgress/UpdateUpgradeProgress/UpdateRepairProgress 等迁移至此
    /// </summary>
    public partial class BuildingManager
    {
        // 注意：CreateBuilding方法已在主文件中实现，这里不重复定义
        // 主文件中有两个CreateBuilding方法：
        // - public int CreateBuilding(BuildingData buildingData) - 接口方法
        // - private int CreateBuilding(BuildingType type, Vector3 position, int ownerId) - 实现方法

        // 注意：以下方法已在主文件中实现，这里不重复定义：
        // - public bool UpgradeBuilding(int buildingId) - 已在主文件中实现
        // - public void RepairBuilding(int buildingId) - 已在主文件中实现（注意参数不同）
        // - public void DestroyBuilding(int buildingId) - 已在主文件中实现（注意返回类型不同）

        /// <summary>
        /// 取消建造
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否成功取消</returns>
        public bool CancelConstruction(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                return false;
            }

            if (buildingData.State == BuildingState.Operational)
            {
                return false; // 已完成建造，无法取消
            }

            DestroyBuilding(buildingId);
            return true;
        }

        /// <summary>
        /// 获取建造进度
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>建造进度（0-1）</returns>
        public float GetConstructionProgress(int buildingId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                return 0f;
            }

            return buildingData.ConstructionProgress;
        }

        /// <summary>
        /// 设置建造进度
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="progress">进度（0-1）</param>
        /// <returns>是否成功设置</returns>
        public bool SetConstructionProgress(int buildingId, float progress)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                return false;
            }

            buildingData.ConstructionProgress = Mathf.Clamp01(progress);
            
            if (buildingData.ConstructionProgress >= 1f && buildingData.State == BuildingState.UnderConstruction)
            {
                buildingData.State = BuildingState.Operational;
                buildingData.Health = buildingData.MaxHealth;
                DAHLog.Info(LogCategory.BUILDING, $"[BuildingManager] 建筑建造完成: ID={buildingId}");
            }

            _buildings[buildingId] = buildingData;
            return true;
        }
    }
}