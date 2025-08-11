namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 进化信息数据结构
    /// </summary>
    [System.Serializable]
    public struct EvolutionInfo
    {
        /// <summary>
        /// 进化等级
        /// </summary>
        public int Level;
        
        /// <summary>
        /// 进化路径ID
        /// </summary>
        public string PathId;
        
        /// <summary>
        /// 已解锁的能力
        /// </summary>
        public string[] UnlockedAbilities;
        
        /// <summary>
        /// 创建默认进化信息
        /// </summary>
        /// <returns>默认进化信息</returns>
        public static EvolutionInfo CreateDefault()
        {
            return new EvolutionInfo
            {
                Level = 0,
                PathId = "",
                UnlockedAbilities = new string[0]
            };
        }
    }
}