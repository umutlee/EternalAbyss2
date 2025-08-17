using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯扩张服务接口
    /// 负责菌毯的自动和手动扩张逻辑
    /// </summary>
    public interface ICreepExpansionService : IService
    {
        /// <summary>
        /// 扩张速度
        /// </summary>
        float ExpansionRate { get; set; }

        /// <summary>
        /// 扩张阈值
        /// </summary>
        float ExpansionThreshold { get; set; }

        /// <summary>
        /// 是否启用自动扩张
        /// </summary>
        bool AutoExpansionEnabled { get; set; }

        /// <summary>
        /// 开始自动扩张
        /// </summary>
        /// <param name="sourcePosition">源点位置</param>
        /// <param name="playerId">玩家ID</param>
        void StartAutoExpansion(Vector3 sourcePosition, int playerId);

        /// <summary>
        /// 停止自动扩张
        /// </summary>
        /// <param name="sourcePosition">源点位置</param>
        void StopAutoExpansion(Vector3 sourcePosition);

        /// <summary>
        /// 手动扩张到指定位置
        /// </summary>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="expansionType">扩张类型</param>
        /// <returns>是否成功开始扩张</returns>
        bool ExpandToPosition(Vector3 targetPosition, int playerId, CreepExpansionType expansionType = CreepExpansionType.Normal);

        /// <summary>
        /// 手动扩张到指定区域
        /// </summary>
        /// <param name="center">区域中心</param>
        /// <param name="radius">扩张半径</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="expansionType">扩张类型</param>
        /// <returns>是否成功开始扩张</returns>
        bool ExpandToArea(Vector3 center, float radius, int playerId, CreepExpansionType expansionType = CreepExpansionType.Normal);

        /// <summary>
        /// 计算扩张路径
        /// </summary>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>扩张路径</returns>
        Vector3[] CalculateExpansionPath(Vector3 from, Vector3 to, int playerId);

        /// <summary>
        /// 检查位置是否可以扩张
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否可以扩张</returns>
        bool CanExpandToPosition(Vector3 position, int playerId);

        /// <summary>
        /// 获取扩张成本
        /// </summary>
        /// <param name="from">起始位置</param>
        /// <param name="to">目标位置</param>
        /// <param name="expansionType">扩张类型</param>
        /// <returns>扩张成本</returns>
        float GetExpansionCost(Vector3 from, Vector3 to, CreepExpansionType expansionType);

        /// <summary>
        /// 获取扩张前沿
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>扩张前沿位置</returns>
        NativeArray<Vector3> GetExpansionFront(int playerId);

        /// <summary>
        /// 更新扩张逻辑
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void UpdateExpansion(float deltaTime);

        /// <summary>
        /// 添加扩张队列
        /// </summary>
        /// <param name="request">扩张请求</param>
        void AddExpansionRequest(CreepExpansionRequest request);

        /// <summary>
        /// 移除扩张队列
        /// </summary>
        /// <param name="requestId">请求ID</param>
        void RemoveExpansionRequest(int requestId);

        /// <summary>
        /// 获取活跃的扩张请求
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>扩张请求列表</returns>
        NativeArray<CreepExpansionRequest> GetActiveExpansionRequests(int playerId);

        /// <summary>
        /// 暂停所有扩张
        /// </summary>
        void PauseAllExpansion();

        /// <summary>
        /// 恢复所有扩张
        /// </summary>
        void ResumeAllExpansion();

        /// <summary>
        /// 获取扩张统计信息
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>扩张统计</returns>
        CreepExpansionStatistics GetExpansionStatistics(int playerId);

        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="paused">是否暂停</param>
        void SetPaused(bool paused);
    }

    /// <summary>
    /// 菌毯扩张请求
    /// </summary>
    public struct CreepExpansionRequest
    {
        public int RequestId;
        public int PlayerId;
        public Vector3 SourcePosition;
        public Vector3 TargetPosition;
        public CreepExpansionType ExpansionType;
        public float Priority;
        public float StartTime;
        public float EstimatedDuration;
        public bool IsActive;
    }

    /// <summary>
    /// 菌毯扩张统计信息
    /// </summary>
    public struct CreepExpansionStatistics
    {
        public int ActiveRequests;
        public int CompletedRequests;
        public float TotalExpansionArea;
        public float AverageExpansionRate;
        public float TotalExpansionCost;
    }
}