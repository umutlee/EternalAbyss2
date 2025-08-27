using System;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Data
{
    public partial struct TerrainModification
    {
        public Vector3 Position;          // 供舊碼使用
        public float   Radius;            // 舊碼 "Range" 對應半徑
        public float   Value;             // 值/強度（舊碼 Strength）
        public TerrainModificationType Type;
        public AnimationCurve Falloff;    // 若本體使用其他表達，可留空
        public DateTime Timestamp;

        // 有些舊碼以 int 表示地形型別
        public int TerrainTypeValue;
        public TerrainType TerrainType => (TerrainType)TerrainTypeValue;
    }
}