using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯统计信息
    /// </summary>
    [System.Serializable]
    public class CreepStatistics
    {
        [Header("基础统计")]
        public int TotalTiles;
        public int ActiveTiles;
        public int HealthyTiles;
        public int GrowingTiles;
        public int StarvingTiles;
        public int DyingTiles;
        public int BasicTiles;
        public int EnhancedTiles;
        public int SpecializedTiles;
        
        [Header("覆盖统计")]
        public float TotalCoverage;
        public float ActiveCoverage;
        public float CoveragePercentage;
        public float TotalArea;
        
        [Header("健康统计")]
        public float TotalHealth;
        public float AverageHealth;
        public float TotalResourcesGenerated;
        
        [Header("资源统计")]
        public int NutritionSources;
        public float TotalNutritionGenerated;
        public float TotalNutritionConsumed;
        public float NutritionBalance;
        
        [Header("网络统计")]
        public int ConnectedRegions;
        public int IsolatedRegions;
        public float NetworkEfficiency;
        
        [Header("性能统计")]
        public float UpdateTime;
        public int UpdatesPerSecond;
        public float MemoryUsage;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CreepStatistics()
        {
            Reset();
        }
        
        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void Reset()
        {
            TotalTiles = 0;
            ActiveTiles = 0;
            HealthyTiles = 0;
            GrowingTiles = 0;
            StarvingTiles = 0;
            DyingTiles = 0;
            BasicTiles = 0;
            EnhancedTiles = 0;
            SpecializedTiles = 0;
            
            TotalCoverage = 0f;
            ActiveCoverage = 0f;
            CoveragePercentage = 0f;
            TotalArea = 0f;
            
            TotalHealth = 0f;
            AverageHealth = 0f;
            TotalResourcesGenerated = 0f;
            
            NutritionSources = 0;
            TotalNutritionGenerated = 0f;
            TotalNutritionConsumed = 0f;
            NutritionBalance = 0f;
            
            ConnectedRegions = 0;
            IsolatedRegions = 0;
            NetworkEfficiency = 0f;
            
            UpdateTime = 0f;
            UpdatesPerSecond = 0;
            MemoryUsage = 0f;
        }
        
        /// <summary>
        /// 计算覆盖百分比
        /// </summary>
        public void CalculateCoveragePercentage()
        {
            if (TotalCoverage > 0f)
            {
                CoveragePercentage = (ActiveCoverage / TotalCoverage) * 100f;
            }
            else
            {
                CoveragePercentage = 0f;
            }
        }
        
        /// <summary>
        /// 计算营养平衡
        /// </summary>
        public void CalculateNutritionBalance()
        {
            NutritionBalance = TotalNutritionGenerated - TotalNutritionConsumed;
        }
        
        /// <summary>
        /// 计算网络效率
        /// </summary>
        public void CalculateNetworkEfficiency()
        {
            if (ConnectedRegions + IsolatedRegions > 0)
            {
                NetworkEfficiency = (float)ConnectedRegions / (ConnectedRegions + IsolatedRegions);
            }
            else
            {
                NetworkEfficiency = 0f;
            }
        }
    }
}