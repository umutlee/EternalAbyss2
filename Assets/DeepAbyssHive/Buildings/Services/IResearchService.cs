using System.Collections.Generic;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 研究服务接口
    /// 提供科技研究相关功能
    /// </summary>
    public interface IResearchService : ICommandService
    {
        /// <summary>
        /// 开始研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="buildingId">研究建筑ID</param>
        /// <returns>是否成功</returns>
        bool StartResearch(string researchId, int playerId, int buildingId);

        /// <summary>
        /// 取消研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功</returns>
        bool CancelResearch(string researchId, int playerId);

        /// <summary>
        /// 完成研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功</returns>
        bool CompleteResearch(string researchId, int playerId);

        /// <summary>
        /// 检查研究是否已完成
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否已完成</returns>
        bool IsResearchCompleted(string researchId, int playerId);

        /// <summary>
        /// 检查研究是否可用
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否可用</returns>
        bool IsResearchAvailable(string researchId, int playerId);

        /// <summary>
        /// 获取研究进度
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>进度百分比（0-1）</returns>
        float GetResearchProgress(string researchId, int playerId);

        /// <summary>
        /// 获取玩家已完成的研究
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>已完成的研究ID列表</returns>
        List<string> GetCompletedResearch(int playerId);

        /// <summary>
        /// 获取玩家可用的研究
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>可用的研究ID列表</returns>
        List<string> GetAvailableResearch(int playerId);

        /// <summary>
        /// 获取当前进行中的研究
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>进行中的研究ID列表</returns>
        List<string> GetActiveResearch(int playerId);

        /// <summary>
        /// 获取研究模板
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <returns>研究模板</returns>
        ResearchTemplate GetResearchTemplate(string researchId);

        /// <summary>
        /// 检查研究前置条件
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>前置条件检查结果</returns>
        ResearchPrerequisiteResult CheckResearchPrerequisites(string researchId, int playerId);

        /// <summary>
        /// 加速研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="speedMultiplier">速度倍数</param>
        /// <returns>是否成功</returns>
        bool AccelerateResearch(string researchId, int playerId, float speedMultiplier);

        /// <summary>
        /// 获取研究解锁的内容
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <returns>解锁内容</returns>
        ResearchUnlocks GetResearchUnlocks(string researchId);
    }

    /// <summary>
    /// 研究前置条件检查结果
    /// </summary>
    public struct ResearchPrerequisiteResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public List<string> MissingPrerequisites;
        public List<BuildingType> MissingBuildings;
    }

    /// <summary>
    /// 研究解锁内容
    /// </summary>
    public struct ResearchUnlocks
    {
        public string[] UnlockedBuildings;
        public UnitType[] UnlockedUnitTypes;
        public string[] UnlockedTechnologies;
        public string[] UnlockedAbilities;
    }
}