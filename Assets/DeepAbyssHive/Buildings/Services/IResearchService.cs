using System.Collections.Generic;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 研究服務接口
    /// 提供研究相關的查詢和命令功能
    /// </summary>
    public interface IResearchService : IService
    {
        /// <summary>
        /// 開始研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功開始研究</returns>
        bool StartResearch(string researchId, int playerId);

        /// <summary>
        /// 取消研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功取消研究</returns>
        bool CancelResearch(string researchId, int playerId);

        /// <summary>
        /// 檢查研究是否完成
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否已完成</returns>
        bool IsResearchCompleted(string researchId, int playerId);

        /// <summary>
        /// 獲取可用的研究列表
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>可用研究ID列表</returns>
        List<string> GetAvailableResearch(int playerId);

        /// <summary>
        /// 獲取研究進度
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>研究進度 (0-1)</returns>
        float GetResearchProgress(string researchId, int playerId);
    }
}