using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Enums;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯查询服务接口
    /// 提供所有菌毯相关的只读查询功能
    /// </summary>
    public interface ICreepQueryService : IQueryService
    {
        /// <summary>
        /// 检查位置是否被菌毯覆盖
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否被覆盖</returns>
        bool IsPositionCovered(Vector3 position);

        /// <summary>
        /// 获取位置的菌毯强度
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>菌毯强度（0-1）</returns>
        float GetCreepStrength(Vector3 position);

        /// <summary>
        /// 获取位置的菌毯密度
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>菌毯密度（0-1）</returns>
        float GetCreepDensity(Vector3 position);

        /// <summary>
        /// 获取指定范围内的菌毯覆盖率
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>覆盖率（0-1）</returns>
        float GetCreepCoverageInRange(Vector3 center, float radius);

        /// <summary>
        /// 获取菌毯网格数据
        /// </summary>
        /// <param name="gridX">网格X坐标</param>
        /// <param name="gridZ">网格Z坐标</param>
        /// <returns>网格数据</returns>
        CreepGridCell GetCreepGrid(int gridX, int gridZ);

        /// <summary>
        /// 获取指定区域的菌毯网格
        /// </summary>
        /// <param name="minX">最小X坐标</param>
        /// <param name="minZ">最小Z坐标</param>
        /// <param name="maxX">最大X坐标</param>
        /// <param name="maxZ">最大Z坐标</param>
        /// <returns>网格数据数组</returns>
        NativeArray<CreepGridCell> GetCreepGridRange(int minX, int minZ, int maxX, int maxZ);

        /// <summary>
        /// 获取最近的菌毯边缘
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="maxDistance">最大搜索距离</param>
        /// <returns>最近的边缘位置</returns>
        Vector3 GetNearestCreepEdge(Vector3 position, float maxDistance = 50f);

        /// <summary>
        /// 检查两点间是否有菌毯连接
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="minStrength">最小强度要求</param>
        /// <returns>是否连接</returns>
        bool IsCreepConnected(Vector3 start, Vector3 end, float minStrength = 0.1f);

        /// <summary>
        /// 获取菌毯网络信息
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>网络信息</returns>
        CreepNetworkInfo GetCreepNetwork(Vector3 position);

        /// <summary>
        /// 获取所有菌毯源点
        /// </summary>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>源点列表</returns>
        NativeArray<CreepSource> GetCreepSources(int playerId = -1);

        /// <summary>
        /// 获取菌毯扩张前沿
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>扩张前沿位置列表</returns>
        NativeArray<Vector3> GetCreepExpansionFront(int playerId);

        /// <summary>
        /// 计算菌毯扩张路径
        /// </summary>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>扩张路径</returns>
        Vector3[] CalculateCreepExpansionPath(Vector3 from, Vector3 to, int playerId);

        /// <summary>
        /// 获取菌毯统计信息
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>统计信息</returns>
        CreepStatistics GetCreepStatistics(int playerId);

        /// <summary>
        /// 检查位置是否适合菌毯生长
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否适合</returns>
        bool IsSuitableForCreepGrowth(Vector3 position);

        /// <summary>
        /// 获取菌毯生长速度
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>生长速度倍数</returns>
        float GetCreepGrowthRate(Vector3 position);
    }

    /// <summary>
    /// 菌毯网络信息
    /// </summary>
    public struct CreepNetworkInfo
    {
        public int NetworkId;
        public int PlayerId;
        public Vector3 CenterPosition;
        public float TotalArea;
        public int SourceCount;
        public bool IsConnectedToMain;
    }

    /// <summary>
    /// 菌毯统计信息
    /// </summary>
    public struct CreepStatistics
    {
        public float TotalArea;
        public float AverageStrength;
        public float AverageDensity;
        public int NetworkCount;
        public int SourceCount;
        public float GrowthRate;
        public float DecayRate;
    }
}