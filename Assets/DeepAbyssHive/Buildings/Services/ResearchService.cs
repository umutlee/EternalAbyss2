using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Core.Utils;
using ResearchPrerequisiteResult = DeepAbyssHive.Buildings.Data.ResearchPrerequisiteResult;
using ResearchUnlocks = DeepAbyssHive.Buildings.Data.ResearchUnlocks;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 研究服务实现
    /// 提供科技研究相关功能
    /// </summary>
    public class ResearchService : IResearchService, IService
    {
        public string ServiceName => "ResearchService";
        public bool IsInitialized { get; private set; }
        
        private Dictionary<string, ResearchTemplate> _researchTemplates;
        private Dictionary<int, HashSet<string>> _playerCompletedResearch;
        private Dictionary<int, Dictionary<string, ResearchProgress>> _playerActiveResearch;
        private Dictionary<int, HashSet<string>> _playerAvailableResearch;

        /// <summary>
        /// 研究进度数据
        /// </summary>
        private struct ResearchProgress
        {
            public float Progress;
            public float StartTime;
            public int BuildingId;
            public float SpeedMultiplier;
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            _researchTemplates = new Dictionary<string, ResearchTemplate>();
            _playerCompletedResearch = new Dictionary<int, HashSet<string>>();
            _playerActiveResearch = new Dictionary<int, Dictionary<string, ResearchProgress>>();
            _playerAvailableResearch = new Dictionary<int, HashSet<string>>();

            LoadResearchTemplates();
            IsInitialized = true;

            Debug.Log("[ResearchService] 研究服务初始化完成");
        }

        public void Cleanup()
        {
            _researchTemplates?.Clear();
            _playerCompletedResearch?.Clear();
            _playerActiveResearch?.Clear();
            _playerAvailableResearch?.Clear();
            IsInitialized = false;

            Debug.Log("[ResearchService] 研究服务清理完成");
        }

        public void Pause()
        {
            // 暂停研究进度更新
        }

        public void Resume()
        {
            // 恢复研究进度更新
        }

        public bool IsCommandAvailable(string commandName, int playerId)
        {
            // 检查研究相关命令是否可用
            switch (commandName)
            {
                case "StartResearch":
                case "CancelResearch":
                case "AccelerateResearch":
                    return IsInitialized;
                default:
                    return false;
            }
        }

        public bool StartResearch(string researchId, int playerId, int buildingId)
        {
            if (!IsResearchAvailable(researchId, playerId))
            {
                Debug.LogWarning($"[ResearchService] 研究 {researchId} 对玩家 {playerId} 不可用");
                return false;
            }

            if (!_playerActiveResearch.ContainsKey(playerId))
                _playerActiveResearch[playerId] = new Dictionary<string, ResearchProgress>();

            if (_playerActiveResearch[playerId].ContainsKey(researchId))
            {
                Debug.LogWarning($"[ResearchService] 研究 {researchId} 已在进行中");
                return false;
            }

            var progress = new ResearchProgress
            {
                Progress = 0f,
                StartTime = Time.time,
                BuildingId = buildingId,
                SpeedMultiplier = 1f
            };

            _playerActiveResearch[playerId][researchId] = progress;
            Debug.Log($"[ResearchService] 开始研究 {researchId} (玩家: {playerId}, 建筑: {buildingId})");
            return true;
        }

        public bool StartResearch(string researchId, int buildingId)
        {
            // TODO: 實作真實流程：檢查前置、資源、入列或即時完成、寫入解鎖
            return false;
        }

        public bool CancelResearch(string researchId, int playerId)
        {
            if (!_playerActiveResearch.ContainsKey(playerId) || 
                !_playerActiveResearch[playerId].ContainsKey(researchId))
            {
                return false;
            }

            _playerActiveResearch[playerId].Remove(researchId);
            Debug.Log($"[ResearchService] 取消研究 {researchId} (玩家: {playerId})");
            return true;
        }

        public bool CompleteResearch(string researchId, int playerId)
        {
            if (!_playerActiveResearch.ContainsKey(playerId) || 
                !_playerActiveResearch[playerId].ContainsKey(researchId))
            {
                return false;
            }

            // 移除进行中的研究
            _playerActiveResearch[playerId].Remove(researchId);

            // 添加到已完成研究
            if (!_playerCompletedResearch.ContainsKey(playerId))
                _playerCompletedResearch[playerId] = new HashSet<string>();

            _playerCompletedResearch[playerId].Add(researchId);

            // 更新可用研究
            UpdateAvailableResearch(playerId);

            Debug.Log($"[ResearchService] 完成研究 {researchId} (玩家: {playerId})");
            return true;
        }

        public bool IsResearchCompleted(string researchId, int playerId)
        {
            return _playerCompletedResearch.ContainsKey(playerId) && 
                   _playerCompletedResearch[playerId].Contains(researchId);
        }

        public bool IsResearchAvailable(string researchId, int playerId)
        {
            if (IsResearchCompleted(researchId, playerId))
                return false;

            if (!_playerAvailableResearch.ContainsKey(playerId))
            {
                UpdateAvailableResearch(playerId);
            }

            return _playerAvailableResearch[playerId].Contains(researchId);
        }

        public float GetResearchProgress(string researchId, int playerId)
        {
            if (!_playerActiveResearch.ContainsKey(playerId) || 
                !_playerActiveResearch[playerId].ContainsKey(researchId))
            {
                return 0f;
            }

            var progress = _playerActiveResearch[playerId][researchId];
            var template = GetResearchTemplate(researchId);
            if (template == null) return 0f;

            var elapsedTime = Time.time - progress.StartTime;
            var adjustedTime = elapsedTime * progress.SpeedMultiplier;
            return Mathf.Clamp01(adjustedTime / template.ResearchTime);
        }

        public List<string> GetCompletedResearch(int playerId)
        {
            if (!_playerCompletedResearch.ContainsKey(playerId))
                return new List<string>();

            return new List<string>(_playerCompletedResearch[playerId]);
        }

        public List<string> GetAvailableResearch(int playerId)
        {
            if (!_playerAvailableResearch.ContainsKey(playerId))
            {
                UpdateAvailableResearch(playerId);
            }

            return new List<string>(_playerAvailableResearch[playerId]);
        }

        public List<string> GetActiveResearch(int playerId)
        {
            if (!_playerActiveResearch.ContainsKey(playerId))
                return new List<string>();

            return new List<string>(_playerActiveResearch[playerId].Keys);
        }

        public ResearchTemplate GetResearchTemplate(string researchId)
        {
            _researchTemplates.TryGetValue(researchId, out var template);
            return template;
        }

        public ResearchPrerequisiteResult CheckResearchPrerequisites(string researchId, int playerId)
        {
            var template = GetResearchTemplate(researchId);
            if (template == null)
            {
                return new ResearchPrerequisiteResult
                {
                    IsValid = false,
                    ErrorMessage = "研究模板不存在",
                    MissingPrerequisites = new List<string>(),
                    MissingBuildings = new List<BuildingType>()
                };
            }

            var missingPrereqs = new List<string>();
            var missingBuildings = new List<BuildingType>();

            // 检查前置研究
            if (template.Prerequisites != null)
            {
                foreach (var prerequisite in template.Prerequisites)
                {
                    if (!IsResearchCompleted(prerequisite, playerId))
                    {
                        missingPrereqs.Add(prerequisite);
                    }
                }
            }

            // 检查前置建筑
            if (template.Prerequisites != null)
            {
                foreach (var buildingType in template.Prerequisites)
                {
                    // TODO: 检查玩家是否拥有所需建筑
                    // 这里需要与 BuildingQueryService 集成
                }
            }

            var result = new ResearchPrerequisiteResult
            {
                IsValid = missingPrereqs.Count == 0 && missingBuildings.Count == 0,
                ErrorMessage = missingPrereqs.Count > 0 || missingBuildings.Count > 0 ? "不满足研究前置条件" : "",
                MissingPrerequisites = missingPrereqs.ToArray(),
                MissingBuildings = missingBuildings.Select(b => b.ToString()).ToArray()
            };

            return result;
        }

        public bool AccelerateResearch(string researchId, int playerId, float speedMultiplier)
        {
            if (!_playerActiveResearch.ContainsKey(playerId) || 
                !_playerActiveResearch[playerId].ContainsKey(researchId))
            {
                return false;
            }

            var progress = _playerActiveResearch[playerId][researchId];
            progress.SpeedMultiplier = speedMultiplier;
            _playerActiveResearch[playerId][researchId] = progress;

            Debug.Log($"[ResearchService] 加速研究 {researchId} (倍数: {speedMultiplier})");
            return true;
        }

        public ResearchUnlocks GetResearchUnlocks(string researchId)
        {
            var template = GetResearchTemplate(researchId);
            if (template == null)
            {
                return new ResearchUnlocks
                {
                    UnlockedBuildings = new string[0],
                    UnlockedUnitTypes = new DeepAbyssHive.Units.Enums.UnitType[0],
                    UnlockedTechnologies = new string[0],
                    UnlockedAbilities = new string[0]
                };
            }

            return new ResearchUnlocks
            {
                UnlockedBuildings = template.UnlockedBuildings ?? new string[0],
                UnlockedUnitTypes = template.UnlockedUnitTypes ?? new DeepAbyssHive.Units.Enums.UnitType[0],
                UnlockedTechnologies = template.UnlockedTechnologies ?? new string[0],
                UnlockedAbilities = new string[0] // 这个字段在ResearchTemplate中不存在，设为空数组
            };
        }

        /// <summary>
        /// 更新研究进度
        /// </summary>
        public void UpdateResearchProgress(float deltaTime)
        {
            foreach (var playerResearch in _playerActiveResearch)
            {
                var playerId = playerResearch.Key;
                var researchDict = playerResearch.Value;
                var completedResearch = new List<string>();

                foreach (var research in researchDict)
                {
                    var researchId = research.Key;
                    var progress = research.Value;
                    var template = GetResearchTemplate(researchId);

                    if (template != null)
                    {
                        var newProgress = progress.Progress + (deltaTime * progress.SpeedMultiplier / template.ResearchTime);
                        progress.Progress = newProgress;
                        researchDict[researchId] = progress;

                        if (newProgress >= 1f)
                        {
                            completedResearch.Add(researchId);
                        }
                    }
                }

                // 完成研究
                foreach (var researchId in completedResearch)
                {
                    CompleteResearch(researchId, playerId);
                }
            }
        }

        /// <summary>
        /// 加载研究模板
        /// </summary>
        private void LoadResearchTemplates()
        {
            // TODO: 从配置文件或 ScriptableObject 加载研究模板
            // 这里先添加一些示例数据
            
            Debug.Log("[ResearchService] 研究模板加载完成");
        }

        /// <summary>
        /// 更新玩家可用研究
        /// </summary>
        private void UpdateAvailableResearch(int playerId)
        {
            if (!_playerAvailableResearch.ContainsKey(playerId))
                _playerAvailableResearch[playerId] = new HashSet<string>();

            var availableResearch = _playerAvailableResearch[playerId];
            availableResearch.Clear();

            foreach (var template in _researchTemplates.Values)
            {
                if (!IsResearchCompleted(template.Id, playerId))
                {
                    var prerequisiteResult = CheckResearchPrerequisites(template.Id, playerId);
                    if (prerequisiteResult.IsValid)
                    {
                        availableResearch.Add(template.Id);
                    }
                }
            }
        }


    }
}