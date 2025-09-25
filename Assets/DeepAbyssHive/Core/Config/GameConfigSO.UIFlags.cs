using UnityEngine;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// GameConfigSO UI 和開發工具旗標部分類
    /// M4-T99x: UI Guards 和 Defines 系統
    /// </summary>
    public sealed partial class GameConfigSO : ScriptableObject
    {
        [Header("UI / Dev Tools")]
        [Tooltip("啟用開發用 HUD 組件 (HealthHUD, KeyHintsHUD, PlacementStatusHUD 等)")]
        public bool devHudEnabled = true;
        
        [Tooltip("啟用 Smart Console 系統")]
        public bool smartConsoleEnabled = true;
        
        [Tooltip("啟用 Toast 通知系統")]
        public bool toastEnabled = true;
        
        [Tooltip("啟用建築目錄 HUD")]
        public bool buildingCatalogHudEnabled = true;
    }
}