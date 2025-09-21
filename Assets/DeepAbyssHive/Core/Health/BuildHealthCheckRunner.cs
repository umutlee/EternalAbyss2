using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeepAbyssHive.Core.Health
{
    public class BuildHealthCheckRunner : MonoBehaviour
    {
        private const string FirstLaunchKey = "dah_first_launch_done";
        private static bool _created;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Ensure()
        {
            if (_created) return;
            var go = new GameObject("BuildHealthCheck");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<BuildHealthCheckRunner>();
            _created = true;
        }

        private void Start()
        {
            try
            {
                Info("BOOT", "BuildHealthCheck start");
                var isDevBuild = Debug.isDebugBuild;
                Info("CONFIG", $"isDevBuild={isDevBuild}, platform={Application.platform}, product={Application.productName}");

                DumpSystemSnapshot();
                QuickChecks();

                if (!PlayerPrefs.HasKey(FirstLaunchKey))
                {
                    Info("HEALTH", "First launch detected -> extended snapshot");
                    ExtendedSnapshot();
                    PlayerPrefs.SetInt(FirstLaunchKey, 1);
                    PlayerPrefs.Save();
                }

                Info("BOOT", "BuildHealthCheck done");
            }
            catch (Exception ex)
            {
                Warn("HEALTH", $"BuildHealthCheck aborted: {ex.GetType().Name}: {ex.Message}");
            }
        }

        private void DumpSystemSnapshot()
        {
            Info("HEALTH", $"Unity {Application.unityVersion}, device={SystemInfo.deviceName}/{SystemInfo.deviceModel}, os={SystemInfo.operatingSystem}");
            Info("HEALTH", $"CPU={SystemInfo.processorType} x{SystemInfo.processorCount}, RAM={SystemInfo.systemMemorySize} MB");
            Info("HEALTH", $"GPU={SystemInfo.graphicsDeviceName} {SystemInfo.graphicsDeviceVersion}, VRAM={SystemInfo.graphicsMemorySize} MB, API={SystemInfo.graphicsDeviceType}");
            Info("CONFIG", $"Quality={QualitySettings.names[QualitySettings.GetQualityLevel()]}, vSync={QualitySettings.vSyncCount}, targetFPS={Application.targetFrameRate}");
            Info("CONFIG", $"ColorSpace={QualitySettings.activeColorSpace}, RP={(GraphicsSettings.currentRenderPipeline ? GraphicsSettings.currentRenderPipeline.name : "Built-in")}");
        }

        private void QuickChecks()
        {
            if (QualitySettings.vSyncCount == 0 && Application.targetFrameRate <= 0)
                Warn("HEALTH", "No vSync and targetFrameRate <= 0 -> FPS unlocked (expected?)");

#if !UNITY_EDITOR
            if (Application.genuineCheckAvailable && !Application.genuine)
                Warn("HEALTH", "Application not genuine (store integrity check failed).");
#endif
        }

        private void ExtendedSnapshot()
        {
            Info("HEALTH", $"Screen={Screen.currentResolution.width}x{Screen.currentResolution.height}@{Screen.currentResolution.refreshRateRatio}");
            Info("HEALTH", $"SystemLanguage={Application.systemLanguage}, InstallMode={Application.installMode}, SandboxType={Application.sandboxType}");
        }

        private void Info(string cat, string msg) => LogSafe("Info", cat, msg);
        private void Warn(string cat, string msg) => LogSafe("Warn", cat, msg);

        private void LogSafe(string level, string category, string message)
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
                            if (string.Equals(v.ToString(), category, StringComparison.OrdinalIgnoreCase)) { catValue = v; break; }
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
            catch { Debug.Log($"[{category}] {message}"); }
        }
    }
}