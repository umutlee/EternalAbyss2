using System;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Compat
{
    public static class TerrainTypeConversions
    {
        public static DeepAbyssHive.Terrain.Enums.TerrainType ToEnum(this DeepAbyssHive.Terrain.Data.TerrainType t)
            => (DeepAbyssHive.Terrain.Enums.TerrainType)(int)t;

        public static DeepAbyssHive.Terrain.Data.TerrainType ToData(this DeepAbyssHive.Terrain.Enums.TerrainType t)
            => (DeepAbyssHive.Terrain.Data.TerrainType)(int)t;

        public static DeepAbyssHive.Terrain.Enums.TerrainType[,] ToEnum2D(this DeepAbyssHive.Terrain.Data.TerrainType[,] src)
        {
            var h = src.GetLength(0); var w = src.GetLength(1);
            var dst = new DeepAbyssHive.Terrain.Enums.TerrainType[h, w];
            for (int y=0; y<h; y++) for(int x=0; x<w; x++) dst[y,x] = ((DeepAbyssHive.Terrain.Enums.TerrainType)(int)src[y,x]);
            return dst;
        }

        public static DeepAbyssHive.Terrain.Data.TerrainType[,] ToData2D(this DeepAbyssHive.Terrain.Enums.TerrainType[,] src)
        {
            var h = src.GetLength(0); var w = src.GetLength(1);
            var dst = new DeepAbyssHive.Terrain.Data.TerrainType[h, w];
            for (int y=0; y<h; y++) for(int x=0; x<w; x++) dst[y,x] = ((DeepAbyssHive.Terrain.Data.TerrainType)(int)src[y,x]);
            return dst;
        }
    }
}