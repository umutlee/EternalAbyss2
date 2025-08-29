using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using UnityEngine;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建築狀態服務介面
    /// </summary>
    public interface IBuildingStateService
    {
        /// <summary>
        /// 獲取建築狀態
        /// </summary>
        BuildingState GetBuildingState(int buildingId);
        
        /// <summary>
        /// 設置建築狀態
        /// </summary>
        bool SetBuildingState(int buildingId, BuildingState state);
        
        /// <summary>
        /// 更新建築位置
        /// </summary>
        bool UpdateBuildingPosition(int buildingId, Vector3 position);
        
        /// <summary>
        /// 獲取建築健康度
        /// </summary>
        float GetBuildingHealth(int buildingId);
    }
}