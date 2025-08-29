using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Terrain.Enums;
using DeepAbyssHive.Terrain.Data;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 地形管理器，负责管理分块地形系统 - 地形修改部分
    /// </summary>
    public partial class TerrainManager
    {
        #region 地形修改
        /// <summary>
        /// 修改指定世界坐标处的地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="modification">地形修改数据</param>
        public void ModifyTerrainAt(Vector3 worldPosition, TerrainModification modification)
        {
            _pendingModifications.Enqueue(modification);
            Debug.Log($"[{_managerName}] 添加地形修改: 位置={worldPosition}, 类型={modification.TerrainTypeValue}");
        }
        
        /// <summary>
        /// 修改地形
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <param name="radius">修改半径</param>
        /// <param name="terrainType">目标地形类型</param>
        /// <returns>是否成功添加修改</returns>
        public bool ModifyTerrain(Vector3 worldPosition, float radius, TerrainType terrainType)
        {
            TerrainModification modification = new TerrainModification
            {
                Position = worldPosition,
                Radius = radius,
                TerrainType = (TerrainType)terrainType,
                Timestamp = System.DateTime.Now
            };
            
            _pendingModifications.Enqueue(modification);
            
            Debug.Log($"[{_managerName}] 添加地形修改: 位置={worldPosition}, 半径={radius}, 类型={terrainType}");
            
            return true;
        }

        /// <summary>
        /// 处理待处理的地形修改
        /// </summary>
        private void ProcessPendingModifications()
        {
            _modificationProcessTimer += Time.deltaTime;
            
            if (_modificationProcessTimer < _modificationProcessInterval)
                return;
                
            _modificationProcessTimer = 0f;
            
            int processCount = 0;
            while (_pendingModifications.Count > 0 && processCount < _maxModificationsPerFrame)
            {
                TerrainModification modification = _pendingModifications.Dequeue();
                ApplyTerrainModification(modification);
                _modificationHistory.Add(modification);
                
                processCount++;
            }
            
            if (processCount > 0)
            {
                Debug.Log($"[{_managerName}] 处理了 {processCount} 个地形修改，剩余 {_pendingModifications.Count} 个");
            }
        }

        /// <summary>
        /// 应用地形修改
        /// </summary>
        /// <param name="modification">地形修改</param>
        private void ApplyTerrainModification(TerrainModification modification)
        {
            Vector2Int centerChunkCoord = WorldToChunkCoord(modification.Position);
            Vector2Int localCoord = WorldToLocalCoord(modification.Position);
            
            // 计算修改影响的区块范围
            int chunkRadius = Mathf.CeilToInt(modification.Radius / (_chunkSize * _tileSize));
            
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    // 如果区块已加载，则直接修改
                    if (_chunkTerrainData.ContainsKey(chunkCoord))
                    {
                        ModifyChunkTerrain(chunkCoord, modification);
                    }
                }
            }
        }

        /// <summary>
        /// 修改区块地形
        /// </summary>
        /// <param name="chunkCoord">区块坐标</param>
        /// <param name="modification">地形修改</param>
        private void ModifyChunkTerrain(Vector2Int chunkCoord, TerrainModification modification)
        {
            TerrainType[,] terrainData = _chunkTerrainData[chunkCoord];
            Vector3 chunkWorldPos = ChunkToWorldPosition(chunkCoord);
            
            // 计算修改在区块内的影响范围
            float radiusSquared = modification.Radius * modification.Radius;
            
            for (int x = 0; x < _chunkSize; x++)
            {
                for (int y = 0; y < _chunkSize; y++)
                {
                    // 计算当前瓦片的世界坐标
                    Vector3 tileWorldPos = chunkWorldPos + new Vector3(x * _tileSize, 0, y * _tileSize);
                    
                    // 计算与修改中心的距离
                    float distanceSquared = Vector3.SqrMagnitude(new Vector3(tileWorldPos.x, 0, tileWorldPos.z) - 
                                                               new Vector3(modification.Position.x, 0, modification.Position.z));
                    
                    // 如果在修改半径内，则应用修改
                    if (distanceSquared <= radiusSquared)
                    {
                        terrainData[x, y] = (int)modification.TerrainType;
                    }
                }
            }
            
            // 更新地形块
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                _terrainChunks[chunkCoord].UpdateTerrainData(terrainData);
            }
        }

        /// <summary>
        /// 撤销最后一次地形修改
        /// </summary>
        /// <returns>是否成功撤销</returns>
        public bool UndoLastModification()
        {
            if (_modificationHistory.Count == 0)
            {
                Debug.LogWarning($"[{_managerName}] 没有可撤销的地形修改");
                return false;
            }
            
            // 获取最后一次修改
            TerrainModification lastModification = _modificationHistory[_modificationHistory.Count - 1];
            _modificationHistory.RemoveAt(_modificationHistory.Count - 1);
            
            // 重新生成受影响的区块
            Vector2Int centerChunkCoord = WorldToChunkCoord(lastModification.Position);
            int chunkRadius = Mathf.CeilToInt(lastModification.Radius / (_chunkSize * _tileSize));
            
            for (int cx = -chunkRadius; cx <= chunkRadius; cx++)
            {
                for (int cy = -chunkRadius; cy <= chunkRadius; cy++)
                {
                    Vector2Int chunkCoord = new Vector2Int(centerChunkCoord.x + cx, centerChunkCoord.y + cy);
                    
                    if (_terrainChunks.ContainsKey(chunkCoord))
                    {
                        // 卸载并重新加载区块
                        UnloadChunk(chunkCoord);
                        LoadChunk(chunkCoord);
                    }
                }
            }
            
            Debug.Log($"[{_managerName}] 撤销了最后一次地形修改");
            
            return true;
        }

        /// <summary>
        /// 清除所有地形修改
        /// </summary>
        public void ClearAllModifications()
        {
            _pendingModifications.Clear();
            _modificationHistory.Clear();
            
            // 重新生成所有区块
            RegenerateAllChunks();
            
            Debug.Log($"[{_managerName}] 清除了所有地形修改");
        }
        #endregion
    }
}