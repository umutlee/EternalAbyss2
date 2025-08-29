namespace DeepAbyssHive.Terrain.Enums
{
    /// <summary>
    /// 地形修改類型枚舉
    /// </summary>
    public enum TerrainModificationType
    {
        /// <summary>
        /// 無修改
        /// </summary>
        None = 0,
        
        /// <summary>
        /// 提升地形
        /// </summary>
        Raise = 1,
        
        /// <summary>
        /// 降低地形
        /// </summary>
        Lower = 2,
        
        /// <summary>
        /// 平整地形
        /// </summary>
        Flatten = 3,
        
        /// <summary>
        /// 平滑地形
        /// </summary>
        Smooth = 4,
        
        /// <summary>
        /// 繪製紋理
        /// </summary>
        Paint = 5,
        
        /// <summary>
        /// 添加細節物件
        /// </summary>
        AddDetail = 6,
        
        /// <summary>
        /// 移除細節物件
        /// </summary>
        RemoveDetail = 7,
        
        /// <summary>
        /// 添加樹木
        /// </summary>
        AddTree = 8,
        
    /// <summary>
    /// 移除樹木
    /// </summary>
    RemoveTree = 9,
    
    // Legacy compatibility values
    /// <summary>
    /// 類型變更（相容性）
    /// </summary>
    TypeChange = 10,
    
    /// <summary>
    /// 高度變更（相容性）
    /// </summary>
    HeightChange = 11,
    
    /// <summary>
    /// 組合修改（相容性）
    /// </summary>
    Combined = 12,
    
    /// <summary>
    /// 挖掘地形
    /// </summary>
    Dig = 13,
    
    /// <summary>
    /// 填充地形
    /// </summary>
    Fill = 14,
    
    /// <summary>
    /// 創建斜坡
    /// </summary>
    Ramp = 15,
    
    /// <summary>
    /// 創建隧道
    /// </summary>
    Tunnel = 16
    }
}