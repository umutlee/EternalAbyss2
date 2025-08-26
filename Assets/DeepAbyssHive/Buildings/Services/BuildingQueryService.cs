        private static int GetBuildingPlayerId(BuildingData b) => b.OwnerId;
        
        /// <summary>
        /// 獲取建築物的玩家ID（支援 in 參數）
        /// </summary>
        /// <param name="buildingData">建築物資料</param>
        /// <returns>玩家ID</returns>
        public static int GetBuildingPlayerId(in BuildingData buildingData) => buildingData.OwnerId;
        public List<BuildingData> GetBuildingsOfType(Type buildingType, int playerId = -1)
        public bool CanPlaceBuilding(Type buildingType, Vector3 position, int playerId)
        public PlacementValidationResult ValidateBuildingPlacement(Type buildingType, Vector3 position, int playerId)
        public int GetNearestBuilding(Vector3 position, Type buildingType, int playerId, float maxDistance = float.MaxValue)
        public BuildingTemplate GetBuildingTemplate(Type buildingType)
