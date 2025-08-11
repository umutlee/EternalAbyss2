using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex.Implementations;

namespace DeepAbyssHive.SpatialIndex.Managers
{
    /// <summary>
    /// 空间索引管理器
    /// 统一管理游戏中的所有空间索引系统
    /// </summary>
    public class SpatialIndexManager : MonoBehaviour, IManager
    {
        [Header("空间索引配置")]
        [SerializeField] private bool _useOctree = true;
        [SerializeField] private Bounds _worldBounds = new Bounds(Vector3.zero, Vector3.one * 1000f);
        [SerializeField] private int _maxDepth = 8;
        [SerializeField] private int _maxObjectsPerNode = 10;
        [SerializeField] private bool _autoResize = true;

        [Header("性能优化")]
        [SerializeField] private bool _enableBatching = true;
        [SerializeField] private int _batchSize = 100;
        [SerializeField] private float _updateInterval = 0.1f;
        [SerializeField] private bool _enableAsyncQueries = true;

        [Header("调试信息")]
        [SerializeField] private bool _showDebugInfo = false;
        [SerializeField] private bool _showBounds = false;
        [SerializeField] private Color _boundsColor = Color.green;

        // 空间索引实例
        private ISpatialIndex<SpatialNode> _spatialIndex;
        private Dictionary<string, ISpatialIndex<SpatialNode>> _categoryIndices;
        
        // 对象管理
        private Dictionary<int, SpatialNode> _allNodes;
        private Queue<SpatialNode> _pendingInserts;
        private Queue<SpatialNode> _pendingUpdates;
        private Queue<SpatialNode> _pendingRemovals;
        
        // 性能统计
        private int _totalQueries = 0;
        private float _totalQueryTime = 0f;
        private int _frameQueries = 0;
        
        // 事件
        public event System.Action<SpatialNode> OnNodeAdded;
        public event System.Action<SpatialNode> OnNodeRemoved;
        public event System.Action<SpatialNode> OnNodeUpdated;

        // IManager接口实现
        public bool IsInitialized { get; private set; }
        public string ManagerName => "SpatialIndexManager";

        /// <summary>
        /// 初始化管理器
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

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
            _categoryIndices = new Dictionary<string, ISpatialIndex<SpatialNode>>();
            _allNodes = new Dictionary<int, SpatialNode>();
            _pendingInserts = new Queue<SpatialNode>();
            _pendingUpdates = new Queue<SpatialNode>();
            _pendingRemovals = new Queue<SpatialNode>();

            // 启动更新协程
            if (_enableBatching)
            {
                StartCoroutine(BatchUpdateCoroutine());
            }

            IsInitialized = true;
            Debug.Log($"[{ManagerName}] 初始化完成 - 使用{(_useOctree ? "八叉树" : "四叉树")}索引");
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        /// <param name="deltaTime">时间增量</param>
        private void Update()
        {
            UpdateManager();
        }

        /// <summary>
        /// 更新管理器
        /// </summary>
        public void UpdateManager()
        {
            if (!IsInitialized) return;

            // 处理待处理的操作
            if (!_enableBatching)
            {
                ProcessPendingOperations();
            }

            // 重置帧查询计数
            _frameQueries = 0;
        }

        /// <summary>
        /// 固定更新管理器
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间增量</param>
        private void FixedUpdate()
        {
            // 固定更新逻辑
        }

        void IManager.Update(float deltaTime)
        {
            UpdateManager();
        }

        void IManager.FixedUpdate(float fixedDeltaTime)
        {
            // 需要時加入固定更新邏輯
        }

        /// <summary>
        /// 后更新管理器
        /// </summary>
        public void LateUpdate()
        {
            // 后更新逻辑
        }

        /// <summary>
        /// 暂停管理器
        /// </summary>
        public void Pause()
        {
            // 暂停逻辑
        }

        /// <summary>
        /// 恢复管理器
        /// </summary>
        public void Resume()
        {
            // 恢复逻辑
        }

        /// <summary>
        /// 获取管理器名称
        /// </summary>
        /// <returns>管理器名称</returns>
        public string GetManagerName()
        {
            return ManagerName;
        }

        /// <summary>
        /// 清理管理器
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
            Debug.Log($"[{ManagerName}] 清理完成");
        }

        /// <summary>
        /// 添加空间节点
        /// </summary>
        public void AddNode(GameObject gameObject, Vector3 position, Vector3 size, 
                           string category = "", int layer = 0, bool isStatic = false)
        {
            if (!IsInitialized || gameObject == null) return;

            int id = gameObject.GetInstanceID();
            if (_allNodes.ContainsKey(id)) return;

            var node = new SpatialNode(id, gameObject, position, new Bounds(position, size), 
                                     category, layer, isStatic);
            
            _allNodes[id] = node;

            if (_enableBatching)
            {
                _pendingInserts.Enqueue(node);
            }
            else
            {
                InsertNodeImmediate(node);
            }
        }

        /// <summary>
        /// 更新空间节点
        /// </summary>
        public void UpdateNode(GameObject gameObject, Vector3 newPosition, Vector3 newSize)
        {
            if (!IsInitialized || gameObject == null) return;

            int id = gameObject.GetInstanceID();
            if (!_allNodes.ContainsKey(id)) return;

            var node = _allNodes[id];
            var oldPosition = node.Position;
            
            node.UpdatePosition(newPosition);
            node.UpdateBounds(new Bounds(newPosition, newSize));

            if (_enableBatching)
            {
                _pendingUpdates.Enqueue(node);
            }
            else
            {
                UpdateNodeImmediate(node, oldPosition);
            }
        }

        /// <summary>
        /// 移除空间节点
        /// </summary>
        public void RemoveNode(GameObject gameObject)
        {
            if (!IsInitialized || gameObject == null) return;

            int id = gameObject.GetInstanceID();
            if (!_allNodes.ContainsKey(id)) return;

            var node = _allNodes[id];
            _allNodes.Remove(id);

            if (_enableBatching)
            {
                _pendingRemovals.Enqueue(node);
            }
            else
            {
                RemoveNodeImmediate(node);
            }
        }

        /// <summary>
        /// 查询指定区域内的对象
        /// </summary>
        public List<GameObject> QueryRange(Vector3 center, Vector3 size, string category = "")
        {
            if (!IsInitialized) return new List<GameObject>();

            float startTime = Time.realtimeSinceStartup;
            
            var index = string.IsNullOrEmpty(category) ? _spatialIndex : GetCategoryIndex(category);
            var nodes = index.QueryRange(center, size);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            return nodes.Where(n => n.GameObject != null)
                       .Select(n => n.GameObject)
                       .ToList();
        }

        /// <summary>
        /// 查询最近的对象
        /// </summary>
        public List<GameObject> QueryNearest(Vector3 position, float maxDistance, int maxResults = 10, string category = "")
        {
            if (!IsInitialized) return new List<GameObject>();

            float startTime = Time.realtimeSinceStartup;
            
            var index = string.IsNullOrEmpty(category) ? _spatialIndex : GetCategoryIndex(category);
            var nodes = index.QueryNearest(position, maxDistance, maxResults);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            return nodes.Where(n => n.GameObject != null)
                       .Select(n => n.GameObject)
                       .ToList();
        }

        /// <summary>
        /// 高级查询
        /// </summary>
        public List<GameObject> Query(SpatialQuery query)
        {
            if (!IsInitialized) return new List<GameObject>();

            float startTime = Time.realtimeSinceStartup;
            
            // 如果八叉树支持高级查询，使用它
            if (_spatialIndex is OctreeSpatialIndex octree)
            {
                var nodes = octree.Query(query);
                UpdateQueryStats(Time.realtimeSinceStartup - startTime);
                
                return nodes.Where(n => n.GameObject != null)
                           .Select(n => n.GameObject)
                           .ToList();
            }
            
            // 否则使用基础查询
            var basicNodes = _spatialIndex.QueryRange(query.Center, query.Bounds.size);
            var filteredNodes = basicNodes.Where(query.Matches).ToList();
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            return filteredNodes.Where(n => n.GameObject != null)
                               .Select(n => n.GameObject)
                               .ToList();
        }

        /// <summary>
        /// 射线查询
        /// </summary>
        public List<GameObject> QueryRaycast(Ray ray, float maxDistance, string category = "")
        {
            if (!IsInitialized) return new List<GameObject>();

            float startTime = Time.realtimeSinceStartup;
            
            var index = string.IsNullOrEmpty(category) ? _spatialIndex : GetCategoryIndex(category);
            var nodes = index.QueryRaycast(ray, maxDistance);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            return nodes.Where(n => n.GameObject != null)
                       .Select(n => n.GameObject)
                       .ToList();
        }

        /// <summary>
        /// 重建空间索引
        /// </summary>
        public void RebuildIndex()
        {
            if (!IsInitialized) return;

            _spatialIndex.Rebuild();
            foreach (var index in _categoryIndices.Values)
            {
                index.Rebuild();
            }

            Debug.Log($"[{ManagerName}] 空间索引重建完成");
        }

        /// <summary>
        /// 优化空间索引
        /// </summary>
        public void OptimizeIndex()
        {
            if (!IsInitialized) return;

            // 如果是八叉树，执行优化
            if (_spatialIndex is OctreeSpatialIndex octree)
            {
                octree.Optimize();
            }

            foreach (var index in _categoryIndices.Values)
            {
                if (index is OctreeSpatialIndex categoryOctree)
                {
                    categoryOctree.Optimize();
                }
            }

            Debug.Log($"[{ManagerName}] 空间索引优化完成");
        }

        /// <summary>
        /// 获取性能统计
        /// </summary>
        public string GetPerformanceStats()
        {
            if (!IsInitialized) return "未初始化";

            float avgQueryTime = _totalQueries > 0 ? _totalQueryTime / _totalQueries : 0f;
            
            string stats = $"空间索引统计:\n" +
                          $"- 总对象数: {_allNodes.Count}\n" +
                          $"- 总查询数: {_totalQueries}\n" +
                          $"- 平均查询时间: {avgQueryTime:F4}ms\n" +
                          $"- 当前帧查询数: {_frameQueries}\n" +
                          $"- 索引深度: {_spatialIndex.GetDepth()}\n";

            if (_spatialIndex is OctreeSpatialIndex octree)
            {
                stats += octree.GetPerformanceStats();
            }
            else if (_spatialIndex is QuadTreeSpatialIndex quadTree)
            {
                stats += quadTree.GetPerformanceStats();
            }

            return stats;
        }

        // 私有方法实现
        private void InsertNodeImmediate(SpatialNode node)
        {
            _spatialIndex.Insert(node, node.Position, node.Bounds.size);
            
            // 添加到分类索引
            if (!string.IsNullOrEmpty(node.Category))
            {
                var categoryIndex = GetCategoryIndex(node.Category);
                categoryIndex.Insert(node, node.Position, node.Bounds.size);
            }

            OnNodeAdded?.Invoke(node);
        }

        private void UpdateNodeImmediate(SpatialNode node, Vector3 oldPosition)
        {
            _spatialIndex.Update(node, oldPosition, node.Position, node.Bounds.size);
            
            // 更新分类索引
            if (!string.IsNullOrEmpty(node.Category))
            {
                var categoryIndex = GetCategoryIndex(node.Category);
                categoryIndex.Update(node, oldPosition, node.Position, node.Bounds.size);
            }

            OnNodeUpdated?.Invoke(node);
        }

        private void RemoveNodeImmediate(SpatialNode node)
        {
            _spatialIndex.Remove(node, node.Position, node.Bounds.size);
            
            // 从分类索引移除
            if (!string.IsNullOrEmpty(node.Category))
            {
                var categoryIndex = GetCategoryIndex(node.Category);
                categoryIndex.Remove(node, node.Position, node.Bounds.size);
            }

            OnNodeRemoved?.Invoke(node);
        }

        private ISpatialIndex<SpatialNode> GetCategoryIndex(string category)
        {
            if (!_categoryIndices.ContainsKey(category))
            {
                if (_useOctree)
                {
                    _categoryIndices[category] = new OctreeSpatialIndex(_worldBounds, _maxDepth, _maxObjectsPerNode);
                }
                else
                {
                    _categoryIndices[category] = new QuadTreeSpatialIndex(_worldBounds, _maxDepth, _maxObjectsPerNode);
                }
            }
            
            return _categoryIndices[category];
        }

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
                UpdateNodeImmediate(node, node.Position); // 简化处理
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

        private System.Collections.IEnumerator BatchUpdateCoroutine()
        {
            while (IsInitialized)
            {
                ProcessPendingOperations();
                yield return new WaitForSeconds(_updateInterval);
            }
        }

        private void UpdateQueryStats(float queryTime)
        {
            _totalQueries++;
            _totalQueryTime += queryTime;
            _frameQueries++;
        }

        // Unity生命周期
        private void OnDrawGizmos()
        {
            if (!_showBounds || !IsInitialized) return;

            Gizmos.color = _boundsColor;
            Gizmos.DrawWireCube(_worldBounds.center, _worldBounds.size);
        }

        private void OnGUI()
        {
            if (!_showDebugInfo || !IsInitialized) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label(GetPerformanceStats());
            GUILayout.EndArea();
        }
    }
}