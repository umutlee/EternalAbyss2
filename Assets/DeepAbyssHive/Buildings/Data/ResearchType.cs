using System;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 研究類型枚舉
    /// 定義建築系統中可用的研究項目類型
    /// </summary>
    [Serializable]
    public enum ResearchType
    {
        /// <summary>無研究</summary>
        None = 0,
        
        /// <summary>基礎建築技術</summary>
        BasicConstruction = 1,
        
        /// <summary>進階建築技術</summary>
        AdvancedConstruction = 2,
        
        /// <summary>資源採集效率</summary>
        ResourceEfficiency = 3,
        
        /// <summary>防禦系統</summary>
        DefenseSystems = 4,
        
        /// <summary>能源管理</summary>
        EnergyManagement = 5,
        
        /// <summary>菌毯整合</summary>
        CreepIntegration = 6,
        
        /// <summary>生物適應</summary>
        BiologicalAdaptation = 7,
        
        /// <summary>進化加速</summary>
        EvolutionAcceleration = 8,
        
        /// <summary>環境控制</summary>
        EnvironmentalControl = 9,
        
        /// <summary>網路連接</summary>
        NetworkConnectivity = 10
    }
}