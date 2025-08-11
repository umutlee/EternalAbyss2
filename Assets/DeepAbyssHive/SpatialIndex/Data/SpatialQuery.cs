using UnityEngine;
using System.Collections.Generic;
using System;

namespace DeepAbyssHive.SpatialIndex.Data
{
    /// <summary>
    /// 空间查询参数
    /// 定义空间索引查询的各种条件和参数
    /// </summary>
    [System.Serializable]
    public class SpatialQuery
    {
        [Header("查询区域")]
        [SerializeField] private Vector3 _center;
        [SerializeField] private float _radius;
        [SerializeField] private Bounds _bounds;
        [SerializeField] private QueryType _queryType;

        [Header("过滤条件")]
        [SerializeField] private List<string> _categories;
        [SerializeField] private List<string> _tags;
        [SerializeField] private List<int> _layers;
        [SerializeField] private bool _includeStatic = true;
        [SerializeField] private bool _includeDynamic = true;

        [Header("排序和限制")]
        [SerializeField] private SortType _sortType;
        [SerializeField] private int _maxResults = -1;
        [SerializeField] private float _minDistance = 0f;
        [SerializeField] private float _maxDistance = float.MaxValue;

        [Header("高级选项")]
        [SerializeField] private bool _predictMovement = false;
        [SerializeField] private float _predictionTime = 0f;
        [SerializeField] private Func<SpatialNode, bool> _customFilter;

        // 查询类型枚举
        public enum QueryType
        {
            Circle,     // 圆形查询
            Bounds,     // 边界框查询
            Point,      // 点查询
            Ray,        // 射线查询
            Frustum     // 视锥查询
        }

        // 排序类型枚举
        public enum SortType
        {
            None,           // 不排序
            Distance,       // 按距离排序
            DistanceDesc,   // 按距离倒序
            Category,       // 按类别排序
            Layer,          // 按层级排序
            Custom          // 自定义排序
        }

        // 属性访问器
        public Vector3 Center => _center;
        public float Radius => _radius;
        public Bounds Bounds => _bounds;
        public QueryType Type => _queryType;
        public List<string> Categories => _categories;
        public List<string> Tags => _tags;
        public List<int> Layers => _layers;
        public bool IncludeStatic => _includeStatic;
        public bool IncludeDynamic => _includeDynamic;
        public SortType Sort => _sortType;
        public int MaxResults => _maxResults;
        public float MinDistance => _minDistance;
        public float MaxDistance => _maxDistance;
        public bool PredictMovement => _predictMovement;
        public float PredictionTime => _predictionTime;
        public Func<SpatialNode, bool> CustomFilter => _customFilter;

        /// <summary>
        /// 构造函数 - 圆形查询
        /// </summary>
        public SpatialQuery(Vector3 center, float radius)
        {
            _center = center;
            _radius = radius;
            _queryType = QueryType.Circle;
            _bounds = new Bounds(center, Vector3.one * radius * 2);
            InitializeDefaults();
        }

        /// <summary>
        /// 构造函数 - 边界框查询
        /// </summary>
        public SpatialQuery(Bounds bounds)
        {
            _bounds = bounds;
            _center = bounds.center;
            _radius = bounds.size.magnitude * 0.5f;
            _queryType = QueryType.Bounds;
            InitializeDefaults();
        }

        /// <summary>
        /// 构造函数 - 点查询
        /// </summary>
        public SpatialQuery(Vector3 point)
        {
            _center = point;
            _radius = 0f;
            _queryType = QueryType.Point;
            _bounds = new Bounds(point, Vector3.zero);
            InitializeDefaults();
        }

        /// <summary>
        /// 初始化默认值
        /// </summary>
        private void InitializeDefaults()
        {
            _categories = new List<string>();
            _tags = new List<string>();
            _layers = new List<int>();
            _sortType = SortType.Distance;
            _maxResults = -1;
            _minDistance = 0f;
            _maxDistance = float.MaxValue;
            _includeStatic = true;
            _includeDynamic = true;
            _predictMovement = false;
            _predictionTime = 0f;
        }

        /// <summary>
        /// 添加类别过滤
        /// </summary>
        public SpatialQuery WithCategory(string category)
        {
            if (!string.IsNullOrEmpty(category) && !_categories.Contains(category))
            {
                _categories.Add(category);
            }
            return this;
        }

        /// <summary>
        /// 添加标签过滤
        /// </summary>
        public SpatialQuery WithTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !_tags.Contains(tag))
            {
                _tags.Add(tag);
            }
            return this;
        }

        /// <summary>
        /// 添加层级过滤
        /// </summary>
        public SpatialQuery WithLayer(int layer)
        {
            if (!_layers.Contains(layer))
            {
                _layers.Add(layer);
            }
            return this;
        }

        /// <summary>
        /// 设置排序方式
        /// </summary>
        public SpatialQuery WithSort(SortType sortType)
        {
            _sortType = sortType;
            return this;
        }

        /// <summary>
        /// 设置最大结果数量
        /// </summary>
        public SpatialQuery WithMaxResults(int maxResults)
        {
            _maxResults = maxResults;
            return this;
        }

        /// <summary>
        /// 设置距离范围
        /// </summary>
        public SpatialQuery WithDistanceRange(float minDistance, float maxDistance)
        {
            _minDistance = minDistance;
            _maxDistance = maxDistance;
            return this;
        }

        /// <summary>
        /// 设置静态对象包含选项
        /// </summary>
        public SpatialQuery WithStatic(bool includeStatic)
        {
            _includeStatic = includeStatic;
            return this;
        }

        /// <summary>
        /// 设置动态对象包含选项
        /// </summary>
        public SpatialQuery WithDynamic(bool includeDynamic)
        {
            _includeDynamic = includeDynamic;
            return this;
        }

        /// <summary>
        /// 启用运动预测
        /// </summary>
        public SpatialQuery WithMovementPrediction(float predictionTime)
        {
            _predictMovement = true;
            _predictionTime = predictionTime;
            return this;
        }

        /// <summary>
        /// 设置自定义过滤器
        /// </summary>
        public SpatialQuery WithCustomFilter(Func<SpatialNode, bool> filter)
        {
            _customFilter = filter;
            return this;
        }

        /// <summary>
        /// 检查节点是否匹配查询条件
        /// </summary>
        public bool Matches(SpatialNode node)
        {
            if (node == null || !node.IsValid())
                return false;

            // 检查静态/动态过滤
            if (node.IsStatic && !_includeStatic)
                return false;
            if (!node.IsStatic && !_includeDynamic)
                return false;

            // 检查类别过滤
            if (_categories.Count > 0 && !_categories.Contains(node.Category))
                return false;

            // 检查层级过滤
            if (_layers.Count > 0 && !_layers.Contains(node.Layer))
                return false;

            // 检查标签过滤
            if (_tags.Count > 0)
            {
                bool hasMatchingTag = false;
                foreach (string tag in _tags)
                {
                    if (node.HasTag(tag))
                    {
                        hasMatchingTag = true;
                        break;
                    }
                }
                if (!hasMatchingTag)
                    return false;
            }

            // 检查空间条件
            Vector3 nodePosition = _predictMovement ? 
                node.PredictPosition(_predictionTime) : node.Position;

            float distance = Vector3.Distance(_center, nodePosition);
            if (distance < _minDistance || distance > _maxDistance)
                return false;

            // 根据查询类型检查空间匹配
            switch (_queryType)
            {
                case QueryType.Circle:
                    if (distance > _radius)
                        return false;
                    break;
                case QueryType.Bounds:
                    if (!_bounds.Contains(nodePosition))
                        return false;
                    break;
                case QueryType.Point:
                    if (distance > 0.01f) // 小的容差值
                        return false;
                    break;
            }

            // 检查自定义过滤器
            if (_customFilter != null && !_customFilter(node))
                return false;

            return true;
        }

        /// <summary>
        /// 获取查询信息字符串
        /// </summary>
        public override string ToString()
        {
            return $"SpatialQuery[Type:{_queryType}, Center:{_center}, Radius:{_radius}, " +
                   $"Categories:{_categories.Count}, Tags:{_tags.Count}, MaxResults:{_maxResults}]";
        }
    }
}