using System;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯擴張類型枚舉
    /// 定義不同的菌毯擴張模式和行為
    /// </summary>
    [Serializable]
    public enum CreepExpansionType
    {
        /// <summary>
        /// 正常擴張 - 標準的菌毯生長模式
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// 快速擴張 - 加速的菌毯生長，消耗更多資源
        /// </summary>
        Rapid = 1,
        
        /// <summary>
        /// 緩慢擴張 - 節約資源的緩慢生長模式
        /// </summary>
        Slow = 2,
        
        /// <summary>
        /// 定向擴張 - 朝特定方向的有針對性擴張
        /// </summary>
        Directional = 3,
        
        /// <summary>
        /// 防禦性擴張 - 優先覆蓋戰略位置的擴張模式
        /// </summary>
        Defensive = 4,
        
        /// <summary>
        /// 攻擊性擴張 - 快速向敵方區域擴張的模式
        /// </summary>
        Aggressive = 5,
        
        /// <summary>
        /// 資源導向擴張 - 優先向資源點擴張的模式
        /// </summary>
        ResourceOriented = 6,
        
        /// <summary>
        /// 手動擴張 - 由玩家直接控制的擴張模式
        /// </summary>
        Manual = 7
    }
}