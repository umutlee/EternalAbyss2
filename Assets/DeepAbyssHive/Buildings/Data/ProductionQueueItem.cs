using System;
using UnityEngine;

namespace DeepAbyssHive.Buildings
{
    /// <summary>
    /// 生产队列项目
    /// </summary>
    [Serializable]
    public class ProductionQueueItem
    {
        [SerializeField] private string _id;
        [SerializeField] private ProductionType _productionType;
        [SerializeField] private int _quantity;
        [SerializeField] private float _progress;
        [SerializeField] private float _totalTime;
        [SerializeField] private DateTime _startTime;
        [SerializeField] private bool _isPaused;

        /// <summary>
        /// 唯一标识符
        /// </summary>
        public string Id => _id;

        /// <summary>
        /// 生产类型
        /// </summary>
        public ProductionType ProductionType => _productionType;

        /// <summary>
        /// 生产数量
        /// </summary>
        public int Quantity
        {
            get => _quantity;
            set => _quantity = Mathf.Max(1, value);
        }

        /// <summary>
        /// 当前进度 (0-1)
        /// </summary>
        public float Progress
        {
            get => _progress;
            set => _progress = Mathf.Clamp01(value);
        }

        /// <summary>
        /// 总生产时间
        /// </summary>
        public float TotalTime => _totalTime;

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime => _startTime;

        /// <summary>
        /// 是否暂停
        /// </summary>
        public bool IsPaused
        {
            get => _isPaused;
            set => _isPaused = value;
        }

        /// <summary>
        /// 是否已完成
        /// </summary>
        public bool IsCompleted => _progress >= 1f;

        /// <summary>
        /// 剩余时间
        /// </summary>
        public float RemainingTime => _totalTime * (1f - _progress);

        /// <summary>
        /// 构造函数
        /// </summary>
        public ProductionQueueItem(ProductionType productionType, int quantity, float totalTime)
        {
            _id = Guid.NewGuid().ToString();
            _productionType = productionType;
            _quantity = Mathf.Max(1, quantity);
            _progress = 0f;
            _totalTime = totalTime;
            _startTime = DateTime.Now;
            _isPaused = false;
        }

        /// <summary>
        /// 更新生产进度
        /// </summary>
        public void UpdateProgress(float deltaTime)
        {
            if (_isPaused || IsCompleted) return;

            if (_totalTime > 0f)
            {
                _progress += deltaTime / _totalTime;
                _progress = Mathf.Clamp01(_progress);
            }
        }

        /// <summary>
        /// 重置进度
        /// </summary>
        public void ResetProgress()
        {
            _progress = 0f;
            _startTime = DateTime.Now;
        }

        /// <summary>
        /// 获取完成百分比
        /// </summary>
        public float GetCompletionPercentage()
        {
            return _progress * 100f;
        }

        /// <summary>
        /// 获取预计完成时间
        /// </summary>
        public DateTime GetEstimatedCompletionTime()
        {
            if (IsCompleted) return _startTime;
            
            float remainingSeconds = RemainingTime;
            return DateTime.Now.AddSeconds(remainingSeconds);
        }

        /// <summary>
        /// 转换为字符串
        /// </summary>
        public override string ToString()
        {
            return $"ProductionQueueItem[{_productionType}, Qty:{_quantity}, Progress:{_progress:P1}]";
        }
    }

    /// <summary>
    /// 生产类型枚举
    /// </summary>
    public enum ProductionType
    {
        Unit,           // 单位生产
        Research,       // 研究
        Upgrade,        // 升级
        Resource,       // 资源生产
        Equipment       // 装备制造
    }
}