using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Compat
{
    /// <summary>
    /// 建築狀態兼容層：目前僅提供無害的 Normalize，避免引用不存在的舊枚舉值（例如 Built）。
    /// 後續如需從舊值映射，可在這裡補對應表，但不要直接引用不存在的枚舉成員。
    /// </summary>
    public static class BuildingStateCompat
    {
        public static BuildingState Normalize(this BuildingState s) => s;
    }
}