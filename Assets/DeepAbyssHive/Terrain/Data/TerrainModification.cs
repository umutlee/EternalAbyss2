using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using System;

namespace DeepAbyssHive.Terrain.Data
{
    /// <summary>
    /// 地形修改数据结构
    /// </summary>
    public struct TerrainModification
    {
        /// <summary>
        /// 修改类型
        /// </summary>
        public enum ModificationType
        {
            /// <summary>
            /// 高度修改
            /// </summary>
            Height,
            
            /// <summary>
            /// 地形类型修改
            /// </summary>
            TerrainType,
            
            /// <summary>
            /// 菌毯密度修改
            /// </summary>
            CreepDensity
        }
        
        /// <summary>
        /// 修改类型
        /// </summary>
        public ModificationType Type;
        
        /// <summary>
        /// 修改半径
        /// </summary>
        public float Radius;
        
        /// <summary>
        /// 修改强度
        /// </summary>
        public float Strength;
        
        /// <summary>
        /// 地形类型（当Type为TerrainType时使用）
        /// </summary>
        public TerrainType TerrainTypeValue;
        
        /// <summary>
        /// 所有者ID（当Type为CreepDensity时使用）
        /// </summary>
        public int OwnerId;
        
        /// <summary>
        /// 修改位置
        /// </summary>
        public Vector3 Position;
        
        /// <summary>
        /// 新地形类型
        /// </summary>
        public TerrainType NewTerrainType;
        
        /// <summary>
        /// 原始地形类型
        /// </summary>
        public TerrainType OriginalTerrainType;
        
        /// <summary>
        /// 修改时间戳
        /// </summary>
        public float Timestamp;
        
        /// <summary>
        /// 修改ID
        /// </summary>
        public string ModificationId;
        
        /// <summary>
        /// 创建高度修改
        /// </summary>
        /// <param name="radius">修改半径</param>
        /// <param name="strength">修改强度</param>
        /// <returns>地形修改数据</returns>
        public static TerrainModification CreateHeightModification(float radius, float strength)
        {
            return new TerrainModification
            {
                Type = ModificationType.Height,
                Radius = radius,
                Strength = strength
            };
        }
        
        /// <summary>
        /// 创建地形类型修改
        /// </summary>
        /// <param name="radius">修改半径</param>
        /// <param name="terrainType">地形类型</param>
        /// <returns>地形修改数据</returns>
        public static TerrainModification CreateTerrainTypeModification(float radius, TerrainType terrainType)
        {
            return new TerrainModification
            {
                Type = ModificationType.TerrainType,
                Radius = radius,
                TerrainTypeValue = terrainType
            };
        }
        
        /// <summary>
        /// 创建菌毯密度修改
        /// </summary>
        /// <param name="radius">修改半径</param>
        /// <param name="strength">修改强度</param>
        /// <param name="ownerId">所有者ID</param>
        /// <returns>地形修改数据</returns>
        public static TerrainModification CreateCreepModification(float radius, float strength, int ownerId)
        {
            return new TerrainModification
            {
                Type = ModificationType.CreepDensity,
                Radius = radius,
                Strength = strength,
                OwnerId = ownerId
            };
        }
    }
}