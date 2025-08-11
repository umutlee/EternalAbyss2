using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Core.Interfaces;

namespace DeepAbyssHive.Buildings.Interfaces
{
    /// <summary>
    /// 建筑管理器接口
    /// </summary>
    public interface IBuildingManager : IManager
    {
        /// <summary>
        /// 创建建筑
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        /// <returns>建筑ID</returns>
        int CreateBuilding(BuildingData buildingData);
        
        /// <summary>
        /// 获取建筑数据
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>建筑数据</returns>
        BuildingData GetBuildingData(int buildingId);
        
        /// <summary>
        /// 更新建筑数据
        /// </summary>
        /// <param name="buildingData">建筑数据</param>
        void UpdateBuilding(BuildingData buildingData);
        
        /// <summary>
        /// 删除建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        void RemoveBuilding(int buildingId);
        
        /// <summary>
        /// 检查建筑放置是否有效
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="size">大小</param>
        /// <param name="requiresCreep">是否需要菌毯</param>
        /// <returns>是否可以放置</returns>
        bool IsValidPlacement(Vector3 position, Vector2Int size, bool requiresCreep);
        
        /// <summary>
        /// 开始建造建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        void StartConstruction(int buildingId);
        
        /// <summary>
        /// 开始升级建筑
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="upgradePathId">升级路径ID</param>
        void StartUpgrade(int buildingId, string upgradePathId);
        
        /// <summary>
        /// 添加生产队列项
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="productionItem">生产队列项</param>
        void AddProductionQueueItem(int buildingId, ProductionQueueItem productionItem);
        
        /// <summary>
        /// 取消生产队列项
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="queueIndex">队列索引</param>
        void CancelProductionQueueItem(int buildingId, int queueIndex);
        
        /// <summary>
        /// 开始研究
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <param name="researchId">研究ID</param>
        void StartResearch(int buildingId, string researchId);
        
        /// <summary>
        /// 取消研究
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        void CancelResearch(int buildingId);
        
        /// <summary>
        /// 获取建筑周围的菌毯扩张范围
        /// </summary>
        /// <param name="buildingId">建筑ID</param>
        /// <returns>菌毯扩张范围</returns>
        float GetCreepExpansionRadius(int buildingId);
    }
}