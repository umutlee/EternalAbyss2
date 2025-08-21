using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Config;

namespace DeepAbyssHive.Terrain.Services
{
    /// <summary>
    /// 地形生成服务实现
    /// 负责地形块的生成、噪声计算和程序化地形创建
    /// </summary>
    public class TerrainGenerationService : IService
    {
        #region 属性

        public string ServiceName => "TerrainGenerationService";
        public bool IsInitialized { get; private set; }

        #endregion

        #region 私有字段
        private TerrainConfigSO _config;
        private Dictionary<Vector2Int, TerrainType[,]> _chunkTerrainData;
        private Dictionary<Vector2Int, ITerrainChunk> _terrainChunks;
        
        // 噪声参数
        private float _noiseScale;
        private float _heightScale;
        private int _seed;
        private int _chunkSize;
        private float _tileSize;
        #endregion

        #region 构造函数
        public TerrainGenerationService(TerrainConfigSO config, 
            Dictionary<Vector2Int, TerrainType[,]> chunkTerrainData,
            Dictionary<Vector2Int, ITerrainChunk> terrainChunks)
        {
            _config = config;
            _chunkTerrainData = chunkTerrainData;
            _terrainChunks = terrainChunks;
            
            InitializeParameters();
        }
        #endregion

        #region IService 实现
        public void Initialize()
        {
            if (IsInitialized) return;
            
            InitializeParameters();
            IsInitialized = true;
            
            Debug.Log($"[{ServiceName}] 地形生成服务初始化完成");
        }

        public void Cleanup()
        {
            IsInitialized = false;
            Debug.Log($"[{ServiceName}] 地形生成服务清理完成");
        }
        #endregion

        #region 地形生成方法
        public ITerrainChunk GenerateChunk(Vector2Int chunkCoord)
        {
            TerrainType[,] terrainData = GenerateChunkTerrain(chunkCoord);
            return CreateTerrainChunk(chunkCoord, terrainData);
        }

        public float GenerateHeight(Vector3 worldPosition)
        {
            return GenerateHeight(new Vector2(worldPosition.x, worldPosition.z));
        }

        public TerrainType GenerateTerrainType(Vector3 worldPosition, float height)
        {
            return DetermineTerrainType(height, new Vector2(worldPosition.x, worldPosition.z));
        }

        public float GenerateNoise(float x, float y, float scale, int octaves, float persistence, float lacunarity)
        {
            float noise = 0f;
            float amplitude = 1f;
            float frequency = scale;
            
            for (int i = 0; i < octaves; i++)
            {
                noise += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                amplitude *= persistence;
                frequency *= lacunarity;
            }
            
            return noise;
        }

        public void InitializeWithConfig(TerrainConfigSO config)
        {
            _config = config;
            InitializeParameters();
            IsInitialized = true;
        }

        public void SetSeed(int seed)
        {
            _seed = seed;
            UnityEngine.Random.InitState(_seed);
        }

        private TerrainType[,] GenerateChunkTerrain(Vector2Int chunkCoord)
        {
            TerrainType[,] terrainData = new TerrainType[_chunkSize, _chunkSize];
            
            for (int x = 0; x < _chunkSize; x++)
            {
                for (int z = 0; z < _chunkSize; z++)
                {
                    Vector2 worldPos = ChunkLocalToWorld(chunkCoord, new Vector2Int(x, z));
                    float height = GenerateHeight(worldPos);
                    terrainData[x, z] = DetermineTerrainType(height, worldPos);
                }
            }
            
            return terrainData;
        }

        public float GenerateHeight(Vector2 worldPosition)
        {
            // 使用多层噪声生成高度
            float height = 0f;
            float amplitude = 1f;
            float frequency = _noiseScale;
            
            // 基础噪声层
            height += Mathf.PerlinNoise(
                (worldPosition.x + _seed) * frequency,
                (worldPosition.y + _seed) * frequency
            ) * amplitude;
            
            // 添加细节噪声层
            amplitude *= 0.5f;
            frequency *= 2f;
            height += Mathf.PerlinNoise(
                (worldPosition.x + _seed) * frequency,
                (worldPosition.y + _seed) * frequency
            ) * amplitude;
            
            // 添加更细的噪声层
            amplitude *= 0.5f;
            frequency *= 2f;
            height += Mathf.PerlinNoise(
                (worldPosition.x + _seed) * frequency,
                (worldPosition.y + _seed) * frequency
            ) * amplitude;
            
            return height * _heightScale;
        }

        public TerrainType DetermineTerrainType(float height, Vector2 worldPosition)
        {
            // 基于高度和位置确定地形类型
            if (height < -2f)
                return TerrainType.Water;
            else if (height < 0f)
                return TerrainType.Mud;
            else if (height < 3f)
                return TerrainType.Normal;
            else if (height < 8f)
                return TerrainType.Rock;
            else
                return TerrainType.Rock;
        }

        public ITerrainChunk CreateTerrainChunk(Vector2Int chunkCoord, TerrainType[,] terrainData)
        {
            Vector3 worldPosition = ChunkToWorldPosition(chunkCoord);
            
            // 创建地形块游戏对象
            GameObject chunkObject = new GameObject($"TerrainChunk_{chunkCoord.x}_{chunkCoord.y}");
            chunkObject.transform.position = worldPosition;
            
            // 创建地形块实例
            var chunk = new TerrainChunk(chunkCoord, _chunkSize, _tileSize, terrainData, chunkObject);
            
            return chunk;
        }

        public bool RegenerateChunk(Vector2Int chunkCoord)
        {
            try
            {
                // 生成新的地形数据
                TerrainType[,] newTerrainData = GenerateChunkTerrain(chunkCoord);
                
                // 更新缓存的地形数据
                _chunkTerrainData[chunkCoord] = newTerrainData;
                
                // 如果地形块已加载，更新它
                if (_terrainChunks.TryGetValue(chunkCoord, out ITerrainChunk existingChunk))
                {
                    existingChunk.UpdateTerrainData(newTerrainData);
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[TerrainGenerationService] 重新生成地形块失败 {chunkCoord}: {ex.Message}");
                return false;
            }
        }

        public void SetGenerationParameters(float noiseScale, float heightScale, int seed)
        {
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _seed = seed;
            
            // 重新初始化随机种子
            UnityEngine.Random.InitState(_seed);
        }

        public Vector2 GetNoiseValue(Vector2 worldPosition)
        {
            float noiseX = Mathf.PerlinNoise(
                (worldPosition.x + _seed) * _noiseScale,
                (worldPosition.y + _seed) * _noiseScale
            );
            
            float noiseY = Mathf.PerlinNoise(
                (worldPosition.x + _seed + 1000) * _noiseScale,
                (worldPosition.y + _seed + 1000) * _noiseScale
            );
            
            return new Vector2(noiseX, noiseY);
        }
        #endregion

        #region 私有辅助方法
        private void InitializeParameters()
        {
            if (_config != null)
            {
                _noiseScale = _config.noiseScale;
                _heightScale = _config.heightScale;
                _seed = _config.seed;
                _chunkSize = _config.chunkSize;
                _tileSize = _config.tileSize;
            }
            else
            {
                // 默认值
                _noiseScale = 0.1f;
                _heightScale = 10f;
                _seed = 12345;
                _chunkSize = 64;
                _tileSize = 1f;
            }
            
            UnityEngine.Random.InitState(_seed);
        }

        private Vector2 ChunkLocalToWorld(Vector2Int chunkCoord, Vector2Int localCoord)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            float worldX = chunkCoord.x * chunkWorldSize + localCoord.x * _tileSize;
            float worldZ = chunkCoord.y * chunkWorldSize + localCoord.y * _tileSize;
            return new Vector2(worldX, worldZ);
        }

        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0, worldZ);
        }
        #endregion
    }

}
