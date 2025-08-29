using System;
using UnityEngine;

namespace DeepAbyssHive.Terrain.Data
{
    using DeepAbyssHive.Terrain.Enums;

    /// <summary>
    /// Canonical data struct for terrain edits. This replaces the redacted/ellipsis version.
    /// </summary>
    public struct TerrainModification
    {
        // Core
        public Vector3 Position;
        public float   Radius;   // area of effect
        public float   Value;    // strength/intensity
        public TerrainModificationType Type;

        // Timing
        public DateTime Timestamp;

    // Terrain type bridge (enum <-> int)
    public int TerrainTypeValue; // raw int for legacy sites that expect int
    public TerrainType TerrainType
    {
        get => (TerrainType)TerrainTypeValue;
        set => TerrainTypeValue = (int)value;
    }

    // Compatibility properties for legacy code
    public AnimationCurve Falloff;
    public bool changeTerrainType;
    public int newTerrainType;

        // Compatibility properties (from partial file)
        
        /// <summary>
        /// 目標高度（用於平整操作）
        /// </summary>
        public float TargetHeight;
        
        /// <summary>
        /// 紋理索引（用於繪製操作）
        /// </summary>
        public int TextureIndex;
        
        /// <summary>
        /// 是否使用衰減
        /// </summary>
        public bool UseFalloff;
        
        /// <summary>
        /// 衰減曲線
        /// </summary>
        public AnimationCurve FalloffCurve;

        /// <summary>
        /// 建構子，提供預設值
        /// </summary>
        public TerrainModification(Vector3 position, float radius, float value, TerrainModificationType type) : this()
        {
            Position = position;
            Radius = radius;
            Value = value;
            Type = type;
            Timestamp = DateTime.Now;
            TerrainTypeValue = 0;
            TargetHeight = 0.0f;
            TextureIndex = 0;
            UseFalloff = true;
            FalloffCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
        }
    }

    /// <summary>
    /// Expanded ops to match legacy call sites.
    /// </summary>
    public enum TerrainModificationType
    {
        // Original minimal set (keep for compatibility)
        None         = -1,
        TypeChange   = 0,
        HeightChange = 1,
        Combined     = 2,

        // Extended set used by services
        Flatten = 10,
        Dig     = 11,
        Fill    = 12,
        Ramp    = 13,
        Tunnel  = 14
    }
}