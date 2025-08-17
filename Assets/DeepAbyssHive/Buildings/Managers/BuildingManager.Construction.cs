using UnityEngine;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 建造功能 - 委托给 IBuildingConstructionService
    /// 保持向后兼容的API，内部委托给建造服务处理
    /// </summary>
    public partial class BuildingManager
    {
        /// <summary>
        /// 取消建造（委托给建造服务）
        /// </summary>
        public bool CancelConstruction(int constructionId)
        {
            return _constructionService?.CancelConstruction(constructionId) ?? false;
        }

        /// <summary>
        /// 完成建造（委托给建造服务）
        /// </summary>
        public int CompleteConstruction(int constructionId)
        {
            return _constructionService?.CompleteConstruction(constructionId) ?? -1;
        }

        /// <summary>
        /// 取消升级（委托给建造服务）
        /// </summary>
        public bool CancelUpgrade(int buildingId)
        {
            return _constructionService?.CancelUpgrade(buildingId) ?? false;
        }

        /// <summary>
        /// 修理建筑（委托给建造服务）
        /// </summary>
        public bool RepairBuilding(int buildingId, float repairAmount = -1f)
        {
            return _constructionService?.RepairBuilding(buildingId, repairAmount) ?? false;
        }

        /// <summary>
        /// 设置建筑状态（委托给建造服务）
        /// </summary>
        public bool SetBuildingState(int buildingId, BuildingState state)
        {
            return _constructionService?.SetBuildingState(buildingId, state) ?? false;
        }

        /// <summary>
        /// 暂停/恢复建筑功能（委托给建造服务）
        /// </summary>
        public bool SetBuildingPaused(int buildingId, bool paused)
        {
            return _constructionService?.SetBuildingPaused(buildingId, paused) ?? false;
        }

        /// <summary>
        /// 获取建造进度（委托给建造服务）
        /// </summary>
        public float GetConstructionProgress(int constructionId)
        {
            return _constructionService?.GetConstructionProgress(constructionId) ?? 0f;
        }

        /// <summary>
        /// 获取升级进度（委托给建造服务）
        /// </summary>
        public float GetUpgradeProgress(int buildingId)
        {
            return _constructionService?.GetUpgradeProgress(buildingId) ?? 0f;
        }

        /// <summary>
        /// 加速建造（委托给建造服务）
        /// </summary>
        public bool AccelerateConstruction(int constructionId, float speedMultiplier)
        {
            return _constructionService?.AccelerateConstruction(constructionId, speedMultiplier) ?? false;
        }

        /// <summary>
        /// 加速升级（委托给建造服务）
        /// </summary>
        public bool AccelerateUpgrade(int buildingId, float speedMultiplier)
        {
            return _constructionService?.AccelerateUpgrade(buildingId, speedMultiplier) ?? false;
        }
    }
}