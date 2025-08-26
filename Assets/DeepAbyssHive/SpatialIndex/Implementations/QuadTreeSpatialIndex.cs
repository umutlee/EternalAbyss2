using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using DeepAbyssHive.SpatialIndex.Interfaces;
using DeepAbyssHive.SpatialIndex.Data;
using DeepAbyssHive.SpatialIndex;

namespace DeepAbyssHive.SpatialIndex.Implementations
{
    /// <summary>
    /// 四叉树空间索引实现
    /// 用于高效的2D空间查询和管理
    /// </summary>
    public class QuadTreeSpatialIndex : Interfaces.ISpatialIndex
    {
        [Header("四叉树配置")]
        [SerializeField] private Bounds _worldBounds;
        [SerializeField] private int _maxDepth = 8;
        [SerializeField] private int _maxObjectsPerNode = 10;
        [SerializeField] private bool _autoResize = true;

        [Header("性能统计")]
        [SerializeField] private int _totalNodes = 0;
        [SerializeField] private int _totalObjects = 0;
        [SerializeField] private int _queryCount = 0;
        [SerializeField] private float _averageQueryTime = 0f;

        // 内部数据
        private QuadTreeNode _root;
        private Dictionary<int, SpatialNode> _objects;
        private Dictionary<int, QuadTreeNode> _objectToNode;
        private int _nextId = 1;
        private List<SpatialNode> _queryResults;

        // 事件
        public event System.Action<SpatialNode> OnObjectAdded;
        public event System.Action<SpatialNode> OnObjectRemoved;
        public event System.Action<SpatialNode> OnObjectMoved;

        /// <summary>
        /// 四叉树节点类
        /// </summary>
        private class QuadTreeNode
        {
            public Bounds Bounds { get; set; }
            public List<SpatialNode> Objects { get; set; }
            public QuadTreeNode[] Children { get; set; }
            public int Depth { get; set; }
            public bool IsLeaf => Children == null;

            public QuadTreeNode(Bounds bounds, int depth)
            {
                Bounds = bounds;
                Depth = depth;
                Objects = new List<SpatialNode>();
                Children = null;
            }

            public void Subdivide()
            {
                if (!IsLeaf) return;

                Children = new QuadTreeNode[4];
                Vector3 center = Bounds.center;
                Vector3 size = Bounds.size * 0.5f;

                // 创建四个子节点
                Children[0] = new QuadTreeNode(new Bounds(center + new Vector3(-size.x * 0.5f, 0, -size.z * 0.5f), size), Depth + 1);
                Children[1] = new QuadTreeNode(new Bounds(center + new Vector3(size.x * 0.5f, 0, -size.z * 0.5f), size), Depth + 1);
                Children[2] = new QuadTreeNode(new Bounds(center + new Vector3(-size.x * 0.5f, 0, size.z * 0.5f), size), Depth + 1);
                Children[3] = new QuadTreeNode(new Bounds(center + new Vector3(size.x * 0.5f, 0, size.z * 0.5f), size), Depth + 1);
            }

            public int GetChildIndex(Vector3 position)
            {
                Vector3 center = Bounds.center;
                int index = 0;
                if (position.x > center.x) index += 1;
                if (position.z > center.z) index += 2;
                return index;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public QuadTreeSpatialIndex(Bounds worldBounds, int maxDepth = 8, int maxObjectsPerNode = 10)
        {
            _worldBounds = worldBounds;
            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;
            
            Initialize();
        }

        /// <summary>
        /// 初始化四叉树
        /// </summary>
        private void Initialize()
        {
            _root = new QuadTreeNode(_worldBounds, 0);
            _objects = new Dictionary<int, SpatialNode>();
            _objectToNode = new Dictionary<int, QuadTreeNode>();
            _queryResults = new List<SpatialNode>();
            _totalNodes = 1;
            _totalObjects = 0;
        }

        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        public void Insert(object obj, Vector3 position, Vector3 size)
        {
            if (obj is SpatialNode spatialNode)
            {
                Insert(spatialNode, position, size);
            }
            else
            {
                Debug.LogWarning($"QuadTreeSpatialIndex: 尝试插入非SpatialNode对象: {obj?.GetType().Name ?? "null"}");
            }
        }
        
        /// <summary>
        /// 插入SpatialNode对象到空间索引
        /// </summary>
        private void Insert(SpatialNode obj, Vector3 position, Vector3 size)
        {
            if (obj == null) return;

            // 检查是否需要扩展世界边界
            if (_autoResize && !_worldBounds.Contains(position))
            {
                ExpandWorldBounds(position);
            }

            // 创建边界
            Bounds bounds = new Bounds(position, size);
            obj.UpdateBounds(bounds);

            // 插入到四叉树
            InsertIntoNode(_root, obj);
            
            // 更新记录
            _objects[obj.Id] = obj;
            _totalObjects++;

            OnObjectAdded?.Invoke(obj);
        }

        /// <summary>
        /// 更新对象在空间索引中的位置
        /// </summary>
        public void Update(object obj, Vector3 oldPosition, Vector3 newPosition, Vector3 size)
        {
            if (obj is SpatialNode spatialNode)
            {
                Update(spatialNode, oldPosition, newPosition, size);
            }
            else
            {
                Debug.LogWarning($"QuadTreeSpatialIndex: 尝试更新非SpatialNode对象: {obj?.GetType().Name ?? "null"}");
            }
        }
        
        /// <summary>
        /// 更新SpatialNode对象在空间索引中的位置
        /// </summary>
        private void Update(SpatialNode obj, Vector3 oldPosition, Vector3 newPosition, Vector3 size)
        {
            if (obj == null || !_objects.ContainsKey(obj.Id)) return;

            // 移除旧位置
            if (_objectToNode.ContainsKey(obj.Id))
            {
                _objectToNode[obj.Id].Objects.Remove(obj);
                _objectToNode.Remove(obj.Id);
            }

            // 更新对象位置
            obj.UpdatePosition(newPosition);
            obj.UpdateBounds(new Bounds(newPosition, size));

            // 重新插入
            InsertIntoNode(_root, obj);

            OnObjectMoved?.Invoke(obj);
        }

        /// <summary>
        /// 从空间索引中移除对象
        /// </summary>
        public void Remove(object obj, Vector3 position, Vector3 size)
        {
            if (obj is SpatialNode spatialNode)
            {
                Remove(spatialNode, position, size);
            }
            else
            {
                Debug.LogWarning($"QuadTreeSpatialIndex: 尝试移除非SpatialNode对象: {obj?.GetType().Name ?? "null"}");
            }
        }
        
        /// <summary>
        /// 从空间索引中移除SpatialNode对象
        /// </summary>
        private void Remove(SpatialNode obj, Vector3 position, Vector3 size)
        {
            if (obj == null || !_objects.ContainsKey(obj.Id)) return;

            // 从节点中移除
            if (_objectToNode.ContainsKey(obj.Id))
            {
                _objectToNode[obj.Id].Objects.Remove(obj);
                _objectToNode.Remove(obj.Id);
            }

            // 从记录中移除
            _objects.Remove(obj.Id);
            _totalObjects--;

            OnObjectRemoved?.Invoke(obj);
        }

        /// <summary>
        /// 查询指定区域内的所有对象
        /// </summary>
        public NativeArray<int> QueryRange(Vector3 position, float radius)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            Bounds queryBounds = new Bounds(position, new Vector3(radius * 2, radius * 2, radius * 2));
            
            QueryRangeRecursive(_root, queryBounds, _queryResults);
            
            // 过滤距离
            var filteredResults = _queryResults.Where(obj => 
                Vector3.Distance(obj.Position, position) <= radius).ToList();
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            
            // 转换为NativeArray
            NativeArray<int> result = new NativeArray<int>(filteredResults.Count, Allocator.Temp);
            for (int i = 0; i < filteredResults.Count; i++)
            {
                result[i] = filteredResults[i].Id;
            }
            
            return result;
        }

        /// <summary>
        /// 查询指定区域内的所有对象
        /// </summary>
        public List<object> QueryRange(Vector3 position, Vector3 size)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            Bounds queryBounds = new Bounds(position, size);
            
            QueryRangeRecursive(_root, queryBounds, _queryResults);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return _queryResults.Cast<object>().ToList().ToSpatialNodes();
        }

        /// <summary>
        /// 查询指定点最近的对象
        /// </summary>
        public List<object> QueryNearest(Vector3 position, float maxDistance, int maxResults)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            Bounds queryBounds = new Bounds(position, new Vector3(maxDistance * 2, maxDistance * 2, maxDistance * 2));
            
            QueryRangeRecursive(_root, queryBounds, _queryResults);
            
            // 按距离排序并限制结果数量
            var sortedResults = _queryResults
                .Where(obj => obj.DistanceTo(position) <= maxDistance)
                .OrderBy(obj => obj.SqrDistanceTo(position))
                .Take(maxResults > 0 ? maxResults : _queryResults.Count)
                .ToList();
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return sortedResults.Cast<object>().ToList().ToSpatialNodes();
        }

        /// <summary>
        /// 查询与射线相交的对象
        /// </summary>
        public List<object> QueryRaycast(Ray ray, float maxDistance)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            QueryRaycastRecursive(_root, ray, maxDistance, _queryResults);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return _queryResults.Cast<object>().ToList().ToSpatialNodes();
        }

        /// <summary>
        /// 清空空间索引
        /// </summary>
        public void Clear()
        {
            _objects.Clear();
            _objectToNode.Clear();
            _root = new QuadTreeNode(_worldBounds, 0);
            _totalNodes = 1;
            _totalObjects = 0;
        }

        /// <summary>
        /// 重建空间索引
        /// </summary>
        public void Rebuild()
        {
            var allObjects = _objects.Values.ToList();
            Clear();
            
            foreach (var obj in allObjects)
            {
                Insert(obj, obj.Position, obj.Bounds.size);
            }
        }

        /// <summary>
        /// 获取空间索引中的对象数量
        /// </summary>
        public int GetCount()
        {
            return _totalObjects;
        }

        /// <summary>
        /// 获取空间索引的深度
        /// </summary>
        public int GetDepth()
        {
            return GetMaxDepthRecursive(_root);
        }

        /// <summary>
        /// 获取空间索引的边界
        /// </summary>
        public Bounds GetBounds()
        {
            return _worldBounds;
        }

        /// <summary>
        /// 将对象插入到指定节点
        /// </summary>
        private void InsertIntoNode(QuadTreeNode node, SpatialNode obj)
        {
            // 如果是叶子节点且未达到分割条件
            if (node.IsLeaf)
            {
                node.Objects.Add(obj);
                _objectToNode[obj.Id] = node;

                // 检查是否需要分割
                if (node.Objects.Count > _maxObjectsPerNode && node.Depth < _maxDepth)
                {
                    SubdivideNode(node);
                }
                return;
            }

            // 找到合适的子节点
            int childIndex = node.GetChildIndex(obj.Position);
            if (childIndex >= 0 && childIndex < 4 && node.Children[childIndex].Bounds.Intersects(obj.Bounds))
            {
                InsertIntoNode(node.Children[childIndex], obj);
            }
            else
            {
                // 对象跨越多个子节点，保留在当前节点
                node.Objects.Add(obj);
                _objectToNode[obj.Id] = node;
            }
        }

        /// <summary>
        /// 分割节点
        /// </summary>
        private void SubdivideNode(QuadTreeNode node)
        {
            node.Subdivide();
            _totalNodes += 4;

            // 重新分配对象到子节点
            var objectsToRedistribute = new List<SpatialNode>(node.Objects);
            node.Objects.Clear();

            foreach (var obj in objectsToRedistribute)
            {
                _objectToNode.Remove(obj.Id);
                InsertIntoNode(node, obj);
            }
        }

        /// <summary>
        /// 递归查询范围
        /// </summary>
        private void QueryRangeRecursive(QuadTreeNode node, Bounds queryBounds, List<SpatialNode> results)
        {
            if (!node.Bounds.Intersects(queryBounds)) return;

            // 检查当前节点的对象
            foreach (var obj in node.Objects)
            {
                if (obj.IntersectsWith(queryBounds))
                {
                    results.Add(obj);
                }
            }

            // 递归检查子节点
            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    QueryRangeRecursive(child, queryBounds, results);
                }
            }
        }

        /// <summary>
        /// 递归查询射线
        /// </summary>
        private void QueryRaycastRecursive(QuadTreeNode node, Ray ray, float maxDistance, List<SpatialNode> results)
        {
            if (!node.Bounds.IntersectRay(ray)) return;

            // 检查当前节点的对象
            foreach (var obj in node.Objects)
            {
                if (obj.Bounds.IntersectRay(ray))
                {
                    float distance = Vector3.Distance(ray.origin, obj.Position);
                    if (distance <= maxDistance)
                    {
                        results.Add(obj);
                    }
                }
            }

            // 递归检查子节点
            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    QueryRaycastRecursive(child, ray, maxDistance, results);
                }
            }
        }

        /// <summary>
        /// 获取最大深度
        /// </summary>
        private int GetMaxDepthRecursive(QuadTreeNode node)
        {
            if (node.IsLeaf) return node.Depth;

            int maxDepth = node.Depth;
            foreach (var child in node.Children)
            {
                maxDepth = Mathf.Max(maxDepth, GetMaxDepthRecursive(child));
            }
            return maxDepth;
        }

        /// <summary>
        /// 扩展世界边界
        /// </summary>
        private void ExpandWorldBounds(Vector3 position)
        {
            Vector3 min = Vector3.Min(_worldBounds.min, position - Vector3.one);
            Vector3 max = Vector3.Max(_worldBounds.max, position + Vector3.one);
            _worldBounds = new Bounds((min + max) * 0.5f, max - min);
            
            // 重建四叉树
            Rebuild();
        }

        /// <summary>
        /// 更新查询统计
        /// </summary>
        private void UpdateQueryStats(float queryTime)
        {
            _queryCount++;
            _averageQueryTime = (_averageQueryTime * (_queryCount - 1) + queryTime) / _queryCount;
        }

        /// <summary>
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"QuadTree Stats - Nodes: {_totalNodes}, Objects: {_totalObjects}, " +
                   $"Queries: {_queryCount}, Avg Query Time: {_averageQueryTime:F4}ms";
        }
    }
}