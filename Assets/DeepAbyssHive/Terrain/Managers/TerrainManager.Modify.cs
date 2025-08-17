using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器 - 地形修改部分（委托模式）
    /// 所有方法委托给 TerrainModificationService
    /// </summary>
    public partial class TerrainManager
    {
        #region 地形修改 - 委托给服务
        /// <summary>
        /// 修改指定世界坐标处的地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="modification">地形修改数据</param>
        public void ModifyTerrainAt(Vector3 worldPosition, TerrainModification modification)
        {
            _modificationService?.ModifyTerrainAt(worldPosition, modification);
        }
        
        #endregion
    }
}