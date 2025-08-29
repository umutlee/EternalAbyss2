using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Terrain.Interfaces;
using DeepAbyssHive.Terrain.Enums;
// 針對 TerrainModificationType 名稱衝突，使用別名區分 Data 與 Enums 的定義
using TerrainModificationTypeData = DeepAbyssHive.Terrain.Data.TerrainModificationType;
using TerrainType = DeepAbyssHive.Terrain.Enums.TerrainType;
using TerrainTypeData = DeepAbyssHive.Terrain.Data.TerrainType;
using DeepAbyssHive.Terrain.Data;
using DeepAbyssHive.Terrain.Config;

namespace DeepAbyssHive.Terrain.Services
{
    /// <summary>
    /// 地形修改服务实现
    /// 负责地形修改队列处理、历史记录和撤销操作
    /// </summary>
    public class TerrainModificationService : IService
    {
        #region 属性

        public string ServiceName => "TerrainModificationService";
        public bool IsInitialized { get; private set; }

        #endregion

        #region 私有字段
        private TerrainConfigSO _config;
        private Dictionary<Vector2Int, TerrainType[,]> _chunkTerrainData;
        private Dictionary<Vector2Int, ITerrainChunk> _terrainChunks;
        
        // 修改队列和历史
        private Queue<TerrainModification> _pendingModifications;
        private List<TerrainModification> _modificationHistory;
        
        // 处理参数
        private float _modificationProcessTimer;
        private float _modificationProcessInterval;
        private int _maxModificationsPerFrame;
        private int _chunkSize;
        private float _tileSize;
        #endregion

        #region 构造函数
        public TerrainModificationService(TerrainConfigSO config,
            Dictionary<Vector2Int, TerrainType[,]> chunkTerrainData,
            Dictionary<Vector2Int, ITerrainChunk> terrainChunks)
        {
            _config = config;
            _chunkTerrainData = chunkTerrainData;
            _terrainChunks = terrainChunks;
            
            _pendingModifications = new Queue<TerrainModification>();
            _modificationHistory = new List<TerrainModification>();
            
            InitializeParameters();
        }
        #endregion

        #region IService 实现
        public void Initialize()
        {
            if (IsInitialized) return;
            
            InitializeParameters();
            IsInitialized = true;
            
            Debug.Log($"[{ServiceName}] 地形修改服务初始化完成");
        }

        public void Cleanup()
        {
            _pendingModifications?.Clear();
            _modificationHistory?.Clear();
            IsInitialized = false;
            
            Debug.Log($"[{ServiceName}] 地形修改服务清理完成");
        }
        #endregion

        #region ITerrainModificationService 实现
        public bool ModifyTerrain(Vector3 worldPosition, float radius, TerrainType terrainType)
        {
            TerrainModification modification = new TerrainModification
            {
                Position = worldPosition,
                Radius = radius,
                TerrainType = terrainType,
                Timestamp = DateTime.Now
            };
            
            _pendingModifications.Enqueue(modification);
            
            Debug.Log($"[TerrainModificationService] 添加地形修改: 位置={worldPosition}, 半径={radius}, 类型={terrainType}");
            
            return true;
        }

        public void ModifyTerrainAt(Vector3 worldPosition, TerrainModification modification)
        {
            modification.Position = worldPosition;
            modification.Timestamp = DateTime.Now;
            
            _pendingModifications.Enqueue(modification);
            Debug.Log($"[TerrainModificationService] 添加地形修改: 位置={worldPosition}, 类型={modification.TerrainTypeValue}");
        }

        public void ProcessPendingModifications(float deltaTime)
        {
            _modificationProcessTimer += deltaTime;
            
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
                Debug.Log($"[TerrainModificationService] 处理了 {processCount} 个地形修改，剩余 {_pendingModifications.Count} 个");
            }
        }

        public bool UndoLastModification()
        {
            if (_modificationHistory.Count == 0)
            {
                Debug.LogWarning($"[TerrainModificationService] 没有可撤销的地形修改");
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
                        // 触发重新生成事件
                        OnChunkRegenerationRequested?.Invoke(chunkCoord);
                    }
                }
            }
            
            Debug.Log($"[TerrainModificationService] 撤销了最后一次地形修改");
            
            return true;
        }

        public void ClearAllModifications()
        {
            _pendingModifications.Clear();
            _modificationHistory.Clear();
            
            Debug.Log($"[TerrainModificationService] 清除了所有地形修改");
            
            // 触发全部重新生成事件
            OnAllChunksRegenerationRequested?.Invoke();
        }

        public int GetPendingModificationCount()
        {
            return _pendingModifications.Count;
        }

        public int GetModificationHistoryCount()
        {
            return _modificationHistory.Count;
        }

        public List<TerrainModification> GetModificationHistory()
        {
            return new List<TerrainModification>(_modificationHistory);
        }

        public void SetModificationParameters(float processInterval, int maxPerFrame)
        {
            _modificationProcessInterval = processInterval;
            _maxModificationsPerFrame = maxPerFrame;
            
            Debug.Log($"[TerrainModificationService] 更新修改参数: 间隔={_modificationProcessInterval}, 最大每帧={_maxModificationsPerFrame}");
        }
        // 新增的介面方法實現
        public bool ModifyHeight(Vector3 position, float radius, float heightDelta, AnimationCurve falloff = null)
        {
            var modification = new TerrainModification
            {
                Position = position,
                Radius = radius,
                Value = heightDelta,
                Type = TerrainModificationTypeData.HeightChange,
                Falloff = falloff,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool SetTerrainType(Vector3 position, float radius, TerrainType terrainType)
        {
            var modification = new TerrainModification
            {
                Position = position,
                Radius = radius,
                TerrainType = terrainType,
                Type = TerrainModificationTypeData.TypeChange,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool FlattenTerrain(Vector3 center, Vector2 size, float targetHeight = -1f)
        {
            var modification = new TerrainModification
            {
                Position = center,
                Radius = Mathf.Max(size.x, size.y) / 2f,
                Value = targetHeight,
                Type = TerrainModificationTypeData.Flatten,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool DigTerrain(Vector3 position, float radius, float depth)
        {
            var modification = new TerrainModification
            {
                Position = position,
                Radius = radius,
                Value = -depth,
                Type = TerrainModificationTypeData.Dig,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool FillTerrain(Vector3 position, float radius, float height)
        {
            var modification = new TerrainModification
            {
                Position = position,
                Radius = radius,
                Value = height,
                Type = TerrainModificationTypeData.Fill,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool CreateRamp(Vector3 start, Vector3 end, float width)
        {
            var modification = new TerrainModification
            {
                Position = (start + end) / 2f,
                Radius = width,
                Value = Vector3.Distance(start, end),
                Type = TerrainModificationTypeData.Ramp,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool CreateTunnel(Vector3 start, Vector3 end, float radius)
        {
            var modification = new TerrainModification
            {
                Position = (start + end) / 2f,
                Radius = radius,
                Value = Vector3.Distance(start, end),
                Type = TerrainModificationTypeData.Tunnel,
                Timestamp = System.DateTime.Now
            };
            return ApplyModification(modification);
        }

        public bool ApplyModification(TerrainModification modification)
        {
            modification.Timestamp = System.DateTime.Now;
            _pendingModifications.Enqueue(modification);
            Debug.Log($"[TerrainModificationService] 添加地形修改: 類型={modification.Type}, 位置={modification.Position}");
            return true;
        }

        public bool UndoModification(int modificationId)
        {
            // 簡化實現：撤銷最後一次修改
            return UndoLastModification();
        }

        public bool RedoModification(int modificationId)
        {
            // TODO: 實現重做功能
            Debug.LogWarning("[TerrainModificationService] RedoModification 尚未實現");
            return false;
        }

        public void ClearModificationHistory(int keepCount = 0)
        {
            if (keepCount <= 0)
            {
                ClearAllModifications();
            }
            else
            {
                while (_modificationHistory.Count > keepCount)
                {
                    _modificationHistory.RemoveAt(0);
                }
            }
        }

        public bool SaveModifications(string filePath)
        {
            // TODO: 實現保存功能
            Debug.LogWarning("[TerrainModificationService] SaveModifications 尚未實現");
            return false;
        }

        public bool LoadModifications(string filePath)
        {
            // TODO: 實現加載功能
            Debug.LogWarning("[TerrainModificationService] LoadModifications 尚未實現");
            return false;
        }

        public bool RegenerateChunk(int chunkX, int chunkZ)
        {
            Vector2Int chunkCoord = new Vector2Int(chunkX, chunkZ);
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                OnChunkRegenerationRequested?.Invoke(chunkCoord);
                return true;
            }
            return false;
        }

        public int ApplyModificationBatch(TerrainModification[] modifications)
        {
            int successCount = 0;
            foreach (var modification in modifications)
            {
                if (ApplyModification(modification))
                {
                    successCount++;
                }
            }
            return successCount;
        }
        #endregion

        #region 事件
        public event System.Action<Vector2Int> OnChunkRegenerationRequested;
        public event System.Action OnAllChunksRegenerationRequested;
        #endregion

        #region 私有方法
        private void InitializeParameters()
        {
            if (_config != null)
            {
                _modificationProcessInterval = _config.modificationProcessInterval;
                _maxModificationsPerFrame = _config.maxModificationsPerFrame;
                _chunkSize = _config.chunkSize;
                _tileSize = _config.tileSize;
            }
            else
            {
                // 默认值
                _modificationProcessInterval = 0.1f;
                _maxModificationsPerFrame = 5;
                _chunkSize = 64;
                _tileSize = 1f;
            }
            
            _modificationProcessTimer = 0f;
        }

        private void ApplyTerrainModification(TerrainModification modification)
        {
            Vector2Int centerChunkCoord = WorldToChunkCoord(modification.Position);
            
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
                        terrainData[x, y] = modification.TerrainType;
                    }
                }
            }
            
            // 更新地形块
            if (_terrainChunks.ContainsKey(chunkCoord))
            {
                _terrainChunks[chunkCoord].UpdateTerrainData(terrainData);
            }
        }

        private Vector2Int WorldToChunkCoord(Vector3 worldPosition)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            int chunkX = Mathf.FloorToInt(worldPosition.x / chunkWorldSize);
            int chunkZ = Mathf.FloorToInt(worldPosition.z / chunkWorldSize);
            return new Vector2Int(chunkX, chunkZ);
        }

        private Vector3 ChunkToWorldPosition(Vector2Int chunkCoord)
        {
            float chunkWorldSize = _chunkSize * _tileSize;
            float worldX = chunkCoord.x * chunkWorldSize;
            float worldZ = chunkCoord.y * chunkWorldSize;
            return new Vector3(worldX, 0, worldZ);
        }
        #endregion
    }

}