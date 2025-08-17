using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Config;

namespace DeepAbyssHive.Terrain.Services
{
    /// <summary>
    /// 地形生成服务接口
    /// 负责地形块的生成、噪声计算和高度生成
    /// </summary>
    public interface ITerrainGenerationService
    {
        #region 地形生成
        /// <summary>
        /// 生成地形块
        /// </summary>
        /// <param name="chunkCoord">块坐标</param>
        /// <returns>生成的地形块</returns>
        ITerrainChunk GenerateChunk(Vector2Int chunkCoord);

        /// <summary>
        /// 生成地形高度
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>地形高度</returns>
        float GenerateHeight(Vector3 worldPosition);

        /// <summary>
        /// 生成地形类型
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="height">高度值</param>
        /// <returns>地形类型</returns>
        TerrainType GenerateTerrainType(Vector3 worldPosition, float height);

        /// <summary>
        /// 生成噪声值
        /// </summary>
        /// <param name="x">X坐标</param>
        /// <param name="y">Y坐标</param>
        /// <param name="scale">缩放</param>
        /// <param name="octaves">倍频</param>
        /// <param name="persistence">持续性</param>
        /// <param name="lacunarity">间隙性</param>
        /// <returns>噪声值</returns>
        float GenerateNoise(float x, float y, float scale, int octaves, float persistence, float lacunarity);
        #endregion

        #region 配置
        /// <summary>
        /// 初始化生成服务
        /// </summary>
        /// <param name="config">地形配置</param>
        void Initialize(TerrainConfigSO config);

        /// <summary>
        /// 设置随机种子
        /// </summary>
        /// <param name="seed">种子值</param>
        void SetSeed(int seed);
        #endregion
    }
}