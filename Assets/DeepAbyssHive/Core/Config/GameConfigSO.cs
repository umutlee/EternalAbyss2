using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Config
{
    /// <summary>
    /// 放置/驗證規則的全域設定（從 Resources/Configs/GameConfig.asset 讀取）
    /// </summary>
    [CreateAssetMenu(fileName = "GameConfig", menuName = "DeepAbyssHive/Configs/Game Config")]
    public class GameConfigSO : ScriptableObject
    {
        [Header("Placement Validation")]
        [Tooltip("啟用並聯校驗：Physics 與 SpatialIndex 皆須通過才允許放置")]
        public bool useSpatialIndexForPlacement = false;

        [Tooltip("放置物之間的最小中心間距（世界單位）")]
        public float minSpacing = 0.0f;

        [Tooltip("碰撞檢查額外外擴邊界（世界單位）")]
        public float margin = 0.0f;

        [Tooltip("是否要求必須在菌毯上才可放置")]
        public bool requireCreep = false;

        [Header("Placement UX")]
        [Tooltip("格點對齊步長（世界單位；0=關閉）。啟用後放置中心會對齊到該步長的網格。")]
        public float snapSize = 0.0f;

        [Tooltip("旋轉步進（度；0=關閉自由旋轉）。僅量化 Y 軸旋轉，並在預覽/放置均套用。")]
        public float rotationStepDegrees = 0.0f;

        [Header("Dev / Testing")]
        [Tooltip("Placement SMOKE 觸發鍵（None=停用）。")]
        public KeyCode placementSmokeKey = KeyCode.F7;
        [Tooltip("建築刪除主鍵（None=停用）。")]
        public KeyCode buildingDeleteKey1 = KeyCode.Delete;
        [Tooltip("建築刪除副鍵（None=停用）。")]
        public KeyCode buildingDeleteKey2 = KeyCode.X;
        [Tooltip("DEV：建築放置器切換鍵（None=停用）。")]
        public KeyCode buildPlacerToggleKey = KeyCode.B;
        [Tooltip("DEV：生成單位熱鍵（None=停用）。")]
        public KeyCode devUnitsSpawnKey = KeyCode.F9;
        [Tooltip("DEV：單位測試熱鍵（指派目標；None=停用）。")]
        public KeyCode devUnitsTestKey = KeyCode.F10;
        [Tooltip("DEV：一次生成的單位數量。")]
        public int devSpawnCount = 200;
        [Tooltip("DEV：啟用詳細日誌輸出（影響各系統的 Debug.Log 詳細程度）。")]
        public bool devVerboseLogs = false;

        [Header("Input / Cursor")]
        [Tooltip("右鍵是否鎖定游標（mouselook）。預設關閉以避免與建造/點地互動衝突。")]
        public bool rmbLocksCursor = false;

        // -------- Units / Dynamic obstacle guard (外放) --------
        [Header("Units / Dynamic Obstacle Guard")]
        [Tooltip("單位每隔幾秒檢測一次前方是否被 Building 層阻擋（<=0 表示使用元件預設）。")]
        public float unitDynCheckInterval = 0.5f;
        [Tooltip("連續 re-path 的冷卻（秒；<=0 表示使用元件預設）。")]
        public float unitDynRepathCooldown = 1.0f;
        [Tooltip("SphereCast 半徑（<=0 表示使用元件預設）。")]
        public float unitObstacleProbeRadius = 0.35f;
        [Tooltip("探測距離額外裕度（<0 表示使用元件預設）。")]
        public float unitObstacleProbeExtra = 0.5f;

        // -------- Building Watcher（外放）--------
        [Header("Runtime Watchers")]
        [Tooltip("Building 監看器掃描週期（秒；<=0 表示使用元件預設）。")]
        public float buildingWatcherInterval = 0.25f;
        [Tooltip("對建築 bounds 半徑的外擴量（<0 表示使用元件預設）。")]
        public float buildingWatcherPadRadius = 0.5f;

        // -------- Health Logger（外放）--------
        [Header("Health / Telemetry")]
        [Tooltip("是否啟用 Runtime 健康監測輸出（FPS/記憶體/單位/建築）。")]
        public bool healthLogEnabled = true;
        [Tooltip("健康監測輸出間隔（秒）。")]
        public float healthLogInterval = 10f;

        // -------- Pathfinding / Budget --------
        [Header("Pathfinding / Budget")]
        [Tooltip("每幀最多啟動的算路任務數（<=0 表示使用預設值 8）。")]
        public int pathJobsPerFrame = 8;

        [Header("Units × Creep")]
        [Tooltip("在 Creep 上的速度倍率（1 = 不變）。")]
        public float creepSpeedMul = 1.25f;
        [Tooltip("不在 Creep 上的速度倍率（1 = 不變）。")]
        public float offCreepSpeedMul = 1.0f;
        [Tooltip("UnitAgent 取樣是否在 Creep 上的週期（秒）。")]
        public float creepSampleInterval = 0.25f;

        [Header("Units × Batch Target Dispatch")]
        [Tooltip("目標派發分批大小。每批處理的單位數量，避免大量單位同時派發造成卡頓。")]
        public int batchTargetDispatchSize = 10;
        [Tooltip("分批派發間隔（秒）。每批之間的等待時間。")]
        public float batchTargetDispatchInterval = 0.1f;

        #region Buildings Catalog & Selection
        [Header("Buildings: Catalog & Selection")]
        [Tooltip("建築循環選取：下一個")]
        public KeyCode buildingCycleNextKey = KeyCode.Tab;
        [Tooltip("建築循環選取：上一個（建議 Shift+Tab 之外的備用鍵，如 BackQuote）")]
        public KeyCode buildingCyclePrevKey = KeyCode.BackQuote;
        [Tooltip("預設的建築選單（BuildingCatalogSO 資產）")]
        public DeepAbyssHive.Buildings.Config.BuildingCatalogSO buildingCatalog;
        #endregion

        [Header("Time Controls")]
        [Tooltip("暫停/恢復遊戲熱鍵（None=停用）")]
        public KeyCode pauseToggleKey = KeyCode.Space;
        [Tooltip("時間倍率循環熱鍵（1x→2x→4x→1x；None=停用）")]
        public KeyCode timeScaleCycleKey = KeyCode.T;

        [Header("Overlay Controls")]
        [Tooltip("地形覆蓋層切換熱鍵（None=停用）")]
        public KeyCode overlayTerrainKey = KeyCode.F3;
        [Tooltip("菌毯覆蓋層切換熱鍵（None=停用）")]
        public KeyCode overlayCreepKey = KeyCode.F4;
        [Tooltip("HUD 時間狀態字體大小")]
        public int hudTimeFontSize = 16;

        [Header("Save/Load Controls")]
        [Tooltip("快存熱鍵（None=停用）")]
        public KeyCode saveKey = KeyCode.F5;
        [Tooltip("快讀熱鍵（None=停用）")]
        public KeyCode loadKey = KeyCode.F9;
        [Tooltip("存檔槽位名稱")]
        public string saveSlot = "autosave";

        [Header("Creep Line Tool (Dev)")]
        [Tooltip("菌毯畫線工具切換熱鍵（None=停用）")]
        public KeyCode creepLineToolToggleKey = KeyCode.C;
        [Tooltip("菌毯畫線半徑（世界單位）")]
        public float creepLineRadius = 2.0f;
        [Tooltip("菌毯畫線步長（世界單位）")]
        public float creepLineStep = 0.5f;
        [Tooltip("地形 Raycast 層級遮罩（0=使用全部層）")]
        public int terrainRaycastMask = -1;

        [Header("Error Guard / Defensive")]
        [Tooltip("啟用錯誤防護��統")]
        public bool errorGuardEnabled = true;
        [Tooltip("每秒最多許可的錯誤日誌數量")]
        public int errorRateLimitPerSec = 5;
        [Tooltip("縮短堆疊追蹤輸出")]
        public bool shortenStackTrace = true;
        [Tooltip("測試錯誤觸發熱鍵（None=停用）")]
        public KeyCode throwTestErrorKey = KeyCode.F12;

        [Header("Dev HUD Controls")]
        [Tooltip("建築目錄 HUD 切換鍵（None=停用）")]
        public KeyCode buildingHudToggleKey = KeyCode.F8;
        [Tooltip("是否預設顯示建築目錄 HUD")]
        public bool showBuildingHUD = true;
    }

    /// <summary>
    /// 提供目前執行中的 Game 設定快取與載入
    /// </summary>
    public static class GameConfigProvider
    {
        private static GameConfigSO _current;

        /// <summary>
        /// 優先讀取 Resources/Configs/GameConfig；若無則回退為記憶體預設
        /// </summary>
        public static GameConfigSO Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<GameConfigSO>("Configs/GameConfig");
                    if (_current == null)
                    {
                        _current = ScriptableObject.CreateInstance<GameConfigSO>();
                        _current.name = "GameConfig (Runtime Default)";
                    }
                }
                return _current;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LogConfigOnLoad()
        {
            var cfg = Current;
            // 顯示新增外放參數，便於驗收與查錯（含 M4-T09 新增的 buildPlacerToggleKey/devVerboseLogs）
            DAHLog.Info(LogCategory.CONFIG, $"[DEV HUD] Game: useSpatialIndex={cfg.useSpatialIndexForPlacement}, minSpacing={cfg.minSpacing:0.###}, margin={cfg.margin:0.###}, requireCreep={cfg.requireCreep}, snapSize={cfg.snapSize:0.###}, rotStep={cfg.rotationStepDegrees:0.#}, smokeKey={cfg.placementSmokeKey}, delKey1={cfg.buildingDeleteKey1}, delKey2={cfg.buildingDeleteKey2}, placerToggle={cfg.buildPlacerToggleKey}, spawnKey={cfg.devUnitsSpawnKey}, testKey={cfg.devUnitsTestKey}, spawnCount={cfg.devSpawnCount}, verboseLogs={cfg.devVerboseLogs}, creepMul={cfg.creepSpeedMul:0.##}/{cfg.offCreepSpeedMul:0.##}, creepDt={cfg.creepSampleInterval:0.##}s, rmbLock={cfg.rmbLocksCursor}, unitDyn={cfg.unitDynCheckInterval:0.##}/{cfg.unitDynRepathCooldown:0.##}s, probe={cfg.unitObstacleProbeRadius:0.##}+{cfg.unitObstacleProbeExtra:0.##}, watcher={cfg.buildingWatcherInterval:0.##}s+{cfg.buildingWatcherPadRadius:0.##}, health={cfg.healthLogEnabled}/{cfg.healthLogInterval:0.##}s, pathJobs={cfg.pathJobsPerFrame}, buildingHUD={cfg.showBuildingHUD}/{cfg.buildingHudToggleKey}, pauseKey={cfg.pauseToggleKey}, timeScaleKey={cfg.timeScaleCycleKey}, overlayTerrain={cfg.overlayTerrainKey}, overlayCreep={cfg.overlayCreepKey}, hudTimeFont={cfg.hudTimeFontSize}, saveKey={cfg.saveKey}, loadKey={cfg.loadKey}, saveSlot='{cfg.saveSlot}', creepLineTool={cfg.creepLineToolToggleKey}, creepLineRadius={cfg.creepLineRadius:0.#}, creepLineStep={cfg.creepLineStep:0.#}, errorGuard={cfg.errorGuardEnabled}, errorRate={cfg.errorRateLimitPerSec}/s, shortStack={cfg.shortenStackTrace}, testErrorKey={cfg.throwTestErrorKey}");
        }
    }
}