using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Data;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建筑建造服务接口
    /// 提供建筑建造、升级、修理等功能
    /// </summary>
    public interface IBuildingConstructionService : ICommandService
    {
        /// <summary>
        /// 开始建造建筑
        /// </summary>
        /// <param name="buildingType">建筑类型</param>
        /// <param name="position">位置</param>
        /// <param name="playerId">玩家ID</param>
        /// <param name="rotation">旋转（可选）</param>
        /// <returns>建造ID，失败返回-1</returns>
        int StartConstruction(BuildingType buildingType, Vector3 position, int playerId, Quaternion? rotation = null);

        /// <summary>
        /// 取消建造
        /// </summary>
        /// <param name="constructionId">建造ID</param>
        /// <returns>是否成功</returns>
        bool CancelConstruction(int constructionId);

        /// <summary>
        /// 完成建造
        /// </summary>
        /// <param name="constructionId">建造ID</param>
        /// <returns>建筑ID，失败返回-1</returns>
        int CompleteConstruction(int constructionId);

        /// <summary>
        /// 升级建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否成功</returns>
        bool UpgradeBuilding(int buildingId);

        /// <summary>
        /// 取消升级
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否成功</returns>
        bool CancelUpgrade(int buildingId);

        /// <summary>
        /// 修理建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="repairAmount">修理量（-1表示完全修理）</param>
        /// <returns>是否成功</returns>
        bool RepairBuilding(int buildingId, float repairAmount = -1f);

        /// <summary>
        /// 销毁建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>是否成功</returns>
        bool DestroyBuilding(int buildingId);

        /// <summary>
        /// 设置建筑状态
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="state">新状态</param>
        /// <returns>是否成功</returns>
        bool SetBuildingState(int buildingId, BuildingState state);

        /// <summary>
        /// 暂停/恢复建筑功能
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="paused">是否暂停</param>
        /// <returns>是否成功</returns>
        bool SetBuildingPaused(int buildingId, bool paused);

        /// <summary>
        /// 获取建造进度
        /// </summary>
        /// <param name="constructionId">建造ID</param>
        /// <returns>进度百分比（0-1）</returns>
        float GetConstructionProgress(int constructionId);

        /// <summary>
        /// 获取升级进度
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>进度百分比（0-1）</returns>
        float GetUpgradeProgress(int buildingId);

        /// <summary>
        /// 加速建造
        /// </summary>
        /// <param name="constructionId">建造ID</param>
        /// <param name="speedMultiplier">速度倍数</param>
        /// <returns>是否成功</returns>
        bool AccelerateConstruction(int constructionId, float speedMultiplier);

        /// <summary>
        /// 加速升级
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="speedMultiplier">速度倍数</param>
        /// <returns>是否成功</returns>
        bool AccelerateUpgrade(int buildingId, float speedMultiplier);
    }
}