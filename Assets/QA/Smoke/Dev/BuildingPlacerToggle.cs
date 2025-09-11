using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Common.Placement;

namespace DeepAbyssHive.QA.Smoke.Dev
{
    /// <summary>
    /// [EA-M4-T09|2025-09-11] 建築放置器切換工具
    /// - 透過熱鍵切換 BuildingPlacer 的啟用/停用狀態
    /// - 優先使用 GameConfig.buildPlacerToggleKey，None 時回退到 Inspector 設定
    /// - 外掛設計，不修改既有 BuildingPlacer 邏輯
    /// - 支援多個 BuildingPlacer 同時切換
    /// </summary>
    public class BuildingPlacerToggle : MonoBehaviour
    {
        [Header("Fallback Settings")]
        [Tooltip("GameConfig.buildPlacerToggleKey 為 None 時的後備按鍵")]
        public KeyCode fallbackToggleKey = KeyCode.B;
        
        [Tooltip("是否在切換時輸出日誌")]
        public bool logToggleActions = true;

        private BuildingPlacer[] _placers;
        private bool _lastToggleState = false;

        void Start()
        {
            // 找到場景中所有的 BuildingPlacer
            _placers = FindObjectsOfType<BuildingPlacer>();
            
            if (_placers.Length == 0)
            {
                Debug.LogWarning("[BuildingPlacerToggle] 場景中未找到 BuildingPlacer 組件");
                enabled = false;
                return;
            }

            var cfg = GameConfigProvider.Current;
            bool useVerbose = cfg != null && cfg.devVerboseLogs;
            
            if (useVerbose || logToggleActions)
            {
                Debug.Log($"[BuildingPlacerToggle] 找到 {_placers.Length} 個 BuildingPlacer，準備切換控制");
            }
        }

        void Update()
        {
            var cfg = GameConfigProvider.Current;
            
            // 決定使用的按鍵：優先 GameConfig，None 時回退
            KeyCode effectiveKey = (cfg != null && cfg.buildPlacerToggleKey != KeyCode.None) 
                ? cfg.buildPlacerToggleKey 
                : fallbackToggleKey;
            
            // 如果兩者都是 None，停用功能
            if (effectiveKey == KeyCode.None) return;

            // 檢測按鍵按下
            bool currentToggleState = Input.GetKeyDown(effectiveKey);
            
            if (currentToggleState && !_lastToggleState)
            {
                TogglePlacers();
            }
            
            _lastToggleState = currentToggleState;
        }

        private void TogglePlacers()
        {
            if (_placers == null || _placers.Length == 0) return;

            // 以第一個 BuildingPlacer 的狀態為基準決定切換方向
            bool targetState = !_placers[0].enabled;
            
            foreach (var placer in _placers)
            {
                if (placer != null)
                {
                    placer.enabled = targetState;
                }
            }

            var cfg = GameConfigProvider.Current;
            bool useVerbose = cfg != null && cfg.devVerboseLogs;
            
            if (useVerbose || logToggleActions)
            {
                string stateText = targetState ? "啟用" : "停用";
                Debug.Log($"[BuildingPlacerToggle] {stateText} {_placers.Length} 個 BuildingPlacer");
            }
        }

        void OnValidate()
        {
            // Inspector 中的提示
            if (fallbackToggleKey == KeyCode.None)
            {
                Debug.LogWarning("[BuildingPlacerToggle] fallbackToggleKey 設為 None，請確保 GameConfig.buildPlacerToggleKey 有效");
            }
        }
    }
}