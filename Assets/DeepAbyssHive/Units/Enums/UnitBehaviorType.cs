namespace DeepAbyssHive.Units.Enums
{
    /// <summary>
    /// 單位行為類型枚舉
    /// </summary>
    public enum UnitBehaviorType
    {
        /// <summary>
        /// 閒置行為
        /// </summary>
        Idle = 0,
        
        /// <summary>
        /// 攻擊性行為
        /// </summary>
        Aggressive = 1,
        
        /// <summary>
        /// 防禦性行為
        /// </summary>
        Defensive = 2,
        
        /// <summary>
        /// 巡邏行為
        /// </summary>
        Patrol = 3,
        
        /// <summary>
        /// 跟隨行為
        /// </summary>
        Follow = 4,
        
        /// <summary>
        /// 逃跑行為
        /// </summary>
        Flee = 5
    }
}