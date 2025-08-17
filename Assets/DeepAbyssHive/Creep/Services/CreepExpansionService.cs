using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Creep.Data;
using DeepAbyssHive.Creep.Enums;
using DeepAbyssHive.Creep.Config;

namespace DeepAbyssHive.Creep.Services
{
    /// <summary>
    /// 菌毯扩张服务实现
    /// 负责菌毯的自动和手动扩张逻辑
    /// </summary>
    public class CreepExpansionService : ICreepExpansionService, IService, ICommandService
    {
        #region 私有字段

        private ICreepGridService _gridService;
        private ICreepSourceService _sourceService;
        private ICreepNetworkService _networkService;
        private CreepConfigSO _config;

        private readonly Dictionary<int, CreepExpansionRequest> _expansionRequests;
        private readonly Dictionary<Vector3, bool> _autoExpansionSources;
        private int _nextRequestId = 1;

        private float _expansionRate = 1f;
        private float _expansionThreshold = 0.8f;
        private bool _autoExpansionEnabled = true;
        private bool _isPaused = false;

        #endregion

        #region 属性

        public float ExpansionRate 
        { 
            get => _expansionRate; 
            set => _expansionRate = Mathf.Max(0f, value); 
        }

        public float ExpansionThreshold 
        { 
            get => _expansionThreshold; 
            set => _expansionThreshold = Mathf.Clamp01(value); 
        }

        public bool AutoExpansionEnabled 
        { 
            get => _autoExpansionEnabled; 
            set => _autoExpansionEnabled = value; 
        }

        public string ServiceName => "CreepExpansionService";
        public bool IsInitialized { get; private set; }
        public bool IsCommandAvailable => IsInitialized;

        #endregion

        #region 构造函数

        public CreepExpansionService(
            ICreepGridService gridService,
            ICreepSourceService sourceService,
            ICreepNetworkService networkService)
        {
            _gridService = gridService;
            _sourceService = sourceService;
            _networkService = networkService;
            
            _expansionRequests = new Dictionary<int, CreepExpansionRequest>();
            _autoExpansionSources = new Dictionary<Vector3, bool>();
        }

        #endregion

        #region IService 实现

        public void Initialize()
        {
            if (IsInitialized) return;

            LoadConfiguration();
            _expansionRequests.Clear();
            _autoExpansionSources.Clear();
            
            IsInitialized = true;
        }

        public void Cleanup()
        {
            if (!IsInitialized) return;

            _expansionRequests.Clear();
            _autoExpansionSources.Clear();
            
            IsInitialized = false;
        }

        #endregion

        #region ICreepExpansionService 实现

        public void StartAutoExpansion(Vector3 sourcePosition, int playerId)
        {
            _autoExpansionSources[sourcePosition] = true;
        }

        public void StopAutoExpansion(Vector3 sourcePosition)
        {
            _autoExpansionSources.Remove(sourcePosition);
        }

        public bool ExpandToPosition(Vector3 targetPosition, int playerId, CreepExpansionType expansionType = CreepExpansionType.Normal)
        {
            if (!CanExpandToPosition(targetPosition, playerId))
                return false;

            // 找到最近的菌毯源点
            var nearestSource = _sourceService.GetNearestCreepSource(targetPosition, playerId);
            if (nearestSource.SourceId == 0)
                return false;

            // 创建扩张请求
            var request = new CreepExpansionRequest
            {
                RequestId = _nextRequestId++,
                PlayerId = playerId,
                SourcePosition = nearestSource.Position,
                TargetPosition = targetPosition,
                ExpansionType = expansionType,
                Priority = GetExpansionPriority(nearestSource.Position, targetPosition, expansionType),
                StartTime = Time.time,
                EstimatedDuration = CalculateExpansionDuration(nearestSource.Position, targetPosition, expansionType),
                IsActive = true
            };

            _expansionRequests[request.RequestId] = request;
            return true;
        }

        public bool ExpandToArea(Vector3 center, float radius, int playerId, CreepExpansionType expansionType = CreepExpansionType.Normal)
        {
            // 在区域内创建多个扩张点
            int pointCount = Mathf.CeilToInt(radius * 2f);
            bool anySuccess = false;

            for (int i = 0; i < pointCount; i++)
            {
                float angle = (float)i / pointCount * 2f * Mathf.PI;
                Vector3 targetPos = center + new Vector3(
                    Mathf.Cos(angle) * radius,
                    0f,
                    Mathf.Sin(angle) * radius
                );

                if (ExpandToPosition(targetPos, playerId, expansionType))
                {
                    anySuccess = true;
                }
            }

            return anySuccess;
        }

        public Vector3[] CalculateExpansionPath(Vector3 from, Vector3 to, int playerId)
        {
            // 简化的路径计算 - 直线路径
            var path = new List<Vector3>();
            
            float distance = Vector3.Distance(from, to);
            int stepCount = Mathf.CeilToInt(distance / _gridService.GridCellSize);
            
            for (int i = 0; i <= stepCount; i++)
            {
                float t = (float)i / stepCount;
                Vector3 point = Vector3.Lerp(from, to, t);
                path.Add(point);
            }

            return path.ToArray();
        }

        public bool CanExpandToPosition(Vector3 position, int playerId)
        {
            Vector2Int gridPos = _gridService.WorldToGridPosition(position);
            
            // 检查位置是否有效
            if (!_gridService.IsValidGridPosition(gridPos))
                return false;

            // 检查是否已有菌毯
            if (_gridService.HasCreepAt(gridPos))
                return false;

            // 检查是否有相邻的菌毯
            var neighbors = _gridService.GetNeighborPositions(gridPos, false);
            foreach (var neighbor in neighbors)
            {
                if (_gridService.HasCreepAt(neighbor))
                {
                    CreepData neighborData = _gridService.GetGridCell(neighbor);
                    if (neighborData.PlayerId == playerId && neighborData.Strength >= _expansionThreshold)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public float GetExpansionCost(Vector3 from, Vector3 to, CreepExpansionType expansionType)
        {
            float distance = Vector3.Distance(from, to);
            float baseCost = distance * 10f; // 基础成本

            // 根据扩张类型调整成本
            switch (expansionType)
            {
                case CreepExpansionType.Fast:
                    return baseCost * 1.5f;
                case CreepExpansionType.Reinforced:
                    return baseCost * 2f;
                case CreepExpansionType.Normal:
                default:
                    return baseCost;
            }
        }

        public NativeArray<Vector3> GetExpansionFront(int playerId)
        {
            var frontPositions = new List<Vector3>();
            var activePositions = _gridService.GetActiveCreepPositions();

            for (int i = 0; i < activePositions.Length; i++)
            {
                Vector2Int gridPos = activePositions[i];
                CreepData data = _gridService.GetGridCell(gridPos);
                
                if (data.PlayerId == playerId)
                {
                    // 检查是否是前沿（有空的邻居）
                    var neighbors = _gridService.GetNeighborPositions(gridPos, false);
                    foreach (var neighbor in neighbors)
                    {
                        if (!_gridService.HasCreepAt(neighbor) && _gridService.IsValidGridPosition(neighbor))
                        {
                            frontPositions.Add(_gridService.GridToWorldPosition(gridPos));
                            break;
                        }
                    }
                }
            }

            activePositions.Dispose();

            var result = new NativeArray<Vector3>(frontPositions.Count, Allocator.Temp);
            for (int i = 0; i < frontPositions.Count; i++)
            {
                result[i] = frontPositions[i];
            }

            return result;
        }

        public void UpdateExpansion(float deltaTime)
        {
            if (!IsInitialized || _isPaused)
                return;

            // 更新自动扩张
            if (_autoExpansionEnabled)
            {
                UpdateAutoExpansion(deltaTime);
            }

            // 更新手动扩张请求
            UpdateExpansionRequests(deltaTime);
        }

        public void AddExpansionRequest(CreepExpansionRequest request)
        {
            request.RequestId = _nextRequestId++;
            _expansionRequests[request.RequestId] = request;
        }

        public void RemoveExpansionRequest(int requestId)
        {
            _expansionRequests.Remove(requestId);
        }

        public NativeArray<CreepExpansionRequest> GetActiveExpansionRequests(int playerId)
        {
            var activeRequests = new List<CreepExpansionRequest>();
            
            foreach (var request in _expansionRequests.Values)
            {
                if (request.PlayerId == playerId && request.IsActive)
                {
                    activeRequests.Add(request);
                }
            }

            var result = new NativeArray<CreepExpansionRequest>(activeRequests.Count, Allocator.Temp);
            for (int i = 0; i < activeRequests.Count; i++)
            {
                result[i] = activeRequests[i];
            }

            return result;
        }

        public void PauseAllExpansion()
        {
            _isPaused = true;
        }

        public void ResumeAllExpansion()
        {
            _isPaused = false;
        }

        public CreepExpansionStatistics GetExpansionStatistics(int playerId)
        {
            var stats = new CreepExpansionStatistics();
            
            foreach (var request in _expansionRequests.Values)
            {
                if (request.PlayerId == playerId)
                {
                    if (request.IsActive)
                    {
                        stats.ActiveRequests++;
                        stats.TotalExpansionCost += GetExpansionCost(
                            request.SourcePosition, 
                            request.TargetPosition, 
                            request.ExpansionType);
                    }
                    else
                    {
                        stats.CompletedRequests++;
                    }
                }
            }

            // 计算扩张面积和速度
            var frontPositions = GetExpansionFront(playerId);
            stats.TotalExpansionArea = frontPositions.Length * _gridService.GridCellSize * _gridService.GridCellSize;
            stats.AverageExpansionRate = _expansionRate;
            frontPositions.Dispose();

            return stats;
        }

        #endregion

        #region 私有方法

        private void LoadConfiguration()
        {
            if (_config != null)
            {
                _expansionRate = _config.expansionRate;
                _expansionThreshold = _config.expansionThreshold;
            }
        }

        private void UpdateAutoExpansion(float deltaTime)
        {
            foreach (var kvp in _autoExpansionSources)
            {
                if (kvp.Value) // 如果启用自动扩张
                {
                    Vector3 sourcePos = kvp.Key;
                    // 实现自动扩张逻辑
                    ProcessAutoExpansion(sourcePos, deltaTime);
                }
            }
        }

        private void ProcessAutoExpansion(Vector3 sourcePosition, float deltaTime)
        {
            Vector2Int sourceGrid = _gridService.WorldToGridPosition(sourcePosition);
            var neighbors = _gridService.GetNeighborPositions(sourceGrid, false);

            foreach (var neighbor in neighbors)
            {
                if (!_gridService.HasCreepAt(neighbor) && _gridService.IsValidGridPosition(neighbor))
                {
                    // 尝试扩张到这个位置
                    Vector3 targetPos = _gridService.GridToWorldPosition(neighbor);
                    // 这里应该根据扩张速度和时间来决定是否扩张
                    if (Random.value < _expansionRate * deltaTime * 0.1f)
                    {
                        CreateCreepAtPosition(neighbor, sourcePosition);
                    }
                }
            }
        }

        private void UpdateExpansionRequests(float deltaTime)
        {
            var completedRequests = new List<int>();

            foreach (var kvp in _expansionRequests)
            {
                var request = kvp.Value;
                if (!request.IsActive)
                    continue;

                // 检查扩张是否完成
                if (Time.time - request.StartTime >= request.EstimatedDuration)
                {
                    // 完成扩张
                    Vector2Int targetGrid = _gridService.WorldToGridPosition(request.TargetPosition);
                    CreateCreepAtPosition(targetGrid, request.SourcePosition);
                    
                    completedRequests.Add(request.RequestId);
                }
            }

            // 移除已完成的请求
            foreach (int requestId in completedRequests)
            {
                _expansionRequests.Remove(requestId);
            }
        }

        private void CreateCreepAtPosition(Vector2Int gridPosition, Vector3 sourcePosition)
        {
            // 创建新的菌毯数据
            var creepData = new CreepData
            {
                Position = _gridService.GridToWorldPosition(gridPosition),
                Strength = 0.5f, // 初始强度
                Density = 0.5f,
                PlayerId = GetPlayerIdFromSource(sourcePosition),
                CreationTime = Time.time,
                LastUpdateTime = Time.time
            };

            _gridService.SetGridCell(gridPosition, creepData);
        }

        private int GetPlayerIdFromSource(Vector3 sourcePosition)
        {
            var source = _sourceService.GetNearestCreepSource(sourcePosition);
            return source.PlayerId;
        }

        private float GetExpansionPriority(Vector3 from, Vector3 to, CreepExpansionType expansionType)
        {
            float distance = Vector3.Distance(from, to);
            float basePriority = 1f / (distance + 1f); // 距离越近优先级越高

            switch (expansionType)
            {
                case CreepExpansionType.Fast:
                    return basePriority * 1.5f;
                case CreepExpansionType.Reinforced:
                    return basePriority * 0.8f;
                case CreepExpansionType.Normal:
                default:
                    return basePriority;
            }
        }

        private float CalculateExpansionDuration(Vector3 from, Vector3 to, CreepExpansionType expansionType)
        {
            float distance = Vector3.Distance(from, to);
            float baseDuration = distance / (_expansionRate * _gridService.GridCellSize);

            switch (expansionType)
            {
                case CreepExpansionType.Fast:
                    return baseDuration * 0.5f;
                case CreepExpansionType.Reinforced:
                    return baseDuration * 2f;
                case CreepExpansionType.Normal:
                default:
                    return baseDuration;
            }
        }

        #endregion
    }
}