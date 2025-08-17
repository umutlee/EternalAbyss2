using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Units.Data;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 单位管理器查询部分 - 委托给 UnitQueryService 处理
    /// </summary>
    public partial class UnitManager
    {
        /// <summary>
        /// 获取单位热数据 - 委托给查询服务
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位热数据</returns>
        public UnitHotData GetUnitHotData(int unitId)
        {
            return _queryService?.GetUnitHotData(unitId) ?? new UnitHotData();
        }

        /// <summary>
        /// 获取单位冷数据 - 委托给查询服务
        /// </summary>
        /// <param name="unitId">单位ID</param>
        /// <returns>单位冷数据</returns>
        public UnitColdData? GetUnitColdData(int unitId)
        {
            return _queryService?.GetUnitColdData(unitId);
        }

    }
}
