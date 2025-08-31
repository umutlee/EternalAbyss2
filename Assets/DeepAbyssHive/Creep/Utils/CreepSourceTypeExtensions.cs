using DeepAbyssHive.Creep.Data;
using CreepSourceTypeEnum = DeepAbyssHive.Creep.Enums.CreepSourceType;

namespace DeepAbyssHive.Creep.Utils
{
    /// <summary>
    /// 把舊的 Enums.CreepSourceType 與新的 Data.CreepSourceType 做顯式轉換，
    /// 避免到處 int <-> enum 來回轉造成 CS0266/CS1503/CS0150。
    /// </summary>
    public static class CreepSourceTypeExtensions
    {
        public static CreepSourceType ToData(this CreepSourceTypeEnum t)
            => (CreepSourceType)(int)t;

        public static CreepSourceTypeEnum ToEnum(this CreepSourceType t)
            => (CreepSourceTypeEnum)(int)t;
    }
}