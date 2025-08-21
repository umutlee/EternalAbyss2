using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using SpatialISpatialIndex = DeepAbyssHive.SpatialIndex.Interfaces.ISpatialIndex;
using CoreISpatialIndex = DeepAbyssHive.Core.Interfaces.ISpatialIndex;
using SIRaycastHit = DeepAbyssHive.SpatialIndex.Data.RaycastHit;
using DeepAbyssHive.SpatialIndex.Data;

namespace DeepAbyssHive.SpatialIndex.Implementations
{
    /// <summary>
    /// 八叉树空间索引实现
    /// 用于高效的3D空间查询和管理
    /// </summary>
    public class OctreeSpatialIndex : SpatialISpatialIndex
    {
        [Header("八叉树配置")]
        [SerializeField] private Bounds _worldBounds;
        [SerializeField] private int _maxDepth = 8;
        [SerializeField] private int _maxObjectsPerNode = 10;
        [SerializeField] private bool _autoResize = true;
        [SerializeField] private float _minNodeSize = 1f;

        [Header("性能统计")]
        [SerializeField] private int _totalNodes = 0;
        [SerializeField] private int _totalObjects = 0;
        [SerializeField] private int _queryCount = 0;
        [SerializeField] private float _averageQueryTime = 0f;

        // 内部数据
        private OctreeNode _root;
        private Dictionary<int, SpatialNode> _objects;
        private Dictionary<int, OctreeNode> _objectToNode;
        private List<SpatialNode> _queryResults;

        // 事件
        public event System.Action<SpatialNode> OnObjectAdded;
        public event System.Action<SpatialNode> OnObjectRemoved;
        public event System.Action<SpatialNode> OnObjectMoved;

        /// <summary>
        /// 八叉树节点类
        /// </summary>
        private class OctreeNode
        {
            public Bounds Bounds { get; set; }
            public List<SpatialNode> Objects { get; set; }
            public OctreeNode[] Children { get; set; }
            public int Depth { get; set; }
            public bool IsLeaf => Children == null;

            public OctreeNode(Bounds bounds, int depth)
            {
                Bounds = bounds;
                Depth = depth;
                Objects = new List<SpatialNode>();
                Children = null;
            }

            public void Subdivide()
            {
                if (!IsLeaf) return;

                Children = new OctreeNode[8];
                Vector3 center = Bounds.center;
                Vector3 size = Bounds.size * 0.5f;

                // 创建八个子节点
                for (int i = 0; i < 8; i++)
                {
                    Vector3 offset = new Vector3(
                        (i & 1) == 0 ? -size.x * 0.5f : size.x * 0.5f,
                        (i & 2) == 0 ? -size.y * 0.5f : size.y * 0.5f,
                        (i & 4) == 0 ? -size.z * 0.5f : size.z * 0.5f
                    );
                    
                    Children[i] = new OctreeNode(new Bounds(center + offset, size), Depth + 1);
                }
            }

            public int GetChildIndex(Vector3 position)
            {
                Vector3 center = Bounds.center;
                int index = 0;
                if (position.x > center.x) index += 1;
                if (position.y > center.y) index += 2;
                if (position.z > center.z) index += 4;
                return index;
            }

            public List<int> GetIntersectingChildren(Bounds bounds)
            {
                List<int> indices = new List<int>();
                if (IsLeaf) return indices;

                for (int i = 0; i < 8; i++)
                {
                    if (Children[i].Bounds.Intersects(bounds))
                    {
                        indices.Add(i);
                    }
                }
                return indices;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public OctreeSpatialIndex(Bounds worldBounds, int maxDepth = 8, int maxObjectsPerNode = 10)
        {
            _worldBounds = worldBounds;
            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;
            _minNodeSize = worldBounds.size.magnitude / Mathf.Pow(2, maxDepth);
            
            Initialize();
        }

        /// <summary>
        /// 初始化八叉树
        /// </summary>
        private void Initialize()
        {
            _root = new OctreeNode(_worldBounds, 0);
            _objects = new Dictionary<int, SpatialNode>();
            _objectToNode = new Dictionary<int, OctreeNode>();
            _queryResults = new List<SpatialNode>();
            _totalNodes = 1;
            _totalObjects = 0;
        }

        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        public void Insert(SpatialNode obj, Vector3 position, Vector3 size)
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

            // 插入到八叉树
            InsertIntoNode(_root, obj);
            
            // 更新记录
            _objects[obj.Id] = obj;
            _totalObjects++;

            OnObjectAdded?.Invoke(obj);
        }

        /// <summary>
        /// 更新对象在空间索引中的位置
        /// </summary>
        public void Update(SpatialNode obj, Vector3 oldPosition, Vector3 newPosition, Vector3 size)
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
        public void Remove(SpatialNode obj, Vector3 position, Vector3 size)
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
        /// 查询指定区域内的所有对象（重载方法）
        /// </summary>
        public List<SpatialNode> QueryRange(Vector3 center, Vector3 size)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            Bounds queryBounds = new Bounds(center, size);
            
            QueryRangeRecursive(_root, queryBounds, _queryResults);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return new List<SpatialNode>(_queryResults);
        }

        /// <summary>
        /// 使用SpatialQuery进行高级查询
        /// </summary>
        public List<SpatialNode> Query(SpatialQuery query)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            
            switch (query.Type)
            {
                case SpatialQuery.QueryType.Circle:
                    QueryCircle(query);
                    break;
                case SpatialQuery.QueryType.Bounds:
                    QueryRangeRecursive(_root, query.Bounds, _queryResults);
                    break;
                case SpatialQuery.QueryType.Point:
                    QueryPoint(query);
                    break;
                default:
                    QueryRangeRecursive(_root, query.Bounds, _queryResults);
                    break;
            }

            // 应用过滤和排序
            var filteredResults = _queryResults.Where(query.Matches).ToList();
            
            // 排序结果
            switch (query.Sort)
            {
                case SpatialQuery.SortType.Distance:
                    filteredResults = filteredResults.OrderBy(obj => obj.SqrDistanceTo(query.Center)).ToList();
                    break;
                case SpatialQuery.SortType.DistanceDesc:
                    filteredResults = filteredResults.OrderByDescending(obj => obj.SqrDistanceTo(query.Center)).ToList();
                    break;
                case SpatialQuery.SortType.Category:
                    filteredResults = filteredResults.OrderBy(obj => obj.Category).ToList();
                    break;
                case SpatialQuery.SortType.Layer:
                    filteredResults = filteredResults.OrderBy(obj => obj.Layer).ToList();
                    break;
            }

            // 限制结果数量
            if (query.MaxResults > 0 && filteredResults.Count > query.MaxResults)
            {
                filteredResults = filteredResults.Take(query.MaxResults).ToList();
            }
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return filteredResults;
        }

        /// <summary>
        /// 查询指定点最近的对象
        /// </summary>
        public List<SpatialNode> QueryNearest(Vector3 position, float maxDistance, int maxResults)
        {
            var query = new SpatialQuery(position, maxDistance)
                .WithMaxResults(maxResults)
                .WithSort(SpatialQuery.SortType.Distance);
            
            return Query(query);
        }

        /// <summary>
        /// 查询与射线相交的对象
        /// </summary>
        public List<SpatialNode> QueryRaycast(Ray ray, float maxDistance)
        {
            float startTime = Time.realtimeSinceStartup;
            
            _queryResults.Clear();
            QueryRaycastRecursive(_root, ray, maxDistance, _queryResults);
            
            UpdateQueryStats(Time.realtimeSinceStartup - startTime);
            return new List<SpatialNode>(_queryResults);
        }

        /// <summary>
        /// 清空空间索引
        /// </summary>
        public void Clear()
        {
            _objects.Clear();
            _objectToNode.Clear();
            _root = new OctreeNode(_worldBounds, 0);
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
        /// 获取性能统计信息
        /// </summary>
        public string GetPerformanceStats()
        {
            return $"Octree Stats - Nodes: {_totalNodes}, Objects: {_totalObjects}, " +
                   $"Queries: {_queryCount}, Avg Query Time: {_averageQueryTime:F4}ms, Max Depth: {GetDepth()}";
        }

        /// <summary>
        /// 优化八叉树结构
        /// </summary>
        public void Optimize()
        {
            OptimizeNode(_root);
        }

        #region 私有方法

        private void InsertIntoNode(OctreeNode node, SpatialNode obj)
        {
            if (node.IsLeaf)
            {
                node.Objects.Add(obj);
                _objectToNode[obj.Id] = node;

                if (node.Objects.Count > _maxObjectsPerNode && 
                    node.Depth < _maxDepth && 
                    node.Bounds.size.magnitude > _minNodeSize)
                {
                    SubdivideNode(node);
                }
                return;
            }

            var intersectingChildren = node.GetIntersectingChildren(obj.Bounds);
            
            if (intersectingChildren.Count == 1)
            {
                InsertIntoNode(node.Children[intersectingChildren[0]], obj);
            }
            else
            {
                node.Objects.Add(obj);
                _objectToNode[obj.Id] = node;
            }
        }

        private void SubdivideNode(OctreeNode node)
        {
            node.Subdivide();
            _totalNodes += 8;

            var objectsToRedistribute = new List<SpatialNode>(node.Objects);
            node.Objects.Clear();

            foreach (var obj in objectsToRedistribute)
            {
                _objectToNode.Remove(obj.Id);
                InsertIntoNode(node, obj);
            }
        }

        private void QueryRangeRecursive(OctreeNode node, Bounds queryBounds, List<SpatialNode> results)
        {
            if (!node.Bounds.Intersects(queryBounds)) return;

            foreach (var obj in node.Objects)
            {
                if (obj.IntersectsWith(queryBounds))
                {
                    results.Add(obj);
                }
            }

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    QueryRangeRecursive(child, queryBounds, results);
                }
            }
        }

        private void QueryCircle(SpatialQuery query)
        {
            QueryRangeRecursive(_root, query.Bounds, _queryResults);
            _queryResults.RemoveAll(obj => obj.DistanceTo(query.Center) > query.Radius);
        }

        private void QueryPoint(SpatialQuery query)
        {
            QueryRangeRecursive(_root, new Bounds(query.Center, new Vector3(0.1f, 0.1f, 0.1f)), _queryResults);
        }

        private void QueryRaycastRecursive(OctreeNode node, Ray ray, float maxDistance, List<SpatialNode> results)
        {
            if (!node.Bounds.IntersectRay(ray)) return;

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

            if (!node.IsLeaf)
            {
                foreach (var child in node.Children)
                {
                    QueryRaycastRecursive(child, ray, maxDistance, results);
                }
            }
        }

        private int GetMaxDepthRecursive(OctreeNode node)
        {
            if (node.IsLeaf) return node.Depth;

            int maxDepth = node.Depth;
            foreach (var child in node.Children)
            {
                maxDepth = Mathf.Max(maxDepth, GetMaxDepthRecursive(child));
            }
            return maxDepth;
        }

        private void ExpandWorldBounds(Vector3 position)
        {
            Vector3 min = Vector3.Min(_worldBounds.min, position - Vector3.one);
            Vector3 max = Vector3.Max(_worldBounds.max, position + Vector3.one);
            _worldBounds = new Bounds((min + max) * 0.5f, max - min);
            
            Rebuild();
        }

        private void UpdateQueryStats(float queryTime)
        {
            _queryCount++;
            _averageQueryTime = (_averageQueryTime * (_queryCount - 1) + queryTime) / _queryCount;
        }

        private void OptimizeNode(OctreeNode node)
        {
            if (node.IsLeaf) return;

            foreach (var child in node.Children)
            {
                OptimizeNode(child);
            }

            int totalObjects = node.Objects.Count;
            foreach (var child in node.Children)
            {
                totalObjects += child.Objects.Count;
            }

            if (totalObjects <= _maxObjectsPerNode)
            {
                foreach (var child in node.Children)
                {
                    foreach (var obj in child.Objects)
                    {
                        node.Objects.Add(obj);
                        _objectToNode[obj.Id] = node;
                    }
                }
                
                node.Children = null;
                _totalNodes -= 8;
            }
        }

        #endregion
    }
}