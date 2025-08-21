namespace DeepAbyssHive.Creep
{
    /// <summary>
    /// 菌毯瓦片狀態
    /// </summary>
    public enum CreepTileStatus
    {
        /// <summary>
        /// 正常狀態
        /// </summary>
        Normal = 0,
        
        /// <summary>
        /// 成長中
        /// </summary>
        Growing = 1,
        
        /// <summary>
        /// 衰減中
        /// </summary>
        Decaying = 2,
        
        /// <summary>
        /// 飢餓狀態
        /// </summary>
        Starving = 3,
        
        /// <summary>
        /// 受損狀態
        /// </summary>
        Damaged = 4,
        
        /// <summary>
        /// 死亡狀態
        /// </summary>
        Dead = 5,
        
        /// <summary>
        /// 休眠狀態
        /// </summary>
        Dormant = 6
    }
}