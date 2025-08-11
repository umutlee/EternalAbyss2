using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Managers
{
    /// <summary>
    /// BuildingManager 研究系统
    /// 说明：
    /// - 本文件为partial占位，不改变任何对外API与行为
    /// - 后续将把 StartResearch(int,string) / CancelResearch(int)
    ///   以及内部研究模板、进度更新、完成写入等方法迁移至此：
    ///   - bool StartResearch(string researchId, int playerId)
    ///   - bool IsResearchCompleted(string researchId, int playerId)
    ///   - string[] GetCompletedResearch(int playerId)
    ///   - void UpdateResearch(float deltaTime)
    ///   - void InitializeResearchTemplates()
    ///   - void CompleteResearch(string researchId, int playerId)
    /// </summary>
    public partial class BuildingManager
    {
        /// <summary>
        /// 开始研究（建筑版本）
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="researchId">研究ID</param>
        public void StartResearch(int buildingId, string researchId)
        {
            if (!_buildings.TryGetValue(buildingId, out BuildingData buildingData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试在不存在的建筑中开始研究: {buildingId}");
                return;
            }
            
            StartResearch(researchId, buildingData.OwnerId);
        }

        /// <summary>
        /// 开始研究（玩家版本）
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功</returns>
        public bool StartResearch(string researchId, int playerId)
        {
            if (!_researchTemplates.TryGetValue(researchId, out ResearchTemplate template))
            {
                Debug.LogError($"[{_managerName}] 研究模板不存在: {researchId}");
                return false;
            }
            
            // 检查是否已经研究过
            if (IsResearchCompleted(researchId, playerId))
            {
                Debug.LogWarning($"[{_managerName}] 研究已完成: {researchId}");
                return false;
            }
            
            // 检查前置研究
            if (template.Prerequisites != null && template.Prerequisites.Length > 0)
            {
                foreach (string prerequisite in template.Prerequisites)
                {
                    if (!IsResearchCompleted(prerequisite, playerId))
                    {
                        Debug.LogWarning($"[{_managerName}] 前置研究未完成: {prerequisite}");
                        return false;
                    }
                }
            }
            
            // 开始研究
            CompleteResearch(researchId, playerId);
            
            Debug.Log($"[{_managerName}] 开始研究: {researchId}, 玩家={playerId}");
            
            return true;
        }

        /// <summary>
        /// 取消研究
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        public void CancelResearch(int buildingId)
        {
            // 简化实现，实际项目中需要完整的研究系统
            Debug.Log($"[{_managerName}] 取消研究: 建筑={buildingId}");
        }

        /// <summary>
        /// 检查研究是否完成
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否完成</returns>
        public bool IsResearchCompleted(string researchId, int playerId)
        {
            if (!_playerResearch.TryGetValue(playerId, out List<string> completedResearch))
            {
                return false;
            }
            
            return completedResearch.Contains(researchId);
        }

        /// <summary>
        /// 获取玩家已完成的研究
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>已完成的研究ID数组</returns>
        public string[] GetCompletedResearch(int playerId)
        {
            if (!_playerResearch.TryGetValue(playerId, out List<string> completedResearch))
            {
                return new string[0];
            }
            
            return completedResearch.ToArray();
        }

        /// <summary>
        /// 更新研究
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void UpdateResearch(float deltaTime)
        {
            // 简化实现，实际项目中需要完整的研究系统
        }

        /// <summary>
        /// 初始化研究模板
        /// </summary>
        private void InitializeResearchTemplates()
        {
            // 从配置文件或资源中加载研究模板
            // 这里使用简化的硬编码实现
        }

        /// <summary>
        /// 完成研究
        /// </summary>
        /// <param name="researchId">研究ID</param>
        /// <param name="playerId">玩家ID</param>
        private void CompleteResearch(string researchId, int playerId)
        {
            if (!_playerResearch.ContainsKey(playerId))
            {
                _playerResearch[playerId] = new List<string>();
            }
            
            if (!_playerResearch[playerId].Contains(researchId))
            {
                _playerResearch[playerId].Add(researchId);
            }
        }
    }
}