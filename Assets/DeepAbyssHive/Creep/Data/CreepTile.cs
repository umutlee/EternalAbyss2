using System;
using System.Collections.Generic;
using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯瓦片状态
    /// </summary>
    public enum CreepTileStatus
    {
        Healthy,    // 健康
        Growing,    // 生长中
        Starving,   // 营养不足
        Dying       // 死亡中
    }
    
    /// <summary>
    /// 菌毯瓦片类型
    /// </summary>
    public enum CreepTileType
    {
        Basic,      // 基础
        Enhanced,   // 增强
        Source,     // 源点
        Specialized // 特化
    }
    
    /// <summary>
    /// 菌毯瓦片数据
    /// </summary>
    [System.Serializable]
    public class CreepTile
    {
        [Header("位置信息")]
        public Vector2Int Position;
        public Vector3 WorldPosition;
        
        [Header("瓦片属性")]
        public CreepTileType TileType;
        public CreepTileStatus Status;
        
        [Header("生命值")]
        public float Health;
        public float MaxHealth;
        
        [Header("生长属性")]
        public float GrowthLevel;
        public float MaxGrowthLevel;
        public float GrowthRate;
        
        [Header("状态标记")]
        public bool IsActive;
        public bool IsNutritionSource;
        public bool NeedsUpdate;
        
        [Header("连接信息")]
        public List<CreepTile> ConnectedTiles;
        
        [Header("时间信息")]
        public float CreationTime;
        public float LastUpdateTime;
        
        [Header("资源信息")]
        public float TotalResourcesGenerated;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CreepTile()
        {
            ConnectedTiles = new List<CreepTile>();
            Status = CreepTileStatus.Growing;
            TileType = CreepTileType.Basic;
            Health = 100f;
            MaxHealth = 100f;
            GrowthLevel = 0f;
            MaxGrowthLevel = 1f;
            GrowthRate = 0.1f;
            IsActive = true;
            IsNutritionSource = false;
            NeedsUpdate = true;
            CreationTime = Time.time;
            LastUpdateTime = Time.time;
            TotalResourcesGenerated = 0f;
        }
        
        /// <summary>
        /// 更新瓦片状态
        /// </summary>
        public void UpdateTile(float deltaTime)
        {
            LastUpdateTime = Time.time;
            
            // 更新生长等级
            if (Status == CreepTileStatus.Growing && GrowthLevel < MaxGrowthLevel)
            {
                GrowthLevel += GrowthRate * deltaTime;
                if (GrowthLevel >= MaxGrowthLevel)
                {
                    Status = CreepTileStatus.Healthy;
                }
            }
            
            // 检查健康状态
            if (Health <= 0f)
            {
                Status = CreepTileStatus.Dying;
                IsActive = false;
            }
            else if (Health < MaxHealth * 0.3f)
            {
                Status = CreepTileStatus.Starving;
            }
        }
        
        /// <summary>
        /// 连接到其他瓦片
        /// </summary>
        public void ConnectTo(CreepTile other)
        {
            if (other != null && !ConnectedTiles.Contains(other))
            {
                ConnectedTiles.Add(other);
                other.ConnectedTiles.Add(this);
            }
        }
        
        /// <summary>
        /// 断开与其他瓦片的连接
        /// </summary>
        public void DisconnectFrom(CreepTile other)
        {
            if (other != null)
            {
                ConnectedTiles.Remove(other);
                other.ConnectedTiles.Remove(this);
            }
        }
    }
}