using UnityEngine;

namespace DeepAbyssHive.Terrain.Data
{
    /// <summary>
    /// 地形块数据
    /// </summary>
    [System.Serializable]
    public partial struct TerrainChunk
    {
        public Vector2Int ChunkCoord;
        public Vector3 WorldPosition;
        // removed duplicate field; keep the property version declared elsewhere
        public int[,] TypeMap;
        public bool IsGenerated;
        public bool IsModified;
        public float LastModifiedTime;

        /// <summary>
        /// TerrainChunk 建構子
        /// </summary>
        public TerrainChunk(Vector2Int chunkCoord, int chunkSize, float tileSize, int[,] terrainData, GameObject chunkObject = null)
        {
            ChunkCoord = chunkCoord;
            WorldPosition = new Vector3(chunkCoord.x * chunkSize * tileSize, 0f, chunkCoord.y * chunkSize * tileSize);
            TypeMap = terrainData ?? new int[chunkSize, chunkSize];
            IsGenerated = terrainData != null;
            IsModified = false;
            LastModifiedTime = Time.time;
        }
    }
}