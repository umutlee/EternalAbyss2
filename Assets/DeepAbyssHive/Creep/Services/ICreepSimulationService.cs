using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Enums;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯模拟服务接口
    /// 提供菌毯生长、衰减、修改等功能
    /// </summary>
    public interface ICreepSimulationService : ICommandService
    {
        /// <summary>
        /// 添加菌毯源点
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="strength">强度</param>
        /// <param name="radius">影响半径</param>
        /// <returns>源点ID，失败返回-1</returns>
        int AddCreepSource(Vector3 position, int playerId, float strength = 1f, float radius = 10f);

        /// <summary>
        /// 移除菌毯源点
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <returns>是否成功</returns>
        bool RemoveCreepSource(int sourceId);

        /// <summary>
        /// 修改菌毯源点属性
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <param name="strength">新强度</param>
        /// <param name="radius">新半径</param>
        /// <returns>是否成功</returns>
        bool ModifyCreepSource(int sourceId, float? strength = null, float? radius = null);

        /// <summary>
        /// 强制菌毯生长
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="strength">强度</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功</returns>
        bool ForceCreepGrowth(Vector3 position, float radius, float strength, int playerId);

        /// <summary>
        /// 强制菌毯衰减
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="decayRate">衰减速度</param>
        /// <returns>是否成功</returns>
        bool ForceCreepDecay(Vector3 position, float radius, float decayRate = 1f);

        /// <summary>
        /// 清除指定区域的菌毯
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <returns>是否成功</returns>
        bool ClearCreep(Vector3 position, float radius);

        /// <summary>
        /// 设置菌毯强度
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="strength">强度</param>
        /// <param name="playerId">玩家ID</param>
        /// <returns>是否成功</returns>
        bool SetCreepStrength(Vector3 position, float radius, float strength, int playerId);

        /// <summary>
        /// 连接菌毯网络
        /// </summary>
        /// <param name="position1">位置1</param>
        /// <param name="position2">位置2</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="strength">连接强度</param>
        /// <returns>是否成功</returns>
        bool ConnectCreepNetworks(Vector3 position1, Vector3 position2, int playerId, float strength = 0.5f);

        /// <summary>
        /// 分割菌毯网络
        /// </summary>
        /// <param name="position">分割位置</param>
        /// <param name="radius">分割半径</param>
        /// <returns>是否成功</returns>
        bool SplitCreepNetwork(Vector3 position, float radius);

        /// <summary>
        /// 合并菌毯网络
        /// </summary>
        /// <param name="networkId1">网络1ID</param>
        /// <param name="networkId2">网络2ID</param>
        /// <returns>是否成功</returns>
        bool MergeCreepNetworks(int networkId1, int networkId2);

        /// <summary>
        /// 设置菌毯生长速度
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="speedMultiplier">速度倍数</param>
        void SetCreepGrowthSpeed(int playerId, float speedMultiplier);

        /// <summary>
        /// 设置菌毯衰减速度
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <param name="decayMultiplier">衰减倍数</param>
        void SetCreepDecaySpeed(int playerId, float decayMultiplier);

        /// <summary>
        /// 暂停/恢复菌毯模拟
        /// </summary>
        /// <param name="paused">是否暂停</param>
        void SetSimulationPaused(bool paused);

        /// <summary>
        /// 重置菌毯模拟
        /// </summary>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        void ResetCreepSimulation(int playerId = -1);

        /// <summary>
        /// 保存菌毯状态
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool SaveCreepState(string filePath);

        /// <summary>
        /// 加载菌毯状态
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>是否成功</returns>
        bool LoadCreepState(string filePath);

        /// <summary>
        /// 优化菌毯网络
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        void OptimizeCreepNetworks(int playerId);

        /// <summary>
        /// 设置暂停状态
        /// </summary>
        /// <param name="paused">是否暂停</param>
        void SetPaused(bool paused);
    }
}