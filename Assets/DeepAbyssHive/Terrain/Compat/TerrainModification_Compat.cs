
using UnityEngine;

namespace DeepAbyssHive.Terrain.Compat
{
    /// 舊名讀取器（暫回預設，後續補真實映射）
    public static class TerrainModificationCompat
    {
        public static Vector3 Position(object mod) => Vector3.zero;
        public static float Radius(object mod) => 0f;
        public static float TerrainTypeValue(object mod) => 0f;
        public static float Value(object mod) => 0f;
        public static int Type(object mod) => 0;
        public static float Timestamp(object mod) => 0f;
    }
}
