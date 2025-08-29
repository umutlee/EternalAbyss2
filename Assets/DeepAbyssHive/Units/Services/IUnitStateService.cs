using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using UnityEngine;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 單位狀態服務介面
    /// </summary>
    public interface IUnitStateService
    {
        /// <summary>
        /// 獲取單位熱數據
        /// </summary>
        UnitHotData GetUnitHotData(int unitId);
        
        /// <summary>
        /// 更新單位位置
        /// </summary>
        bool UpdateUnitPosition(int unitId, Vector3 position);
        
        /// <summary>
        /// 獲取單位狀態
        /// </summary>
        UnitState GetUnitState(int unitId);
        
        /// <summary>
        /// 設置單位狀態
        /// </summary>
        bool SetUnitState(int unitId, UnitState state);
    }
}