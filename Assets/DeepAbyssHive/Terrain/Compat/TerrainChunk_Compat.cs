using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Interfaces;

namespace DeepAbyssHive.Terrain.Compat
{
    /// <summary>
    /// ITerrainChunk 相容層實作
    /// 提供與 RTSEngine.Terrain 系統的相容性
    /// </summary>
    public class TerrainChunk_Compat : ITerrainChunk
    {
        #region 私有字段
        private Vector2Int _chunkCoord;
        private TerrainType[,] _terrainData;
        private float[,] _heightData;
        private bool _isGenerated;
        private bool _isDirty;
        #endregion

        #region 屬性
        public Vector2Int ChunkCoord => _chunkCoord;
        public bool IsGenerated => _isGenerated;
        public bool IsDirty => _isDirty;
        
        // ITerrainChunk 介面屬性
        public Vector2Int Coordinates => _chunkCoord;
        public Bounds Bounds => new Bounds(new Vector3(_chunkCoord.x * 64, 0, _chunkCoord.y * 64), new Vector3(64, 10, 64));
        public TerrainType[,] TerrainTypes => _terrainData;
        public float[,] HeightMap => _heightData;
        public bool IsLoaded => _isGenerated;
        public int CurrentLODLevel { get; private set; } = 0;
        #endregion

        #region 構造函數
        public TerrainChunk_Compat(Vector2Int chunkCoord)
        {
            _chunkCoord = chunkCoord;
            _isGenerated = false;
            _isDirty = false;
        }
        #endregion

        #region ITerrainChunk 實作
        public void GenerateChunk(TerrainType[,] terrainData, float[,] heightData = null)
        {
            _terrainData = terrainData;
            _heightData = heightData;
            _isGenerated = true;
            _isDirty = false;
            
            Debug.Log($"[TerrainChunk_Compat] 生成區塊 {_chunkCoord}");
        }

        public void UpdateTerrainData(TerrainType[,] terrainData)
        {
            _terrainData = terrainData;
            _isDirty = true;
            
            Debug.Log($"[TerrainChunk_Compat] 更新區塊地形數據 {_chunkCoord}");
        }

        public void SetTerrainType(Vector2Int localPos, TerrainType terrainType)
        {
            SetTerrainTypeAt(localPos.x, localPos.y, terrainType);
        }

        public void Load()
        {
            _isGenerated = true;
            Debug.Log($"[TerrainChunk_Compat] 加載區塊 {_chunkCoord}");
        }

        public void Unload()
        {
            _isGenerated = false;
            Debug.Log($"[TerrainChunk_Compat] 卸載區塊 {_chunkCoord}");
        }

        public void ModifyHeight(Vector2Int localPosition, float height)
        {
            SetHeightAt(localPosition.x, localPosition.y, height);
        }

        public void SetLODLevel(int level)
        {
            CurrentLODLevel = level;
        }

        public float GetCreepDensity(Vector2Int localPosition)
        {
            // TODO: 實作菌毯密度獲取
            return 0f;
        }

        public void SetCreepDensity(Vector2Int localPosition, float density, int ownerId)
        {
            // TODO: 實作菌毯密度設置
        }

        public void UpdateTerrain(float deltaTime)
        {
            // TODO: 實作地形更新
        }

        public void Cleanup()
        {
            DestroyChunk();
        }

        public void UpdateHeightData(float[,] heightData)
        {
            _heightData = heightData;
            _isDirty = true;
            
            Debug.Log($"[TerrainChunk_Compat] 更新區塊高度數據 {_chunkCoord}");
        }

        public TerrainType GetTerrainTypeAt(int localX, int localY)
        {
            if (_terrainData == null || localX < 0 || localY < 0 || 
                localX >= _terrainData.GetLength(0) || localY >= _terrainData.GetLength(1))
            {
                return TerrainType.Void;
            }
            
            return _terrainData[localX, localY];
        }

        public float GetHeightAt(int localX, int localY)
        {
            if (_heightData == null || localX < 0 || localY < 0 || 
                localX >= _heightData.GetLength(0) || localY >= _heightData.GetLength(1))
            {
                return 0f;
            }
            
            return _heightData[localX, localY];
        }

        public void SetTerrainTypeAt(int localX, int localY, TerrainType terrainType)
        {
            if (_terrainData == null || localX < 0 || localY < 0 || 
                localX >= _terrainData.GetLength(0) || localY >= _terrainData.GetLength(1))
            {
                return;
            }
            
            _terrainData[localX, localY] = terrainType;
            _isDirty = true;
        }

        public void SetHeightAt(int localX, int localY, float height)
        {
            if (_heightData == null || localX < 0 || localY < 0 || 
                localX >= _heightData.GetLength(0) || localY >= _heightData.GetLength(1))
            {
                return;
            }
            
            _heightData[localX, localY] = height;
            _isDirty = true;
        }

        public void MarkDirty()
        {
            _isDirty = true;
        }

        public void ClearDirty()
        {
            _isDirty = false;
        }

        public void DestroyChunk()
        {
            _terrainData = null;
            _heightData = null;
            _isGenerated = false;
            _isDirty = false;
            
            Debug.Log($"[TerrainChunk_Compat] 銷毀區塊 {_chunkCoord}");
        }
        #endregion

        #region 公共方法
        public TerrainType[,] GetTerrainData()
        {
            return _terrainData;
        }

        public float[,] GetHeightData()
        {
            return _heightData;
        }

        public bool HasTerrainData()
        {
            return _terrainData != null;
        }

        public bool HasHeightData()
        {
            return _heightData != null;
        }
        #endregion
    }
}