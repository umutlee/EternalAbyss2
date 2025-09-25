using System;

namespace DeepAbyssHive.Core.Economy
{
    /// <summary>
    /// 定義遊戲中的資源類型
    /// </summary>
    [Serializable]
    public enum ResourceType
    {
        Energy = 0,
        Minerals = 1,
        Biomass = 2,
        Research = 3
    }
}