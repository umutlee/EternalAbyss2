using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Units.Services
{
    /// <summary>
    /// 單位查詢服務 - 提供單位數據查詢功能
    /// </summary>
    public partial class UnitQueryService : IUnitQueryService, IService
    {
        private readonly IUnitDataService _unitDataService;
        private readonly IUnitStateService _unitStateService;

        public UnitQueryService(IUnitDataService unitDataService, IUnitStateService unitStateService)
        {
            _unitDataService = unitDataService ?? throw new ArgumentNullException(nameof(unitDataService));
            _unitStateService = unitStateService ?? throw new ArgumentNullException(nameof(unitStateService));
        }

        public UnitQueryService()
            : this(DeepAbyssHive.Core.Services.ServiceLocator.Get<IUnitDataService>(),
                   DeepAbyssHive.Core.Services.ServiceLocator.Get<IUnitStateService>()) { }

        #region IUnitQueryService 實作
        public NativeArray<UnitData> GetUnitsInRange(Vector3 center, float radius, int playerId = -1)
        {
            // TODO: 實作獲取指定範圍內的單位
            return new NativeArray<UnitData>(0, Allocator.Temp);
        }

        public NativeArray<UnitData> GetUnitsOfType(UnitType unitType, int playerId = -1)
        {
            // TODO: 實作獲取指定類型的單位
            return new NativeArray<UnitData>(0, Allocator.Temp);
        }

        public NativeArray<UnitData> GetPlayerUnits(int playerId)
        {
            // TODO: 實作獲取玩家的所有單位
            return new NativeArray<UnitData>(0, Allocator.Temp);
        }

        public UnitData? GetUnitData(int unitId)
        {
            // TODO: 實作獲取單位數據
            return null;
        }

        public bool UnitExists(int unitId)
        {
            // TODO: 實作檢查單位是否存在
            return false;
        }

        public Dictionary<UnitType, int> GetUnitCounts(int playerId)
        {
            // TODO: 實作獲取單位數量統計
            return new Dictionary<UnitType, int>();
        }

        public int GetNearestEnemyUnit(Vector3 position, int playerId, float maxDistance = float.MaxValue)
        {
            // TODO: 實作獲取最近的敵方單位
            return -1;
        }

        public int GetNearestFriendlyUnit(Vector3 position, int playerId, float maxDistance = float.MaxValue)
        {
            // TODO: 實作獲取最近的友方單位
            return -1;
        }

        public bool IsPositionOccupied(Vector3 position, float radius = 1f)
        {
            // TODO: 實作檢查位置是否被單位占用
            return false;
        }

        public List<Vector3> GetUnitPath(int unitId)
        {
            // TODO: 實作獲取單位的移動路徑
            return new List<Vector3>();
        }

        public UnitState GetUnitState(int unitId)
        {
            // TODO: 實作獲取單位的當前狀態
            return UnitState.Idle;
        }

        // IQueryService 實作
        public T Query<T>(string queryId, object[] parameters = null)
        {
            // TODO: 實作通用查詢
            return default(T);
        }

        public bool CanQuery(string queryId)
        {
            // TODO: 實作查詢可用性檢查
            return false;
        }
        #endregion
    }


}