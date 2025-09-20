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

### 建築放置系統 (BuildingPlacer) ✅ M3 階段全面完成
- **輸入**: Raycast 從 Scene/Main Camera
- **圖層**: Terrain 層用於 Chunk，Building 層用於建築
- **視覺**: 預覽綠/紅色 + 腳印 Gizmo，統一色表管理
- **互動**: 放置後自動選中/框取，保持 Prefab 原始縮放

#### M3-T01~T03: 核心放置系統 ✅ 已完成
- **PlacementValidator.cs**: 統一驗證系統，支援最小間距、格點對齊、有向碰撞
- **BuildingPlacer.cs**: 實時預覽驗證、Grid Snap、旋轉支援、預覽/放置一致性
- **PlacementStatusHUD.cs**: 狀態顯示和調試信息，統一 UI 工具
- **GameConfigSO.cs**: 全面配置化參數 (minSpacing, snapSize, rotationStepDegrees, 熱鍵管理)

#### M3-T04: SpatialIndex 最近鄰委派 ✅ 已完成
- **NoNeighborWithinRadiusPredicate**: 可選的 SpatialIndex 最近鄰檢查
- **向後兼容**: 無 SpatialIndex 時自動回退到 Physics.OverlapSphere
- **性能優化**: 優先使用高效的空間索引，回退到可靠的物理檢測

#### M3-T05: 防護欄/錯誤策略系統 ✅ 已完成
- **ErrorPolicy.cs**: 提供 Try/安全協程封裝與例外格式化，支援結構化錯誤處理
- **ErrorGuardRunner.cs**: 全域錯誤監聽與率限系統，防止Console刷屏
- **GameConfig 整合**: errorGuardEnabled/errorRateLimitPerSec/shortenStackTrace/throwTestErrorKey 參數外放
- **安全協程**: StartSafeCoroutine() 方法，協程例外不會炸掉引擎
- **率限機制**: 每秒最多N則錯誤輸出，避免例外洪水攻擊
- **測試工具**: F12熱鍵觸發測試例外，驗證錯誤捕捉機制
- **驗收**: 零侵入v1版本，提供錯誤不致中斷的防護機制

#### M3-T06: 建築旋轉支援 ✅ 已完成
- **有向碰撞檢測**: Physics.OverlapBox 支援 Quaternion rotation
- **API 擴展**: ValidateByConfig(center, halfExtents, rotation, mask, extraMargin)
- **向後兼容**: 保持舊版 ValidateByConfig(Bounds, mask, extraMargin) API

#### M3-T07: 建築刪除工具 ✅ 已完成
- **BuildingDeleteTool.cs**: 配置化熱鍵的射線選取刪除工具
- **獨立設計**: 不依賴 Placer，可掛載在任意場景物件
- **精確選取**: 使用射線檢測 Building 層物件

#### M3-T08: 旋轉步進配置 ✅ 已完成
- **rotationStepDegrees**: GameConfig 中的旋轉量化參數
- **SnapRotationY**: 僅量化 Y 軸旋轉，避免建築意外傾斜
- **統一量化**: 預覽和放置使用相同的 Grid Snap + Rotation Step

#### M3-T09: 預覽/放置一致性 ✅ 已完成
- **預覽快取系統**: 避免預覽和放置的微小差異
- **統一驗證流程**: Update() 和 PlaceNow() 使用相同的量化和驗證邏輯
- **性能優化**: 重用驗證結果，避免重複計算

#### M3-T10: 遮罩與層一致性 ✅ 已完成
- **PlacementLayerUtil.cs**: 統一管理圖層遮罩計算
- **GetPlacementBlockMask()**: 放置驗證用遮罩，排除 Terrain/IgnoreRaycast
- **GetBuildingOnlyMask()**: Building 層專用遮罩，用於刪除工具

#### M3-T11: HUD 與訊息一致性 ✅ 已完成
- **PlacementUiUtil.cs**: 統一管理顏色和訊息邏輯
- **ColorFor()**: 統一顏色規則，支援預覽透明度控制
- **TextFor()**: 統一 HUD 訊息格式，去除系統前綴

#### M3-T12/T13: SMOKE 測試擴充 + Editor 工具 ✅ 已完成
- **擴充測試**: 45°/90° 旋轉測試、刪除重建邏輯測試
- **GameConfigMenu.cs**: Editor 菜單自動創建/選取 GameConfig
- **完整驗證**: 四種測試案例確保系統穩定性

#### M3-Final: Dev 熱鍵統一管理 ✅ 已完成
- **集中配置**: placementSmokeKey, buildingDeleteKey1/2 統一在 GameConfig
- **智能回退**: GameConfig None 時回退到 Inspector 設置
- **避免衝突**: 統一管理所有 Dev 工具熱鍵
- **BuildingDeleteTool.cs**: Delete/X 鍵射線選取刪除 Building 層物件
- **獨立設計**: 不依賴 Placer，可掛載在任意場景物件
- **開發輔助**: 適用於開發階段快速清理建築

#### M3-T08: 旋轉步進配置 ✅ 已完成
- **rotationStepDegrees**: GameConfig 中的旋轉量化參數 (0=自由旋轉)
- **SnapRotationY**: 僅量化 Y 軸旋轉，避免建築意外傾斜
- **統一量化**: 預覽和放置使用相同的 Grid Snap + Rotation Step

## 熱鍵管理最佳實踐 (M3 建立)
**重要原則**: 以後所有遊戲功能操作按鍵都應該使用可自定義的做法，並集中到 GameConfig.asset 管理
- **集中配置**: 避免各組件分散定義按鍵造成衝突
- **智能回退**: GameConfig 設為 None 時回退到 Inspector 設置
- **向後兼容**: 保持現有設置不受影響
- **統一管理**: 新功能按鍵都應遵循此模式

## 開發工具位置
- **QA 工具**: Assets/QA/Smoke/Dev/ (不要創建新的 QA/Dev 目錄)
- **配置文件**: Assets/Resources/Configs/
- **規則文件**: .codebuddy/.rules/

## DEV 工具強化 (2025-09-10)
- **RmbLockGuard.cs**: 強化版游標鎖定守門員，無條件守門 + Update/LateUpdate 雙重解鎖
- **UnitDevSpawner.cs**: 增強版單位生成器，F6 備用鍵 + y=0 平面回退 + 詳細日誌
- **FocusGameOnPlay.cs**: Editor 工具，Play 模式時自動聚焦 Game 視窗
- **SelectRuntimeHelpers.cs**: 統一菜單命名為 DeepAbyssHive，可選取 DontDestroyOnLoad 內的 Managers

## M3 階段新增的關鍵文件
- **PlacementValidator.cs**: 統一驗證系統 (支援有向碰撞)
- **PlacementSmoke.cs**: F7 自動化測試腳本
- **BuildingDeleteTool.cs**: Delete/X 建築刪除工具
- **PlacementStatusHUD.cs**: 實時狀態顯示 HUD
- **GameConfigSO.cs**: 擴展配置參數 (minSpacing, snapSize, rotationStepDegrees, rmbLocksCursor)

## 階段性備份政策 ✅ 已建立
- **備份時機**: 每完成一個 Milestone 階段 (M3-T01, M3-T02 等)
- **操作流程**: git commit + push + Neo4j 記錄
- **回滾保證**: 每個階段都標記為安全回滾點
- **追蹤機制**: 記錄為 ProjectMilestone 便於管理

## M3 階段完成的設計規範

### 1. 統一驗證系統 ✅ 已實現並完善
- **PlacementValidator**: 統一的建築放置驗證邏輯，支援有向碰撞檢測
- **驗證流程**: 邊界檢查 → Physics 碰撞 → SpatialIndex 並聯 → 最小間距 → 菌毯需求
- **API 雙版本**: 舊版 AABB + 新版有向 (支援旋轉)，完全向後兼容
- **委派系統**: SpatialIndex/菌毯/邊界/最近鄰的可選委派接口

### 2. 配置化設計 ✅ 已實現並擴展
- **GameConfigSO**: 統一配置參數管理，包含 Dev 工具熱鍵
- **核心參數**: minSpacing, margin, snapSize, rotationStepDegrees
- **開關控制**: useSpatialIndexForPlacement, requireCreep
- **熱鍵管理**: placementSmokeKey, buildingDeleteKey1/2 集中配置
- **實時日誌**: 配置載入時自動輸出所有參數

### 3. 統一工具系統 ✅ 已實現
- **PlacementLayerUtil**: 統一圖層遮罩管理，避免各處手寫不一致
- **PlacementUiUtil**: 統一顏色和訊息管理，確保 UI 一致性
- **Editor 工具**: GameConfigMenu 自動創建/選取配置資產

### 4. 開發工具完備 ✅ 已實現並擴展
- **PlacementSmoke**: 配置化熱鍵自動化測試，四種測試案例
- **BuildingDeleteTool**: 配置化熱鍵快速刪除建築
- **PlacementStatusHUD**: 實時狀態顯示和調試信息，統一 UI
- **階段性備份**: 每個任務完成後 git 備份 + Neo4j 記錄

### 5. 熱鍵管理最佳實踐 ✅ 已建立
- **集中配置**: 所有遊戲功能操作按鍵統一在 GameConfig.asset 管理
- **智能回退**: GameConfig 設為 None 時自動回退到 Inspector 設置
- **避免衝突**: 統一管理避免不同組件使用重複按鍵
- **向後兼容**: 保持現有 Inspector 設置不受影響

## M3 階段已實現的設計規範

### 1. 錯誤碼與放置結果系統 ✅ 已實現
```csharp
// PlaceResultCode 枚舉 (已實現)
enum PlaceResultCode {
    OK,                    // 放置成功
    E_PLACE_COLLISION,     // 與其他物件重疊 (包含最小間距違反)
    E_OUT_OF_BOUNDS,       // 超出可用區域
    E_REQUIRE_CREEP,       // 需要菌毯但條件不符
    E_INVALID_TYPE         // Prefab/類型不合法
}

// Result<T> 統一回傳容器 (已實現)
struct Result<T> {
    bool ok;               // 快速成功判斷
    PlaceResultCode code;  // 詳細錯誤碼
    T data;               // 回傳數據 (Bounds)
    string message;       // 人類可讀錯誤訊息
}
```
**狀態**: 已實現並在 M3 中廣泛使用，支援最小間距驗證

### 2. SpatialIndex 並聯校驗系統 ✅ 已實現
```csharp
// GameConfig 開關 (已實現)
bool useSpatialIndexForPlacement;

// 雙重驗證邏輯 (已實現)
if (useSpatialIndexForPlacement) {
    // 1. Unity Physics 檢查
    // 2. SpatialIndex.QueryBounds 檢查
    // 兩者都通過才允許放置
}
```
**狀態**: 已實現委派系統，支援可選的 SpatialIndex 並聯校驗

### 3. 委派接口系統 ✅ 已實現
```csharp
// 已實現的委派接口
public static Func<Bounds, LayerMask, float, bool> SpatialIndexPredicate;
public static Func<Vector3, float, LayerMask, bool> NoNeighborWithinRadiusPredicate;
public static Func<Bounds, bool> RequireCreepPredicate;
public static Func<Bounds, bool> OutOfBoundsPredicate;
```
**狀態**: 完整的委派系統，支援外部系統接入驗證邏輯


## M4 階段：單位/路徑最小整合 (2025-09-06 開始)

### 目標與原則
- **目標**: 地面單位能從 A 走到 B，≈200 隻同時移動仍流暢
- **原則**: 最小可行修補，1–3 檔、≤200 行、零編譯錯、Smoke 綠燈
- **選型**: 輕量「格網 A*」（與 Creep Grid/Chunk 對齊）
- **整合**: Creep Grid 移速加成、Building 層動態阻擋、Terrain 坡度/高差門檻

### M4 任務拆解

### 落地順序
1. **T01+T02**: Grid + A* → Console 單測可跑通
2. **T03**: Agent/Manager → 單位可以走
3. **T04**: Spawner → 200 隻壓測
4. **T05**: Creep 速度加成 → 體感差異明顯 ✅ 已完成
5. **T06**: 建築動態障礙 → 與 M3 打通 ✅ 已完成
6. **T07**: SMOKE-Units → 綠燈標準固化

#### M4-T01: Nav Grid 介面與取樣器 ✅ 待開始
- **IPathGrid**: 取樣可走/成本/鄰接，對齊 creep grid 尺寸
- **GridSampler**: IsWalkable(x,y) 綜合坡度/高差/Building 層/地圖邊界
- **Cost(x,y)**: 基礎成本 + off-creep 罰值（讓單位偏好走 creep）
- **來源**: CreepManager、TerrainManager、Physics（Building）
- **驗收**: DEV HUD 取樣 100×100 區塊，統計 walkable% 與平均 cost

#### M4-T02: A* 單檔輕量實作 ✅ 待開始
- **檔案**: DeepAbyssHive/Units/Pathfinding/GridAStar.cs
- **功能**: 8 向或 4 向連通、開放表（小根堆）＋封閉表（位元陣列）
- **Heuristic**: Manhattan（4向）或 Octile（8向）
- **安全閥**: 節點展開/路徑長度上限，避免最壞情況卡住
- **驗收**: 單次求路 < 1ms（PC），失敗時回 Result<List<Vector3>>

#### M4-T03: UnitAgent 與 Path 請求管線 ✅ 已完成
- **UnitAgent.cs**: SetDestination(Vector3)、沿路徑移動、面向控制、到站檢測
- **UnitPathQueue.cs**: 靜態佇列系統，每幀處理 N 筆（預設 32）
- **自動啟動**: RuntimeInitializeOnLoadMethod 掛載 Runner 到 Managers
- **內建 Grid**: 預設 PathGridSampler 供測試，支援外部 GridProvider 替換

#### M4-T04: Dev Spawner & 指令（200 隻壓測） ✅ 已完成
- **UnitDevSpawner.cs**: GameConfig 熱鍵（預設 F9）生成 N 隻，F10 指派目標
- **配置外放**: devUnitsSpawnKey/devUnitsTestKey/devSpawnCount 統一在 GameConfig
- **智能回退**: 支援 Prefab 或即時生成 Capsule+UnitAgent 作為後備
- **驗收**: F9 生成單位、F10 指派目標，Console 有簡短日誌輸出

#### M4-T05: Creep 交互（移速加成） ✅ 已完成
- **取樣頻率**: UnitAgent 每 0.25s 取樣當前格 (可配置 creepSampleInterval)
- **速度調整**: on-creep → speed *= creepSpeedMul (1.25)，off-creep → speed *= offCreepSpeedMul (1.0)
- **GameConfig**: creepSpeedMul/offCreepSpeedMul/creepSampleInterval 已外放
- **實現**: SampleCreepIfDue() 方法 + OnCreepPredicate 委派接口
- **驗收**: 走進菌毯路段時明顯提速，Console 印狀態切換

#### M4-T06: 障礙動態更新（建築放置/刪除） ✅ 已完成
- **UnitAgent 動態檢測**: 週期性 SphereCast 沿「當前→下一 waypoint」探測 Building 層
- **自動重路**: 命中障礙時對「當前位置→最終目標」重新排路，避免卡住
- **冷卻機制**: dynamicRepathCooldown 防止連續重路抖動
- **配置參數**: dynamicCheckInterval/dynamicRepathCooldown/obstacleProbeRadius/obstacleProbeExtra
- **驗收**: 建築生成/刪除後，單位能自動繞行，不會穿越新障礙

#### M4-T07: SMOKE 套件（Units） ✅ 待開始
- **UnitsSmoke.cs**: 熱鍵（預設 F10）生成 200 隻，隨機起訖 5 組
- **測試流程**: 跑路 8 秒，輸出 req/s、path/ms、avgLen、fail%
- **綠燈標準**: fail < 2%，平均求路 < 1.5ms（PC）
- **驗收**: 一鍵測出穩定數據

#### M4-T08: Runtime 健康監測系統 ✅ 已完成 (2025-09-11)
- **HealthMonitor.cs**: 週期性輸出 FPS/記憶體/單位數/建築數統計
- **GameConfig 外放**: healthLogEnabled/healthLogInterval 參數控制監測開關與間隔
- **自動啟動**: RuntimeInitializeOnLoadMethod 自動掛載到 DontDestroyOnLoad
- **統計內容**: FPS、記憶體使用、活躍單位數、建築數量
- **驗收**: Console 定期輸出 [Health] 統計，可通過 GameConfig 控制

#### M4-T09: Keybinding 外放與 Key Hints HUD ✅ 已完成 (2025-09-11)
- **KeyHintsHUD.cs**: 實時顯示當前可用熱鍵提示的 HUD 系統
- **GameConfig 統一**: 所有開發工具熱鍵集中到 GameConfig 管理
- **動態提示**: 根據當前場景狀態顯示相關熱鍵 (建築放置、單位生成等)
- **UI 整合**: 與現有 HUD 系統協調，避免重疊顯示
- **驗收**: 螢幕右上角顯示熱鍵提示，隨場景狀態動態更新

#### M4-T10: UnitAgent DevLog 節流機制 ✅ 已完成 (2025-09-11)
- **日誌節流**: UnitAgent 移動/尋路日誌加入頻率限制，避免 Console 洪水攻擊
- **GameConfig 控制**: devVerboseLogs 開關控制詳細日誌輸出
- **性能優化**: 大量單位移動時不再產生過量日誌，保持 Console 可讀性
- **智能過濾**: 關鍵事件 (路徑完成、障礙檢測) 仍正常輸出
- **驗收**: 200 隻單位移動時 Console 日誌量可控，不影響性能

#### M4-T11: Smart Console 開發工具系統 ✅ 已完成 (2025-09-12)
- **DLog.cs**: 統一日誌 API，支援 T/D/I/W/E/F 六級別 + 格式化版本
- **SmartConsoleWindow.cs**: Editor 視窗，支援分類過濾、搜尋、摺疊、速率限制
- **DevLogSettingsSO.cs**: ScriptableObject 配置系統，管理緩衝區大小、速率限制等
- **DevLogBootstrap.cs**: Editor 自動初始化，確保設定載入
- **RuntimeMiniConsole.cs**: 可選的 Runtime 控制台顯示組件
- **DevLogSettings.asset**: 預設配置資產，包含所有系統參數
- **功能特性**: 標籤分類、即時過濾、自動摺疊重複訊息、速率限制防洪水
- **快捷鍵**: Ctrl+Alt+L 開啟 Smart Console 視窗
- **驗收**: 完整的開發日誌系統，支援高頻日誌場景，UI 響應流暢

#### M4-T12: Path jobs 分幀配額（平滑算路尖峰） ✅ 已完成 (2025-09-11)
- **PathJobScheduler.cs**: 分幀配額調度器，避免同幀大量算路尖峰
- **GameConfig 外放**: pathJobsPerFrame 參數（預設 8），控制每幀最多啟動算路數
- **診斷日誌**: devVerboseLogs 開啟時輸出調度統計 [PathSched] 
- **UnitAgent 整合**: SetDestination 改用 PathJobScheduler.Enqueue 分散觸發
- **最小侵入**: 保持既有 UnitPathQueue 算法，僅在觸發層面分幀
- **修復完成**: 解決 CS0102 重複欄位定義編譯錯誤，診斷補丁完成
- **驗收**: 大量單位同時尋路時幀率更穩定，Console 有調度統計輸出

#### M4-T13: SmartConsole Debug.Log 統一遷移 ✅ 已完成 (2025-09-13)
- **DAHLog 統一日誌系統**: 完成全專案 Debug.Log 到 DAHLog 的系統性遷移
- **核心系統遷移**: Creep、Units、Tests、Core/Config、Core/Bootstrap 全部完成
- **LogCategory 分類**: SERVICE、MANAGER、CONFIG、COMMON 等分類管理
- **保留策略**: Editor 腳本、QA/Dev 工具、DAHLog 本身保留原 Debug 調用
- **最終狀態**: DeepAbyssHive 和 Core 目錄下僅剩 DAHLog.cs 和註釋中的 Debug 調用
- **編譯錯誤修復**: 修復 256+ 編譯錯誤，包括 LogCategory 缺失、DAHLog.Warning 方法、參數錯誤
- **驗收**: 全案搜索確認無遺漏，統一日誌格式，編譯成功無錯誤

#### M4-GameConfig: Unit 動態障礙檢測與 Building 監看器參數外放 ✅ 已完成 (2025-09-11)
- **動態障礙參數**: unitDynCheckInterval/unitDynRepathCooldown/unitObstacleProbeRadius/unitObstacleProbeExtra
- **Building 監看器**: buildingWatcherInterval/buildingWatcherPadRadius 參數外放
- **統一配置**: 所有 Unit 和 Building 相關參數集中到 GameConfig 管理
- **實時調整**: 支援 Runtime 調整參數，便於性能調優
- **驗收**: GameConfig Inspector 可調整所有相關參數，立即生效

## M4-T14 Assembly Definition 架構重構 ✅ 已完成 (2025-09-14)

### 問題背景
- **循環依賴危機**: 12 個 Assembly Definition 文件形成複雜循環依賴網
- **編譯錯誤爆發**: 初次修復嘗試導致 80+ 編譯錯誤，Core 模組無法找到業務模組引用
- **架構債務**: 過度細分的 Assembly Definition 導致維護複雜度指數增長

### M4-T14 解決方案：統一 Assembly Definition
- **核心原則**: 單一根級 Assembly Definition，消除內部模組邊界
- **文件結構**:
  - `Assets/DeepAbyssHive/DeepAbyssHive.Runtime.asmdef` (統一運行時程式集)
  - `Assets/DeepAbyssHive/Editor/DeepAbyssHive.Editor.asmdef` (Editor 專用程式集)
- **刪除文件**: 移除所有子模組 .asmdef 文件 (Buildings, Common, Creep, Core, Research, SpatialIndex, Terrain, Tests, Units)

### 實施過程與挑戰
1. **第一次嘗試失敗**: 創建分層架構，Core 作為底層，導致 Core 無法管理業務模組
2. **M4-T14 正確理解**: 統一程式集邊界，所有業務邏輯在同一 Assembly 內
3. **清理衝突**: 解決重複 Assembly Definition 文件問題
4. **命名空間修復**: 修復 CreepConfigSO 等類型的命名空間引用問題

### 關鍵修復 (2025-09-14)
#### EA-M4-FIX-01: CreepManager.LoadConfiguration 語法修復 ✅ 已完成
- **問題**: LoadConfiguration() 方法大括號錯位造成 CS1022 編譯錯誤
- **解決**: 修正大括號結構，將後備參數包入 `if (_config == null)` 區塊
- **影響文件**: `Assets/DeepAbyssHive/Creep/Managers/CreepManager.Core.cs`
- **結果**: 消除語法錯誤，統一日誌訊息格式

#### EA-M4-FIX-02: ISpatialIndex 別名統一與 CreepTileStatus 標準化 ✅ 已完成
- **問題**: 混用完整限定名與別名，CreepTileStatus 枚舉來源不一致
- **解決**: 統一使用 ISpatialIndex 別名，以 CreepTileStatusCompat 為唯一枚舉來源
- **影響文件**: `Assets/DeepAbyssHive/Creep/Managers/CreepManager.Core.cs`
- **結果**: 提升代碼一致性，避免枚舉名稱衝突

#### EA-M4-T14.1: Assembly Definition 目錄結構修正 ✅ 已完成
- **問題**: Unity 限制每個目錄只能有一個 .asmdef 文件
- **解決**: Editor/Dev asmdef 移至各自子目錄，避免與 Runtime 衝突
- **目標結構**:
  - `Assets/DeepAbyssHive/DeepAbyssHive.Runtime.asmdef` (根目錄)
  - `Assets/DeepAbyssHive/Editor/DeepAbyssHive.Editor.asmdef` (Editor 子目錄)
  - `Assets/DeepAbyssHive/Dev/DeepAbyssHive.Dev.asmdef` (Dev 子目錄)
- **安全機制**: 自動清理舊路徑的遺留文件

#### EA-M4-T14.2: Assembly Definition 衝突稽核與清理工具 ✅ 已完成
- **功能**: 新增 asmdef 衝突檢查與一鍵清理工具
- **稽核機制**: 自動檢測同目錄多個 .asmdef 文件的衝突情況
- **清理工具**: "Clean Root asmdef dups" 按鈕，安全刪除根目錄誤放的 Editor/Dev asmdef
- **防呆設計**: 重複執行安全，提供詳細的操作日誌

### 經驗教訓
- **過度工程化風險**: Assembly Definition 過度細分會帶來維護負擔
- **依賴關係複雜性**: 業務模組間的相互依賴難以用 Assembly 邊界清晰分離
- **Unity 限制**: 每個目錄只能有一個 .asmdef 文件的限制
- **漸進式重構**: 大型架構變更需要分步驟進行，避免同時引入多個變數
- **工具化修復**: 複雜的架構問題需要配套的檢測和修復工具

### 最終狀態
- ✅ **零編譯錯誤**: 所有 CS 錯誤已解決，包括語法和命名空間問題
- ✅ **統一程式集**: DeepAbyssHive.Runtime 包含所有業務邏輯
- ✅ **Editor 分離**: DeepAbyssHive.Editor 獨立處理 Editor 工具
- ✅ **架構簡化**: 消除循環依賴，降低維護複雜度
- ✅ **工具完備**: 提供稽核和修復工具，防止未來重複問題
- ✅ **代碼標準化**: 統一別名使用和枚舉來源，提升代碼品質

#### M4-T15: 開發工具 HUD 系統整合 ✅ 已完成 (2025-09-14)
- **KeyHintsHUD 增強**: 新增 Building 相關熱鍵提示 (B鍵放置、Delete/X刪除)
- **PlacementStatusHUD 優化**: 改善 HUD 顯示邏輯，避免重複顯示
- **統一 HUD 管理**: 所有開發 HUD 組件協調顯示，避免 UI 重疊
- **GameConfig 整合**: HUD 顯示狀態與 GameConfig 參數同步
- **驗收**: 多個 HUD 組件同時運行無衝突，提示信息準確

#### M4-T16: Health HUD 系統實現 ✅ 已完成 (2025-09-14)
- **HealthHUD.cs**: 可拖拽 IMGUI 視窗，顯示 FPS/記憶體/單位數/建築數統計
- **自動啟動**: RuntimeInitializeOnLoadMethod 自動初始化，DontDestroyOnLoad 持久化
- **位置持久化**: EditorPrefs/PlayerPrefs 記憶視窗位置，支援 Editor/Runtime 雙模式
- **GameConfig 整合**: 讀取 healthLogEnabled/healthLogInterval 參數控制顯示
- **Smart Console 整合**: 優先使用 Smart Console，回退到 Debug.Log
- **Editor 菜單**: DeepAbyssHive/HUD/Toggle Health HUD 菜單項目
- **反射統計**: 自動統計 UnitManager.ActiveUnits 和場景 Building 物件數量
- **語法修復**: 修復 GUI.Window 中的無效十六進制數字 0xDAH001 → 0xDA0001
- **驗收**: 可拖拽 HUD 視窗正常顯示，統計數據準確，無編譯錯誤

#### M4-T17: 建築目錄循環選取系統 ✅ 已完成 (2025-09-15)
- **BuildingCycler.cs**: Runtime 單例管理器，支援建築目錄循環選取
- **反射注入**: 自動注入到 BuildingPlacer，無需手動配置
- **HUD 顯示**: 實時顯示當前選中建築名稱和索引
- **GameConfig 整合**: buildingCycleKey 熱鍵配置 (預設 Tab)
- **智能回退**: 支援 Inspector 後備熱鍵設置
- **驗收**: Tab 鍵循環選取建築，HUD 顯示當前選項

#### M4-T17f: 建築預覽同步修復 ✅ 已完成 (2025-09-16)
- **BuildingCatalogBinder.cs**: 修復 Tab/BackQuote 切換建築時預覽模型不同步問題
- **反射注入**: 自動找到 BuildingPlacer 並注入 SetSelectedBuilding 方法
- **GameConfig 整合**: buildingCycleNextKey/buildingCyclePrevKey 熱鍵配置
- **自動啟動**: RuntimeInitializeOnLoadMethod 自動初始化，DontDestroyOnLoad 持久化
- **最小侵入**: 不修改現有文件，避免與 BuildingSelectionProvider 衝突
- **驗收**: Tab/BackQuote 切換建築時預覽模型正確同步更新

#### M4-T17X: 統一標準化工具包 ✅ 已完成 (2025-09-14)
- **MenuPaths.cs**: 中央常數管理，統一所有 Editor 菜單路徑
- **CreateAssetMenuRewriter.cs**: 自動重寫 CreateAssetMenu 屬性，確保命名一致
- **AssetPathEnforcer.cs**: 強制資產路徑規範，自動移動到正確位置
- **UnitTemplateMigrator.cs**: 精確 Lint + Rebind 防呆機制，支援選擇性替換
- **BuildingPrefabWizard.cs**: 批量建築 Prefab 生成工具，支援居中、貼地、碰撞器
- **UnifiedEditorMenu.cs**: 統一選單系統，集中管理選單層級結構
- **驗收**: 所有 Editor 工具統一命名為 DeepAbyssHive，路徑規範化

#### M4-T18: 單位貼地系統 ✅ 已完成 (2025-09-15)
- **UnitGroundFollower.cs**: 可選的單位地面貼附功能
- **平滑貼地**: 支援平滑插值和坡面對齊
- **GameConfig 外放**: groundFollowEnabled/groundSampleInterval/groundLerpSpeed 參數
- **性能優化**: 預算化取樣，避免每幀大量 Raycast
- **驗收**: 單位能平滑貼附地面，支援坡面行走

#### M4-T19: Unit Grounding 正式版 ✅ 已完成 (2025-09-15)
- **UnitGroundFollowerManager.cs**: 預算化貼地與走失保險機制
- **走失保險**: 單位超出地圖邊界時自動傳送回安全位置
- **參數外放**: unitGroundingEnabled/maxUnitsPerFrame/groundCheckRadius/lostUnitTeleportY
- **無限迴圈修復**: 加入 visits 計數器限制最大遍歷次數
- **驗收**: 修正單位越走越不見問題，加入預算化貼地與走失保險機制

#### M4-T19c: Foot Offset 自動偵測 ✅ 已完成 (2025-09-15)
- **自動計算**: 透過自動計算每個單位的 footOffset (pivot 到模型底部距離)
- **腳貼地效果**: 實現腳貼地效果，解決單位半身入土問題
- **Renderer 邊界**: 使用 Renderer.bounds 自動偵測模型底部
- **UnitAgent 整合**: footOffset 參數整合到 UnitAgent 系統
- **驗收**: 單位腳部正確貼地，無半身入土現象

#### M4-XT: 四合一配置管理工具包 ✅ 已完成 (2025-09-15)
- **ConfigMenuUnifier.cs**: 菜單統一工具，確保配置菜單命名一致
- **ConfigAssetEnforcer.cs**: 位置強制工具，自動檢查和創建缺失配置
- **ConfigSingletons.cs**: 單例管理工具，防止配置重複實例化
- **GameConfigLoadLock.cs**: 載入策略鎖定，確保配置載入順序正確
- **DAHLog 整合**: 所有配置工具統一使用 DAHLog.Info/Warn/Error 日誌系統
- **驗收**: 配置系統管理工具完備，支援統一日誌和錯誤處理

#### M4-T17i: BuildingCatalog UX 與 Binder 修復 ✅ 已完成 (2025-09-17)
- **BuildingCatalogBinder.cs**: 新增節流機制，防止過度驗證觸發
- **節流參數**: _verifyCooldownSec (2秒冷卻)、_maxResets (最多3次重置)
- **方法重命名**: ApplyPrefabToPlacer → TryApplyPrefabToPlacer (避免方法名衝突)
- **BuildingCatalogHUD_IMGUI.cs**: 新增可拖拽 IMGUI HUD，顯示建築目錄狀態
- **自動啟動**: RuntimeInitializeOnLoadMethod 自動初始化，DontDestroyOnLoad 持久化
- **編譯修復**: 修復 DelayedVerify 方法參數缺失、無效十六進制數字語法錯誤
- **驗收**: 建築目錄系統穩定運行，HUD 正常顯示，無編譯錯誤

#### M4-T20: 建築選擇系統完整修復 ✅ 已完成 (2025-09-18)
- **TMPro 編譯錯誤修復**: 在 DeepAbyssHive.Runtime.asmdef 中添加 Unity.TextMeshPro 引用
- **Tab/BackQuote 實時切換**: BuildingCatalogBinder.SyncPreviewToPlacer 添加自動進入放置模式
- **按鈕事件修復**: BuildingCatalogHUD 的 Prev/Next/Close 按鈕添加 GUI.FocusControl(null)
- **預覽同步優化**: 修復 TryEnterPlacingMode 邏輯，確保建築模型正確跟隨滑鼠
- **按鈕放置模式**: Select 方法添加 TryRefreshPreview 調用，確保按鈕點擊後立即顯示模型
- **系統回滾**: 移除有問題的 uGUI BuildingSelectionUI，恢復穩定的 IMGUI 系統
- **驗收**: Tab/BackQuote 實時切換建築、按鈕正常工作、預覽模型正確顯示、無需按兩次B鍵

---

## MVP 各個里程碑完成總結

## M2 階段完成總結
✅ **菌毯系統**: Frontier 擴張、門檻系統、冷卻機制  
✅ **地形生成**: Perlin 噪聲、Mesh/Collider 生成  
✅ **開發工具**: CreepBrushAndRunner、EditorFlyCamera

## M3 階段完成總結 ✅ 全面完成
✅ **建築放置系統**: 最小間距、格點對齊、旋轉支援、刪除工具、預覽一致性  
✅ **SpatialIndex 整合**: 委派系統、最近鄰檢查、向後兼容  
✅ **統一工具系統**: 遮罩管理、UI 工具、顏色訊息統一  
✅ **配置化設計**: GameConfig 統一參數管理、熱鍵集中配置  
✅ **開發工具**: SMOKE 測試擴充、刪除工具、狀態 HUD、Editor 菜單  
✅ **API 擴展**: 有向碰撞檢測、雙版本 API、完全向後兼容  
✅ **測試驗證**: 四種測試案例、自動化回歸驗證  
✅ **熱鍵管理**: 集中配置避免衝突、智能回退機制  

## M4 階段完成總結 ✅ 大部分完成 (2025-09-16)
✅ **單位路徑系統**: Grid A*、UnitAgent、動態障礙檢測、菌毯速度加成  
✅ **Assembly Definition 重構**: 統一程式集架構，解決循環依賴問題  
✅ **DAHLog 統一日誌**: 全專案 Debug.Log 遷移，支援分類過濾  
✅ **開發工具 HUD**: Health HUD、Key Hints HUD、統一 HUD 管理  
✅ **建築循環選取**: Tab 鍵循環選取建築，HUD 實時顯示  
✅ **建築預覽同步**: Tab/BackQuote 切換建築時預覽模型正確同步更新  
✅ **單位貼地系統**: 預算化貼地、走失保險、Foot Offset 自動偵測  
✅ **標準化工具包**: Editor 菜單統一、資產路徑規範、配置管理工具  
✅ **配置系統強化**: 四合一配置管理工具，統一日誌和錯誤處理  
#### M4-T05: ScenarioLoader QA 工具系統 ✅ 已完成 (2025-09-18)
- **ScenarioLoader.cs**: 反射式場景腳本載入器，支援多種操作類型
- **JSON 場景配置**: 支援 log、setCameraPos、toggleOverlay、creepCircle、spawnUnits 操作
- **GameConfig 整合**: scenarioPrevKey/scenarioNextKey/scenarioLoadKey/defaultScenarioName 熱鍵配置
- **反射對接**: 自動尋找 CreepBrushRunner、UnitDevSpawner、HUDOverlayController 等組件
- **預設場景**: blank、stress_200_units、expand_chokepoint 三個測試場景
- **自動啟動**: RuntimeInitializeOnLoadMethod 自動初始化，DontDestroyOnLoad 持久化
- **驗收**: PageUp/PageDown 切換場景，F7 載入執行，支援單位生成和菌毯繪製

⏳ **待完成**: Units SMOKE 測試系統 (M4-T07)




---
## 📌 附錄：2025-09-11 更新（M4 PathJobScheduler 診斷補丁完成）

### M4-T12 PathJobScheduler 診斷補丁完成 ✅ 2025-09-11
- **目的**: 平滑算路尖峰，避免同幀大量 UnitPathQueue.Enqueue 造成卡頓
- **實現**: PathJobScheduler 分幀配額調度器，每幀最多處理 N 筆算路請求
- **配置**: GameConfig.pathJobsPerFrame（預設 8），可調整每幀算路配額
- **診斷**: devVerboseLogs=true 時輸出 [PathSched] 統計信息
- **整合**: UnitAgent.SetDestination 改用 PathJobScheduler.Enqueue
- **兼容**: 保持既有 UnitPathQueue 算法不變，僅在觸發層面分幀
- **修復**: 解決 CS0102 重複欄位定義編譯錯誤

---

## 📌 附錄：2025-09-06 更新（M3 最終化＋M4 起跑）

### A) M3 完成清單（以 Neo4j / git 為準）
- ✅ **M3-T04** SpatialIndex 最近鄰委派支援（commit: 2145acf）
- ✅ **M3-T05** Placement SMOKE 腳本（commit: 35d9f52）
- ✅ **M3-T06** 建築旋轉支援（有向 OverlapBox）（commit: bef0651）
- ✅ **M3-T07** 建築刪除工具（可配置熱鍵）（commit: bef0651）
- ✅ **M3-T08** 旋轉步進外放 `GameConfig.rotationStepDegrees`（commit: ac542ad）
- ✅ **M3-T09** 預覽/放置一致性：重用 snapped 中心/旋轉/Bounds/驗證結果（commit: ＊見 git）
- ✅ **M3-T10** SMOKE 擴充：旋轉與刪除重建案例（Case C/D）（commit: ＊見 git）
- ✅ **M3-T11** Editor 菜單：**DeepAbyss → Configs → Create or Select GameConfig**（commit: ＊見 git）

> 進場期望 Console：  
> `[BOOT] ...`、`[DEV HUD] Game: ... snapSize=.., rotStep=.., smokeKey=.., delKey1=.., delKey2=..`、`[SMOKE] ... PASS`

---

### B) 放置驗證「行為契約」總結（以程式為準）
- **Result 型別**：`Result<T> { bool ok; PlaceResultCode code; T data?; string message? }`
- **錯誤碼**：`OK / E_PLACE_COLLISION / E_OUT_OF_BOUNDS / E_REQUIRE_CREEP / E_INVALID_TYPE`
- **統一入口**：`PlacementValidator.ValidateByConfig(center, halfExtents, rotation, blockMask, extraMargin)`
  - Physics：**有向** `OverlapBox`（rotation 生效）
  - SpatialIndex 並聯（開關 `GameConfig.useSpatialIndexForPlacement`）→ 任一失敗即 `E_PLACE_COLLISION`
  - MinSpacing：優先 `NoNeighborWithinRadiusPredicate(center, radius, mask)`；無委派時回退 `OverlapSphere`
  - Creep 要求：`RequireCreepPredicate(bounds)` 未通過 → `E_REQUIRE_CREEP`
  - 越界：`OutOfBoundsPredicate(bounds)` 為真 → `E_OUT_OF_BOUNDS`
  - **HUD 鉤子**：`PlacementValidator.LastResult` 供 `PlacementStatusHUD` 即時顯示
- **UI 一致性**：`PlacementUiUtil.ColorFor(result, forPreview)`（Preview 固定 α=0.35）、`PlacementUiUtil.TextFor(result)`
- **遮罩規則**：`PlacementLayerUtil.GetPlacementBlockMask()`（排除 `Terrain`/`Ignore Raycast`）；刪除工具用 `GetBuildingOnlyMask()`
- **預覽/放置一致性**：順序必為  
  `Snap(center) → SnapRotation → Bounds → ValidateByConfig(有向) → Tint`；`PlaceNow()` **重用**預覽快取結果

---

### C) Dev 熱鍵集中（GameConfig）
- `placementSmokeKey`（預設 F7）／`buildingDeleteKey1`（Delete）／`buildingDeleteKey2`（X）  
  > 任一設為 `None` → 回退對應元件的 Inspector 後備鍵。

---

### D) SMOKE（Placement）規格（F7）
- **Case A**：`d < minSpacing` → `E_PLACE_COLLISION`（PASS）
- **Case B**：`d > minSpacing` → `OK`（PASS）
- **Case C**：旋轉 45°/90°，邊界距離 → `OK`（PASS）
- **Case D**：刪除錨點後同點重試 → 非 `E_PLACE_COLLISION`（PASS）

---

### E) Editor 菜單
- **DeepAbyss → Configs → Create or Select GameConfig**  
  若無 `Assets/Resources/Configs/GameConfig.asset` → 自動建立並選取。

---

### F) M4：單位/路徑最小整合（當前里程碑）
**目標**：地面單位可從 A 走到 B，≈200 隻同時移動仍流暢；與 Creep/Building/Terrain 門檻協作。

**任務拆解**
1. **T01 Nav Grid 介面與取樣器**（`IPathGrid` + `PathGridSampler`）✅ 已完成  
   - Walkable：地面命中、坡度 ≤ `maxSlopeDegrees`、鄰近高差 ≤ `maxStepHeight`、無 Building 碰撞  
   - Cost：base=1；在 Creep 上乘 `creepCostMul`（預設 0.85）
2. **T02 輕量 Grid A\***（4/8 向，安全閥；Octile/Manhattan）✅ 已完成  
3. **T03 UnitAgent / UnitManager 管線**（請求佇列，每幀 N 筆，外放 `pathRequestsPerFrame`）✅ 已完成  
4. **T04 Dev Spawner & 指令**（集中熱鍵；預設 200 隻壓測）  
5. **T05 Creep 交互**（移速加成 on/off creep）  
6. **T06 障礙動態更新**（建築放置/刪除 → 路網局部無效化）✅ 已完成  
7. **T07 Units SMOKE**（輸出 req/s、avg ms、fail%）

**驗收標準**
- 單次求路 < 1.5ms（PC）；fail < 2%  
- 生成 200 隻，持續 10 秒不卡頓；SMOKE 綠燈  
- 與 M3 放置規則協作：新建築 → 可繞行；刪除 → 路徑恢復