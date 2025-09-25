using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// GameConfig 經濟系統擴展 - partial 類避免修改主檔案
    /// </summary>
    public partial class GameConfigSO
    {
        [Header("=== Economy System ===")]
        [Tooltip("啟用資源系統")]
        public bool resourcesEnabled = true;
        
        [Tooltip("資源系統開發測試熱鍵")]
        public KeyCode resourcesDevTestKey = KeyCode.F5;
        
        [Header("Resource Settings")]
        [Tooltip("初始能量")]
        public float initialEnergy = 100f;
        
        [Tooltip("初始生物質")]
        public float initialBiomass = 50f;
        
        [Tooltip("初始礦物")]
        public float initialMinerals = 25f;
        
        [Header("Resource Generation")]
        [Tooltip("能量每秒生成率")]
        public float energyPerSecond = 1f;
        
        [Tooltip("生物質每秒生成率")]
        public float biomassPerSecond = 0.5f;
        
        [Tooltip("礦物每秒生成率")]
        public float mineralsPerSecond = 0.25f;
    }
}