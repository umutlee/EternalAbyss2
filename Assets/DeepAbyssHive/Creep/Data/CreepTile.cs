using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯瓦片数据类
    /// 表示菌毯网络中的单个瓦片单元
    /// </summary>
    [Serializable]
    public class CreepTile
    {
        [Header("基础信息")]
        public Vector2Int Position;
        public Vector3 WorldPosition;
        public CreepTileType TileType;
        public CreepTileStatus Status;
        
        [Header("生命值和成长")]
        public float Health;
        public float MaxHealth;
        public float GrowthLevel;
        public float MaxGrowthLevel;
        public float GrowthRate;
        
        [Header("状态标记")]
        public bool IsActive;
        public bool IsNutritionSource;
        public bool NeedsUpdate;
        
        [Header("连接关系")]
        public List<CreepTile> ConnectedTiles;
        
        [Header("时间信息")]
        public float CreationTime;
        public float LastUpdateTime;
        
        [Header("统计信息")]
        public float TotalResourcesGenerated;
        
        public CreepTile()
        {
            ConnectedTiles = new List<CreepTile>();
            Status = CreepTileStatus.Growing;
            TileType = CreepTileType.Basic;
            IsActive = true;
            Health = 100f;
            MaxHealth = 100f;
            GrowthLevel = 0f;
            MaxGrowthLevel = 1f;
            GrowthRate = 0.1f;
        }
    }
    
    /// <summary>
    /// 菌毯瓦片类型枚举
    /// </summary>
    public enum CreepTileType
    {
        Basic,      // 基础菌毯
        Enhanced,   // 增强菌毯
        Specialized // 特化菌毯
    }
    
    /// <summary>
    /// 菌毯瓦片状态枚举
    /// </summary>
    public enum CreepTileStatus
    {
        Growing,    // 成长中
        Healthy,    // 健康
        Starving,   // 饥饿
        Dying       // 死亡中
    }
}