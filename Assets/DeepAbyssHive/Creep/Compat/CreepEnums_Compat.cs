using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Compat
{
    public static class CreepSourceTypeCompat
    {
        public const CreepSourceType Basic = (CreepSourceType)2;
        public const CreepSourceType Enhanced = (CreepSourceType)3;
        public const CreepSourceType Specialized = (CreepSourceType)4;
        public const CreepSourceType Manual = (CreepSourceType)1;
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
