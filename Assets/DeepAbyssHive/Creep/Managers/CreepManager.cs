using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Creep.Interfaces;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;

namespace DeepAbyssHive.Creep.Managers
{
    /// <summary>
    /// 菌毯管理器，负责管理菌毯系统
    /// </summary>
    public class CreepManager : MonoBehaviour, ICreepManager, IManager
    {
        #region 私有字段
        private Dictionary<Vector2Int, CreepData> _creepGrid = new Dictionary<Vector2Int, CreepData>();
        private Dictionary<int, List<Vector2Int>> _playerCreepSources = new Dictionary<int, List<Vector2Int>>();
        private Dictionary<int, CreepNetworkData> _creepNetworks = new Dictionary<int, CreepNetworkData>();
        private Queue<Vector2Int> _expansionQueue = new Queue<Vector2Int>();
        private ISpatialIndex<CreepData> _spatialIndex;
        
        private bool _isInitialized = false;
        private bool _isPaused = false;
        private string _managerName = "CreepManager";
        
        // 菌毯配置
        private float _gridSize = 1.0f; // 菌毯网格大小
        private float _expansionRate = 0.1f; // 菌毯扩张速率
        private float _decayRate = 0.05f; // 菌毯衰减速率
        private float _minDensity = 0.01f; // 最小菌毯密度
        private float _maxDensity = 1.0f; // 最大菌毯密度
        private int _maxExpansionsPerFrame = 50; // 每帧最大扩张数量
        
        // 性能优化
        private float _updateTimer = 0f;
        private float _updateInterval = 0.1f; // 更新间隔
        private int _currentUpdateIndex = 0;
        private List<Vector2Int> _activeCreepCells = new List<Vector2Int>();
        #endregion

        #region Unity生命周期
        /// <summary>
        /// Awake方法
        /// </summary>
        private void Awake()
        {
            // 在Awake中进行基本初始化
            _managerName = "CreepManager";
        }

        /// <summary>
        /// 设置空间索引
        /// </summary>
        /// <param name="spatialIndex">空间索引系统</param>
        public void SetSpatialIndex(ISpatialIndex<CreepData> spatialIndex)
        {
            _spatialIndex = spatialIndex;
        }
        #endregion

        // 已移除重复的IManager接口实现，保留下方的完整实现


        #region ICreepManager接口实现
        /// <summary>
        /// 创建菌毯节点
        /// </summary>
        /// <param name="creepData">菌毯数据</param>
        /// <returns>菌毯ID</returns>
        public int CreateCreepNode(CreepData creepData)
        {
            Vector2Int gridPos = WorldToGridPosition(creepData.Position);
            
            // 生成新的菌毯ID
            int creepId = _creepGrid.Count;
            
            // 检查是否已存在菌毯
            if (_creepGrid.ContainsKey(gridPos))
            {
                Debug.LogWarning($"[{_managerName}] 尝试在已存在菌毯的位置创建节点: {creepData.Position}");
                return -1;
            }
            
            // 设置菌毯ID
            creepData.CreepId = creepId;
            
            // 添加到网格
            _creepGrid[gridPos] = creepData;
            _activeCreepCells.Add(gridPos);
            
            // 添加到空间索引
            if (_spatialIndex != null)
            {
                _spatialIndex.Insert(creepData, creepData.Position, Vector3.one * _gridSize);
            }
            
            // 记录玩家的菌毯源点
            if (creepData.IsSource)
            {
                if (!_playerCreepSources.ContainsKey(creepData.OwnerId))
                {
                    _playerCreepSources[creepData.OwnerId] = new List<Vector2Int>();
                }
                
                if (!_playerCreepSources[creepData.OwnerId].Contains(gridPos))
                {
                    _playerCreepSources[creepData.OwnerId].Add(gridPos);
                }
                
                // 初始化菌毯网络
                if (!_creepNetworks.ContainsKey(creepData.OwnerId))
                {
                    _creepNetworks[creepData.OwnerId] = new CreepNetworkData
                    {
                        OwnerId = creepData.OwnerId,
                        TotalArea = 0f,
                        ConnectedSources = new List<Vector3>(),
                        NetworkEfficiency = 1.0f
                    };
                }
                
                _creepNetworks[creepData.OwnerId].ConnectedSources.Add(creepData.Position);
                
                // 添加到扩张队列
                _expansionQueue.Enqueue(gridPos);
            }
            
            Debug.Log($"[{_managerName}] 创建菌毯节点: ID={creepId}, 位置={creepData.Position}, 所有者={creepData.OwnerId}");
            return creepId;
        }

        /// <summary>
        /// 获取菌毯数据
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        /// <returns>菌毯数据</returns>
        public CreepData GetCreepData(int creepId)
        {
            foreach (var pair in _creepGrid)
            {
                if (pair.Value.CreepId == creepId)
                {
                    return pair.Value;
                }
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的菌毯数据: ID={creepId}");
            return new CreepData();
        }

        /// <summary>
        /// 更新菌毯数据
        /// </summary>
        /// <param name="creepData">菌毯数据</param>
        public void UpdateCreep(CreepData creepData)
        {
            Vector2Int gridPos = WorldToGridPosition(creepData.Position);
            
            if (_creepGrid.ContainsKey(gridPos))
            {
                _creepGrid[gridPos] = creepData;
                
                // 更新空间索引
                if (_spatialIndex != null)
                {
                    _spatialIndex.Update(creepData, creepData.Position, creepData.Position, Vector3.one * _gridSize);
                }
            }
            else
            {
                Debug.LogWarning($"[{_managerName}] 尝试更新不存在的菌毯: 位置={creepData.Position}");
            }
        }

        /// <summary>
        /// 删除菌毯节点
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        public void RemoveCreepNode(int creepId)
        {
            Vector2Int gridPosToRemove = Vector2Int.zero;
            CreepData creepToRemove = new CreepData();
            bool found = false;
            
            foreach (var pair in _creepGrid)
            {
                if (pair.Value.CreepId == creepId)
                {
                    gridPosToRemove = pair.Key;
                    creepToRemove = pair.Value;
                    found = true;
                    break;
                }
            }
            
            if (!found)
            {
                Debug.LogWarning($"[{_managerName}] 尝试删除不存在的菌毯节点: ID={creepId}");
                return;
            }
            
            // 从玩家源点列表中移除
            if (creepToRemove.IsSource && _playerCreepSources.ContainsKey(creepToRemove.OwnerId))
            {
                _playerCreepSources[creepToRemove.OwnerId].Remove(gridPosToRemove);
            }
            
            // 从菌毯网络中移除
            if (_creepNetworks.ContainsKey(creepToRemove.OwnerId))
            {
                _creepNetworks[creepToRemove.OwnerId].ConnectedSources.Remove(creepToRemove.Position);
            }
            
            // 移除菌毯
            RemoveCreepAtPosition(gridPosToRemove);
            
            Debug.Log($"[{_managerName}] 删除菌毯节点: ID={creepId}, 位置={creepToRemove.Position}");
        }

        /// <summary>
        /// 检查位置是否有菌毯覆盖
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID（可选）</param>
        /// <returns>是否有菌毯覆盖</returns>
        public bool HasCreepCoverage(Vector3 position, int ownerId = -1)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            
            if (!_creepGrid.TryGetValue(gridPos, out CreepData creepData))
                return false;
            
            if (creepData.Density < _minDensity)
                return false;
            
            if (ownerId >= 0 && creepData.OwnerId != ownerId)
                return false;
            
            return true;
        }

        /// <summary>
        /// 获取位置处的菌毯强度
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID（可选）</param>
        /// <returns>菌毯强度（0-1）</returns>
        public float GetCreepStrength(Vector3 position, int ownerId = -1)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            
            if (_creepGrid.TryGetValue(gridPos, out CreepData creepData))
            {
                if (ownerId >= 0 && creepData.OwnerId != ownerId)
                    return 0f;
                    
                return creepData.Density;
            }
            
            return 0f;
        }

        /// <summary>
        /// 扩张菌毯
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        /// <param name="expansionAmount">扩张量</param>
        public void ExpandCreep(int creepId, float expansionAmount)
        {
            CreepData creepData = GetCreepData(creepId);
            if (creepData.CreepId != creepId)
            {
                Debug.LogWarning($"[{_managerName}] 尝试扩张不存在的菌毯: ID={creepId}");
                return;
            }
            
            Vector2Int centerGrid = WorldToGridPosition(creepData.Position);
            int gridRadius = Mathf.CeilToInt(expansionAmount / _gridSize);
            
            // 在半径范围内扩张菌毯
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector2Int gridPos = centerGrid + new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorldPosition(gridPos);
                    
                    // 检查距离
                    float distance = Vector3.Distance(creepData.Position, worldPos);
                    if (distance > expansionAmount)
                        continue;
                    
                    // 计算扩张强度（距离越近，扩张越强）
                    float expansionStrength = _expansionRate * (1f - distance / expansionAmount);
                    
                    // 扩张菌毯
                    ExpandCreepAtPosition(gridPos, worldPos, expansionStrength, creepData.OwnerId);
                }
            }
        }

        /// <summary>
        /// 收缩菌毯
        /// </summary>
        /// <param name="creepId">菌毯ID</param>
        /// <param name="shrinkAmount">收缩量</param>
        public void ShrinkCreep(int creepId, float shrinkAmount)
        {
            CreepData creepData = GetCreepData(creepId);
            if (creepData.CreepId != creepId)
            {
                Debug.LogWarning($"[{_managerName}] 尝试收缩不存在的菌毯: ID={creepId}");
                return;
            }
            
            Vector2Int gridPos = WorldToGridPosition(creepData.Position);
            
            if (_creepGrid.TryGetValue(gridPos, out CreepData existingCreep))
            {
                existingCreep.Density = Mathf.Max(0f, existingCreep.Density - shrinkAmount);
                existingCreep.LastUpdateTime = Time.time;
                
                if (existingCreep.Density <= _minDensity && !existingCreep.IsSource)
                {
                    // 移除菌毯
                    RemoveCreepAtPosition(gridPos);
                }
                else
                {
                    _creepGrid[gridPos] = existingCreep;
                }
            }
        }

        /// <summary>
        /// 损坏菌毯
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="damageAmount">损坏量</param>
        public void DamageCreep(Vector3 position, float radius, float damageAmount)
        {
            Vector2Int centerGrid = WorldToGridPosition(position);
            int gridRadius = Mathf.CeilToInt(radius / _gridSize);
            
            // 在半径范围内损坏菌毯
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector2Int gridPos = centerGrid + new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorldPosition(gridPos);
                    
                    // 检查距离
                    float distance = Vector3.Distance(position, worldPos);
                    if (distance > radius)
                        continue;
                    
                    if (!_creepGrid.TryGetValue(gridPos, out CreepData creepData))
                        continue;
                    
                    // 计算损坏强度（距离越近，损坏越强）
                    float damageStrength = damageAmount * (1f - distance / radius);
                    
                    // 损坏菌毯
                    creepData.Density = Mathf.Max(0f, creepData.Density - damageStrength);
                    creepData.LastUpdateTime = Time.time;
                    
                    if (creepData.Density <= _minDensity && !creepData.IsSource)
                    {
                        // 移除菌毯
                        RemoveCreepAtPosition(gridPos);
                    }
                    else
                    {
                        _creepGrid[gridPos] = creepData;
                    }
                }
            }
        }

        /// <summary>
        /// 修复菌毯
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="radius">半径</param>
        /// <param name="healAmount">修复量</param>
        /// <param name="ownerId">所有者ID</param>
        public void HealCreep(Vector3 position, float radius, float healAmount, int ownerId)
        {
            Vector2Int centerGrid = WorldToGridPosition(position);
            int gridRadius = Mathf.CeilToInt(radius / _gridSize);
            
            // 在半径范围内修复菌毯
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector2Int gridPos = centerGrid + new Vector2Int(x, y);
                    Vector3 worldPos = GridToWorldPosition(gridPos);
                    
                    // 检查距离
                    float distance = Vector3.Distance(position, worldPos);
                    if (distance > radius)
                        continue;
                    
                    if (!_creepGrid.TryGetValue(gridPos, out CreepData creepData))
                        continue;
                    
                    if (creepData.OwnerId != ownerId)
                        continue;
                    
                    // 计算修复强度（距离越近，修复越强）
                    float healStrength = healAmount * (1f - distance / radius);
                    
                    // 修复菌毯
                    creepData.Density = Mathf.Min(_maxDensity, creepData.Density + healStrength);
                    creepData.LastUpdateTime = Time.time;
                    _creepGrid[gridPos] = creepData;
                }
            }
        }

        /// <summary>
        /// 获取菌毯网络数据
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <returns>菌毯网络数据</returns>
        public CreepNetworkData GetCreepNetworkData(int networkId)
        {
            if (_creepNetworks.TryGetValue(networkId, out CreepNetworkData networkData))
            {
                return networkData;
            }
            
            Debug.LogWarning($"[{_managerName}] 尝试获取不存在的菌毯网络数据: ID={networkId}");
            return new CreepNetworkData
            {
                OwnerId = networkId,
                TotalArea = 0f,
                ConnectedSources = new List<Vector3>(),
                NetworkEfficiency = 0f
            };
        }

        /// <summary>
        /// 合并菌毯网络
        /// </summary>
        /// <param name="networkId1">网络ID1</param>
        /// <param name="networkId2">网络ID2</param>
        /// <returns>合并后的网络ID</returns>
        public int MergeCreepNetworks(int networkId1, int networkId2)
        {
            if (!_creepNetworks.TryGetValue(networkId1, out CreepNetworkData network1))
            {
                Debug.LogWarning($"[{_managerName}] 尝试合并不存在的菌毯网络: ID={networkId1}");
                return -1;
            }
            
            if (!_creepNetworks.TryGetValue(networkId2, out CreepNetworkData network2))
            {
                Debug.LogWarning($"[{_managerName}] 尝试合并不存在的菌毯网络: ID={networkId2}");
                return -1;
            }
            
            // 合并网络数据
            network1.TotalArea += network2.TotalArea;
            network1.ConnectedSources.AddRange(network2.ConnectedSources);
            network1.NetworkEfficiency = (network1.NetworkEfficiency + network2.NetworkEfficiency) / 2f;
            
            // 更新网络
            _creepNetworks[networkId1] = network1;
            _creepNetworks.Remove(networkId2);
            
            Debug.Log($"[{_managerName}] 合并菌毯网络: {networkId1} + {networkId2} = {networkId1}");
            return networkId1;
        }

        /// <summary>
        /// 分割菌毯网络
        /// </summary>
        /// <param name="networkId">网络ID</param>
        /// <param name="position">分割位置</param>
        /// <param name="radius">分割半径</param>
        /// <returns>分割后的网络ID数组</returns>
        public int[] SplitCreepNetwork(int networkId, Vector3 position, float radius)
        {
            if (!_creepNetworks.TryGetValue(networkId, out CreepNetworkData originalNetwork))
            {
                Debug.LogWarning($"[{_managerName}] 尝试分割不存在的菌毯网络: ID={networkId}");
                return new int[0];
            }
            
            // 简化实现：创建两个新网络
            int newNetworkId1 = networkId;
            int newNetworkId2 = _creepNetworks.Count;
            
            // 分割连接的源点
            List<Vector3> sources1 = new List<Vector3>();
            List<Vector3> sources2 = new List<Vector3>();
            
            foreach (var source in originalNetwork.ConnectedSources)
            {
                float distance = Vector3.Distance(source, position);
                if (distance <= radius)
                {
                    sources1.Add(source);
                }
                else
                {
                    sources2.Add(source);
                }
            }
            
            // 更新原网络
            originalNetwork.ConnectedSources = sources1;
            originalNetwork.TotalArea *= 0.5f; // 简化：假设面积平分
            originalNetwork.NetworkEfficiency *= 0.8f; // 分割会降低效率
            _creepNetworks[newNetworkId1] = originalNetwork;
            
            // 创建新网络
            if (sources2.Count > 0)
            {
                CreepNetworkData newNetwork = new CreepNetworkData
                {
                    OwnerId = originalNetwork.OwnerId,
                    TotalArea = originalNetwork.TotalArea,
                    ConnectedSources = sources2,
                    NetworkEfficiency = originalNetwork.NetworkEfficiency
                };
                _creepNetworks[newNetworkId2] = newNetwork;
                
                Debug.Log($"[{_managerName}] 分割菌毯网络: {networkId} -> {newNetworkId1}, {newNetworkId2}");
                return new int[] { newNetworkId1, newNetworkId2 };
            }
            
            Debug.Log($"[{_managerName}] 菌毯网络分割失败，源点不足: {networkId}");
            return new int[] { newNetworkId1 };
        }

        /// <summary>
        /// 添加菌毯源点
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID</param>
        /// <param name="initialRadius">初始半径</param>
        public void AddCreepSource(Vector3 position, int ownerId, float initialRadius)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            
            // 检查是否已存在菌毯
            if (_creepGrid.ContainsKey(gridPos))
            {
                // 如果已存在，增强密度
                CreepData existingCreep = _creepGrid[gridPos];
                if (existingCreep.OwnerId == ownerId)
                {
                    existingCreep.Density = Mathf.Min(_maxDensity, existingCreep.Density + 0.5f);
                    existingCreep.LastUpdateTime = Time.time;
                    _creepGrid[gridPos] = existingCreep;
                }
                else
                {
                    Debug.LogWarning($"[{_managerName}] 尝试在敌方菌毯上添加源点: {position}");
                    return;
                }
            }
            else
            {
                // 创建新的菌毯源点
                CreepData creepData = new CreepData
                {
                    Position = position,
                    Density = _maxDensity,
                    OwnerId = ownerId,
                    IsSource = true,
                    SourceRadius = initialRadius,
                    LastUpdateTime = Time.time,
                    CreationTime = Time.time
                };
                
                _creepGrid[gridPos] = creepData;
                _activeCreepCells.Add(gridPos);
                
                // 添加到空间索引
                if (_spatialIndex != null)
                {
                    _spatialIndex.Insert(creepData, position, Vector3.one * _gridSize);
                }
            }
            
            // 记录玩家的菌毯源点
            if (!_playerCreepSources.ContainsKey(ownerId))
            {
                _playerCreepSources[ownerId] = new List<Vector2Int>();
            }
            
            if (!_playerCreepSources[ownerId].Contains(gridPos))
            {
                _playerCreepSources[ownerId].Add(gridPos);
            }
            
            // 初始化菌毯网络
            if (!_creepNetworks.ContainsKey(ownerId))
            {
                _creepNetworks[ownerId] = new CreepNetworkData
                {
                    OwnerId = ownerId,
                    TotalArea = 0f,
                    ConnectedSources = new List<Vector3>(),
                    NetworkEfficiency = 1.0f
                };
            }
            
            _creepNetworks[ownerId].ConnectedSources.Add(position);
            
            // 添加到扩张队列
            _expansionQueue.Enqueue(gridPos);
            
            Debug.Log($"[{_managerName}] 添加菌毯源点: 位置={position}, 所有者={ownerId}, 半径={initialRadius}");
        }

        /// <summary>
        /// 移除菌毯源点
        /// </summary>
        /// <param name="position">位置</param>
        /// <param name="ownerId">所有者ID</param>
        public void RemoveCreepSource(Vector3 position, int ownerId)
        {
            Vector2Int gridPos = WorldToGridPosition(position);
            
            if (!_creepGrid.TryGetValue(gridPos, out CreepData creepData))
            {
                Debug.LogWarning($"[{_managerName}] 尝试移除不存在的菌毯源点: {position}");
                return;
            }
            
            if (creepData.OwnerId != ownerId)
            {
                Debug.LogWarning($"[{_managerName}] 尝试移除其他玩家的菌毯源点: {position}");
                return;
            }
            
            // 标记为非源点
            creepData.IsSource = false;
            creepData.SourceRadius = 0f;
            _creepGrid[gridPos] = creepData;
            
            // 从玩家源点列表中移除
            if (_playerCreepSources.ContainsKey(ownerId))
            {
                _playerCreepSources[ownerId].Remove(gridPos);
            }
            
            // 从菌毯网络中移除
            if (_creepNetworks.ContainsKey(ownerId))
            {
                _creepNetworks[ownerId].ConnectedSources.Remove(position);
            }
            
            Debug.Log($"[{_managerName}] 移除菌毯源点: 位置={position}, 所有者={ownerId}");
        }

        /// <summary>
        /// 获取范围内的菌毯数据
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>菌毯数据列表</returns>
        public List<CreepData> QueryCreepInRange(Vector3 position, float radius)
        {
            if (_spatialIndex != null)
            {
                // 使用空间索引查询
                return _spatialIndex.QueryRange(position, new Vector3(radius * 2, radius * 2, radius * 2));
            }
            
            // 如果没有空间索引，使用暴力搜索
            List<CreepData> creepInRange = new List<CreepData>();
            Vector2Int centerGrid = WorldToGridPosition(position);
            int gridRadius = Mathf.CeilToInt(radius / _gridSize);
            
            for (int x = -gridRadius; x <= gridRadius; x++)
            {
                for (int y = -gridRadius; y <= gridRadius; y++)
                {
                    Vector2Int gridPos = centerGrid + new Vector2Int(x, y);
                    
                    if (_creepGrid.TryGetValue(gridPos, out CreepData creepData))
                    {
                        if (Vector3.Distance(creepData.Position, position) <= radius)
                        {
                            creepInRange.Add(creepData);
                        }
                    }
                }
            }
            
            return creepInRange;
        }
        
        /// <summary>
        /// 获取范围内的菌毯数据（兼容旧API）
        /// </summary>
        /// <param name="position">中心位置</param>
        /// <param name="radius">半径</param>
        /// <returns>菌毯数据数组</returns>
        public CreepData[] GetCreepInRange(Vector3 position, float radius)
        {
            return QueryCreepInRange(position, radius).ToArray();
        }

        /// <summary>
        /// 清除指定所有者的所有菌毯
        /// </summary>
        /// <param name="ownerId">所有者ID</param>
        public void ClearCreepForPlayer(int ownerId)
        {
            List<Vector2Int> cellsToRemove = new List<Vector2Int>();
            
            foreach (var pair in _creepGrid)
            {
                if (pair.Value.OwnerId == ownerId)
                {
                    cellsToRemove.Add(pair.Key);
                }
            }
            
            foreach (var gridPos in cellsToRemove)
            {
                RemoveCreepAtPosition(gridPos);
            }
            
            // 清除玩家数据
            _playerCreepSources.Remove(ownerId);
            _creepNetworks.Remove(ownerId);
            
            Debug.Log($"[{_managerName}] 清除玩家菌毯: 所有者={ownerId}, 清除数量={cellsToRemove.Count}");
        }
        #endregion

        #region IManager接口实现
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized)
                return;
                
            Debug.Log($"[{_managerName}] 初始化菌毯管理器");
            
            // 初始化数据结构
            _creepGrid = new Dictionary<Vector2Int, CreepData>();
            _playerCreepSources = new Dictionary<int, List<Vector2Int>>();
            _creepNetworks = new Dictionary<int, CreepNetworkData>();
            _expansionQueue = new Queue<Vector2Int>();
            _activeCreepCells = new List<Vector2Int>();
            
            // 重置计时器
            _updateTimer = 0f;
            _currentUpdateIndex = 0;
            
            _isInitialized = true;
            Debug.Log($"[{_managerName}] 菌毯管理器初始化完成");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void Update()
        {
            if (!_isInitialized || _isPaused)
                return;
            
            _updateTimer += Time.deltaTime;
            
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                UpdateCreepSystem();
            }
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            _isPaused = true;
            Debug.Log($"[{_managerName}] 菌毯管理器已暂停");
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            _isPaused = false;
            Debug.Log($"[{_managerName}] 菌毯管理器已恢复");
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        public void FixedUpdate()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 在这里可以添加物理相关的更新逻辑
        }

        // 2) 顯式介面實作，滿足 IManager
        void IManager.Update(float deltaTime)
        {
            if (!_isInitialized || _isPaused) return;

            _updateTimer += deltaTime;
            if (_updateTimer >= _updateInterval)
            {
                _updateTimer = 0f;
                UpdateCreepSystem();
            }
        }

        void IManager.FixedUpdate(float fixedDeltaTime)
        {
            if (!_isInitialized || _isPaused) return;
            // 需要時處理物理相關更新
        }
        /// <summary>
        /// 后更新管理器
        /// </summary>
        public void LateUpdate()
        {
            if (!_isInitialized || _isPaused)
                return;
                
            // 在这里可以添加后更新逻辑
        }

        /// <summary>
        /// 清理管理器
        /// </summary>
        public void Cleanup()
        {
            if (!_isInitialized)
                return;
            
            Debug.Log($"[{_managerName}] 销毁菌毯管理器");
            
            // 清理数据
            _creepGrid?.Clear();
            _playerCreepSources?.Clear();
            _creepNetworks?.Clear();
            _expansionQueue?.Clear();
            _activeCreepCells?.Clear();
            
            _isInitialized = false;
            Debug.Log($"[{_managerName}] 菌毯管理器销毁完成");
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        public string GetManagerName()
        {
            return _managerName;
        }

        /// <summary>
        /// 检查管理器是否已初始化
        /// </summary>
        public bool IsInitialized()
        {
            return _isInitialized;
        }

        /// <summary>
        /// 检查管理器是否已暂停
        /// </summary>
        public bool IsPaused()
        {
            return _isPaused;
        }
        #endregion

        #region 私有方法
        /// <summary>
        /// 更新菌毯系统
        /// </summary>
        private void UpdateCreepSystem()
        {
            // 处理菌毯扩张
            ProcessCreepExpansion();
            
            // 处理菌毯衰减
            ProcessCreepDecay();
            
            // 更新菌毯网络
            UpdateCreepNetworks();
        }

        /// <summary>
        /// 处理菌毯扩张
        /// </summary>
        private void ProcessCreepExpansion()
        {
            int expansionsThisFrame = 0;
            
            while (_expansionQueue.Count > 0 && expansionsThisFrame < _maxExpansionsPerFrame)
            {
                Vector2Int sourceGrid = _expansionQueue.Dequeue();
                
                if (!_creepGrid.TryGetValue(sourceGrid, out CreepData sourceCreep))
                    continue;
                
                if (!sourceCreep.IsSource)
                    continue;
                
                // 向相邻格子扩张
                Vector2Int[] directions = {
                    Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                    Vector2Int.up + Vector2Int.left, Vector2Int.up + Vector2Int.right,
                    Vector2Int.down + Vector2Int.left, Vector2Int.down + Vector2Int.right
                };
                
                foreach (var direction in directions)
                {
                    Vector2Int targetGrid = sourceGrid + direction;
                    Vector3 targetWorldPos = GridToWorldPosition(targetGrid);
                    
                    // 检查距离是否在源点半径内
                    float distance = Vector3.Distance(sourceCreep.Position, targetWorldPos);
                    if (distance > sourceCreep.SourceRadius)
                        continue;
                    
                    // 计算扩张强度
                    float expansionStrength = _expansionRate * (1f - distance / sourceCreep.SourceRadius);
                    
                    // 扩张菌毯
                    ExpandCreepAtPosition(targetGrid, targetWorldPos, expansionStrength, sourceCreep.OwnerId);
                    
                    expansionsThisFrame++;
                    if (expansionsThisFrame >= _maxExpansionsPerFrame)
                        break;
                }
                
                // 重新加入队列以继续扩张
                _expansionQueue.Enqueue(sourceGrid);
            }
        }

        /// <summary>
        /// 处理菌毯衰减
        /// </summary>
        private void ProcessCreepDecay()
        {
            List<Vector2Int> cellsToRemove = new List<Vector2Int>();
            
            // 分帧处理活跃菌毯格子
            int cellsPerFrame = Mathf.Max(1, _activeCreepCells.Count / 10);
            int endIndex = Mathf.Min(_currentUpdateIndex + cellsPerFrame, _activeCreepCells.Count);
            
            for (int i = _currentUpdateIndex; i < endIndex; i++)
            {
                Vector2Int gridPos = _activeCreepCells[i];
                
                if (!_creepGrid.TryGetValue(gridPos, out CreepData creepData))
                {
                    cellsToRemove.Add(gridPos);
                    continue;
                }
                
                // 源点不衰减
                if (creepData.IsSource)
                    continue;
                
                // 计算衰减量
                float timeSinceUpdate = Time.time - creepData.LastUpdateTime;
                float decayAmount = _decayRate * timeSinceUpdate;
                
                // 应用衰减
                creepData.Density = Mathf.Max(0f, creepData.Density - decayAmount);
                creepData.LastUpdateTime = Time.time;
                
                if (creepData.Density <= _minDensity)
                {
                    cellsToRemove.Add(gridPos);
                }
                else
                {
                    _creepGrid[gridPos] = creepData;
                }
            }
            
            // 移除衰减完的菌毯
            foreach (var gridPos in cellsToRemove)
            {
                RemoveCreepAtPosition(gridPos);
            }
            
            // 更新索引
            _currentUpdateIndex = endIndex;
            if (_currentUpdateIndex >= _activeCreepCells.Count)
            {
                _currentUpdateIndex = 0;
            }
        }

        /// <summary>
        /// 更新菌毯网络
        /// </summary>
        private void UpdateCreepNetworks()
        {
            foreach (var networkPair in _creepNetworks)
            {
                int ownerId = networkPair.Key;
                CreepNetworkData networkData = networkPair.Value;
                
                // 计算总面积
                float totalArea = 0f;
                foreach (var pair in _creepGrid)
                {
                    if (pair.Value.OwnerId == ownerId)
                    {
                        totalArea += pair.Value.Density * _gridSize * _gridSize;
                    }
                }
                
                networkData.TotalArea = totalArea;
                
                // 计算网络效率（基于连接的源点数量和总面积）
                float sourceCount = networkData.ConnectedSources.Count;
                if (sourceCount > 0 && totalArea > 0)
                {
                    networkData.NetworkEfficiency = Mathf.Min(1.0f, totalArea / (sourceCount * 100f));
                }
                else
                {
                    networkData.NetworkEfficiency = 0f;
                }
                
                _creepNetworks[ownerId] = networkData;
            }
        }

        /// <summary>
        /// 在指定位置扩张菌毯
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        /// <param name="worldPos">世界位置</param>
        /// <param name="expansionStrength">扩张强度</param>
        /// <param name="ownerId">所有者ID</param>
        private void ExpandCreepAtPosition(Vector2Int gridPos, Vector3 worldPos, float expansionStrength, int ownerId)
        {
            if (_creepGrid.TryGetValue(gridPos, out CreepData existingCreep))
            {
                // 如果是同一所有者，增强密度
                if (existingCreep.OwnerId == ownerId)
                {
                    existingCreep.Density = Mathf.Min(_maxDensity, existingCreep.Density + expansionStrength);
                    existingCreep.LastUpdateTime = Time.time;
                    _creepGrid[gridPos] = existingCreep;
                }
                // 如果是敌方菌毯，进行竞争
                else
                {
                    float competitionResult = expansionStrength - existingCreep.Density * 0.5f;
                    if (competitionResult > 0)
                    {
                        // 覆盖敌方菌毯
                        existingCreep.OwnerId = ownerId;
                        existingCreep.Density = competitionResult;
                        existingCreep.LastUpdateTime = Time.time;
                        _creepGrid[gridPos] = existingCreep;
                    }
                }
            }
            else
            {
                // 创建新的菌毯
                CreepData newCreep = new CreepData
                {
                    Position = worldPos,
                    Density = expansionStrength,
                    OwnerId = ownerId,
                    IsSource = false,
                    SourceRadius = 0f,
                    LastUpdateTime = Time.time,
                    CreationTime = Time.time
                };
                
                _creepGrid[gridPos] = newCreep;
                _activeCreepCells.Add(gridPos);
                
                // 添加到空间索引
                if (_spatialIndex != null)
                {
                    _spatialIndex.Insert(newCreep, worldPos, Vector3.one * _gridSize);
                }
            }
        }

        /// <summary>
        /// 移除指定位置的菌毯
        /// </summary>
        /// <param name="gridPos">网格位置</param>
        private void RemoveCreepAtPosition(Vector2Int gridPos)
        {
            if (_creepGrid.TryGetValue(gridPos, out CreepData creepData))
            {
                // 从空间索引中移除
                if (_spatialIndex != null)
                {
                    // ★ 修正點：介面需要 obj + position + size
                    _spatialIndex.Remove(creepData, creepData.Position, Vector3.one * _gridSize);
                }
                
                // 从网格中移除
                _creepGrid.Remove(gridPos);
                _activeCreepCells.Remove(gridPos);
            }
        }

        /// <summary>
        /// 世界坐标转网格坐标
        /// </summary>
        /// <param name="worldPosition">世界坐标</param>
        /// <returns>网格坐标</returns>
        private Vector2Int WorldToGridPosition(Vector3 worldPosition)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPosition.x / _gridSize),
                Mathf.FloorToInt(worldPosition.z / _gridSize)
            );
        }

        /// <summary>
        /// 网格坐标转世界坐标
        /// </summary>
        /// <param name="gridPosition">网格坐标</param>
        /// <returns>世界坐标</returns>
        private Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * _gridSize + _gridSize * 0.5f,
                0f,
                gridPosition.y * _gridSize + _gridSize * 0.5f
            );
        }
        #endregion
    }
}
