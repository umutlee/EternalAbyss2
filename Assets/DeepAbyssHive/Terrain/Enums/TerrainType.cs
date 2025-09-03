namespace DeepAbyssHive.Terrain.Enums
{
    /// <summary>
    /// 地形类型
    /// </summary>
    public enum TerrainType
    {
        Normal,
        Ground = Normal,  // 別名，用於向後兼容
        Rock,
        Water,
        Lava,
        Ice,
        Sand,
        Mud,
        Toxic,
        Void
    }
}