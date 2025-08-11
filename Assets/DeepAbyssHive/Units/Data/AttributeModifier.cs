namespace DeepAbyssHive.Units.Data
{
    /// <summary>
    /// 属性修改器数据结构
    /// </summary>
    [System.Serializable]
    public struct AttributeModifier
    {
        /// <summary>
        /// 修改器类型
        /// </summary>
        public enum ModifierType
        {
            /// <summary>
            /// 加法修改
            /// </summary>
            Add,
            
            /// <summary>
            /// 乘法修改
            /// </summary>
            Multiply,
            
            /// <summary>
            /// 设置值
            /// </summary>
            Set
        }
        
        /// <summary>
        /// 属性名称
        /// </summary>
        public string AttributeName;
        
        /// <summary>
        /// 修改器类型
        /// </summary>
        public ModifierType Type;
        
        /// <summary>
        /// 修改值
        /// </summary>
        public float Value;
        
        /// <summary>
        /// 创建加法修改器
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="value">加法值</param>
        /// <returns>属性修改器</returns>
        public static AttributeModifier CreateAdditive(string attributeName, float value)
        {
            return new AttributeModifier
            {
                AttributeName = attributeName,
                Type = ModifierType.Add,
                Value = value
            };
        }
        
        /// <summary>
        /// 创建乘法修改器
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="value">乘法值</param>
        /// <returns>属性修改器</returns>
        public static AttributeModifier CreateMultiplicative(string attributeName, float value)
        {
            return new AttributeModifier
            {
                AttributeName = attributeName,
                Type = ModifierType.Multiply,
                Value = value
            };
        }
        
        /// <summary>
        /// 创建设置修改器
        /// </summary>
        /// <param name="attributeName">属性名称</param>
        /// <param name="value">设置值</param>
        /// <returns>属性修改器</returns>
        public static AttributeModifier CreateAbsolute(string attributeName, float value)
        {
            return new AttributeModifier
            {
                AttributeName = attributeName,
                Type = ModifierType.Set,
                Value = value
            };
        }
    }
}