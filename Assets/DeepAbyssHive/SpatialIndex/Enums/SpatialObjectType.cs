namespace DeepAbyssHive.SpatialIndex.Enums
{
    /// <summary>空間物件分類（含 All = -1 供萬用查詢）。</summary>
    public enum SpatialObjectType : int
    {
        All        = -1, // 特殊：表示全部類型
        Unknown    = 0,
        Unit       = 1,
        Building   = 2,
        Resource   = 3,
        Effect     = 4,
        Projectile = 5,
        Terrain    = 6,
        Creep      = 7,
    }
}