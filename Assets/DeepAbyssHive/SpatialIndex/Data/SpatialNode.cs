using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.SpatialIndex.Data
{
    /// <summary>
    /// 空间索引节点数据
    /// 用于存储空间索引中的对象信息
    /// </summary>
    [System.Serializable]
    public partial class SpatialNode
    {
        [Header("基础信息")]
        [SerializeField] private int _id;
        [SerializeField] private Vector3 _position;
        [SerializeField] private Bounds _bounds;
        [SerializeField] private GameObject _gameObject;
        
        [Header("分类信息")]
        [SerializeField] private string _category;
        [SerializeField] private int _layer;
        [SerializeField] private bool _isStatic;
        
        [Header("查询优化")]
        [SerializeField] private float _lastUpdateTime;
        [SerializeField] private Vector3 _velocity;
        [SerializeField] private HashSet<string> _tags;

        // 属性访问器
        public int Id => _id;
        public Vector3 Position => _position;
        public Bounds Bounds => _bounds;
        public GameObject GameObject => _gameObject;
        public string Category => _category;
        public int Layer => _layer;
        public bool IsStatic => _isStatic;
        public float LastUpdateTime => _lastUpdateTime;
        public Vector3 Velocity => _velocity;
        public HashSet<string> Tags => _tags;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SpatialNode(int id, GameObject gameObject, Vector3 position, Bounds bounds, 
                          string category = "", int layer = 0, bool isStatic = false)
        {
            _id = id;
            _gameObject = gameObject;
            _position = position;
            _bounds = bounds;
            _category = category;
            _layer = layer;
            _isStatic = isStatic;
            _lastUpdateTime = UnityEngine.Time.time;
            _velocity = Vector3.zero;
            _tags = new HashSet<string>();
        }

        /// <summary>
        /// 更新节点位置
        /// </summary>
        public void UpdatePosition(Vector3 newPosition)
        {
            if (!_isStatic)
            {
                float deltaTime = UnityEngine.Time.time - _lastUpdateTime;
                if (deltaTime > 0)
                {
                    _velocity = (newPosition - _position) / deltaTime;
                }
                
                _position = newPosition;
                _bounds.center = newPosition;
                _lastUpdateTime = UnityEngine.Time.time;
            }
        }

        /// <summary>
        /// 更新节点边界
        /// </summary>
        public void UpdateBounds(Bounds newBounds)
        {
            _bounds = newBounds;
            _position = newBounds.center;
            _lastUpdateTime = UnityEngine.Time.time;
        }

        /// <summary>
        /// 添加标签
        /// </summary>
        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag))
            {
                _tags.Add(tag);
            }
        }

        /// <summary>
        /// 移除标签
        /// </summary>
        public void RemoveTag(string tag)
        {
            _tags.Remove(tag);
        }

        /// <summary>
        /// 检查是否包含标签
        /// </summary>
        public bool HasTag(string tag)
        {
            return _tags.Contains(tag);
        }

        /// <summary>
        /// 检查是否与指定边界相交
        /// </summary>
        public bool IntersectsWith(Bounds bounds)
        {
            return _bounds.Intersects(bounds);
        }

        /// <summary>
        /// 计算到指定位置的距离
        /// </summary>
        public float DistanceTo(Vector3 position)
        {
            return Vector3.Distance(_position, position);
        }

        /// <summary>
        /// 计算到指定位置的平方距离（性能优化）
        /// </summary>
        public float SqrDistanceTo(Vector3 position)
        {
            return (_position - position).sqrMagnitude;
        }

        /// <summary>
        /// 预测未来位置（基于当前速度）
        /// </summary>
        public Vector3 PredictPosition(float deltaTime)
        {
            if (_isStatic || _velocity == Vector3.zero)
            {
                return _position;
            }
            
            return _position + _velocity * deltaTime;
        }

        /// <summary>
        /// 检查节点是否有效
        /// </summary>
        public bool IsValid()
        {
            return _gameObject != null && _id >= 0;
        }

        /// <summary>
        /// 获取节点信息字符串
        /// </summary>
        public override string ToString()
        {
            return $"SpatialNode[ID:{_id}, Pos:{_position}, Category:{_category}, Static:{_isStatic}]";
        }
    }
}