using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Compat
{
    public static class CreepSourceTypeCompat
    {
        public static readonly CreepSourceType Basic = CreepSourceType.CreepColony;
        public static readonly CreepSourceType Enhanced = CreepSourceType.CreepTumor;
        public static readonly CreepSourceType Specialized = CreepSourceType.SubHive;
        public static readonly CreepSourceType Manual = CreepSourceType.MainHive;
    }
    
    /// <summary>
    /// Back-compat shim: old names (Healthy/Weakened/Collapsing) → current enum by ordinal.
    /// 說明：這裡不要直接引用 CreepTileStatus.X 的成員名稱，因為新 enum 名稱已變動。
    /// 先用 0/1/2 對應；若實際順序不同，調整下方數字即可。
    /// </summary>
    public static class CreepTileStatusCompat
    {
        public static CreepTileStatus Healthy    => (CreepTileStatus)0;
        public static CreepTileStatus Weakened   => (CreepTileStatus)1;
        public static CreepTileStatus Collapsing => (CreepTileStatus)2;

        // 方便雙向轉換（舊→新 / 新→舊）：
        public static CreepTileStatus FromLegacyOrdinal(int legacy) => (CreepTileStatus)legacy;
        public static int ToLegacyOrdinal(CreepTileStatus current)  => (int)current;
    }
}
