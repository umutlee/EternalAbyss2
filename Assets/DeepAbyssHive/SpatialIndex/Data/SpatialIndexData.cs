using UnityEngine;
using DeepAbyssHive.SpatialIndex.Enums;

namespace DeepAbyssHive.SpatialIndex.Data
{
    /// <summary>
    /// 空间索引数据结构
    /// </summary>
    [System.Serializable]
    public struct SpatialObjectInfo
    {
        public int ObjectId;
        public Vector3 Position;
        public Bounds Bounds;
        public SpatialObjectType ObjectType;
        public float LastUpdateTime;
    }

    /// <summary>
    /// 空间对象更新信息
    /// </summary>
    [System.Serializable]
    public struct SpatialObjectUpdate
    {
        public int ObjectId;
        public Vector3 NewPosition;
        public Bounds? NewBounds;
    }

    /// <summary>
    /// 射线碰撞结果
    /// </summary>
    [System.Serializable]
    public struct RaycastHit
    {
        public int ObjectId;
        public Vector3 Point;
        public float Distance;
        public Vector3 Normal;
    }

    /// <summary>
    /// 空间索引性能统计
    /// </summary>
    [System.Serializable]
    public struct SpatialIndexPerformanceStats
    {
        public int TotalQueries;
        public float AverageQueryTime;
        public int FrameQueries;
        public int ObjectCount;
        public int PendingOperations;
        public int TotalNodes;
        public int MaxDepth;
        public float AverageUpdateTime;
        public int QueriesPerSecond;
        public int UpdatesPerSecond;
        public float MemoryUsage;
        public float OptimizationRatio;
    }
}