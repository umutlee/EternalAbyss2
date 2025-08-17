using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Creep.Services;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯网络服务实现
    /// 负责菌毯网络的连接性分析和管理
    /// </summary>
    public class CreepNetworkService : ICreepNetworkService, IService
    {
        #region 私有字段

        private readonly Dictionary<int, CreepNetworkInfo> _networks;
        private readonly Dictionary<int, List<int>> _playerNetworks; // 玩家ID -> 网络ID列表
        private ICreepGridService _gridService;
        private int _nextNetworkId = 1;
        private float _lastAnalysisTime = 0f;
        private float _networkAnalysisInterval = 5f; // 网络分析间隔

        #endregion

        #region 属性

        public string ServiceName => "CreepNetworkService";
        public bool IsInitialized { get; private set; }

        #endregion

        #region 构造函数

        public CreepNetworkService(ICreepGridService gridService)
        {
            _gridService = gridService;
            _networks = new Dictionary<int, CreepNetworkInfo>();
            _playerNetworks = new Dictionary<int, List<int>>();
        }

        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (IsInitialized) return;

            _networks.Clear();
            _playerNetworks.Clear();
            _nextNetworkId = 1;
            _lastAnalysisTime = 0f;
            
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            _networks.Clear();
            _playerNetworks.Clear();
            
            IsInitialized = false;
        }

        #endregion

        #region ICreepNetworkService 实现

        public void AnalyzeNetworkConnectivity(int playerId)
        {
            if (!IsInitialized) return;

            // 清除该玩家的现有网络
            if (_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                foreach (int networkId in networkIds)
                {
                    _networks.Remove(networkId);
                }
                _playerNetworks[playerId] = new List<int>();
            }
            else
            {
                _playerNetworks[playerId] = new List<int>();
            }

            // 获取该玩家的所有活跃菌毯位置
            var activePositions = _gridService.GetActiveCreepPositions();
            var visited = new HashSet<Vector2Int>();
            var playerNetworks = new List<HashSet<Vector2Int>>();

            // 使用广度优先搜索找出所有连通区域
            for (int i = 0; i < activePositions.Length; i++)
            {
                Vector2Int pos = activePositions[i];
                if (visited.Contains(pos)) continue;

                CreepData data = _gridService.GetGridCell(pos);
                if (data.PlayerId != playerId) continue;

                // 找到一个新的连通区域
                var network = new HashSet<Vector2Int>();
                var queue = new Queue<Vector2Int>();
                queue.Enqueue(pos);
                visited.Add(pos);
                network.Add(pos);

                while (queue.Count > 0)
                {
                    Vector2Int current = queue.Dequeue();
                    var neighbors = _gridService.GetNeighborPositions(current, false);

                    foreach (var neighbor in neighbors)
                    {
                        if (visited.Contains(neighbor)) continue;
                        if (!_gridService.HasCreepAt(neighbor)) continue;

                        CreepData neighborData = _gridService.GetGridCell(neighbor);
                        if (neighborData.PlayerId != playerId) continue;

                        queue.Enqueue(neighbor);
                        visited.Add(neighbor);
                        network.Add(neighbor);
                    }
                }

                playerNetworks.Add(network);
            }

            activePositions.Dispose();

            // 为每个连通区域创建网络信息
            for (int i = 0; i < playerNetworks.Count; i++)
            {
                var network = playerNetworks[i];
                int networkId = _nextNetworkId++;
                
                // 计算网络中心
                Vector3 center = CalculateNetworkCenter(network);
                
                // 创建网络信息
                var networkInfo = new CreepNetworkInfo
                {
                    NetworkId = networkId,
                    PlayerId = playerId,
                    CenterPosition = center,
                    TotalArea = network.Count * _gridService.GridCellSize * _gridService.GridCellSize,
                    SourceCount = 0, // 需要从源点服务获取
                    IsConnectedToMain = i == 0 // 假设第一个网络是主网络
                };

                _networks[networkId] = networkInfo;
                _playerNetworks[playerId].Add(networkId);
            }
        }

        public CreepNetworkInfo GetNetworkInfo(Vector3 position)
        {
            Vector2Int gridPos = _gridService.WorldToGridPosition(position);
            
            if (!_gridService.HasCreepAt(gridPos))
                return default;

            CreepData data = _gridService.GetGridCell(gridPos);
            int playerId = data.PlayerId;

            // 如果该玩家没有网络或者网络分析过期，重新分析
            if (!_playerNetworks.ContainsKey(playerId) || 
                Time.time - _lastAnalysisTime > _networkAnalysisInterval)
            {
                AnalyzeNetworkConnectivity(playerId);
                _lastAnalysisTime = Time.time;
            }

            // 找到包含该位置的网络
            if (_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                foreach (int networkId in networkIds)
                {
                    if (IsPositionInNetwork(position, networkId))
                    {
                        return _networks[networkId];
                    }
                }
            }

            return default;
        }

        public NativeArray<CreepNetworkInfo> GetPlayerNetworks(int playerId)
        {
            // 如果网络分析过期，重新分析
            if (!_playerNetworks.ContainsKey(playerId) || 
                Time.time - _lastAnalysisTime > _networkAnalysisInterval)
            {
                AnalyzeNetworkConnectivity(playerId);
                _lastAnalysisTime = Time.time;
            }

            if (!_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                return new NativeArray<CreepNetworkInfo>(0, Allocator.Temp);
            }

            var networks = new NativeArray<CreepNetworkInfo>(networkIds.Count, Allocator.Temp);
            for (int i = 0; i < networkIds.Count; i++)
            {
                networks[i] = _networks[networkIds[i]];
            }

            return networks;
        }

        public bool IsConnected(Vector3 start, Vector3 end, float minStrength = 0.1f)
        {
            Vector2Int startGrid = _gridService.WorldToGridPosition(start);
            Vector2Int endGrid = _gridService.WorldToGridPosition(end);

            if (!_gridService.HasCreepAt(startGrid) || !_gridService.HasCreepAt(endGrid))
                return false;

            CreepData startData = _gridService.GetGridCell(startGrid);
            CreepData endData = _gridService.GetGridCell(endGrid);

            if (startData.PlayerId != endData.PlayerId)
                return false;

            if (startData.Strength < minStrength || endData.Strength < minStrength)
                return false;

            // 使用广度优先搜索检查连通性
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            queue.Enqueue(startGrid);
            visited.Add(startGrid);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                
                if (current == endGrid)
                    return true;

                var neighbors = _gridService.GetNeighborPositions(current, false);
                foreach (var neighbor in neighbors)
                {
                    if (visited.Contains(neighbor)) continue;
                    if (!_gridService.HasCreepAt(neighbor)) continue;

                    CreepData neighborData = _gridService.GetGridCell(neighbor);
                    if (neighborData.PlayerId != startData.PlayerId || neighborData.Strength < minStrength)
                        continue;

                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }

            return false;
        }

        public Vector3[] FindConnectionPath(Vector3 start, Vector3 end, float minStrength = 0.1f)
        {
            Vector2Int startGrid = _gridService.WorldToGridPosition(start);
            Vector2Int endGrid = _gridService.WorldToGridPosition(end);

            if (!_gridService.HasCreepAt(startGrid) || !_gridService.HasCreepAt(endGrid))
                return new Vector3[0];

            CreepData startData = _gridService.GetGridCell(startGrid);
            CreepData endData = _gridService.GetGridCell(endGrid);

            if (startData.PlayerId != endData.PlayerId)
                return new Vector3[0];

            // 使用A*算法寻找路径
            var path = FindPath(startGrid, endGrid, startData.PlayerId, minStrength);
            var worldPath = new Vector3[path.Count];

            for (int i = 0; i < path.Count; i++)
            {
                worldPath[i] = _gridService.GridToWorldPosition(path[i]);
            }

            return worldPath;
        }

        public int MergeNetworks(int networkId1, int networkId2)
        {
            if (!_networks.TryGetValue(networkId1, out CreepNetworkInfo network1) ||
                !_networks.TryGetValue(networkId2, out CreepNetworkInfo network2))
            {
                return -1;
            }

            if (network1.PlayerId != network2.PlayerId)
                return -1;

            int playerId = network1.PlayerId;
            
            // 创建新的合并网络
            int mergedNetworkId = _nextNetworkId++;
            var mergedNetwork = new CreepNetworkInfo
            {
                NetworkId = mergedNetworkId,
                PlayerId = playerId,
                CenterPosition = (network1.CenterPosition + network2.CenterPosition) / 2f,
                TotalArea = network1.TotalArea + network2.TotalArea,
                SourceCount = network1.SourceCount + network2.SourceCount,
                IsConnectedToMain = network1.IsConnectedToMain || network2.IsConnectedToMain
            };

            _networks[mergedNetworkId] = mergedNetwork;
            
            // 更新玩家网络列表
            if (_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                networkIds.Remove(networkId1);
                networkIds.Remove(networkId2);
                networkIds.Add(mergedNetworkId);
            }

            // 移除旧网络
            _networks.Remove(networkId1);
            _networks.Remove(networkId2);

            return mergedNetworkId;
        }

        public int[] SplitNetwork(int networkId, Vector3 splitPosition)
        {
            // 网络分割需要重新分析连通性
            if (!_networks.TryGetValue(networkId, out CreepNetworkInfo network))
                return new int[0];

            int playerId = network.PlayerId;
            
            // 移除旧网络
            _networks.Remove(networkId);
            if (_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                networkIds.Remove(networkId);
            }

            // 重新分析连通性
            AnalyzeNetworkConnectivity(playerId);
            _lastAnalysisTime = Time.time;

            // 返回新的网络ID
            if (_playerNetworks.TryGetValue(playerId, out List<int> newNetworkIds))
            {
                return newNetworkIds.ToArray();
            }

            return new int[0];
        }

        public NativeArray<Vector3> GetNetworkBoundary(int networkId)
        {
            if (!_networks.TryGetValue(networkId, out CreepNetworkInfo network))
                return new NativeArray<Vector3>(0, Allocator.Temp);

            var boundary = new List<Vector3>();
            var activePositions = _gridService.GetActiveCreepPositions();

            for (int i = 0; i < activePositions.Length; i++)
            {
                Vector2Int gridPos = activePositions[i];
                CreepData data = _gridService.GetGridCell(gridPos);
                
                if (data.PlayerId != network.PlayerId)
                    continue;

                // 检查是否是边界（至少有一个邻居没有菌毯）
                var neighbors = _gridService.GetNeighborPositions(gridPos, false);
                bool isBoundary = false;
                
                foreach (var neighbor in neighbors)
                {
                    if (!_gridService.HasCreepAt(neighbor) || !_gridService.IsValidGridPosition(neighbor))
                    {
                        isBoundary = true;
                        break;
                    }
                }

                if (isBoundary)
                {
                    boundary.Add(_gridService.GridToWorldPosition(gridPos));
                }
            }

            activePositions.Dispose();

            var result = new NativeArray<Vector3>(boundary.Count, Allocator.Temp);
            for (int i = 0; i < boundary.Count; i++)
            {
                result[i] = boundary[i];
            }

            return result;
        }

        public Vector3 GetNetworkCenter(int networkId)
        {
            if (_networks.TryGetValue(networkId, out CreepNetworkInfo network))
            {
                return network.CenterPosition;
            }
            return Vector3.zero;
        }

        public float GetNetworkArea(int networkId)
        {
            if (_networks.TryGetValue(networkId, out CreepNetworkInfo network))
            {
                return network.TotalArea;
            }
            return 0f;
        }

        public bool IsNetworkIsolated(int networkId)
        {
            if (_networks.TryGetValue(networkId, out CreepNetworkInfo network))
            {
                return !network.IsConnectedToMain;
            }
            return true;
        }

        public NativeArray<CreepNetworkInfo> GetIsolatedNetworks(int playerId)
        {
            var isolatedNetworks = new List<CreepNetworkInfo>();

            if (_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                foreach (int networkId in networkIds)
                {
                    if (_networks.TryGetValue(networkId, out CreepNetworkInfo network) && !network.IsConnectedToMain)
                    {
                        isolatedNetworks.Add(network);
                    }
                }
            }

            var result = new NativeArray<CreepNetworkInfo>(isolatedNetworks.Count, Allocator.Temp);
            for (int i = 0; i < isolatedNetworks.Count; i++)
            {
                result[i] = isolatedNetworks[i];
            }

            return result;
        }

        public bool RepairNetworkConnection(int networkId, int targetNetworkId)
        {
            if (!_networks.TryGetValue(networkId, out CreepNetworkInfo network) ||
                !_networks.TryGetValue(targetNetworkId, out CreepNetworkInfo targetNetwork))
            {
                return false;
            }

            if (network.PlayerId != targetNetwork.PlayerId)
                return false;

            // 合并网络
            MergeNetworks(networkId, targetNetworkId);
            return true;
        }

        public void OptimizeNetworkStructure(int playerId)
        {
            // 优化网络结构，合并可能的网络
            if (!_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
                return;

            bool anyMerged = false;
            for (int i = 0; i < networkIds.Count; i++)
            {
                for (int j = i + 1; j < networkIds.Count; j++)
                {
                    if (CanMergeNetworks(networkIds[i], networkIds[j]))
                    {
                        MergeNetworks(networkIds[i], networkIds[j]);
                        anyMerged = true;
                        break;
                    }
                }
                if (anyMerged) break;
            }

            if (anyMerged)
            {
                // 如果有合并，重新分析连通性
                AnalyzeNetworkConnectivity(playerId);
                _lastAnalysisTime = Time.time;
            }
        }

        public void UpdateNetworks(float deltaTime)
        {
            if (!IsInitialized)
                return;

            // 定期重新分析网络连通性
            if (Time.time - _lastAnalysisTime > _networkAnalysisInterval)
            {
                foreach (int playerId in _playerNetworks.Keys)
                {
                    AnalyzeNetworkConnectivity(playerId);
                }
                _lastAnalysisTime = Time.time;
            }
        }

        public CreepNetworkStatistics GetNetworkStatistics(int playerId)
        {
            var stats = new CreepNetworkStatistics();

            if (!_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
            {
                return stats;
            }

            stats.TotalNetworks = networkIds.Count;
            stats.ConnectedNetworks = 0;
            stats.IsolatedNetworks = 0;
            stats.TotalNetworkArea = 0f;
            stats.TotalConnectionPoints = 0;

            foreach (int networkId in networkIds)
            {
                if (_networks.TryGetValue(networkId, out CreepNetworkInfo network))
                {
                    if (network.IsConnectedToMain)
                    {
                        stats.ConnectedNetworks++;
                    }
                    else
                    {
                        stats.IsolatedNetworks++;
                    }

                    stats.TotalNetworkArea += network.TotalArea;
                }
            }

            stats.AverageNetworkSize = stats.TotalNetworks > 0 ? stats.TotalNetworkArea / stats.TotalNetworks : 0f;
            stats.NetworkConnectivity = stats.TotalNetworks > 0 ? (float)stats.ConnectedNetworks / stats.TotalNetworks : 0f;

            return stats;
        }

        public void CleanupInvalidNetworks()
        {
            var invalidNetworks = new List<int>();

            foreach (var kvp in _networks)
            {
                int networkId = kvp.Key;
                CreepNetworkInfo network = kvp.Value;
                
                // 检查网络是否有效（例如面积为0）
                if (network.TotalArea <= 0f)
                {
                    invalidNetworks.Add(networkId);
                }
            }

            foreach (int networkId in invalidNetworks)
            {
                if (_networks.TryGetValue(networkId, out CreepNetworkInfo network))
                {
                    int playerId = network.PlayerId;
                    _networks.Remove(networkId);
                    
                    if (_playerNetworks.TryGetValue(playerId, out List<int> networkIds))
                    {
                        networkIds.Remove(networkId);
                    }
                }
            }
        }

        public void RebuildNetworkIndex()
        {
            // 重建所有网络索引
            _playerNetworks.Clear();
            
            foreach (var kvp in _networks)
            {
                int networkId = kvp.Key;
                CreepNetworkInfo network = kvp.Value;
                int playerId = network.PlayerId;
                
                if (!_playerNetworks.ContainsKey(playerId))
                {
                    _playerNetworks[playerId] = new List<int>();
                }
                
                _playerNetworks[playerId].Add(networkId);
            }
        }

        public CreepNetworkIntegrityReport CheckNetworkIntegrity(int networkId)
        {
            if (!_networks.TryGetValue(networkId, out CreepNetworkInfo network))
            {
                return new CreepNetworkIntegrityReport
                {
                    NetworkId = networkId,
                    IsIntact = false,
                    BrokenConnections = 0,
                    WeakConnections = 0,
                    CriticalPoints = new Vector3[0],
                    OverallHealth = 0f
                };
            }

            // 分析网络完整性
            var criticalPoints = new List<Vector3>();
            int brokenConnections = 0;
            int weakConnections = 0;
            
            // 简化实现，实际应该分析网络结构
            var boundary = GetNetworkBoundary(networkId);
            float overallHealth = 1.0f;
            
            var report = new CreepNetworkIntegrityReport
            {
                NetworkId = networkId,
                IsIntact = brokenConnections == 0,
                BrokenConnections = brokenConnections,
                WeakConnections = weakConnections,
                CriticalPoints = criticalPoints.ToArray(),
                OverallHealth = overallHealth
            };
            
            boundary.Dispose();
            return report;
        }

        #endregion

        #region 私有方法

        private Vector3 CalculateNetworkCenter(HashSet<Vector2Int> network)
        {
            if (network.Count == 0)
                return Vector3.zero;

            Vector2 sum = Vector2.zero;
            foreach (var pos in network)
            {
                sum += new Vector2(pos.x, pos.y);
            }

            Vector2 center = sum / network.Count;
            return _gridService.GridToWorldPosition(new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y)));
        }

        private bool IsPositionInNetwork(Vector3 position, int networkId)
        {
            if (!_networks.TryGetValue(networkId, out CreepNetworkInfo network))
                return false;

            Vector2Int gridPos = _gridService.WorldToGridPosition(position);
            
            if (!_gridService.HasCreepAt(gridPos))
                return false;

            CreepData data = _gridService.GetGridCell(gridPos);
            if (data.PlayerId != network.PlayerId)
                return false;

            // 使用广度优先搜索检查是否在同一个连通区域
            Vector2Int networkCenterGrid = _gridService.WorldToGridPosition(network.CenterPosition);
            
            return IsConnected(position, network.CenterPosition, 0.1f);
        }

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, int playerId, float minStrength)
        {
            var path = new List<Vector2Int>();
            var openSet = new List<Vector2Int>();
            var closedSet = new HashSet<Vector2Int>();
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            var gScore = new Dictionary<Vector2Int, float>();
            var fScore = new Dictionary<Vector2Int, float>();

            openSet.Add(start);
            gScore[start] = 0;
            fScore[start] = Vector2Int.Distance(start, end);

            while (openSet.Count > 0)
            {
                Vector2Int current = GetLowestFScore(openSet, fScore);
                
                if (current == end)
                {
                    // 重建路径
                    path.Add(current);
                    while (cameFrom.ContainsKey(current))
                    {
                        current = cameFrom[current];
                        path.Insert(0, current);
                    }
                    return path;
                }

                openSet.Remove(current);
                closedSet.Add(current);

                var neighbors = _gridService.GetNeighborPositions(current, false);
                foreach (var neighbor in neighbors)
                {
                    if (closedSet.Contains(neighbor)) continue;
                    if (!_gridService.HasCreepAt(neighbor)) continue;

                    CreepData neighborData = _gridService.GetGridCell(neighbor);
                    if (neighborData.PlayerId != playerId || neighborData.Strength < minStrength)
                        continue;

                    float tentativeGScore = gScore[current] + 1;
                    
                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                    else if (tentativeGScore >= gScore.GetValueOrDefault(neighbor, float.MaxValue))
                    {
                        continue;
                    }

                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeGScore;
                    fScore[neighbor] = tentativeGScore + Vector2Int.Distance(neighbor, end);
                }
            }

            return path; // 没有找到路径
        }

        private Vector2Int GetLowestFScore(List<Vector2Int> openSet, Dictionary<Vector2Int, float> fScore)
        {
            Vector2Int lowest = openSet[0];
            float lowestScore = fScore.GetValueOrDefault(lowest, float.MaxValue);

            for (int i = 1; i < openSet.Count; i++)
            {
                float score = fScore.GetValueOrDefault(openSet[i], float.MaxValue);
                if (score < lowestScore)
                {
                    lowest = openSet[i];
                    lowestScore = score;
                }
            }

            return lowest;
        }

        private bool CanMergeNetworks(int networkId1, int networkId2)
        {
            if (!_networks.TryGetValue(networkId1, out CreepNetworkInfo network1) ||
                !_networks.TryGetValue(networkId2, out CreepNetworkInfo network2))
            {
                return false;
            }

            if (network1.PlayerId != network2.PlayerId)
                return false;

            // 检查两个网络是否相邻
            float distance = Vector3.Distance(network1.CenterPosition, network2.CenterPosition);
            return distance < (network1.TotalArea + network2.TotalArea) * 0.1f; // 简化判断
        }

        #endregion
    }
}