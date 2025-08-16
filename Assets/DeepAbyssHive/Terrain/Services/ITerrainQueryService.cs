using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;

namespace DeepAbyssHive.Terrain.Services
{
    /// <summary>
    /// 地形查询服务接口
    /// 提供所有地形相关的只读查询功能
    /// </summary>
    public interface ITerrainQueryService : IQueryService
    {
        /// <summary>
        /// 获取指定位置的地形块
        /// </summary>
        /// <param name="position">世界坐标位置</param>
        /// <returns>地形块数据</returns>
        TerrainChunk GetChunkAt(Vector3 position);

        /// <summary>
        /// 获取指定坐标的地形块
        /// </summary>
        /// <param name="chunkX">块X坐标</param>
        /// <param name="chunkZ">块Z坐标</param>
        /// <returns>地形块数据</returns>
        TerrainChunk GetChunk(int chunkX, int chunkZ);

        /// <summary>
        /// 检查位置是否可通行
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否可通行</returns>
        bool IsPassable(Vector3 position);

        /// <summary>
        /// 获取位置的地形类型
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>地形类型</returns>
        TerrainType GetTerrainType(Vector3 position);

        /// <summary>
        /// 获取位置的高度
        /// </summary>
        /// <param name="position">位置（忽略Y坐标）</param>
        /// <returns>地形高度</returns>
        float GetHeight(Vector3 position);

        /// <summary>
        /// 获取位置的移动速度修正
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>速度修正系数（1.0为正常速度）</returns>
        float GetMovementSpeedModifier(Vector3 position);

        /// <summary>
        /// 获取指定范围内的地形块
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>地形块数组</returns>
        NativeArray<TerrainChunk> GetChunksInRange(Vector3 center, float radius);

        /// <summary>
        /// 检查区域是否平坦
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="size">区域大小</param>
        /// <param name="maxHeightDifference">最大高度差</param>
        /// <returns>是否平坦</returns>
        bool IsAreaFlat(Vector3 center, Vector2 size, float maxHeightDifference = 1f);

        /// <summary>
        /// 获取最近的指定地形类型位置
        /// </summary>
        /// <param name="position">起始位置</param>
        /// <param name="terrainType">地形类型</param>
        /// <param name="maxDistance">最大搜索距离</param>
        /// <returns>最近的位置，如果没找到返回Vector3.zero</returns>
        Vector3 GetNearestTerrainOfType(Vector3 position, TerrainType terrainType, float maxDistance = 100f);

        /// <summary>
        /// 检查位置是否在地形边界内
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否在边界内</returns>
        bool IsWithinBounds(Vector3 position);

        /// <summary>
        /// 获取地形边界
        /// </summary>
        /// <returns>地形边界</returns>
        Bounds GetTerrainBounds();

        /// <summary>
        /// 获取位置的法向量
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>法向量</returns>
        Vector3 GetNormal(Vector3 position);

        /// <summary>
        /// 获取位置的坡度
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>坡度（度数）</returns>
        float GetSlope(Vector3 position);

        /// <summary>
        /// 检查路径是否可通行
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="unitRadius">单位半径</param>
        /// <returns>是否可通行</returns>
        bool IsPathClear(Vector3 start, Vector3 end, float unitRadius = 0.5f);
    }
}