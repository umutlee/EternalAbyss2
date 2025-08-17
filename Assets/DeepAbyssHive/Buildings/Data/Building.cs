using System;
using UnityEngine;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings
{
    /// <summary>
    /// 建筑实体类
    /// </summary>
    [Serializable]
    public class Building
    {
        [SerializeField] private int _id;
        [SerializeField] private BuildingType _buildingType;
        [SerializeField] private Vector3 _position;
        [SerializeField] private bool _isConstructed;
        [SerializeField] private float _constructionProgress;
        [SerializeField] private int _level;
        [SerializeField] private float _health;
        [SerializeField] private float _maxHealth;

        /// <summary>
        /// 建筑ID
        /// </summary>
        public int Id => _id;

        /// <summary>
        /// 建筑类型
        /// </summary>
        public BuildingType BuildingType => _buildingType;

        /// <summary>
        /// 建筑位置
        /// </summary>
        public Vector3 Position => _position;

        /// <summary>
        /// 是否已建造完成
        /// </summary>
        public bool IsConstructed => _isConstructed;

        /// <summary>
        /// 建造进度 (0-1)
        /// </summary>
        public float ConstructionProgress => _constructionProgress;

        /// <summary>
        /// 建筑等级
        /// </summary>
        public int Level => _level;

        /// <summary>
        /// 当前生命值
        /// </summary>
        public float Health => _health;

        /// <summary>
        /// 最大生命值
        /// </summary>
        public float MaxHealth => _maxHealth;

        /// <summary>
        /// 构造函数
        /// </summary>
        public Building(int id, BuildingType buildingType, Vector3 position)
        {
            _id = id;
            _buildingType = buildingType;
            _position = position;
            _isConstructed = false;
            _constructionProgress = 0f;
            _level = 1;
            _health = 100f;
            _maxHealth = 100f;
        }

        /// <summary>
        /// 设置建造进度
        /// </summary>
        public void SetConstructionProgress(float progress)
        {
            _constructionProgress = Mathf.Clamp01(progress);
            if (_constructionProgress >= 1f)
            {
                _isConstructed = true;
            }
        }

        /// <summary>
        /// 设置生命值
        /// </summary>
        public void SetHealth(float health)
        {
            _health = Mathf.Clamp(health, 0f, _maxHealth);
        }

        /// <summary>
        /// 升级建筑
        /// </summary>
        public void Upgrade()
        {
            _level++;
            _maxHealth *= 1.2f;
            _health = _maxHealth;
        }
    }
}