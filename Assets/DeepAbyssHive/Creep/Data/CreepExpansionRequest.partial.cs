using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.Creep.Data
{
    /// <summary>
    /// CreepExpansionRequest 的相容性擴充
    /// </summary>
    public partial class CreepExpansionRequest
    {
        [Header("擴張請求相容性設定")]
        [SerializeField]
        private Vector3 _targetPosition;
        
        [SerializeField]
        private float _priority = 1.0f;
        
        [SerializeField]
        private float _requestTime;
        
        [SerializeField]
        private string _requesterId = "";
        
        [SerializeField]
        private bool _isUrgent = false;
        
        [SerializeField]
        private float _maxWaitTime = 30.0f;
        
        [SerializeField]
        private List<Vector3> _preferredPath = new List<Vector3>();
        
        [SerializeField]
        private List<string> _avoidTags = new List<string>();
        
        /// <summary>
        /// 目標擴張位置
        /// </summary>
        public Vector3 TargetPosition 
        { 
            get => _targetPosition; 
            set => _targetPosition = value; 
        }
        
        /// <summary>
        /// 擴張優先級
        /// </summary>
        public float Priority 
        { 
            get => _priority; 
            set => _priority = value; 
        }
        
        /// <summary>
        /// 請求時間
        /// </summary>
        public float RequestTime 
        { 
            get => _requestTime; 
            set => _requestTime = value; 
        }
        
        /// <summary>
        /// 請求者 ID
        /// </summary>
        public string RequesterId 
        { 
            get => _requesterId; 
            set => _requesterId = value; 
        }
        
        /// <summary>
        /// 是否為緊急請求
        /// </summary>
        public bool IsUrgent 
        { 
            get => _isUrgent; 
            set => _isUrgent = value; 
        }
        
        /// <summary>
        /// 最大等待時間
        /// </summary>
        public float MaxWaitTime 
        { 
            get => _maxWaitTime; 
            set => _maxWaitTime = value; 
        }
        
        /// <summary>
        /// 偏好路徑
        /// </summary>
        public List<Vector3> PreferredPath => _preferredPath;
        
        /// <summary>
        /// 避免的標籤
        /// </summary>
        public List<string> AvoidTags => _avoidTags;
        
        /// <summary>
        /// 檢查請求是否已過期
        /// </summary>
        /// <returns>如果請求已過期則返回 true</returns>
        public bool IsExpired()
        {
            return Time.time - _requestTime > _maxWaitTime;
        }
        
        /// <summary>
        /// 獲取請求的有效優先級（考慮緊急程度和等待時間）
        /// </summary>
        /// <returns>有效優先級</returns>
        public float GetEffectivePriority()
        {
            float basePriority = _priority;
            
            // 緊急請求優先級翻倍
            if (_isUrgent)
                basePriority *= 2.0f;
            
            // 等待時間越長優先級越高
            float waitTime = Time.time - _requestTime;
            float waitBonus = waitTime / _maxWaitTime;
            
            return basePriority + waitBonus;
        }
        
        /// <summary>
        /// 計算到目標位置的距離
        /// </summary>
        /// <param name="fromPosition">起始位置</param>
        /// <returns>距離</returns>
        public float GetDistanceToTarget(Vector3 fromPosition)
        {
            return Vector3.Distance(fromPosition, _targetPosition);
        }
        
        /// <summary>
        /// 檢查路徑是否包含避免的標籤
        /// </summary>
        /// <param name="pathTags">路徑上的標籤</param>
        /// <returns>如果路徑包含避免的標籤則返回 true</returns>
        public bool PathContainsAvoidedTags(List<string> pathTags)
        {
            foreach (var avoidTag in _avoidTags)
            {
                if (pathTags.Contains(avoidTag))
                    return true;
            }
            return false;
        }
        
        /// <summary>
        /// 創建新的擴張請求
        /// </summary>
        /// <param name="targetPos">目標位置</param>
        /// <param name="requesterId">請求者 ID</param>
        /// <param name="priority">優先級</param>
        /// <param name="isUrgent">是否緊急</param>
        /// <returns>新的擴張請求</returns>
        public static CreepExpansionRequest Create(Vector3 targetPos, string requesterId, float priority = 1.0f, bool isUrgent = false)
        {
            var request = new CreepExpansionRequest();
            request._targetPosition = targetPos;
            request._requesterId = requesterId;
            request._priority = priority;
            request._isUrgent = isUrgent;
            request._requestTime = Time.time;
            return request;
        }
    }
}