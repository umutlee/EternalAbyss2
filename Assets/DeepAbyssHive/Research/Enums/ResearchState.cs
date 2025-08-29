namespace DeepAbyssHive.Research.Enums
{
    /// <summary>
    /// 研究狀態枚舉
    /// </summary>
    public enum ResearchState
    {
        /// <summary>
        /// 未開始
        /// </summary>
        NotStarted = 0,
        
        /// <summary>
        /// 進行中
        /// </summary>
        InProgress = 1,
        
        /// <summary>
        /// 已暫停
        /// </summary>
        Paused = 2,
        
        /// <summary>
        /// 已完成
        /// </summary>
        Completed = 3,
        
        /// <summary>
        /// 已取消
        /// </summary>
        Cancelled = 4
    }
}