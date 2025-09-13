using System;

namespace DeepAbyssHive.Core.Logging
{
    /// <summary>
    /// DAH 日誌分類枚舉。統一使用大寫命名以符合日誌格式。
    /// </summary>
    public enum LogCategory
    {
        SYSTEM = 0,     // 系統核心
        CREEP,          // 菌毯系統
        TERRAIN,        // 地形系統
        UNITS,          // 單位系統
        BUILDINGS,      // 建築系統
        PATHFINDING,    // 路徑規劃
        UI,             // 使用者介面
        DEV,            // 開發工具
        
        // 新增的分類
        PLACEMENT,      // 建築放置系統
        CONFIG,         // 配置系統
        SERVICE,        // 服務層
        MANAGER,        // 管理器層
        CORE,           // 核心系統
        BUILDING,       // 建築系統（別名）
        SPATIAL,        // 空間索引
        TEST            // 測試系統
    }
}