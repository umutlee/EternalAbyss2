using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// CreepData 的相容性擴充
    /// </summary>
    public partial class CreepData
    {
        [Header("Creep 相容性設定")]
        [SerializeField]
        private float _expansionRate = 1.0f;
        
        [SerializeField]
        private float _maxRadius = 50.0f;
        
        [SerializeField]
        private float _decayRate = 0.1f;
        
        [SerializeField]
        private bool _canSpreadToWater = false;
        
        [SerializeField]
        private bool _canSpreadUphill = true;
        
        [SerializeField]
        private float _maxSlope = 45.0f;
        
        [SerializeField]
        private List<string> _blockedByTags = new List<string>();
        
        [SerializeField]
        private Material _creepMaterial;
        
        [SerializeField]
        private Color _creepColor = Color.green;
        
        [SerializeField]
        private float _creepHeight = 0.1f;
        
        /// <summary>
        /// Creep 擴張速率（單位/秒）
        /// </summary>
        public float ExpansionRate => _expansionRate;
        
        /// <summary>
        /// 最大擴張半徑
        /// </summary>
        public float MaxRadius => _maxRadius;
        
        /// <summary>
        /// Creep 衰減速率
        /// </summary>
        public float DecayRate => _decayRate;
        
        /// <summary>
        /// 是否可以擴散到水面
        /// </summary>
        public bool CanSpreadToWater => _canSpreadToWater;
        
        /// <summary>
        /// 是否可以向上坡擴散
        /// </summary>
        public bool CanSpreadUphill => _canSpreadUphill;
        
        /// <summary>
        /// 最大可擴散坡度（度）
        /// </summary>
        public float MaxSlope => _maxSlope;
        
        /// <summary>
        /// 被阻擋的標籤列表
        /// </summary>
        public List<string> BlockedByTags => _blockedByTags;
        
        /// <summary>
        /// Creep 材質
        /// </summary>
        public Material CreepMaterial => _creepMaterial;
        
        /// <summary>
        /// Creep 顏色
        /// </summary>
        public Color CreepColor => _creepColor;
        
        /// <summary>
        /// Creep 高度
        /// </summary>
        public float CreepHeight => _creepHeight;
        
        /// <summary>
        /// 檢查是否可以擴散到指定位置
        /// </summary>
        /// <param name="position">目標位置</param>
        /// <param name="sourcePosition">源位置</param>
        /// <returns>如果可以擴散則返回 true</returns>
        public bool CanSpreadTo(Vector3 position, Vector3 sourcePosition)
        {
            // 檢查距離
            float distance = Vector3.Distance(position, sourcePosition);
            if (distance > _maxRadius)
                return false;
            
            // 檢查坡度
            if (!_canSpreadUphill)
            {
                float heightDiff = position.y - sourcePosition.y;
                float slope = Mathf.Atan2(heightDiff, distance) * Mathf.Rad2Deg;
                if (slope > _maxSlope)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// 計算擴散優先級
        /// </summary>
        /// <param name="position">目標位置</param>
        /// <param name="sourcePosition">源位置</param>
        /// <returns>擴散優先級（越高越優先）</returns>
        public float CalculateSpreadPriority(Vector3 position, Vector3 sourcePosition)
        {
            float distance = Vector3.Distance(position, sourcePosition);
            float heightDiff = position.y - sourcePosition.y;
            
            // 距離越近優先級越高
            float distancePriority = 1.0f - (distance / _maxRadius);
            
            // 向下坡擴散優先級更高
            float slopePriority = _canSpreadUphill ? 0.5f : Mathf.Max(0, -heightDiff / 10.0f);
            
            return distancePriority + slopePriority;
        }
    }
}