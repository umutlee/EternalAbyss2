using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器 - 地形生成部分（委托模式）
    /// 所有方法委托给 TerrainGenerationService
    /// </summary>
    public partial class TerrainManager
    {
        #region 地形生成 - 委托给服务
        /// <summary>
        /// 生成地形块数据
        /// </summary>
        /// <param name="chunkCoord">地形块坐标</param>
        /// <returns>地形类型数组</returns>
        private TerrainType[,] GenerateChunkTerrain(Vector2Int chunkCoord)
        {
            return _generationService?.GenerateChunkTerrain(chunkCoord) ?? new TerrainType[_chunkSize, _chunkSize];
        }

        /// <summary>
        /// 根据噪声值确定地形类型
        /// </summary>
        /// <param name="noiseValue">噪声值</param>
        /// <returns>地形类型</returns>
        private TerrainType DetermineTerrainType(float noiseValue)
        {
            // 委托给生成服务
            Vector2 worldPos = new Vector2(0, 0); // 临时位置
            float height = noiseValue * _heightScale;
            return _generationService?.DetermineTerrainType(height, worldPos) ?? TerrainType.Unknown;
        }

        /// <summary>
        /// 应用地形特征
        /// </summary>
        /// <param name="terrainData">地形数据</param>
        /// <param name="chunkCoord">地形块坐标</param>
        private void ApplyTerrainFeatures(TerrainType[,] terrainData, Vector2Int chunkCoord)
        {
            // 这个方法的逻辑已经整合到 TerrainGenerationService 中
            // 保留空实现以维持兼容性
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
        #endregion
    }
}