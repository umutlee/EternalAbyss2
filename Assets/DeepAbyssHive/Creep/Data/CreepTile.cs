using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Creep.Enums;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯瓦片状态
    /// </summary>
    public enum CreepTileStatus
    {
        Healthy,
        Growing,
        Starving,
        Dying,
        Dead
    }

    /// <summary>
    /// 菌毯瓦片类型
    /// </summary>
    public enum CreepTileType
    {
        Basic,
        Enhanced,
        Specialized
    }

    /// <summary>
    /// 菌毯瓦片数据
    /// </summary>
    [System.Serializable]
    public class CreepTile
    {
        [Header("基础信息")]
        public Vector2Int Position;
        public CreepTileType TileType;
        public CreepTileStatus Status;
        public bool IsActive;
        public bool NeedsUpdate;

        [Header("属性")]
        public float Health;
        public float MaxHealth;
        public float GrowthLevel;
        public float MaxGrowthLevel;
        public float GrowthRate;

        [Header("连接")]
        public List<CreepTile> ConnectedTiles;
        public bool IsNutritionSource;

        [Header("统计")]
        public float LastUpdateTime;
        public float TotalResourcesGenerated;
        public float CreationTime;
        public Vector3 WorldPosition;

        public CreepTile()
        {
            ConnectedTiles = new List<CreepTile>();
            MaxHealth = 100f;
            Health = MaxHealth;
            MaxGrowthLevel = 1f;
            GrowthLevel = 0f;
            GrowthRate = 0.1f;
            IsActive = true;
            Status = CreepTileStatus.Growing;
            TileType = CreepTileType.Basic;
        }
    }
}