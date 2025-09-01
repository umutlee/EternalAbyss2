using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.Units.Managers
{
    /// <summary>
    /// 單位管理器 - 空殼版本
    /// 提供基本的單例模式和註冊/反註冊 API
    /// 真正的單位邏輯之後再補充
    /// </summary>
    public partial class UnitManager : MonoBehaviour
    {
        public static UnitManager Instance { get; private set; }
        
        // 暫時使用 object 類型，之後換成具體的 Unit 型別
        private readonly HashSet<object> _units = new HashSet<object>();

        private void Awake()
        {
            if (Instance != null && Instance != this) 
            { 
                Destroy(gameObject); 
                return; 
            }
            Instance = this;
        }

        /// <summary>
        /// 註冊單位
        /// </summary>
        /// <param name="unit">要註冊的單位</param>
        public void Register(object unit) => _units.Add(unit);

        /// <summary>
        /// 反註冊單位
        /// </summary>
        /// <param name="unit">要反註冊的單位</param>
        public void Unregister(object unit) => _units.Remove(unit);

        /// <summary>
        /// 獲取已註冊的單位數量
        /// </summary>
        /// <returns>單位數量</returns>
        public int GetUnitCount() => _units.Count;

        /// <summary>
        /// 清除所有已註冊的單位
        /// </summary>
        public void ClearAllUnits() => _units.Clear();
    }
}