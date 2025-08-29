using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 單位數據服務介面
    /// </summary>
    public interface IUnitDataService
    {
        /// <summary>
        /// 獲取單位數據
        /// </summary>
        UnitColdData GetUnitData(int unitId);
        
        /// <summary>
        /// 檢查單位是否存在
        /// </summary>
        bool UnitExists(int unitId);
        
        /// <summary>
        /// 獲取單位類型
        /// </summary>
        UnitType GetUnitType(int unitId);
    }
}