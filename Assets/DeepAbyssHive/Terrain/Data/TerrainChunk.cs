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
    }
}