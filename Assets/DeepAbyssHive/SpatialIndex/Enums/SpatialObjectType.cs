namespace DeepAbyssHive.SpatialIndex.Enums
{
    /// <summary>
    /// 空间对象类型
    /// </summary>
    public enum SpatialObjectType
    {
        All = 0,
        Unit = 1,
        Building = 2,
        Resource = 4,
        Terrain = 8,
        Creep = 16,
        Effect = 32,
        Projectile = 64
    }
}