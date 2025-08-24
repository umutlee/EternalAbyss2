namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯网络状态枚举
    /// 定义菌毯网络的不同状态
    /// </summary>
    public enum CreepNetworkState
    {
        /// <summary>未知状态</summary>
        Unknown = 0,
        
        /// <summary>空闲状态 - 网络正常但无活动</summary>
        Idle = 1,
        
        /// <summary>活跃状态 - 网络正在扩张或维护</summary>
        Active = 2,
        
        /// <summary>阻塞状态 - 网络扩张受阻</summary>
        Blocked = 3
    }
}