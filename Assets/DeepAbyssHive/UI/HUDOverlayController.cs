using System;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.UI
{
    /// <summary>
    /// 覆蓋層切換控制（F3/F4）＋ 對外 Button 事件。透過反射嘗試驅動 Manager 上常見的覆蓋層旗標。
    /// 支援從 GameConfig 讀取自訂熱鍵。不存在時使用預設：F3=Terrain，F4=Creep。
    /// </summary>
    public class HUDOverlayController : MonoBehaviour
    {
        public static bool TerrainOverlayOn { get; private set; }
        public static bool CreepOverlayOn { get; private set; }

        internal static KeyCode TerrainKey = KeyCode.F3;
        internal static KeyCode CreepKey = KeyCode.F4;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<HUDOverlayController>() != null) return;
            var go = new GameObject("HUDOverlayController");
            var c = go.AddComponent<HUDOverlayController>();
            var managers = GameObject.Find("Managers"); 
            if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            TryLoadKeysFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"Overlays: terrainKey={TerrainKey}, creepKey={CreepKey}");
            DAHLog.Info(LogCategory.SERVICE, "HUDOverlayController created");
        }

        private void Update()
        {
            if (Input.GetKeyDown(TerrainKey)) ToggleTerrainOverlay();
            if (Input.GetKeyDown(CreepKey)) ToggleCreepOverlay();
        }

        public void ToggleTerrainOverlay()
        {
            SetTerrainOverlay(!TerrainOverlayOn);
        }

        public void ToggleCreepOverlay()
        {
            SetCreepOverlay(!CreepOverlayOn);
        }

        public static void SetTerrainOverlay(bool on)
        {
            TerrainOverlayOn = on;
            if (!ApplyFlagToManager(on, new[] { "TerrainManager", "ITerrainManager" },
                new[] { "showChunkOverlay", "showChunkBounds", "drawChunks", "overlayChunks", "overlayEnabled", "showGrid" }))
            {
                DAHLog.Info(LogCategory.UI, "Terrain overlay flag not applied to manager (no known field); internal state toggled only");
            }
            DAHLog.Info(LogCategory.UI, $"TerrainOverlay={(on ? "ON" : "OFF")}");
        }

        public static void SetCreepOverlay(bool on)
        {
            CreepOverlayOn = on;
            if (!ApplyFlagToManager(on, new[] { "CreepManager", "ICreepSimulationService", "ICreepGridService" },
                new[] { "showCreepOverlay", "drawCreepOverlay", "overlayCreep", "overlayEnabled", "debugOverlay" }))
            {
                DAHLog.Info(LogCategory.UI, "Creep overlay flag not applied to manager (no known field); internal state toggled only");
            }
            DAHLog.Info(LogCategory.UI, $"CreepOverlay={(on ? "ON" : "OFF")}");
        }

        private static bool ApplyFlagToManager(bool value, string[] typeNames, string[] flagNames)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var tn in typeNames)
            {
                var t = asm.GetType(tn) ?? asm.GetType("DeepAbyssHive.Terrain.Managers."+tn) ?? asm.GetType("DeepAbyssHive.Creep.Managers."+tn);
                if (t == null) continue;
                UnityEngine.Object target = FindRuntimeInstanceOf(t);
                if (target == null) continue;
                foreach (var fn in flagNames)
                {
                    var f = t.GetField(fn, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                    if (f != null && f.FieldType == typeof(bool)) { f.SetValue(target, value); return true; }
                    var p = t.GetProperty(fn, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
                    if (p != null && p.CanWrite && p.PropertyType == typeof(bool)) { p.SetValue(target, value); return true; }
                }
            }
            return false;
        }

        private static UnityEngine.Object FindRuntimeInstanceOf(Type t)
        {
            var all = GameObject.FindObjectsOfType<MonoBehaviour>(true);
            foreach (var mb in all) if (t.IsAssignableFrom(mb.GetType())) return mb;
            var inst = t.GetProperty("Instance", BindingFlags.Public|BindingFlags.Static)?.GetValue(null) as UnityEngine.Object;
            return inst;
        }

        private static void TryLoadKeysFromGameConfig()
        {
            try
            {
                var cfg = GameConfigProvider.Current;
                if (cfg == null) return;
                TerrainKey = cfg.overlayTerrainKey;
                CreepKey = cfg.overlayCreepKey;
            } 
            catch { }
        }
    }
}