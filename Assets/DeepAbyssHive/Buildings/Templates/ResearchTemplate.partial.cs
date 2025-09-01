using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DeepAbyssHive.Buildings.Templates
{
    /// <summary>
    /// ResearchTemplate 的相容性擴充
    /// </summary>
    public partial class ResearchTemplate
    {
        [Header("研究相容性設定")]
        [SerializeField]
        private List<string> _prerequisites = new List<string>();
        
        [SerializeField]
        private List<string> _unlocks = new List<string>();
        
        [SerializeField]
        private int _researchPoints = 100;
        
        [SerializeField]
        private float _researchTime = 60.0f;
        
        [SerializeField]
        private bool _isRepeatable = false;
        
        [SerializeField]
        private int _maxLevel = 1;
        
        [SerializeField]
        private string _researchCategory = "General";
        
        [SerializeField]
        private Sprite _researchIcon;
        
        [SerializeField]
        private string _researchDescription = "";
        
        /// <summary>
        /// 研究前置條件
        /// </summary>
        public string[] Prerequisites => _prerequisites.ToArray();
        
        /// <summary>
        /// 研究解鎖內容
        /// </summary>
        public string[] Unlocks => _unlocks.ToArray();
        
        /// <summary>
        /// 所需研究點數
        /// </summary>
        public int ResearchPoints => _researchPoints;
        
        /// <summary>
        /// 研究時間（秒）
        /// </summary>
        public float ResearchTime => _researchTime;
        
        /// <summary>
        /// 是否可重複研究
        /// </summary>
        public bool IsRepeatable => _isRepeatable;
        
        /// <summary>
        /// 最大等級
        /// </summary>
        public int MaxLevel => _maxLevel;
        
        /// <summary>
        /// 研究類別
        /// </summary>
        public string ResearchCategory => _researchCategory;
        
        /// <summary>
        /// 研究圖示
        /// </summary>
        public Sprite ResearchIcon => _researchIcon;
        
        /// <summary>
        /// 研究描述
        /// </summary>
        public string ResearchDescription => _researchDescription;
        
        /// <summary>
        /// 檢查是否滿足前置條件
        /// </summary>
        /// <param name="completedResearch">已完成的研究列表</param>
        /// <returns>如果滿足前置條件則返回 true</returns>
        public bool CheckPrerequisites(List<string> completedResearch)
        {
            foreach (var prerequisite in _prerequisites)
            {
                if (!completedResearch.Contains(prerequisite))
                {
                    return false;
                }
            }
            return true;
        }
        
        /// <summary>
        /// 獲取研究成本（基於等級）
        /// </summary>
        /// <param name="level">研究等級</param>
        /// <returns>該等級的研究成本</returns>
        public int GetResearchCost(int level)
        {
            if (level <= 0) return _researchPoints;
            
            // 每級成本遞增 50%
            return Mathf.RoundToInt(_researchPoints * Mathf.Pow(1.5f, level - 1));
        }
        
        /// <summary>
        /// 獲取研究時間（基於等級）
        /// </summary>
        /// <param name="level">研究等級</param>
        /// <returns>該等級的研究時間</returns>
        public float GetResearchTime(int level)
        {
            if (level <= 0) return _researchTime;
            
            // 每級時間遞增 25%
            return _researchTime * Mathf.Pow(1.25f, level - 1);
        }
    }
}