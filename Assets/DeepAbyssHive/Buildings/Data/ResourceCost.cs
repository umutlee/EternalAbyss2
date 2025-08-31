using System;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 提供 BuildingManager.Config 使用的最低限度具名型別，
    /// 讓像 new ResourceCost { ResourceType = "...", Amount = 100 } 這種初始化能編譯通過。
    /// 之後若你有更完整的資源系統，可在不破壞相容的情況下擴充這個類別。
    /// </summary>
    public class ResourceCost
    {
        public string ResourceType { get; set; } = string.Empty;
        public int Amount { get; set; } = 0;
    }
}