namespace DeepAbyssHive.Creep.Enums
{
    /// <summary>
    /// Creep 狀態枚舉
    /// </summary>
    public enum CreepState
    {
        /// <summary>
        /// 閒置狀態
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 移動狀態
        /// </summary>
        Moving = 1,
        
        /// <summary>
        /// 工作狀態
        /// </summary>
        Working = 2,
        
        /// <summary>
        /// 戰鬥狀態
        /// </summary>
        Combat = 3,
        
        /// <summary>
        /// 死亡狀態
        /// </summary>
        Dead = 4
    }
}