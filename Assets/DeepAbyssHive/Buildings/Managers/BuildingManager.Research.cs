using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 研究功能 - 委托给 IBuildingConstructionService
    /// 保持向后兼容的API，内部委托给建造服务处理
    /// </summary>
    public partial class BuildingManager
    {
        /// <summary>
        /// 开始研究（委托给建造服务）
        /// </summary>
        public bool StartResearch(ResearchType researchType, int buildingId)
        {
            return _constructionService?.StartResearch(researchType, buildingId) ?? false;
        }

        /// <summary>
        /// 取消研究（委托给建造服务）
        /// </summary>
        public bool CancelResearch(ResearchType researchType)
        {
            return _constructionService?.CancelResearch(researchType) ?? false;
        }

        /// <summary>
        /// 完成研究（委托给建造服务）
        /// </summary>
        public bool CompleteResearch(ResearchType researchType)
        {
            return _constructionService?.CompleteResearch(researchType) ?? false;
        }

        /// <summary>
        /// 检查研究是否已完成（委托给查询服务）
        /// </summary>
        public bool IsResearchCompleted(ResearchType researchType, int playerId)
        {
            return _queryService?.IsResearchCompleted(researchType, playerId) ?? false;
        }

        /// <summary>
        /// 检查研究前置条件（委托给查询服务）
        /// </summary>
        public bool CanStartResearch(ResearchType researchType, int playerId)
        {
            return _queryService?.CanStartResearch(researchType, playerId) ?? false;
        }

        /// <summary>
        /// 获取研究进度（委托给建造服务）
        /// </summary>
        public float GetResearchProgress(ResearchType researchType)
        {
            return _constructionService?.GetResearchProgress(researchType) ?? 0f;
        }

        /// <summary>
        /// 获取已完成的研究列表（委托给查询服务）
        /// </summary>
        public List<ResearchType> GetCompletedResearch(int playerId)
        {
            return _queryService?.GetCompletedResearch(playerId) ?? new List<ResearchType>();
        }

        /// <summary>
        /// 获取正在进行的研究列表（委托给建造服务）
        /// </summary>
        public List<ResearchType> GetActiveResearch()
        {
            return _constructionService?.GetActiveResearch() ?? new List<ResearchType>();
        }

        /// <summary>
        /// 加速研究（委托给建造服务）
        /// </summary>
        public bool AccelerateResearch(ResearchType researchType, float speedMultiplier)
        {
            return _constructionService?.AccelerateResearch(researchType, speedMultiplier) ?? false;
        }

        /// <summary>
        /// 获取研究成本（委托给查询服务）
        /// </summary>
        public ResourceCost GetResearchCost(ResearchType researchType)
        {
            return _queryService?.GetResearchCost(researchType) ?? default(ResourceCost);
        }
    }
}