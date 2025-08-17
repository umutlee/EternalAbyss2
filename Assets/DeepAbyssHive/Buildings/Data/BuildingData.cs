using System;
using UnityEngine;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings
{
    /// <summary>
    /// 建筑数据结构
    /// </summary>
    [Serializable]
    public class BuildingData
    {
        [SerializeField] private int _id;
        [SerializeField] private BuildingType _buildingType;
        [SerializeField] private Vector3 _position;
        [SerializeField] private float _constructionProgress;
        [SerializeField] private float _health;
        [SerializeField] private int _level;
        [SerializeField] private bool _isActive;

        /// <summary>
        /// 建筑ID
        /// </summary>
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        /// 建筑类型
        /// </summary>
        public BuildingType BuildingType
        {
            get => _buildingType;
            set => _buildingType = value;
        }

        /// <summary>
        /// 建筑位置
        /// </summary>
        public Vector3 Position
        {
            get => _position;
            set => _position = value;
        }

        /// <summary>
        /// 建造进度 (0-1)
        /// </summary>
        public float ConstructionProgress
        {
            get => _constructionProgress;
            set => _constructionProgress = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 当前生命值
        /// </summary>
        public float Health
        {
            get => _health;
            set => _health = Mathf.Max(0f, value);
        }

        /// <summary>
        /// 建筑等级
        /// </summary>
        public int Level
        {
            get => _level;
            set => _level = Mathf.Max(1, value);
        }

        /// <summary>
        /// 是否激活
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public BuildingData()
        {
            _id = 0;
            _buildingType = BuildingType.Hatchery;
            _position = Vector3.zero;
            _constructionProgress = 0f;
            _health = 100f;
            _level = 1;
            _isActive = true;
        }

        /// <summary>
        /// 带参数的构造函数
        /// </summary>
        public BuildingData(int id, BuildingType buildingType, Vector3 position)
        {
            _id = id;
            _buildingType = buildingType;
            _position = position;
            _constructionProgress = 0f;
            _health = 100f;
            _level = 1;
            _isActive = true;
        }

        /// <summary>
        /// 复制构造函数
        /// </summary>
        public BuildingData(BuildingData other)
        {
            _id = other._id;
            _buildingType = other._buildingType;
            _position = other._position;
            _constructionProgress = other._constructionProgress;
            _health = other._health;
            _level = other._level;
            _isActive = other._isActive;
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"BuildingData[ID:{_id}, Type:{_buildingType}, Pos:{_position}, Progress:{_constructionProgress:P1}]";
        }
    }
}