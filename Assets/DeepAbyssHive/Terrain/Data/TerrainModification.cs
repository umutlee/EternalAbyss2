using UnityEngine;
using DeepAbyssHive.Terrain.Enums;

namespace DeepAbyssHive.Terrain.Data
{
    /// <summary>
    /// 地形修改数据结构
    /// 用于描述对地形的修改操作
    /// </summary>
    [System.Serializable]
    public struct TerrainModification
    {
        /// <summary>
        /// 是否改变地形类型
        /// </summary>
        public bool changeTerrainType;
        
        /// <summary>
        /// 新的地形类型
        /// </summary>
        public TerrainType newTerrainType;
        
        /// <summary>
        /// 是否改变高度
        /// </summary>
        public bool changeHeight;
        
        /// <summary>
        /// 高度变化量
        /// </summary>
        public float heightDelta;
        
        /// <summary>
        /// 修改半径
        /// </summary>
        public float radius;
        
        /// <summary>
        /// 修改强度（0-1）
        /// </summary>
        public float intensity;
        
        /// <summary>
        /// 修改类型
        /// </summary>
        public TerrainModificationType modificationType;
        
        /// <summary>
        /// 创建地形类型修改
        /// </summary>
        /// <param name="newType">新地形类型</param>
        /// <param name="radius">影响半径</param>
        /// <param name="intensity">修改强度</param>
        /// <returns>地形修改数据</returns>
        public static TerrainModification CreateTypeChange(TerrainType newType, float radius = 1f, float intensity = 1f)
        {
            return new TerrainModification
            {
                changeTerrainType = true,
                newTerrainType = newType,
                changeHeight = false,
                heightDelta = 0f,
                radius = radius,
                intensity = intensity,
                modificationType = TerrainModificationType.TypeChange
            };
        }
        
        /// <summary>
        /// 创建高度修改
        /// </summary>
        /// <param name="heightDelta">高度变化量</param>
        /// <param name="radius">影响半径</param>
        /// <param name="intensity">修改强度</param>
        /// <returns>地形修改数据</returns>
        public static TerrainModification CreateHeightChange(float heightDelta, float radius = 1f, float intensity = 1f)
        {
            return new TerrainModification
            {
                changeTerrainType = false,
                newTerrainType = TerrainType.Normal,
                changeHeight = true,
                heightDelta = heightDelta,
                radius = radius,
                intensity = intensity,
                modificationType = TerrainModificationType.HeightChange
            };
        }
        
        /// <summary>
        /// 创建复合修改
        /// </summary>
        /// <param name="newType">新地形类型</param>
        /// <param name="heightDelta">高度变化量</param>
        /// <param name="radius">影响半径</param>
        /// <param name="intensity">修改强度</param>
        /// <returns>地形修改数据</returns>
        public static TerrainModification CreateCombinedChange(TerrainType newType, float heightDelta, float radius = 1f, float intensity = 1f)
        {
            return new TerrainModification
            {
                changeTerrainType = true,
                newTerrainType = newType,
                changeHeight = true,
                heightDelta = heightDelta,
                radius = radius,
                intensity = intensity,
                modificationType = TerrainModificationType.Combined
            };
        }
    }
    
    /// <summary>
    /// 地形修改类型
    /// </summary>
    public enum TerrainModificationType
    {
        /// <summary>
        /// 仅改变地形类型
        /// </summary>
        TypeChange,
        
        /// <summary>
        /// 仅改变高度
        /// </summary>
        HeightChange,
        
        /// <summary>
        /// 复合修改
        /// </summary>
        Combined
    }
}