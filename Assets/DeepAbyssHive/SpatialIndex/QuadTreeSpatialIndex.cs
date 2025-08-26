using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Creep.Data;

namespace DeepAbyssHive.SpatialIndex
{
    /// <summary>
    /// 四叉树空间索引实现
    /// </summary>
    public partial class QuadTreeSpatialIndex : Interfaces.ISpatialIndex
    {
        private QuadTreeNode _root;
        private float _worldSize;
        private int _maxDepth;
        private int _maxObjectsPerNode;
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public QuadTreeSpatialIndex(float worldSize = 1000f, int maxDepth = 8, int maxObjectsPerNode = 10)
        {
            _worldSize = worldSize;
            _maxDepth = maxDepth;
            _maxObjectsPerNode = maxObjectsPerNode;
            
            var bounds = new Bounds(Vector3.zero, new Vector3(worldSize, worldSize, worldSize));
            _root = new QuadTreeNode(bounds, 0, _maxDepth, _maxObjectsPerNode);
        }
        
        /// <summary>
        /// 插入对象到空间索引
        /// </summary>
        public void Insert(CreepData data, Vector3 position, Vector3 size)
        {
            var bounds = new Bounds(position, size);
            _root.Insert(data, bounds);
        }
        
        /// <summary>
        /// 从空间索引中移除对象
        /// </summary>
        public void Remove(CreepData data, Vector3 position, Vector3 size)
        {
            var bounds = new Bounds(position, size);
            _root.Remove(data, bounds);
        }
        
        /// <summary>
        /// 查询指定区域内的对象
        /// </summary>
        public List<CreepData> Query(Vector3 center, float radius)
        {
            var bounds = new Bounds(center, new Vector3(radius * 2f, radius * 2f, radius * 2f));
            return _root.Query(bounds);
        }
        
        /// <summary>
        /// 清空索引
        /// </summary>
        public void Clear()
        {
            var bounds = new Bounds(Vector3.zero, new Vector3(_worldSize, _worldSize, _worldSize));
            _root = new QuadTreeNode(bounds, 0, _maxDepth, _maxObjectsPerNode);
        }

        // ===== MDC INSERT: ISpatialIndex stubs (compile-only) =====
        private System.Collections.Generic.List<DeepAbyssHive.SpatialIndex.Data.SpatialNode> __mdcScratch
            = new System.Collections.Generic.List<DeepAbyssHive.SpatialIndex.Data.SpatialNode>();

        public System.Collections.Generic.List<DeepAbyssHive.SpatialIndex.Data.SpatialNode> QueryRange(UnityEngine.Vector3 center, UnityEngine.Vector3 extents)
        {
            __mdcScratch.Clear();
            // TODO: 實作四分樹的範圍查詢
            return __mdcScratch;
        }

        public System.Collections.Generic.List<DeepAbyssHive.SpatialIndex.Data.SpatialNode> QueryNearest(UnityEngine.Vector3 position, float radius, int maxCount)
        {
            __mdcScratch.Clear();
            // TODO: 實作四分樹的最近鄰查詢
            return __mdcScratch;
        }

        public System.Collections.Generic.List<DeepAbyssHive.SpatialIndex.Data.SpatialNode> QueryRaycast(UnityEngine.Ray ray, float maxDistance)
        {
            __mdcScratch.Clear();
            // TODO: 實作四分樹的射線查詢
            return __mdcScratch;
        }
        // ===== /MDC INSERT =====
    }
    
    /// <summary>
    /// 四叉树节点
    /// </summary>
    public class QuadTreeNode
    {
        private Bounds _bounds;
        private int _depth;
        private int _maxDepth;
        private int _maxObjects;
        private List<CreepData> _objects;
        private QuadTreeNode[] _children;
        private bool _isLeaf;
        
        public QuadTreeNode(Bounds bounds, int depth, int maxDepth, int maxObjects)
        {
            _bounds = bounds;
            _depth = depth;
            _maxDepth = maxDepth;
            _maxObjects = maxObjects;
            _objects = new List<CreepData>();
            _children = null;
            _isLeaf = true;
        }
        
        public void Insert(CreepData data, Bounds bounds)
        {
            if (!_bounds.Intersects(bounds))
                return;
                
            if (_isLeaf)
            {
                _objects.Add(data);
                
                if (_objects.Count > _maxObjects && _depth < _maxDepth)
                {
                    Subdivide();
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    child.Insert(data, bounds);
                }
            }
        }
        
        public void Remove(CreepData data, Bounds bounds)
        {
            if (!_bounds.Intersects(bounds))
                return;
                
            if (_isLeaf)
            {
                _objects.Remove(data);
            }
            else
            {
                foreach (var child in _children)
                {
                    child.Remove(data, bounds);
                }
            }
        }
        
        public List<CreepData> Query(Bounds bounds)
        {
            var result = new List<CreepData>();
            
            if (!_bounds.Intersects(bounds))
                return result;
                
            if (_isLeaf)
            {
                foreach (var obj in _objects)
                {
                    if (bounds.Contains(obj.Position))
                    {
                        result.Add(obj);
                    }
                }
            }
            else
            {
                foreach (var child in _children)
                {
                    result.AddRange(child.Query(bounds));
                }
            }
            
            return result;
        }
        
        private void Subdivide()
        {
            _isLeaf = false;
            _children = new QuadTreeNode[4];
            
            var size = _bounds.size * 0.5f;
            var center = _bounds.center;
            
            _children[0] = new QuadTreeNode(new Bounds(center + new Vector3(-size.x * 0.5f, 0, -size.z * 0.5f), size), _depth + 1, _maxDepth, _maxObjects);
            _children[1] = new QuadTreeNode(new Bounds(center + new Vector3(size.x * 0.5f, 0, -size.z * 0.5f), size), _depth + 1, _maxDepth, _maxObjects);
            _children[2] = new QuadTreeNode(new Bounds(center + new Vector3(-size.x * 0.5f, 0, size.z * 0.5f), size), _depth + 1, _maxDepth, _maxObjects);
            _children[3] = new QuadTreeNode(new Bounds(center + new Vector3(size.x * 0.5f, 0, size.z * 0.5f), size), _depth + 1, _maxDepth, _maxObjects);
            
            foreach (var obj in _objects)
            {
                var bounds = new Bounds(obj.Position, Vector3.one);
                foreach (var child in _children)
                {
                    child.Insert(obj, bounds);
                }
            }
            
            _objects.Clear();
        }
    }
}