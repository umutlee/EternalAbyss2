using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
// 使用别名解决枚举冲突
using DataNS = DeepAbyssHive.Creep.Data;
// 强制使用Data版本的枚举（Enums版本已标记为Legacy）
using CreepSourceType = DeepAbyssHive.Creep.Data.CreepSourceType;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯源点服务接口
    /// 负责菌毯源点的创建、管理和维护
    /// </summary>
    public interface ICreepSourceService : IService
    {
        /// <summary>
        /// 创建菌毯源点
        /// </summary>
        /// <param name="position">源点位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="sourceType">源点类型</param>
        /// <param name="strength">初始强度</param>
        /// <returns>源点ID</returns>
        int CreateCreepSource(Vector3 position, int playerId, DataNS.CreepSourceType sourceType, float strength = 1.0f);

        /// <summary>
        /// 移除菌毯源点
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <returns>是否成功移除</returns>
        bool RemoveCreepSource(int sourceId);

        /// <summary>
        /// 获取菌毯源点
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <returns>源点数据</returns>
        CreepSource GetCreepSource(int sourceId);

        /// <summary>
        /// 获取指定玩家的所有源点
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>源点列表</returns>
        NativeArray<CreepSource> GetPlayerCreepSources(int playerId);

        /// <summary>
        /// 获取指定范围内的源点
        /// </summary>
        /// <param name="center">中心位置</param>
        /// <param name="radius">搜索半径</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <returns>源点列表</returns>
        NativeArray<CreepSource> GetCreepSourcesInRange(Vector3 center, float radius, int playerId = -1);

        /// <summary>
        /// 获取最近的菌毯源点
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID（-1表示所有玩家）</param>
        /// <param name="maxDistance">最大搜索距离</param>
        /// <returns>最近的源点</returns>
        CreepSource GetNearestCreepSource(Vector3 position, int playerId = -1, float maxDistance = float.MaxValue);

        /// <summary>
        /// 更新源点强度
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <param name="strength">新强度</param>
        void UpdateSourceStrength(int sourceId, float strength);

        /// <summary>
        /// 更新源点类型
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <param name="sourceType">新类型</param>
        void UpdateSourceType(int sourceId, DataNS.CreepSourceType sourceType);

        /// <summary>
        /// 激活源点
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        void ActivateSource(int sourceId);

        /// <summary>
        /// 停用源点
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        void DeactivateSource(int sourceId);

        /// <summary>
        /// 检查源点是否活跃
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <returns>是否活跃</returns>
        bool IsSourceActive(int sourceId);

        /// <summary>
        /// 合并源点
        /// </summary>
        /// <param name="sourceId1">源点1 ID</param>
        /// <param name="sourceId2">源点2 ID</param>
        /// <returns>合并后的源点ID</returns>
        int MergeCreepSources(int sourceId1, int sourceId2);

        /// <summary>
        /// 分割源点
        /// </summary>
        /// <param name="sourceId">原源点ID</param>
        /// <param name="newPosition">新源点位置</param>
        /// <param name="strengthRatio">强度分配比例（0-1）</param>
        /// <returns>新源点ID</returns>
        int SplitCreepSource(int sourceId, Vector3 newPosition, float strengthRatio = 0.5f);

        /// <summary>
        /// 更新源点逻辑
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        void UpdateSources(float deltaTime);

        /// <summary>
        /// 获取源点影响范围
        /// </summary>
        /// <param name="sourceId">源点ID</param>
        /// <returns>影响半径</returns>
        float GetSourceInfluenceRadius(int sourceId);

        /// <summary>
        /// 检查位置是否在源点影响范围内
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="sourceId">源点ID</param>
        /// <returns>是否在影响范围内</returns>
        bool IsPositionInSourceInfluence(Vector3 position, int sourceId);

        /// <summary>
        /// 获取源点统计信息
        /// </summary>
        /// <param name="playerId">玩家ID</param>
        /// <returns>源点统计</returns>
        CreepSourceStatistics GetSourceStatistics(int playerId);

        /// <summary>
        /// 清理无效源点
        /// </summary>
        void CleanupInvalidSources();
    }

    /// <summary>
    /// 菌毯源点统计信息
    /// </summary>
    public struct CreepSourceStatistics
    {
        public int TotalSources;
        public int ActiveSources;
        public int InactiveSources;
        public float TotalStrength;
        public float AverageStrength;
        public float TotalInfluenceArea;
        public int SourcesByType_Basic;
        public int SourcesByType_Enhanced;
        public int SourcesByType_Specialized;
    }
}