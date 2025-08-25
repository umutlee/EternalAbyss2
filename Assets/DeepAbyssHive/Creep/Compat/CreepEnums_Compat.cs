
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
    public static class CreepTileStatusCompat
    {
        public const CreepTileStatus Healthy = CreepTileStatus.Healthy;
        public const CreepTileStatus Weakened = CreepTileStatus.Weakened;
        public const CreepTileStatus Collapsing = CreepTileStatus.Collapsing;
    }
}
