using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Buildings.Data;
using DeepAbyssHive.Buildings.Enums;
using DeepAbyssHive.Buildings.Interfaces;
using DeepAbyssHive.Buildings.Services;

namespace DeepAbyssHive.Buildings.Services
{
    public partial class BuildingQueryService : IBuildingQueryService
    {
        // ✅ 助手放在 class 內
        private static int GetBuildingPlayerId(BuildingData b) => b.OwnerId;

        /// <summary>獲取建築物的玩家ID（支援 in 參數）</summary>
        public static int GetBuildingPlayerId(in BuildingData buildingData) => buildingData.OwnerId;

        // 修正為 BuildingType enum 並回傳 List<int>
        public List<BuildingData> GetBuildingsOfType(BuildingType buildingType, int playerId = -1)
        {
            // TODO: 依 playerId 與 buildingType 回傳建築資料清單
            return new List<BuildingData>();
        }

        public bool CanPlaceBuilding(BuildingType buildingType, UnityEngine.Vector3 position, int playerId)
        {
            // TODO: 放置規則檢核
            return false;
        }

        public PlacementValidationResult ValidateBuildingPlacement(BuildingType buildingType, UnityEngine.Vector3 position, int playerId)
        {
            // TODO: 回傳實際驗證資訊
            return default;
        }

        public int GetNearestBuilding(UnityEngine.Vector3 position, BuildingType buildingType, int playerId, float maxDistance = float.MaxValue)
        {
            // TODO: 最近建築 ID
            return -1;
        }

        public BuildingTemplate GetBuildingTemplate(BuildingType buildingType)
        {
            // TODO: 由 enum 取樣板
            return default;
        }

        // ===== MDC INSERT: Interface stubs for compile =====
        // ---- IService ----
        public string ServiceName => "BuildingQueryService";
        public bool IsInitialized { get; private set; }
        public void Initialize()  { IsInitialized = true; }
        public void Cleanup()     { IsInitialized = false; }

        // ---- IQueryService ----
        public bool IsQueryAvailable => IsInitialized;

        // ---- IBuildingQueryService (剩餘) ----
        public List<BuildingData> GetPlayerBuildings(int playerId)
            => new List<BuildingData>();

        public bool BuildingExists(int buildingId) => false;

        public BuildingData? GetBuildingData(int buildingId)
            => default;

        public Dictionary<BuildingType, int> GetBuildingCounts(int playerId)
            => new Dictionary<BuildingType, int>();

        public List<BuildingData> GetBuildingsInRange(Vector3 center, float radius, int playerId = -1)
            => new List<BuildingData>();

        public List<ProductionQueueItem> GetProductionQueue(int buildingId)
            => new List<ProductionQueueItem>();

        public BuildingState GetBuildingState(int buildingId)
            => default;

        public float GetBuildingInfluenceRadius(int buildingId) => 0f;
        // ===== /MDC INSERT =====
    }
}
