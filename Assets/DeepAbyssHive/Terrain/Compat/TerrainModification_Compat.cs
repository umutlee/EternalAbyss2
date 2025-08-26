using UnityEngine;

namespace DeepAbyssHive.Terrain.Data
{
    public partial struct TerrainModification
    {
        // 舊名 Position/Value/Type/Timestamp/TerrainTypeValue/Falloff 等 → 優先直通現欄位；沒有就後備
        public Vector3 Position { get => _compatPosition; set => _compatPosition = value; }
        public float Radius    { get => radius; set => radius = value; }  // 直通現有欄位
        public float Value     { get => _compatValue; set => _compatValue = value; }
        public int   Type      { get => (int)modificationType; set => modificationType = (TerrainModificationType)value; } // 直通現有欄位
        public float Timestamp { get => _compatTimestamp; set => _compatTimestamp = value; }

        // 舊名 TerrainTypeValue → 先兼容為 int
        public int TerrainTypeValue { get => (int)newTerrainType; set => newTerrainType = (DeepAbyssHive.Terrain.Enums.TerrainType)value; }

        public float Falloff { get => _compatFalloff; set => _compatFalloff = value; }

        private Vector3 _compatPosition;
        private float _compatValue;
        private float _compatTimestamp;
        private float _compatFalloff;
    }
}