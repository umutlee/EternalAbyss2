using UnityEngine;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// 菌毯数据类
    /// </summary>
    [System.Serializable]
    public partial class CreepData
    {
        [SerializeField] private Vector3 _position;
        [SerializeField] private float _density;
        [SerializeField] private float _radius;
        [SerializeField] private int _ownerId;
        [SerializeField] private float _creationTime;
        
        /// <summary>
        /// 菌毯位置
        /// </summary>
        public Vector3 Position 
        { 
            get => _position; 
            set => _position = value; 
        }
        
        /// <summary>
        /// 菌毯密度
        /// </summary>
        public float Density 
        { 
            get => _density; 
            set => _density = Mathf.Clamp01(value); 
        }
        
        /// <summary>
        /// 菌毯半径
        /// </summary>
        public float Radius 
        { 
            get => _radius; 
            set => _radius = Mathf.Max(0f, value); 
        }
        
        /// <summary>
        /// 拥有者ID
        /// </summary>
        public int OwnerId 
        { 
            get => _ownerId; 
            set => _ownerId = value; 
        }
        
        /// <summary>
        /// 创建时间
        /// </summary>
        public float CreationTime 
        { 
            get => _creationTime; 
            set => _creationTime = value; 
        }
        
        /// <summary>
        [SerializeField] private bool _isSource;
        [SerializeField] private float _sourceRadius;
        [SerializeField] private float _lastUpdateTime;
        
        /// <summary>
        /// 是否为菌毯源点
        /// </summary>
        public bool IsSource 
        { 
            get => _isSource; 
            set => _isSource = value; 
        }
        
        /// <summary>
        /// 源点半径
        /// </summary>
        public float SourceRadius 
        { 
            get => _sourceRadius; 
            set => _sourceRadius = Mathf.Max(0f, value); 
        }
        
        // removed duplicate auto-property; keep the field version declared elsewhere
        
        /// <summary>
        /// 构造函数
        /// </summary>
        public CreepData(Vector3 position, float density = 1f, float radius = 5f, int ownerId = 0)
        {
            _position = position;
            _density = Mathf.Clamp01(density);
            _radius = Mathf.Max(0f, radius);
            _ownerId = ownerId;
            _creationTime = Time.time;
            _isSource = false;
            _sourceRadius = radius;
            _lastUpdateTime = Time.time;
        }
        
        /// <summary>
        /// 检查指定位置是否在菌毯范围内
        /// </summary>
        public bool ContainsPosition(Vector3 position)
        {
            return Vector3.Distance(_position, position) <= _radius;
        }
        
        /// <summary>
        /// 获取指定位置的菌毯密度
        /// </summary>
        public float GetDensityAt(Vector3 position)
        {
            float distance = Vector3.Distance(_position, position);
            if (distance > _radius) return 0f;
            
            // 根据距离计算密度衰减
            float normalizedDistance = distance / _radius;
            return _density * (1f - normalizedDistance);
        }
    }
}