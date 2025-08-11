namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 适应性特征数据结构
    /// </summary>
    [System.Serializable]
    public struct AdaptiveTrait
    {
        /// <summary>
        /// 特征ID
        /// </summary>
        public string TraitId;
        
        /// <summary>
        /// 特征等级
        /// </summary>
        public int Level;
        
        /// <summary>
        /// 环境类型
        /// </summary>
        public string EnvironmentType;
        
        /// <summary>
        /// 属性修改器
        /// </summary>
        public AttributeModifier[] Modifiers;
        
        /// <summary>
        /// 创建默认适应性特征
        /// </summary>
        /// <param name="traitId">特征ID</param>
        /// <param name="environmentType">环境类型</param>
        /// <returns>默认适应性特征</returns>
        public static AdaptiveTrait CreateDefault(string traitId, string environmentType)
        {
            return new AdaptiveTrait
            {
                TraitId = traitId,
                Level = 1,
                EnvironmentType = environmentType,
                Modifiers = new AttributeModifier[0]
            };
        }
    }
}