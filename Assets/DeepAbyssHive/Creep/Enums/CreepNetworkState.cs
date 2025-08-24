using System;

namespace DeepAbyssHive.Creep.Enums
{
    /// <summary>
    /// 菌毯网络连接状态
    /// </summary>
    [Serializable]
    public enum CreepNetworkState
    {
        /// <summary>
        /// 未知状态
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// 断开连接
        /// </summary>
        Disconnected = 1,
        
        /// <summary>
        /// 已连接
        /// </summary>
        Connected = 2,
        
        /// <summary>
        /// 正在连接
        /// </summary>
        Connecting = 3,
        
        /// <summary>
        /// 连接不稳定
        /// </summary>
        Unstable = 4
    }
}