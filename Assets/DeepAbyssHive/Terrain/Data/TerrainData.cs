using System;
using UnityEngine;

namespace DeepAbyssHive.Terrain.Data
{
    /// <summary>
    /// 地形數據結構
    /// </summary>
    [Serializable]
    public struct TerrainData
    {
        [Header("基本信息")]
        public int TerrainId;
        public string Name;
        public Vector3 Position;
        public Vector3 Size;
        
        [Header("地形屬性")]
        public TerrainType Type;
        public float Height;
        public float Roughness;
        public bool IsPassable;
        
        [Header("資源信息")]
        public ResourceType[] AvailableResources;
        public float[] ResourceDensity;
        
        [Header("環境效果")]
        public EnvironmentEffect[] Effects;
        
        /// <summary>
        /// 預設建構子
        /// </summary>
        public TerrainData(int id, Vector3 position) : this()
        {
            TerrainId = id;
            Position = position;
            Size = Vector3.one;
            Type = TerrainType.Normal;
            Height = 0f;
            Roughness = 0f;
            IsPassable = true;
            AvailableResources = new ResourceType[0];
            ResourceDensity = new float[0];
            Effects = new EnvironmentEffect[0];
        }
        
        /// <summary>
        /// 完整建構子
        /// </summary>
        public TerrainData(int id, Vector3 position, Vector3 size, TerrainType type, bool passable) : this(id, position)
        {
            Size = size;
            Type = type;
            IsPassable = passable;
        }
    }
    
    /// <summary>
    /// 地形類型枚舉
    /// </summary>
    public enum TerrainType
    {
        Normal = 0,
        Mountain = 1,
        Water = 2,
        Desert = 3,
        Forest = 4,
        Swamp = 5
    }
    
    /// <summary>
    /// 資源類型枚舉
    /// </summary>
    public enum ResourceType
    {
        None = 0,
        Metal = 1,
        Energy = 2,
        Biomass = 3,
        Crystal = 4
    }
    
    /// <summary>
    /// 環境效果結構
    /// </summary>
    [Serializable]
    public struct EnvironmentEffect
    {
        public string EffectName;
        public float Intensity;
        public float Duration;
    }
}