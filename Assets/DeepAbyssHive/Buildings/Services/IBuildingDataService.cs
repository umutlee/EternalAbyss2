using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Services
{
    /// <summary>
    /// 建築數據服務介面
    /// </summary>
    public interface IBuildingDataService
    {
        /// <summary>
        /// 獲取建築數據
        /// </summary>
        BuildingData GetBuildingData(int buildingId);
        
        /// <summary>
        /// 檢查建築是否存在
        /// </summary>
        bool BuildingExists(int buildingId);
        
        /// <summary>
        /// 獲取建築類型
        /// </summary>
        BuildingType GetBuildingType(int buildingId);
    }
}