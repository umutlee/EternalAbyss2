using UnityEngine;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Data
{
    /// <summary>
    /// 地形块数据
    /// </summary>
    [System.Serializable]
    public partial struct TerrainChunk : ITerrainChunk
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

        // ITerrainChunk 接口實現
        public Vector2Int Coordinates => ChunkCoord;
        public Bounds Bounds => new Bounds(WorldPosition, Vector3.one * 64f);
        
        public DeepAbyssHive.Terrain.Enums.TerrainType[,] TerrainTypes 
        {
            get 
            {
                if (TypeMap == null) return new DeepAbyssHive.Terrain.Enums.TerrainType[0,0];
                var result = new DeepAbyssHive.Terrain.Enums.TerrainType[TypeMap.GetLength(0), TypeMap.GetLength(1)];
                for (int x = 0; x < TypeMap.GetLength(0); x++)
                {
                    for (int y = 0; y < TypeMap.GetLength(1); y++)
                    {
                        result[x, y] = (DeepAbyssHive.Terrain.Enums.TerrainType)TypeMap[x, y];
                    }
                }
                return result;
            }
        }
        
        public float[,] HeightMap => new float[0,0]; // TODO: 實現
        public bool IsLoaded => IsGenerated;
        public int CurrentLODLevel => 0;

        public void Load() { }
        public void Unload() { }
        public void ModifyHeight(Vector2Int localPosition, float height) { }
        
        public void SetTerrainType(Vector2Int localPosition, DeepAbyssHive.Terrain.Enums.TerrainType type) 
        {
            if (TypeMap != null && localPosition.x >= 0 && localPosition.x < TypeMap.GetLength(0) 
                && localPosition.y >= 0 && localPosition.y < TypeMap.GetLength(1))
            {
                TypeMap[localPosition.x, localPosition.y] = (int)type;
                IsModified = true;
            }
        }
        
        public void SetLODLevel(int level) { }
        public float GetCreepDensity(Vector2Int localPosition) => 0f;
        public void SetCreepDensity(Vector2Int localPosition, float density, int ownerId) { }
        public void UpdateTerrain(float deltaTime) { }
        
        public void UpdateTerrainData(DeepAbyssHive.Terrain.Enums.TerrainType[,] terrainData) 
        {
            if (terrainData != null)
            {
                TypeMap = new int[terrainData.GetLength(0), terrainData.GetLength(1)];
                for (int x = 0; x < terrainData.GetLength(0); x++)
                {
                    for (int y = 0; y < terrainData.GetLength(1); y++)
                    {
                        TypeMap[x, y] = (int)terrainData[x, y];
                    }
                }
                IsModified = true;
            }
        }
        
        public void Cleanup() { }
    }
}