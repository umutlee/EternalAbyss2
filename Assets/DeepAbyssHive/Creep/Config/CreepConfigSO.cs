using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Creep.Config
{
    /// <summary>
    /// Creep（菌毯）全域參數：從 Resources/Configs/CreepConfig.asset 載入。
    /// 目標：將擴張/阻擋/退火等門檻外放，避免硬编码。
    /// </summary>
    [CreateAssetMenu(fileName = "CreepConfig", menuName = "DeepAbyssHive/Configs/Creep Config")]
    public class CreepConfigSO : ScriptableObject
    {
        [Header("Terrain Constraints")]
        [Tooltip("菌毯可接受的最大坡度（度）。")]
        public float maxSlopeDegrees = 35f;

        [Tooltip("相鄰格允許的最大高差（世界座標單位）。")]
        public float maxStepHeight = 1.5f;

        [Header("Expansion Cooling")]
        [Tooltip("鄰居冷卻（影格數，0-255）。")]
        [Range(0, 255)]
        public int neighborCooldownFrames = 8;

        [Header("Blocking")]
        [Tooltip("阻擋菌毯擴張的 Layer Mask（預期含 Building 層）。")]
        public LayerMask buildingBlockMask;
    }

    /// <summary>
    /// 提供存取目前執行中的 Creep 設定（延遲載入，若缺失則用安全預設）。
    /// 不依賴現有 Manager，以降低整合風險（最小可行修補）。
    /// </summary>
    public static class CreepConfigProvider
    {
        private static CreepConfigSO _current;

        /// <summary>
        /// 取得目前 Creep 設定。優先讀取 Resources/Configs/CreepConfig。
        /// 若未建立資產，回退為記憶體預設（不寫回專案）。
        /// </summary>
        public static CreepConfigSO Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<CreepConfigSO>("Configs/CreepConfig");
                    if (_current == null)
                    {
                        // 建立安全預設，避免 NullRef，中途不落地到資產。
                        _current = ScriptableObject.CreateInstance<CreepConfigSO>();
                        _current.name = "CreepConfig (Runtime Default)";
                        // 若專案有定義 Building 層，預設阻擋來源為該層。
                        int buildingLayer = LayerMask.NameToLayer("Building");
                        if (buildingLayer >= 0)
                        {
                            _current.buildingBlockMask = 1 << buildingLayer;
                        }
                    }
                }
                return _current;
            }
        }

        /// <summary>
        /// 場景載入後輸出一次 DEV HUD 快照，協助驗收與除錯。
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LogConfigOnLoad()
        {
            var cfg = Current;
            // 以 DEV HUD 前綴輸出，與專案慣例一致；不影響 SMOKE。
            DAHLog.Info(LogCategory.CONFIG, $"[DEV HUD] Creep: slope={cfg.maxSlopeDegrees:0.#}°, step={cfg.maxStepHeight:0.###}, cooldown={cfg.neighborCooldownFrames}, blockMask={cfg.buildingBlockMask.value}");
        }
    }
}