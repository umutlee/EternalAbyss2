using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.SpatialIndex.Enums;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Implementations;
using SIRaycastHit = DeepAbyssHive.SpatialIndex.Data.RaycastHit;
using URaycastHit = UnityEngine.RaycastHit;

namespace DeepAbyssHive.SpatialIndex.Services
{
    /// <summary>
    /// 空间索引服务实现
    /// 提供高效的空间查询和管理功能
    /// </summary>
    public partial class SpatialIndexService : ISpatialIndexService, IUpdatableService, IService
    {
        // 空间索引实例
        private ISpatialIndex _spatialIndex;
        private Dictionary<string, ISpatialIndex> _categoryIndices;
        
        // 对象管理
        private Dictionary<int, SpatialNode> _allNodes;
        private Queue<SpatialNode> _pendingInserts;
        private Queue<SpatialNode> _pendingUpdates;
        private Queue<SpatialNode> _pendingRemovals;
        
        // 性能统计
        private int _totalQueries = 0;
        private float _totalQueryTime = 0f;
        private int _frameQueries = 0;
        
        // 配置参数
        private Bounds _worldBounds;
        private int _maxDepth;
        private int _maxObjectsPerNode;
        private bool _useOctree;
        private int _batchSize;

        public bool IsInitialized { get; private set; }
        public string ServiceName => "SpatialIndexService";

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(Bounds worldBounds, int maxDepth = 8, int maxObjectsPerNode = 10, bool useOctree = true, int batchSize = 100)
        {
            if (IsInitialized) return;

            _worldBounds = worldBounds;
            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;
            _useOctree = useOctree;
            _batchSize = batchSize;

            // 创建主空间索引
            if (_useOctree)
            {
                _spatialIndex = new OctreeSpatialIndex(_worldBounds, _maxDepth, _maxObjectsPerNode);
            }
            else
            {
                _spatialIndex = new QuadTreeSpatialIndex(_worldBounds, _maxDepth, _maxObjectsPerNode);
            }

            // 初始化数据结构
            _categoryIndices = new Dictionary<string, ISpatialIndex>();
            _allNodes = new Dictionary<int, SpatialNode>();
            _pendingInserts = new Queue<SpatialNode>();
            _pendingUpdates = new Queue<SpatialNode>();
            _pendingRemovals = new Queue<SpatialNode>();

            IsInitialized = true;
        }

        /// <summary>
        /// 添加对象到空间索引
        /// </summary>
        public bool AddObject(int objectId, Vector3 position, Bounds bounds, SpatialObjectType objectType)
        {
            if (!IsInitialized) return false;

            // 創建SpatialNode，使用現有的構造函數
            var node = new SpatialNode(objectId, null, position, bounds, objectType.ToString(), 0, false);

            _pendingInserts.Enqueue(node);
            return true;
        }

        /// <summary>
        /// 从空间索引移除对象
        /// </summary>
        public bool RemoveObject(int objectId)
        {
            if (!IsInitialized || !_allNodes.ContainsKey(objectId)) return false;

            var node = _allNodes[objectId];
            _pendingRemovals.Enqueue(node);
            return true;
        }

        /// <summary>
        /// 更新对象位置
        /// </summary>
        public bool UpdateObject(int objectId, Vector3 newPosition, Bounds? newBounds = null)
        {
            if (!IsInitialized || !_allNodes.ContainsKey(objectId)) return false;

            var node = _allNodes[objectId];
            node.UpdatePosition(newPosition);
            if (newBounds.HasValue)
            {
                node.UpdateBounds(newBounds.Value);
            }
            
            _pendingUpdates.Enqueue(node);
            return true;
        }

        /// <summary>
        /// 查询指定范围内的对象
        /// </summary>
        public NativeArray<int> QueryRange(Vector3 center, float radius, SpatialObjectType objectType = SpatialObjectType.All)
        {
            if (!IsInitialized)
            {
                return new NativeArray<int>(0, Allocator.Temp);
            }

            var startTime = Time.realtimeSinceStartup;
            
            var querySize = new Vector3(radius * 2, radius * 2, radius * 2);
            var results = _spatialIndex.QueryRange(center, querySize);
            
            // 过滤类型和距离
            var filteredResults = new List<int>();
            foreach (var node in results)
            {
                // 比較字符串類型
                if (objectType != SpatialObjectType.All && !node.Category.Equals(objectType.ToString(), System.StringComparison.OrdinalIgnoreCase))
                    continue;
                    
                if (Vector3.Distance(center, node.Position) <= radius)
                {
                    filteredResults.Add(node.Id);
                }
            }

            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            var resultArray = new NativeArray<int>(filteredResults.Count, Allocator.Temp);
            for (int i = 0; i < filteredResults.Count; i++)
            {
                resultArray[i] = filteredResults[i];
            }
            
            return resultArray;
        }

        /// <summary>
        /// 查询指定边界内的对象
        /// </summary>
        public NativeArray<int> QueryBounds(Bounds bounds, SpatialObjectType objectType = SpatialObjectType.All)
        {
            if (!IsInitialized)
            {
                return new NativeArray<int>(0, Allocator.Temp);
            }

            var startTime = Time.realtimeSinceStartup;
            var results = _spatialIndex.QueryRange(bounds.center, bounds.size);
            
            // 过滤类型
            var filteredResults = new List<int>();
            foreach (var node in results)
            {
                if (objectType == SpatialObjectType.All || node.Category.Equals(objectType.ToString(), System.StringComparison.OrdinalIgnoreCase))
                {
                    filteredResults.Add(node.Id);
                }
            }

            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            var resultArray = new NativeArray<int>(filteredResults.Count, Allocator.Temp);
            for (int i = 0; i < filteredResults.Count; i++)
            {
                resultArray[i] = filteredResults[i];
            }
            
            return resultArray;
        }

        /// <summary>
        /// 查询射线碰撞的对象
        /// </summary>
        public List<SIRaycastHit> QueryRaycast(Ray ray, float maxDistance = float.MaxValue, SpatialObjectType objectType = SpatialObjectType.All)
        {
            // 简化实现，实际应该使用更高效的射线查询算法
            var results = new List<SIRaycastHit>();
            
            if (!IsInitialized) return results;

            // 这里需要实现射线与空间索引的交叉查询
            // 暂时返回空列表，后续可以完善
            return results;
        }

        /// <summary>
        /// 查询最近的对象
        /// </summary>
        public int QueryNearest(Vector3 position, SpatialObjectType objectType = SpatialObjectType.All, float maxDistance = float.MaxValue)
        {
            if (!IsInitialized) return -1;

            var startTime = Time.realtimeSinceStartup;
            
            float nearestDistance = float.MaxValue;
            int nearestId = -1;
            
            // 使用较大的查询范围来找到候选对象
            var searchRadius = Mathf.Min(maxDistance, 100f);
            var candidates = QueryRange(position, searchRadius, objectType);
            
            foreach (var objectId in candidates)
            {
                if (_allNodes.ContainsKey(objectId))
                {
                    var distance = Vector3.Distance(position, _allNodes[objectId].Position);
                    if (distance < nearestDistance && distance <= maxDistance)
                    {
                        nearestDistance = distance;
                        nearestId = objectId;
                    }
                }
            }
            
            candidates.Dispose();
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            return nearestId;
        }

        /// <summary>
        /// 查询K个最近的对象
        /// </summary>
        public NativeArray<int> QueryKNearest(Vector3 position, int k, SpatialObjectType objectType = SpatialObjectType.All, float maxDistance = float.MaxValue)
        {
            if (!IsInitialized || k <= 0)
            {
                return new NativeArray<int>(0, Allocator.Temp);
            }

            var startTime = Time.realtimeSinceStartup;
            
            // 使用较大的查询范围来找到候选对象
            var searchRadius = Mathf.Min(maxDistance, 100f);
            var candidates = QueryRange(position, searchRadius, objectType);
            
            // 计算距离并排序
            var distanceList = new List<(int objectId, float distance)>();
            foreach (var objectId in candidates)
            {
                if (_allNodes.ContainsKey(objectId))
                {
                    var distance = Vector3.Distance(position, _allNodes[objectId].Position);
                    if (distance <= maxDistance)
                    {
                        distanceList.Add((objectId, distance));
                    }
                }
            }
            
            candidates.Dispose();
            
            // 排序并取前K个
            distanceList.Sort((a, b) => a.distance.CompareTo(b.distance));
            var resultCount = Mathf.Min(k, distanceList.Count);
            
            var resultArray = new NativeArray<int>(resultCount, Allocator.Temp);
            for (int i = 0; i < resultCount; i++)
            {
                resultArray[i] = distanceList[i].objectId;
            }
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return resultArray;
        }

        /// <summary>
        /// 检查位置是否被占用
        /// </summary>
        public bool IsPositionOccupied(Vector3 position, float radius = 0.5f, int excludeObjectId = -1)
        {
            if (!IsInitialized) return false;

            var nearby = QueryRange(position, radius);
            bool occupied = false;
            
            foreach (var objectId in nearby)
            {
                if (objectId != excludeObjectId)
                {
                    occupied = true;
                    break;
                }
            }
            
            nearby.Dispose();
            return occupied;
        }

        /// <summary>
        /// 获取对象信息
        /// </summary>
        public SpatialObjectInfo? GetObjectInfo(int objectId)
        {
            if (!IsInitialized || !_allNodes.ContainsKey(objectId)) return null;

            var node = _allNodes[objectId];
            // 將字符串轉換為枚舉
            System.Enum.TryParse<SpatialObjectType>(node.Category, true, out var objectType);
            
            return new SpatialObjectInfo
            {
                ObjectId = node.Id,
                Position = node.Position,
                Bounds = node.Bounds,
                ObjectType = objectType
            };
        }

        /// <summary>
        /// 获取所有对象数量
        /// </summary>
        public int GetObjectCount(SpatialObjectType objectType = SpatialObjectType.All)
        {
            if (!IsInitialized) return 0;

            if (objectType == SpatialObjectType.All)
            {
                return _allNodes.Count;
            }

            int count = 0;
            foreach (var node in _allNodes.Values)
            {
                if (node.Category.Equals(objectType.ToString(), System.StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// 清空空间索引
        /// </summary>
        public void Clear(SpatialObjectType objectType = SpatialObjectType.All)
        {
            if (!IsInitialized) return;

            if (objectType == SpatialObjectType.All)
            {
                _spatialIndex.Clear();
                _allNodes.Clear();
                _pendingInserts.Clear();
                _pendingUpdates.Clear();
                _pendingRemovals.Clear();
            }
            else
            {
                // 选择性清除特定类型的对象
                var toRemove = new List<int>();
                foreach (var kvp in _allNodes)
                {
                    if (kvp.Value.Category.Equals(objectType.ToString(), System.StringComparison.OrdinalIgnoreCase))
                    {
                        toRemove.Add(kvp.Key);
                    }
                }
                
                foreach (var objectId in toRemove)
                {
                    RemoveObject(objectId);
                }
            }
        }

        /// <summary>
        /// 优化空间索引
        /// </summary>
        public void Optimize()
        {
            if (!IsInitialized) return;
            
            // 处理待处理的操作
            ProcessPendingOperations();
            
            // 可以添加更多优化逻辑
        }

        /// <summary>
        /// 重建空间索引
        /// </summary>
        public void Rebuild()
        {
            if (!IsInitialized) return;

            var allNodes = new List<SpatialNode>(_allNodes.Values);
            _spatialIndex.Clear();
            _allNodes.Clear();
            
            foreach (var node in allNodes)
            {
                // 將字符串轉換為枚舉
                System.Enum.TryParse<SpatialObjectType>(node.Category, true, out var objectType);
                AddObject(node.Id, node.Position, node.Bounds, objectType);
            }
            
            ProcessPendingOperations();
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public SpatialIndexPerformanceStats GetPerformanceStats()
        {
            return new SpatialIndexPerformanceStats
            {
                TotalQueries = _totalQueries,
                AverageQueryTime = _totalQueries > 0 ? _totalQueryTime / _totalQueries : 0f,
                FrameQueries = _frameQueries,
                ObjectCount = _allNodes.Count,
                PendingOperations = _pendingInserts.Count + _pendingUpdates.Count + _pendingRemovals.Count
            };
        }

        /// <summary>
        /// 设置索引参数
        /// </summary>
        public void SetIndexParameters(int? maxDepth = null, int? maxObjectsPerNode = null, float? minNodeSize = null)
        {
            if (maxDepth.HasValue) _maxDepth = maxDepth.Value;
            if (maxObjectsPerNode.HasValue) _maxObjectsPerNode = maxObjectsPerNode.Value;
            
            // 如果参数改变，可能需要重建索引
        }

        /// <summary>
        /// 批量添加对象
        /// </summary>
        public int AddObjectsBatch(SpatialObjectInfo[] objects)
        {
            if (!IsInitialized) return 0;

            int successCount = 0;
            foreach (var obj in objects)
            {
                if (AddObject(obj.ObjectId, obj.Position, obj.Bounds, obj.ObjectType))
                {
                    successCount++;
                }
            }
            return successCount;
        }

        /// <summary>
        /// 批量移除对象
        /// </summary>
        public int RemoveObjectsBatch(int[] objectIds)
        {
            if (!IsInitialized) return 0;

            int successCount = 0;
            foreach (var objectId in objectIds)
            {
                if (RemoveObject(objectId))
                {
                    successCount++;
                }
            }
            return successCount;
        }

        /// <summary>
        /// 批量更新对象
        /// </summary>
        public int UpdateObjectsBatch(SpatialObjectUpdate[] updates)
        {
            if (!IsInitialized) return 0;

            int successCount = 0;
            foreach (var update in updates)
            {
                if (UpdateObject(update.ObjectId, update.NewPosition, update.NewBounds))
                {
                    successCount++;
                }
            }
            return successCount;
        }

        /// <summary>
        /// <summary>
        /// 更新服务
        /// </summary>
        public void UpdateService(float deltaTime)
        {
            if (!IsInitialized) return;

            ProcessPendingOperations();
            _frameQueries = 0;
        }

        /// <summary>
        /// IUpdatableService.Update 接口實現
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateService(deltaTime);
        }

        /// <summary>
        /// IService.Initialize 接口實現
        /// </summary>
        public void Initialize()
        {
            // 傳入 Bounds，而不是 float
            Initialize(new Bounds(Vector3.zero, new Vector3(1000f, 1000f, 1000f)));
        }

        /// <summary>
        /// <summary>
        /// 相容：允許以 Bounds 查詢，轉為 (center, radius)
        /// </summary>
        public NativeArray<int> QueryAll(Bounds bounds)
        {
            var center = bounds.center;
            var radius = Mathf.Max(bounds.extents.x, bounds.extents.y, bounds.extents.z);
            return QueryRange(center, radius);
        }

        /// <summary>
        /// 清理服务
        /// </summary>
        public void Cleanup()
        {
            if (!IsInitialized) return;

            _spatialIndex?.Clear();
            foreach (var index in _categoryIndices.Values)
            {
                index?.Clear();
            }

            _categoryIndices.Clear();
            _allNodes.Clear();
            _pendingInserts.Clear();
            _pendingUpdates.Clear();
            _pendingRemovals.Clear();

            IsInitialized = false;
        }

        /// <summary>
        /// 处理待处理的操作
        /// </summary>
        private void ProcessPendingOperations()
        {
            // 处理插入
            int insertCount = 0;
            while (_pendingInserts.Count > 0 && insertCount < _batchSize)
            {
                var node = _pendingInserts.Dequeue();
                InsertNodeImmediate(node);
                insertCount++;
            }

            // 处理更新
            int updateCount = 0;
            while (_pendingUpdates.Count > 0 && updateCount < _batchSize)
            {
                var node = _pendingUpdates.Dequeue();
                UpdateNodeImmediate(node);
                updateCount++;
            }

            // 处理移除
            int removeCount = 0;
            while (_pendingRemovals.Count > 0 && removeCount < _batchSize)
            {
                var node = _pendingRemovals.Dequeue();
                RemoveNodeImmediate(node);
                removeCount++;
            }
        }

        /// <summary>
        /// 立即插入节点
        /// </summary>
        private void InsertNodeImmediate(SpatialNode node)
        {
            _spatialIndex.Insert(node, node.Position, node.Bounds.size);
            _allNodes[node.Id] = node;
        }

        /// <summary>
        /// 立即更新节点
        /// </summary>
        private void UpdateNodeImmediate(SpatialNode node)
        {
            _spatialIndex.Remove(node, node.Position, node.Bounds.size);
            _spatialIndex.Insert(node, node.Position, node.Bounds.size);
            _allNodes[node.Id] = node;
        }

        /// <summary>
        /// 立即移除节点
        /// </summary>
        private void RemoveNodeImmediate(SpatialNode node)
        {
            _spatialIndex.Remove(node, node.Position, node.Bounds.size);
            _allNodes.Remove(node.Id);
        }

        /// <summary>
        /// 更新查询统计
        /// </summary>
        private void UpdateQueryStats(float queryTime)
        {
            _totalQueries++;
            _totalQueryTime += queryTime;
            _frameQueries++;
        }
    }
}