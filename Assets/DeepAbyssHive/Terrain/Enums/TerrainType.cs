namespace DeepAbyssHive.Terrain.Enums
{
    /// <summary>
    /// 地形类型枚举
    /// </summary>
    public enum TerrainType
    {
        /// <summary>
        /// 未知地形
        /// </summary>
        Unknown = -1,
        
        /// <summary>
        /// 岩石地形
        /// </summary>
        Rock = 0,
        
        /// <summary>
        /// 泥土地形
        /// </summary>
        Soil = 1,
        
        /// <summary>
        /// 泥土地形 (兼容旧代码)
        /// </summary>
        Dirt = 1,
        
        /// <summary>
        /// 沙地地形
        /// </summary>
        Sand = 2,
        
        /// <summary>
        /// 菌毯地形
        /// </summary>
        Creep = 3,
        
        /// <summary>
        /// 水域地形
        /// </summary>
        Water = 4,
        
        /// <summary>
        /// 酸液地形
        /// </summary>
        Acid = 5,
        
        /// <summary>
        /// 岩浆地形
        /// </summary>
        Lava = 6,
        
        /// <summary>
        /// 冰冻地形
        /// </summary>
        Ice = 7,
        
        /// <summary>
        /// 草地地形
        /// </summary>
        Grass = 8,
        
        /// <summary>
        /// 森林地形
        /// </summary>
        Forest = 9,
        
        /// <summary>
        /// 山地地形
        /// </summary>
        Mountain = 10
    }
}
