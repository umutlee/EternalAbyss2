using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯网络服务接口
    /// 负责菌毯网络的连接性分析和管理
    /// </summary>
    public interface ICreepNetworkService : IService
    {
        /// <summary>
        /// 分析菌毯网络连接性
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        void AnalyzeNetworkConnectivity(int playerId);

        /// <summary>
        /// 获取菌毯网络信息
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>网络信息</returns>
        CreepNetworkInfo GetNetworkInfo(Vector3 position);

        /// <summary>
        /// 获取指定玩家的所有网络
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>网络列表</returns>
        NativeArray<CreepNetworkInfo> GetPlayerNetworks(int playerId);

        /// <summary>
        /// 检查两点间是否有菌毯连接
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="minStrength">最小强度要求</param>
        /// <returns>是否连接</returns>
        bool IsConnected(Vector3 start, Vector3 end, float minStrength = 0.1f);

        /// <summary>
        /// 查找连接路径
        /// </summary>
        /// <param name="start">起始位置</param>
        /// <param name="end">结束位置</param>
        /// <param name="minStrength">最小强度要求</param>
        /// <returns>连接路径</returns>
        Vector3[] FindConnectionPath(Vector3 start, Vector3 end, float minStrength = 0.1f);

        /// <summary>
        /// 合并网络
        /// </summary>
        /// <param name="networkId1">网络1 ID</param>
        /// <param name="networkId2">网络2 ID</param>
        /// <returns>合并后的网络ID</returns>
        int MergeNetworks(int networkId1, int networkId2);

        /// <summary>
        /// 分割网络
        /// </summary>
        /// <param name="networkId">原网络ID</param>
        /// <param name="splitPosition">分割位置</param>
        /// <returns>新网络ID列表</returns>
        int[] SplitNetwork(int networkId, Vector3 splitPosition);

        /// <summary>
        /// 获取网络边界
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>边界点列表</returns>
        NativeArray<Vector3> GetNetworkBoundary(int networkId);

        /// <summary>
        /// 获取网络中心
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>网络中心位置</returns>
        Vector3 GetNetworkCenter(int networkId);

        /// <summary>
        /// 获取网络覆盖面积
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>覆盖面积</returns>
        float GetNetworkArea(int networkId);

        /// <summary>
        /// 检查网络是否孤立
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>是否孤立</returns>
        bool IsNetworkIsolated(int networkId);

        /// <summary>
        /// 获取孤立的网络区域
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>孤立网络列表</returns>
        NativeArray<CreepNetworkInfo> GetIsolatedNetworks(int playerId);

        /// <summary>
        /// 修复网络连接
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <param name="targetNetworkId">目标网络ID</param>
        /// <returns>是否成功修复</returns>
        bool RepairNetworkConnection(int networkId, int targetNetworkId);

        /// <summary>
        /// 优化网络结构
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        void OptimizeNetworkStructure(int playerId);

        /// <summary>
        /// 更新网络逻辑
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void UpdateNetworks(float deltaTime);

        /// <summary>
        /// 获取网络统计信息
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>网络统计</returns>
        CreepNetworkStatistics GetNetworkStatistics(int playerId);

        /// <summary>
        /// 清理无效网络
        /// </summary>
        void CleanupInvalidNetworks();

        /// <summary>
        /// 重建网络索引
        /// </summary>
        void RebuildNetworkIndex();

        /// <summary>
        /// 检查网络完整性
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>完整性报告</returns>
        CreepNetworkIntegrityReport CheckNetworkIntegrity(int networkId);

        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="paused">是否暂停</param>
        void SetPaused(bool paused);
    }

    /// <summary>
    /// 菌毯网络统计信息
    /// </summary>
    public struct CreepNetworkStatistics
    {
        public int TotalNetworks;
        public int ConnectedNetworks;
        public int IsolatedNetworks;
        public float TotalNetworkArea;
        public float AverageNetworkSize;
        public float NetworkConnectivity;
        public int TotalConnectionPoints;
    }

    /// <summary>
    /// 菌毯网络完整性报告
    /// </summary>
    public struct CreepNetworkIntegrityReport
    {
        public int NetworkId;
        public bool IsIntact;
        public int BrokenConnections;
        public int WeakConnections;
        public Vector3[] CriticalPoints;
        public float OverallHealth;
    }
}