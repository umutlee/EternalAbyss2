[新增內容將被追加至此：為確保最小差異，以下為新增的變更記錄區段。若原檔已有相同日期與條目，請保留原有記錄為準。]

## 2025-09-17
- EA-2025-09-17-T17h — Buildings/HUD
  - 修復 BuildingCatalogHUD 與 GameConfig 整合（新增 buildingHudToggleKey / showBuildingHUD 參數；優化讀取邏輯）
- EA-2025-09-17-T17i — Buildings/UX
  - BuildingCatalogBinder 增加節流機制、放置模式檢查、Prefab 名稱比對；新增可拖拽 IMGUI Catalog HUD

## 2025-09-16
- EA-2025-09-16-T17f — Buildings
  - 新增 BuildingCatalogBinder：反射注入 BuildingPlacer、Tab/BackQuote 循環、預覽同步修復
- EA-2025-09-16-T17f-strongbinder — Buildings
  - 增強延遲驗證與靜態 API，緩解預覽不同步
- EA-2025-09-16-T17h3-inject — Buildings
  - 注入改用 TrySetAnyPrefab；擴充命名相容；新增一次性診斷列印
- EA-2025-09-16-T17g-hud — HUD
  - 新增 BuildingCatalogHUD（IMGUI 可拖拽、位置記憶、與 Binder 對齊）
- EA-2025-09-16-Context-Update — Docs
  - context-memo.mdc 同步 M4-T16～T19c 進度

## 2025-09-15
- EA-2025-09-15-T18/T19/T19c — Units
  - 單位貼地系統：預算化貼地、動態走失保險、FootOffset 自動偵測（Collider/Renderer 回退）
- EA-2025-09-15-XT-ConfigTools / Config-DAHLog — Core/Editor
  - 四合一配置管理工具包（菜單統一/資產位置強制/單例管理/載入鎖定）；配置工具遷移至 DAHLog
- EA-2025-09-15-T17-Implementation — Buildings
  - 建築目錄循環選取系統首版（Runtime 單例、反射注入、HUD 顯示、Key 兼容）
- EA-2025-09-15-T17-Rollback — Meta
  - 記錄 T17 回滾與重實作狀態

## 2025-09-14
- EA-2025-09-14-T14/T14.1/T14.2 — Core/Asmdef
  - 統一 Assembly Definition（Runtime/Editor/Dev 分目錄），新增稽核/清理工具，解決循環依賴
- EA-2025-09-14-AsmdefFix — Core
  - 移除重複 asmdef 並修正引用
- EA-2025-09-14-CreepConfigFix / PropertyFix / CreepManagerBracketFix / ClassStructureFix / FIX-02 — Creep
  - 命名空間/屬性/大括號語法修復；CreepTileStatus 枚舉來源統一
- EA-2025-09-14-T15/T16/T16-FIX — HUD
  - GameConfig 啟動快照輸出、Health HUD 實作與十六進制 ID 修復
- EA-2025-09-14-T17/T17C/T17D/T17X 及系列 Fix — Buildings/Editor
  - 建築目錄、Prefab Wizard、統一選單與標準化工具包；修復 GUI.Window ID 十六進制值

## 2025-09-13
- EA-2025-09-13 Smart Console（T11/T13）— Core/Dev
  - 完成 DAHLog 統一、Editor 視窗、設定資產；全面修復 256+ 編譯錯誤並清理 Debug.Log
- EA-2025-09-13-T14.* — Core/Asmdef
  - 統一根級程式集；CreepManager 語法修復；context-memo 經驗記錄

## 2025-09-12
- EA-2025-09-12-Phase1/Phase2 — Logging
  - 分批將核心與建築系統 Debug.Log 遷移至 DAHLog，分類過濾與統一格式
- EA-2025-09-12-T11/T13/SF — Smart Console
  - Editor 視窗完成、錯誤修復、枚舉與 nullable 警告清理

## 2025-09-11
- EA-2025-09-11-T12（+Fix/diagnostics/final）— Units/Path
  - PathJobScheduler 分幀配額；修正回調 Action<List<Vector3>, bool>；外放 pathJobsPerFrame；診斷日誌
- EA-2025-09-11-T08/T09/T10 — HUD/Units
  - Runtime 健康監測 / KeyHints HUD / UnitAgent Dev 日誌節流
- EA-2025-09-11-GameConfig — Core
  - 外放 Unit 動態障礙與 Building 監看器參數

## 2025-09-10
- HUD 拖拽化與 Dev 工具改進、RMB 游標鎖守門、菜單命名統一、UnitDevSpawner 增強
- M4-T06：動態障礙自動重路；QA/Smoke/Dev README 指南新增