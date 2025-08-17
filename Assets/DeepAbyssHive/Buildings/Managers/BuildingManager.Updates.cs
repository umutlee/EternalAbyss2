using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 更新功能 - 委托给服务层
    /// 保持向后兼容的API，内部委托给相应服务处理
    /// </summary>
    public partial class BuildingManager
    {
        /// <summary>
        /// 批量更新建筑状态（委托给建造服务）
        /// </summary>
        public void UpdateBuildings(float deltaTime)
        {
            _constructionService?.UpdateBuildings(deltaTime);
        }

        /// <summary>
        /// 更新建筑生产（委托给建造服务）
        /// </summary>
        public void UpdateProduction(float deltaTime)
        {
            _constructionService?.UpdateProduction(deltaTime);
        }

        /// <summary>
        /// 更新建筑维护（委托给建造服务）
        /// </summary>
        public void UpdateMaintenance(float deltaTime)
        {
            _constructionService?.UpdateMaintenance(deltaTime);
        }

        /// <summary>
        /// 更新建筑效果（委托给建造服务）
        /// </summary>
        public void UpdateBuildingEffects(float deltaTime)
        {
            _constructionService?.UpdateBuildingEffects(deltaTime);
        }

        /// <summary>
        /// 处理建筑损坏（委托给建造服务）
        /// </summary>
        public void ProcessBuildingDamage(int buildingId, float damage)
        {
            _constructionService?.ProcessBuildingDamage(buildingId, damage);
        }

        /// <summary>
        /// 处理建筑修复（委托给建造服务）
        /// </summary>
        public void ProcessBuildingRepair(int buildingId, float repairAmount)
        {
            _constructionService?.ProcessBuildingRepair(buildingId, repairAmount);
        }

        /// <summary>
        /// 更新建筑资源消耗（委托给建造服务）
        /// </summary>
        public void UpdateResourceConsumption(float deltaTime)
        {
            _constructionService?.UpdateResourceConsumption(deltaTime);
        }

        /// <summary>
        /// 更新建筑资源生产（委托给建造服务）
        /// </summary>
        public void UpdateResourceProduction(float deltaTime)
        {
            _constructionService?.UpdateResourceProduction(deltaTime);
        }

        /// <summary>
        /// 处理建筑升级完成（委托给建造服务）
        /// </summary>
        public void ProcessUpgradeCompletion(int buildingId)
        {
            _constructionService?.ProcessUpgradeCompletion(buildingId);
        }

        /// <summary>
        /// 处理建造完成（委托给建造服务）
        /// </summary>
        public void ProcessConstructionCompletion(int constructionId)
        {
            _constructionService?.ProcessConstructionCompletion(constructionId);
        }

        /// <summary>
        /// 获取需要更新的建筑列表（委托给查询服务）
        /// </summary>
        public List<BuildingData> GetBuildingsNeedingUpdate()
        {
            return _queryService?.GetBuildingsNeedingUpdate() ?? new List<BuildingData>();
        }

        /// <summary>
        /// 获取损坏的建筑列表（委托给查询服务）
        /// </summary>
        public List<BuildingData> GetDamagedBuildings(int playerId)
        {
            return _queryService?.GetDamagedBuildings(playerId) ?? new List<BuildingData>();
        }
    }
}