using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Buildings.Components;
using System.Collections.Generic;

namespace DeepAbyssHive.Core.Economy
{
    /// <summary>
    /// ResourceService 適配器，提供 M5-T02 規範所需的 API
    /// 橋接現有 ResourceService 與新的成本檢查系統
    /// </summary>
    public class ResourceServiceAdapter
    {
        /// <summary>
        /// 檢查是否有足夠資源支付指定成本
        /// </summary>
        /// <param name="costs">成本列表</param>
        /// <returns>是否有足夠資源</returns>
        public static bool CanAfford(List<ResourceCost> costs)
        {
            if (costs == null || costs.Count == 0)
                return true;

            var resourceService = ResourceService.Instance;
            if (resourceService == null)
            {
                DAHLog.Warning(LogCategory.ECONOMY, "[ResourceAdapter] ResourceService 實例不存在");
                return false;
            }

            foreach (var cost in costs)
            {
                if (string.IsNullOrEmpty(cost.resourceType))
                    continue;

                int available = (int)resourceService.GetResource(cost.resourceType);
                if (available < cost.amount)
                {
                    DAHLog.Debug(LogCategory.ECONOMY, 
                        $"[ResourceAdapter] 資源不足: {cost.resourceType} (需要:{cost.amount}, 擁有:{available})");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 嘗試消費指定成本的資源
        /// </summary>
        /// <param name="costs">成本列表</param>
        /// <returns>是否成功消費</returns>
        public static bool TrySpend(List<ResourceCost> costs)
        {
            if (costs == null || costs.Count == 0)
                return true;

            // 先檢查是否有足夠資源
            if (!CanAfford(costs))
                return false;

            var resourceService = ResourceService.Instance;
            if (resourceService == null)
            {
                DAHLog.Error(LogCategory.ECONOMY, "[ResourceAdapter] ResourceService 實例不存在，無法消費資源");
                return false;
            }

            // 執行消費
            foreach (var cost in costs)
            {
                if (string.IsNullOrEmpty(cost.resourceType))
                    continue;

                bool success = resourceService.ConsumeResource(cost.resourceType, cost.amount);
                if (!success)
                {
                    DAHLog.Error(LogCategory.ECONOMY, 
                        $"[ResourceAdapter] 消費資源失敗: {cost.resourceType}={cost.amount}");
                    // 注意：這裡可能需要回滾之前的消費，但現有 ResourceService 沒有事務支持
                    return false;
                }
            }

            DAHLog.Info(LogCategory.ECONOMY, 
                $"[ResourceAdapter] 成功消費資源: {GetCostSummary(costs)}");
            return true;
        }

        /// <summary>
        /// 獲取指定資源類型的當前數量
        /// </summary>
        /// <param name="resourceType">資源類型</param>
        /// <returns>資源數量</returns>
        public static int Get(string resourceType)
        {
            if (string.IsNullOrEmpty(resourceType))
                return 0;

            var resourceService = ResourceService.Instance;
            if (resourceService == null)
            {
                DAHLog.Warning(LogCategory.ECONOMY, "[ResourceAdapter] ResourceService 實例不存在");
                return 0;
            }

            return (int)resourceService.GetResource(resourceType);
        }

        /// <summary>
        /// 檢查建築成本標籤並返回是否可負擔
        /// </summary>
        /// <param name="buildingPrefab">建築 Prefab</param>
        /// <param name="shortageInfo">資源不足信息（輸出參數）</param>
        /// <returns>是否可負擔</returns>
        public static bool CanAffordBuilding(GameObject buildingPrefab, out string shortageInfo)
        {
            shortageInfo = "";

            if (buildingPrefab == null)
                return true;

            var costTag = buildingPrefab.GetComponent<BuildingCostTag>();
            if (costTag == null || !costTag.HasCosts())
                return true;

            var costs = costTag.GetCosts();
            if (CanAfford(costs))
                return true;

            // 生成資源不足信息
            var shortages = new List<string>();
            foreach (var cost in costs)
            {
                int available = Get(cost.resourceType);
                if (available < cost.amount)
                {
                    shortages.Add($"{cost.resourceType}(需要:{cost.amount}, 擁有:{available})");
                }
            }

            shortageInfo = string.Join(", ", shortages);
            return false;
        }

        /// <summary>
        /// 嘗試為建築付費
        /// </summary>
        /// <param name="buildingPrefab">建築 Prefab</param>
        /// <param name="costSummary">成本摘要（輸出參數）</param>
        /// <returns>是否成功付費</returns>
        public static bool TryPayForBuilding(GameObject buildingPrefab, out string costSummary)
        {
            costSummary = "免費";

            if (buildingPrefab == null)
                return true;

            var costTag = buildingPrefab.GetComponent<BuildingCostTag>();
            if (costTag == null || !costTag.HasCosts())
                return true;

            var costs = costTag.GetCosts();
            costSummary = GetCostSummary(costs);

            return TrySpend(costs);
        }

        /// <summary>
        /// 扣除資源（M5-T02 API 兼容）
        /// </summary>
        /// <param name="costs">成本列表</param>
        /// <returns>是否成功扣除</returns>
        public static bool DeductResources(List<ResourceCost> costs)
        {
            return TrySpend(costs);
        }

        /// <summary>
        /// 獲取成本摘要字符串
        /// </summary>
        private static string GetCostSummary(List<ResourceCost> costs)
        {
            if (costs == null || costs.Count == 0)
                return "免費";

            var summary = new System.Text.StringBuilder();
            for (int i = 0; i < costs.Count; i++)
            {
                if (i > 0) summary.Append(", ");
                summary.Append($"{costs[i].resourceType}:{costs[i].amount}");
            }

            return summary.ToString();
        }
    }
}