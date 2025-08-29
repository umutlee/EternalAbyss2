using System;
using UnityEngine;

namespace DeepAbyssHive.Research.Data
{
    /// <summary>
    /// 研究數據結構
    /// </summary>
    [Serializable]
    public struct ResearchData
    {
        [Header("基本信息")]
        public string ResearchId;
        public string Name;
        public string Description;
        
        [Header("研究參數")]
        public float ResearchTime;
        public int RequiredLevel;
        public string[] Prerequisites;
        
        [Header("解鎖內容")]
        public string[] UnlockedBuildings;
        public string[] UnlockedUnits;
        public string[] UnlockedTechnologies;
        
        [Header("狀態")]
        public bool IsCompleted;
        public float Progress;
        public DateTime StartTime;
    }
}