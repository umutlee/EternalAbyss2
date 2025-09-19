using UnityEngine;
using System.Reflection;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Save
{
    /// <summary>
    /// 快存/快讀熱鍵監聽（預設 F5/F9），可由 GameConfig 覆蓋；隨遊戲啟動自動掛到 Managers。
    /// </summary>
    public class SaveLoadRunner : MonoBehaviour
    {
        internal static KeyCode SaveKey = KeyCode.F5;
        internal static KeyCode LoadKey = KeyCode.F9;
        internal static string Slot = "autosave";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<SaveLoadRunner>() != null) return;
            var go = new GameObject("SaveLoadRunner"); var r = go.AddComponent<SaveLoadRunner>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            TryLoadFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"SaveLoad: saveKey={SaveKey}, loadKey={LoadKey}, slot='{Slot}' (path={SaveLoadService.GetSlotPath(Slot)})");
            DAHLog.Info(LogCategory.SERVICE, "SaveLoadRunner created");
        }

        private void Update()
        {
            if (Input.GetKeyDown(SaveKey)) SaveLoadService.Save(Slot);
            if (Input.GetKeyDown(LoadKey)) SaveLoadService.Load(Slot);
        }

        private static void TryLoadFromGameConfig()
        {
            try
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    SaveKey = GetKey(cfg, "saveKey", SaveKey);
                    LoadKey = GetKey(cfg, "loadKey", LoadKey);
                    var s = GetString(cfg, "saveSlot", Slot); if (!string.IsNullOrEmpty(s)) Slot = s;
                    break;
                }
            } catch {}
        }

        private static KeyCode GetKey(object cfg, string name, KeyCode fallback)
        {
            var t = cfg.GetType();
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null && f.FieldType == typeof(KeyCode)) return (KeyCode)f.GetValue(cfg);
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null && p.PropertyType == typeof(KeyCode)) return (KeyCode)p.GetValue(cfg);
            return fallback;
        }
        private static string GetString(object cfg, string name, string fallback)
        {
            var t = cfg.GetType();
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null) return (string)f.GetValue(cfg);
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null) return (string)p.GetValue(cfg);
            return fallback;
        }
    }
}