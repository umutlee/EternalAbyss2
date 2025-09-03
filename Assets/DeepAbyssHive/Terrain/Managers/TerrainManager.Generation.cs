using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;
using TerrainTypeData = DeepAbyssHive.Terrain.Data.TerrainType;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器，负责管理分块地形系统 - 地形生成部分
    /// </summary>
    public partial class TerrainManager
    {
        #region 地形生成
        /// <summary>
        /// 加载地形
        /// </summary>
        /// <param name="playerPosition">玩家位置</param>
        public void LoadTerrain(Vector3 playerPosition)
        {
            Vector2Int centerChunk = WorldToChunkCoord(playerPosition);
            _currentCenterChunk = centerChunk;
            
            // 计算需要加载的区块
            HashSet<Vector2Int> chunksToLoad = new HashSet<Vector2Int>();
            int loadRadius = Mathf.RoundToInt(ConfigLoadRadius);
            for (int x = -loadRadius; x <= loadRadius; x++)
            {
                for (int y = -loadRadius; y <= loadRadius; y++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunk.x + x, centerChunk.y + y);
                    chunksToLoad.Add(chunkCoord);
                }
            }
            
            // 卸载不需要的区块
            List<Vector2Int> chunksToUnload = new List<Vector2Int>();
            foreach (var chunkCoord in _terrainChunks.Keys)
            {
                if (!chunksToLoad.Contains(chunkCoord))
                {
                    chunksToUnload.Add(chunkCoord);
                }
            }
            
            foreach (var chunkCoord in chunksToUnload)
            {
                UnloadChunk(chunkCoord);
            }
            
            // 加载新区块
            foreach (var chunkCoord in chunksToLoad)
            {
                if (!_terrainChunks.ContainsKey(chunkCoord))
                {
                    LoadChunk(chunkCoord);
                }
            }
            
            Debug.Log($"[{_managerName}] 地形加载完成，中心区块: {centerChunk}，已加载区块数: {_terrainChunks.Count}");
        }

        /// <summary>
        /// 生成地形块数据
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        /// <returns>地形类型数组</returns>
        private TerrainType[,] GenerateChunkTerrain(Vector2Int chunkCoord)
        {
            TerrainType[,] terrainData = new TerrainType[ConfigChunkSize, ConfigChunkSize];
            
            // 使用柏林噪声生成地形高度
            float offsetX = chunkCoord.x * ConfigChunkSize;
            float offsetY = chunkCoord.y * ConfigChunkSize;
            
            for (int x = 0; x < ConfigChunkSize; x++)
            {
                for (int y = 0; y < ConfigChunkSize; y++)
                {
                    float noiseX = (offsetX + x) * ConfigNoiseScale;
                    float noiseY = (offsetY + y) * ConfigNoiseScale;
                    
                    float perlinValue = Mathf.PerlinNoise(noiseX, noiseY);
                    
                    // 根据噪声值确定地形类型
                    TerrainType terrainType = DetermineTerrainType(perlinValue);
                    terrainData[x, y] = terrainType;
                }
            }
            
            // 应用地形特征（如河流、山脉等）
            ApplyTerrainFeatures(terrainData, chunkCoord);
            
            return terrainData;
        }

        /// <summary>
        /// 根据噪声值确定地形类型
        /// </summary>
        /// <param name="noiseValue">噪声值</param>
        /// <returns>地形类型</returns>
        private TerrainType DetermineTerrainType(float noiseValue)
        {
            // 简单的地形类型确定逻辑
            if (noiseValue < 0.3f)
                return TerrainType.Water;
            else if (noiseValue < 0.4f)
                return TerrainType.Sand;
            else if (noiseValue < 0.7f)
                return TerrainType.Normal;
            else if (noiseValue < 0.8f)
                return TerrainType.Normal;
            else
                return TerrainType.Rock;
        }

        /// <summary>
        /// 应用地形特征
        /// </summary>
        /// <param name="terrainData">地形数据</param>
        /// <param name="chunkCoord">地形块坐标</param>
        private void ApplyTerrainFeatures(TerrainType[,] terrainData, Vector2Int chunkCoord)
        {
            // 这里可以添加更复杂的地形特征生成逻辑
            // 例如：河流、山脉、洞穴等
            
            // 示例：在特定条件下生成河流
            if (chunkCoord.x % 5 == 0)
            {
                int riverY = UnityEngine.Random.Range(0, ConfigChunkSize);
                int riverWidth = UnityEngine.Random.Range(2, 5);
                
                for (int x = 0; x < ConfigChunkSize; x++)
                {
                    for (int y = riverY - riverWidth / 2; y <= riverY + riverWidth / 2; y++)
                    {
                        if (y >= 0 && y < ConfigChunkSize)
                        {
                            terrainData[x, y] = TerrainType.Water;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 设置地形生成参数
        /// </summary>
        /// <param name="noiseScale">噪声缩放</param>
        /// <param name="heightScale">高度缩放</param>
        /// <param name="seed">随机种子</param>
        public void SetTerrainGenerationParameters(float noiseScale, float heightScale, int seed)
        {
            _noiseScale = noiseScale;
            _heightScale = heightScale;
            _seed = seed;
            
            UnityEngine.Random.InitState(_seed);
            
            Debug.Log($"[{_managerName}] 更新地形生成参数: 噪声缩放={_noiseScale}, 高度缩放={_heightScale}, 种子={_seed}");
            
            // 重新生成所有已加载的地形块
            RegenerateAllChunks();
        }

        /// <summary>
        /// 重新生成所有地形块
        /// </summary>
        private void RegenerateAllChunks()
        {
            List<Vector2Int> chunkCoords = new List<Vector2Int>(_terrainChunks.Keys);
            
            foreach (var chunkCoord in chunkCoords)
            {
                // 卸载并重新加载地形块
                UnloadChunk(chunkCoord);
                LoadChunk(chunkCoord);
            }
            
            Debug.Log($"[{_managerName}] 重新生成所有地形块完成，共 {chunkCoords.Count} 个区块");
        }

        /// <summary>
        /// 載入地形分塊
        /// </summary>
        private void LoadChunk(Vector2Int chunkCoord)
        {
            if (_terrainChunks.ContainsKey(chunkCoord))
                return;

            // 生成地形數據
            TerrainType[,] terrainData = GenerateChunkTerrain(chunkCoord);
            _chunkTerrainData[chunkCoord] = terrainData;

            // 創建實際的地形塊物件
            var chunk = CreateTerrainChunk(chunkCoord, terrainData);
            if (chunk != null)
            {
                _terrainChunks[chunkCoord] = chunk;
            }

            Debug.Log($"[{_managerName}] 載入分塊 {chunkCoord}");
        }

        /// <summary>
        /// 卸載地形分塊
        /// </summary>
        private void UnloadChunk(Vector2Int chunkCoord)
        {
            if (_terrainChunks.TryGetValue(chunkCoord, out var chunk))
            {
                chunk?.Cleanup();
                _terrainChunks.Remove(chunkCoord);
            }
            
            if (_chunkTerrainData.ContainsKey(chunkCoord))
            {
                _chunkTerrainData.Remove(chunkCoord);
            }

            Debug.Log($"[{_managerName}] 卸載分塊 {chunkCoord}");
        }
        #endregion
    }
}