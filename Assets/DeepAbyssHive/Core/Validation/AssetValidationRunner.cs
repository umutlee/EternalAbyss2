using System;
using System.Linq;
using UnityEngine;

namespace DeepAbyssHive.Core.Validation
{
    /// <summary>
    /// 在第一次場景載入後做一次輕量驗證。
    /// - Building Layer 存在性
    /// - GameConfig 可取用（反射取 Provider / SO，避免硬相依）
    /// - 存檔版本（若 GameConfig 有 expectedSaveVersion / saveVersionKey）
    /// 只輸出結構化日誌，不拋例外，不中止流程。
    /// </summary>
    public class AssetValidationRunner : MonoBehaviour
    {
        private static bool _created;
        private static readonly string CategoryHealth = "HEALTH";
        private static readonly string CategoryConfig = "CONFIG";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (_created) return;
            var go = new GameObject("Validation");
            DontDestroyOnLoad(go);
            go.AddComponent<AssetValidationRunner>();
            _created = true;
        }

        private void Start()
        {
            try
            {
                LogInfo(CategoryHealth, "Asset validation started");
                ValidateBuildingLayer();
                ValidateGameConfig(out object gameConfig);
                ValidateSaveVersion(gameConfig);
                LogConfigSnapshot(gameConfig);
                LogInfo(CategoryHealth, "Asset validation finished");
            }
            catch (Exception ex)
            {
                LogWarn(CategoryHealth, $"Asset validation aborted: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>檢查 Building Layer 是否存在。</summary>
        private void ValidateBuildingLayer()
        {
            var layerName = "Building";
            int layer = LayerMask.NameToLayer(layerName);
            if (layer < 0)
            {
                LogWarn(CategoryHealth, $"Building layer '{layerName}' not found. Deletion raycasts and placement may fail.");
            }
            else
            {
                LogInfo(CategoryHealth, $"Building layer OK: '{layerName}' (#{layer})");
            }
        }

        /// <summary>檢查 GameConfig 取得管道；有就回傳實例。</summary>
        private void ValidateGameConfig(out object gameConfig)
        {
            if (TryGetGameConfig(out gameConfig))
            {
                LogInfo(CategoryConfig, $"GameConfig located via provider/SO (type: {gameConfig.GetType().Name})");
            }
            else
            {
                LogWarn(CategoryConfig, "GameConfig not found via provider/SO. Using defaults may cause mismatch.");
            }
        }

        /// <summary>嘗試以寬鬆反射取得 GameConfig 實例（避免硬編譯相依）。</summary>
        private bool TryGetGameConfig(out object config)
        {
            // 尝试在記憶體中找到 ScriptableObject：GameConfigSO
            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("GameConfigSO", false))
                .FirstOrDefault(t => t != null) ??
                AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("DeepAbyssHive.Core.Config.GameConfigSO", false))
                .FirstOrDefault(t => t != null);

            if (type != null)
            {
                var all = Resources.FindObjectsOfTypeAll(type);
                if (all != null && all.Length > 0)
                {
                    config = all[0];
                    return true;
                }
            }

            config = null;
            return false;
        }

        /// <summary>若 GameConfig 具備 expectedSaveVersion / saveVersionKey，檢查當前 PlayerPrefs。</summary>
        private void ValidateSaveVersion(object cfg)
        {
            if (cfg == null) return;
            try
            {
                var t = cfg.GetType();
                var keyField = t.GetField("saveVersionKey") ?? t.GetField("SaveVersionKey");
                var expField = t.GetField("expectedSaveVersion") ?? t.GetField("ExpectedSaveVersion");

                string key = keyField?.GetValue(cfg) as string ?? "save_version";
                int expected = expField is null ? -1 : Convert.ToInt32(expField.GetValue(cfg));

                int actual = PlayerPrefs.GetInt(key, -1);
                if (expected >= 0)
                {
                    if (actual == -1) LogWarn(CategoryHealth, $"Save version missing. Expect={expected}, key='{key}'");
                    else if (actual != expected) LogWarn(CategoryHealth, $"Save version mismatch. Expect={expected}, Actual={actual}, key='{key}'");
                    else LogInfo(CategoryHealth, $"Save version OK: {actual}");
                }
            }
            catch (Exception ex)
            {
                LogWarn(CategoryHealth, $"Save version check skipped: {ex.GetType().Name}: {ex.Message}");
            }
        }

        /// <summary>輸出 GameConfig 關鍵參數快照</summary>
        private void LogConfigSnapshot(object cfg)
        {
            if (cfg == null) return;
            try
            {
                var t = cfg.GetType();
                var fields = t.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                var keyFields = fields.Where(f => f.Name.Contains("Key") || f.Name.Contains("Enabled") || f.Name.Contains("Size")).Take(3);
                
                foreach (var f in keyFields)
                {
                    var value = f.GetValue(cfg);
                    LogInfo(CategoryConfig, $"GameConfig.{f.Name} = {value}");
                }
            }
            catch (Exception ex)
            {
                LogWarn(CategoryConfig, $"Config snapshot failed: {ex.Message}");
            }
        }

        #region SmartConsole wrappers
        private void LogInfo(string cat, string msg) => DAHLogSafe("Info", cat, msg);
        private void LogWarn(string cat, string msg) => DAHLogSafe("Warn", cat, msg);

        private void DAHLogSafe(string level, string category, string message)
        {
            try
            {
                var logType = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => a.GetType("DeepAbyssHive.Core.Logging.DAHLog", false))
                    .FirstOrDefault(t => t != null);
                if (logType != null)
                {
                    var catEnum = AppDomain.CurrentDomain.GetAssemblies()
                        .Select(a => a.GetType("DeepAbyssHive.Core.Logging.LogCategory", false))
                        .FirstOrDefault(t => t != null);
                    object catValue = category;
                    if (catEnum != null)
                    {
                        foreach (var v in Enum.GetValues(catEnum))
                        {
                            if (string.Equals(v.ToString(), category, StringComparison.OrdinalIgnoreCase))
                            {
                                catValue = v; break;
                            }
                        }
                    }
                    var m = logType.GetMethods().FirstOrDefault(mi => mi.Name == level && mi.GetParameters().Length >= 2);
                    if (m != null)
                    {
                        var pars = m.GetParameters();
                        if (pars[0].ParameterType.IsEnum) m.Invoke(null, new object[] { catValue, message, null });
                        else m.Invoke(null, new object[] { category, message, null });
                        return;
                    }
                }
                Debug.Log($"[{category}] {message}");
            }
            catch
            {
                Debug.Log($"[{category}] {message}");
            }
        }
        #endregion
    }
}