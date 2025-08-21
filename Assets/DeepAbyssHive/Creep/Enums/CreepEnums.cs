using System;

namespace DeepAbyssHive.Creep.Enums
{
    /// <summary>
    /// 菌毯源點類型枚舉
    /// 定義不同類型的菌毯生成源點
    /// </summary>
    [Serializable]
    public enum CreepSourceType
    {
        Unknown = 0,
        Manual = 1,
        Basic = 2,
        Enhanced = 3,
        Specialized = 4
    }

    /// <summary>
    /// 菌毯瓦片类型
    /// </summary>
    [Serializable]
    public enum CreepTileType
    {
        Unknown = 0,
        Neutral = 1,
        Creep = 2,
        Core = 3,
        Frontier = 4,
        Blocked = 5
    }

    /// <summary>
    /// 菌毯瓦片状态
    /// </summary>
    [Serializable]
    public enum CreepTileStatus
    {
        Unknown = 0,
        Healthy = 1,
        Weakened = 2,
        Collapsing = 3
    }

    /// <summary>
    /// 菌毯擴張類型枚舉
    /// 定義不同的菌毯擴張模式和行為
    /// </summary>
    [Serializable]
    public enum CreepExpansionType
    {
        Normal = 0,
        Fast = 1,
        Reinforced = 2
    }
}