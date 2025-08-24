using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using DeepAbyssHive.Units.Data;
using DeepAbyssHive.Units.Enums;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Units.Services
{
    // Auto-generated compat stubs for UnitQueryService (explicit interface implementations).
    // Purpose: restore compilation with minimal, no-op behavior.
    public partial class UnitQueryService
    {
         // === IQueryService (顯式) ===
        bool IQueryService.IsQueryAvailable => this.IsInitialized;   
        
        // IUnitQueryService explicit interface implementations
        NativeArray<UnitData> IUnitQueryService.GetUnitsInRange(Vector3 center, float radius, int playerId) => default;
        NativeArray<UnitData> IUnitQueryService.GetUnitsOfType(UnitType unitType, int playerId) => default;
        NativeArray<UnitData> IUnitQueryService.GetPlayerUnits(int playerId) => default;
        UnitData? IUnitQueryService.GetUnitData(int unitId) => default;
        bool IUnitQueryService.UnitExists(int unitId) => default;
        Dictionary<UnitType, int> IUnitQueryService.GetUnitCounts(int playerId) => default;
        int IUnitQueryService.GetNearestEnemyUnit(Vector3 position, int playerId, float maxDistance) => default;
        int IUnitQueryService.GetNearestFriendlyUnit(Vector3 position, int playerId, float maxDistance) => default;
        bool IUnitQueryService.IsPositionOccupied(Vector3 position, float radius) => default;
        List<Vector3> IUnitQueryService.GetUnitPath(int unitId) => default;
        UnitState IUnitQueryService.GetUnitState(int unitId) => default;
    }
}
