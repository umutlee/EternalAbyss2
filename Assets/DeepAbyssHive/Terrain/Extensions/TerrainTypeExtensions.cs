using System;
namespace DeepAbyssHive.Terrain
{
    public static class TerrainTypeExtensions
    {
        public static Enums.TerrainType ToEnum(this Data.TerrainType t) => (Enums.TerrainType)(int)t;
        public static Data.TerrainType ToData(this Enums.TerrainType t) => (Data.TerrainType)(int)t;

        public static int[,] ToIntGrid(this Enums.TerrainType[,] grid)
        {
            int w = grid.GetLength(0), h = grid.GetLength(1);
            var r = new int[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    r[x, y] = (int)grid[x, y];
            return r;
        }
    }
}