using UnityEngine;
using System;
using System.Collections.Generic;

namespace DeepAbyssHive.Buildings.Components
{
    /// <summary>
    /// 建築成本標籤組件，定義建築放置所需的資源成本
    /// </summary>
    [System.Serializable]
    public class ResourceCost
    {
        [SerializeField] public string resourceType;
        [SerializeField] public int amount;
        
        public ResourceCost(string type, int amt)
        {
            resourceType = type;
            amount = amt;
        }
    }

    /// <summary>
    /// 建築成本標籤，附加到建築 Prefab 上定義其建造成本
    /// </summary>
    public class BuildingCostTag : MonoBehaviour
    {
        [Header("建築成本配置")]
        [SerializeField] private List<ResourceCost> costs = new List<ResourceCost>();
        
        [Header("調試信息")]
        [SerializeField] private bool enableDebugLog = false;
        
        /// <summary>
        /// 獲取建築的所有成本需求
        /// </summary>
        public List<ResourceCost> GetCosts()
        {
            return new List<ResourceCost>(costs);
        }
        
        /// <summary>
        /// 添加成本項目
        /// </summary>
        public void AddCost(string resourceType, int amount)
        {
            if (string.IsNullOrEmpty(resourceType) || amount <= 0)
            {
                if (enableDebugLog)
                    Debug.LogWarning($"[BuildingCostTag] 無效的成本項目: {resourceType}={amount}");
                return;
            }
            
            costs.Add(new ResourceCost(resourceType, amount));
            
            if (enableDebugLog)
                Debug.Log($"[BuildingCostTag] 添加成本: {resourceType}={amount}");
        }
        
        /// <summary>
        /// 清除所有成本
        /// </summary>
        public void ClearCosts()
        {
            costs.Clear();
            
            if (enableDebugLog)
                Debug.Log("[BuildingCostTag] 清除所有成本");
        }
        
        /// <summary>
        /// 獲取總成本摘要（用於顯示）
        /// </summary>
        public string GetCostSummary()
        {
            if (costs.Count == 0)
                return "免費";
            
            var summary = new System.Text.StringBuilder();
            for (int i = 0; i < costs.Count; i++)
            {
                if (i > 0) summary.Append(", ");
                summary.Append($"{costs[i].resourceType}:{costs[i].amount}");
            }
            
            return summary.ToString();
        }
        
        /// <summary>
        /// 檢查是否有任何成本需求
        /// </summary>
        public bool HasCosts()
        {
            return costs.Count > 0;
        }
        
        private void Awake()
        {
            if (enableDebugLog)
                Debug.Log($"[BuildingCostTag] 初始化建築成本標籤: {GetCostSummary()}");
        }
    }
}