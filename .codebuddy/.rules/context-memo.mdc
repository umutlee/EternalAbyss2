---
description: Eternal Abyss 2 專案關鍵上下文記錄 - 新對話時的核心參考資料
globs: ["Eternal Abyss 2/**"]
alwaysApply: true
tags: ["context", "memo", "project-state"]
---

# Eternal Abyss 2 專案上下文記錄

## 技術規格
- **Unity 版本**: 2022.3.62f1
- **目標平台**: PC/Android/iOS/MacOS
- **開發場景**: Assets/Scenes/Dev/Dev_Playground.unity (Build Index 0)

## 核心架構

### Bootstrap 系統
- **BootEnsureManagers**: 自動創建並初始化所有 Manager
- **核心 Managers**: CreepManager / UnitManager / SpatialIndexManager
- **初始化順序**: 通過 BootEnsureManagers 統一管理，避免依賴問題

### 配置系統 (ScriptableObject)
- **TerrainConfig.asset**: chunkSize/tileSize/loadRadius/maxLODLevels/viewDistance/seed/noiseScale/heightScale/maxModificationsPerFrame
- **CreepConfig.asset**: 菌毯擴張相關參數
- **GameConfig.asset**: 遊戲全局設定
- **位置**: Assets/Resources/Configs/

## 已完成系統狀態

### 地形系統 (TerrainManager)
- **T02**: Chunk 串流系統，支援 _streamTarget 與 HUD 顯示
- **T03**: Perlin 噪聲生成 Mesh/Collider
- **建議參數**: noiseScale=0.05, heightScale=3.0 可得到適合建築的地形
- **架構**: 使用 partial 類分離 Core/Generation/Streaming/Gizmos

### 菌毯系統 (CreepManager) ✅ M2 階段完成
- **核心特性**: 預算化擴張 + 冷卻機制 + 地形門檻 + 建築阻擋
- **開發工具**: CreepBrushAndRunner (左鍵種子) + CreepStatsHUD (性能統計)
- **生命週期**: 隨 Chunk 載入/卸載自動建立/移除網格與冷卻表

#### M2-02: Frontier 擴張系統
- **CreepManager.Expansion.cs**: Queue + HashSet 去重的 frontier 隊列
- **CreepBrushAndRunner.cs**: 左鍵種子 + 自動擴張開發工具
- **CreepStatsHUD.cs**: 實時性能統計 (cells/ms)
- **特性**: 跨 chunk 支援、預算化擴張保證幀率

#### M2-03: 擴張門檻系統
- **CreepManager.Cooling.cs**: 游標式冷卻遞減，避免反覆入列
- **地形門檻**: 坡度限制 (maxSlopeDegrees) + 高度差檢查 (maxStepHeight)
- **建築阻擋**: Physics.CheckBox 檢測，智能 Building 層識別
- **CanEnterFrom**: 三重門檻檢查 (冷卻/坡度/建築)

### 建築放置系統 (BuildingPlacer)
- **輸入**: Raycast 從 Scene/Main Camera
- **圖層**: Terrain 層用於 Chunk，Building 層用於建築
- **視覺**: 預覽綠/紅色 + 腳印 Gizmo
- **互動**: 放置後自動選中/框取，保持 Prefab 原始縮放

## 設計規範 (待實現)

### 1. 錯誤碼與放置結果系統
```csharp
// PlaceResultCode 枚舉
enum PlaceResultCode {
    OK,                    // 放置成功
    E_PLACE_COLLISION,     // 與其他物件重疊
    E_OUT_OF_BOUNDS,       // 超出可用區域
    E_REQUIRE_CREEP,       // 需要菌毯但條件不符
    E_INVALID_TYPE         // Prefab/類型不合法
}

// Result<T> 統一回傳容器
struct Result<T> {
    bool ok;               // 快速成功判斷
    PlaceResultCode code;  // 詳細錯誤碼
    T data;               // 回傳數據 (Bounds/PlacedHandle)
    string message;       // 人類可讀錯誤訊息
}
```
**目的**: 統一所有放置相關 API 的回傳格式，支援建築/單位/裝飾物

### 2. SpatialIndex 並聯校驗系統
```csharp
// GameConfig 開關
bool useSpatialIndexForPlacement;

// 雙重驗證邏輯
if (useSpatialIndexForPlacement) {
    // 1. Unity Physics 檢查
    // 2. SpatialIndex.QueryBounds 檢查
    // 兩者都通過才允許放置
}
```
**目的**: 結合 Unity Physics (可靠) 與 SpatialIndex (高效)，開關式漸進導入

## 待辦事項
- SpatialIndex 放置校驗整合
- 菌毯收縮/淨化機制
- LOD 遲滯閾值優化
- 錯誤碼/Result<T> 系統實現
- 參數外放與模板化

## 開發工具位置
- **QA 工具**: Assets/QA/Smoke/Dev/ (不要創建新的 QA/Dev 目錄)
- **配置文件**: Assets/Resources/Configs/
- **規則文件**: .codebuddy/.rules/

