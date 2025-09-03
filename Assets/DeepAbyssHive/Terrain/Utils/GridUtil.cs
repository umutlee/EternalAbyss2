using UnityEngine;

// Unity 2022.3.62f1 / Targets: PC/Android/iOS/MacOS
namespace DeepAbyssHive.Terrain.Utils
{
    /// <summary>
    /// 地形網格座標轉換工具類
    /// EA-M1-T01: 統一世界↔格點換算，CellSize=2f
    /// </summary>
    public static class GridUtil
    {
        /// <summary>格子大小（世界單位）</summary>
        public const float CellSize = 2f;

        /// <summary>
        /// 世界座標轉換為格點座標
        /// </summary>
        /// <param name="worldPosition">世界座標</param>
        /// <returns>格點座標</returns>
        public static Vector2Int WorldToGrid(Vector3 worldPosition)
        {
            int gridX = Mathf.FloorToInt(worldPosition.x / CellSize);
            int gridZ = Mathf.FloorToInt(worldPosition.z / CellSize);
            return new Vector2Int(gridX, gridZ);
        }

        /// <summary>
        /// 格點座標轉換為世界座標（格子中心點）
        /// </summary>
        /// <param name="gridCoord">格點座標</param>
        /// <returns>世界座標</returns>
        public static Vector3 GridToWorld(Vector2Int gridCoord)
        {
            float worldX = gridCoord.x * CellSize + CellSize * 0.5f;
            float worldZ = gridCoord.y * CellSize + CellSize * 0.5f;
            return new Vector3(worldX, 0f, worldZ);
        }

        /// <summary>
        /// 格點座標轉換為世界座標（指定高度）
        /// </summary>
        /// <param name="gridCoord">格點座標</param>
        /// <param name="height">高度</param>
        /// <returns>世界座標</returns>
        public static Vector3 GridToWorld(Vector2Int gridCoord, float height)
        {
            Vector3 worldPos = GridToWorld(gridCoord);
            worldPos.y = height;
            return worldPos;
        }

        /// <summary>
        /// 驗證往返轉換精度（用於測試）
        /// </summary>
        /// <param name="originalWorld">原始世界座標</param>
        /// <returns>往返轉換誤差是否小於 1e-4</returns>
        public static bool ValidateRoundTrip(Vector3 originalWorld)
        {
            Vector2Int grid = WorldToGrid(originalWorld);
            Vector3 backToWorld = GridToWorld(grid);
            
            // 只檢查 X 和 Z 軸，Y 軸由格子中心點決定
            float errorX = Mathf.Abs(originalWorld.x - (grid.x * CellSize + CellSize * 0.5f));
            float errorZ = Mathf.Abs(originalWorld.z - (grid.y * CellSize + CellSize * 0.5f));
            
            return errorX < 1e-4f && errorZ < 1e-4f;
        }
    }
}