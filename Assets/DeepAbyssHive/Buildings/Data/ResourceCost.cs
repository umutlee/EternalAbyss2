using System;

namespace DeepAbyssHive.Buildings.Data
{
    /// <summary>
    /// 資源成本類別，支援多種資源類型
    /// 提供 BuildingManager.Config 使用的具名型別，
    /// 讓像 new ResourceCost { ResourceType = "...", Amount = 100 } 這種初始化能編譯通過。
    /// </summary>
    public class ResourceCost
    {
        // 通用屬性（向後相容）
        public string ResourceType { get; set; } = string.Empty;
        public int Amount { get; set; } = 0;
        
        // 具體資源類型屬性（供 Extensions 使用）
        public int Minerals { get; set; } = 0;
        public int Gas { get; set; } = 0;
    }
}