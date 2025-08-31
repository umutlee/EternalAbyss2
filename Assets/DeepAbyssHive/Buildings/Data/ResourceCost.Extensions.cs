using System.Collections.Generic;
using DeepAbyssHive.Buildings.Enums;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 讓舊程式碼可以用「ResourceType + Amount」配對來迭代 ResourceCost。
    /// 不改動現有 ResourceCost 的定義（Minerals/Gas），只是提供 ToPairs() 便捷列舉。
    /// </summary>
    public static class ResourceCostExtensions
    {
        public readonly struct ResourcePair
        {
            public ResourcePair(ResourceType resourceType, int amount)
            {
                ResourceType = resourceType;
                Amount = amount;
            }
            public ResourceType ResourceType { get; }
            public int Amount { get; }
        }

        /// <summary>
        /// 依現有欄位映射出 (ResourceType, Amount) 配對。
        /// </summary>
        public static IEnumerable<ResourcePair> ToPairs(this ResourceCost cost)
        {
            if (cost.Minerals > 0)
                yield return new ResourcePair(ResourceType.Minerals, cost.Minerals);
            if (cost.Gas > 0)
                yield return new ResourcePair(ResourceType.Gas, cost.Gas);
        }
    }
}