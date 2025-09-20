using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.QA.Dev.Scenarios
{
    [Serializable] class Vec3 { public float x, y, z; public Vector3 ToV3() => new Vector3(x, y, z); }
    [Serializable] class Op
    {
        public string op;            // spawnUnits | creepCircle | toggleOverlay | setCameraPos | log
        public int count;
        public float scatter;
        public Vec3 near;
        public float radius;
        public Vec3[] points;
        public string target;        // terrain | creep
        public bool on;
        public string message;
        public Vec3 pos;
    }
    [Serializable] class ScenarioFile { public string name; public Op[] ops; }

    /// <summary>QA Scenario Loader：PageUp/Down 選擇、F7 載入。</summary>
    public class ScenarioLoader : MonoBehaviour
    {
        internal static KeyCode PrevKey = KeyCode.PageUp;
        internal static KeyCode NextKey = KeyCode.PageDown;
        internal static KeyCode LoadKey = KeyCode.F7;
        internal static string DefaultScenario = string.Empty;

        private List<ScenarioFile> _scenarios = new List<ScenarioFile>();
        private int _index = 0;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<ScenarioLoader>() != null) return;
            var go = new GameObject("ScenarioLoader"); go.AddComponent<ScenarioLoader>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            DAHLog.Info(LogCategory.SERVICE, "ScenarioLoader created");
        }

        private void Awake()
        {
            TryLoadKeysFromGameConfig();
            LoadAllScenarios();
            DAHLog.Info(LogCategory.CONFIG, $"Scenarios: {string.Join(", ", _scenarios.ConvertAll(s => s.name))}; keys=[prev:{PrevKey}, next:{NextKey}, load:{LoadKey}]");
            if (!string.IsNullOrEmpty(DefaultScenario))
            {
                var idx = _scenarios.FindIndex(s => string.Equals(s.name, DefaultScenario, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0) _index = idx;
            }
            AnnounceSelection();
        }

        private void Update()
        {
            if (_scenarios.Count == 0) return;
            if (Input.GetKeyDown(PrevKey)) { _index = (_index - 1 + _scenarios.Count) % _scenarios.Count; AnnounceSelection(); }
            if (Input.GetKeyDown(NextKey)) { _index = (_index + 1) % _scenarios.Count; AnnounceSelection(); }
            if (Input.GetKeyDown(LoadKey)) { RunScenario(_scenarios[_index]); }
        }

        private void AnnounceSelection() => DAHLog.Debug(LogCategory.COMMON, $"Scenario selected: {_scenarios[_index].name} ({_index+1}/{_scenarios.Count}) — press {LoadKey} to load");

        private void LoadAllScenarios()
        {
            _scenarios.Clear();
            var assets = Resources.LoadAll<TextAsset>("Scenarios");
            foreach (var ta in assets)
            {
                try
                {
                    var s = JsonUtility.FromJson<ScenarioFile>(ta.text);
                    if (s != null && !string.IsNullOrEmpty(s.name)) _scenarios.Add(s);
                }
                catch (Exception ex) { DAHLog.Debug(LogCategory.COMMON, "Scenario parse failed: " + ex.Message); }
            }
        }

        private void RunScenario(ScenarioFile s)
        {
            if (s?.ops == null || s.ops.Length == 0) { DAHLog.Debug(LogCategory.COMMON, $"Scenario '{s?.name}' has no ops"); return; }
            DAHLog.Debug(LogCategory.COMMON, $"Scenario RUN: {s.name} ops={s.ops.Length}");
            foreach (var op in s.ops) Execute(op);
            DAHLog.Debug(LogCategory.COMMON, "Scenario done.");
        }

        private void Execute(Op op)
        {
            switch (op.op)
            {
                case "log": DAHLog.Debug(LogCategory.COMMON, op.message ?? "<log>"); break;
                case "setCameraPos": if (Camera.main != null && op.pos != null) Camera.main.transform.position = op.pos.ToV3(); break;
                case "toggleOverlay": ToggleOverlay(op.target, op.on); break;
                case "creepCircle": if (op.points != null) foreach (var p in op.points) TryCreepPaintAt(p.ToV3(), Mathf.Max(0.1f, op.radius)); break;
                case "spawnUnits":
                    {
                        var pos = op.near != null ? op.near.ToV3() : Vector3.zero;
                        TrySpawnUnits(pos, Mathf.Max(1, op.count), Mathf.Max(0f, op.scatter));
                        break;
                    }
                default: DAHLog.Debug(LogCategory.COMMON, $"Unknown op: {op.op}"); break;
            }
        }

        private void ToggleOverlay(string target, bool on)
        {
            try
            {
                var t = Type.GetType("DeepAbyssHive.HUD.Main.HUDOverlayController");
                if (t != null)
                {
                    var m = target == "terrain" ? t.GetMethod("SetTerrainOverlay", BindingFlags.Public | BindingFlags.Static)
                                                  : t.GetMethod("SetCreepOverlay", BindingFlags.Public | BindingFlags.Static);
                    if (m != null) { m.Invoke(null, new object[] { on }); return; }
                }
            } catch {}
            DAHLog.Warning(LogCategory.COMMON, $"Overlay toggle not applied: target={target}");
        }

        private bool TryCreepPaintAt(Vector3 pos, float radius)
        {
            var types = new[] {
                "DeepAbyssHive.Creep.Tools.CreepBrushRunner",
                "DeepAbyssHive.Creep.Managers.CreepManager",
                "CreepBrushRunner",
                "CreepManager"
            };
            var methods = new[] { "PaintCircle", "ApplyCircle", "PaintAt", "ApplyAt", "BrushCircle" };
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var tn in types)
            {
                var t = asm.GetType(tn); if (t == null) continue;
                var target = UnityEngine.Object.FindObjectOfType(t); if (target == null) continue;
                foreach (var mn in methods)
                {
                    var m = t.GetMethod(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (m == null) continue;
                    var ps = m.GetParameters();
                    if (ps.Length >= 2 && ps[0].ParameterType == typeof(Vector3) && ps[1].ParameterType == typeof(float))
                    { try { m.Invoke(target, new object[] { pos, radius }); return true; } catch {} }
                }
            }
            return false;
        }

        private bool TrySpawnUnits(Vector3 near, int count, float scatter)
        {
            var types = new[] {
                "DeepAbyssHive.Units.Dev.UnitDevSpawner",
                "UnitDevSpawner",
                "DeepAbyssHive.Units.Managers.UnitManager",
                "UnitManager"
            };
            var methods = new[] { "SpawnAt", "DevSpawn", "Spawn" };
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var tn in types)
            {
                var t = asm.GetType(tn); if (t == null) continue;
                var target = UnityEngine.Object.FindObjectOfType(t); if (target == null) continue;
                foreach (var mn in methods)
                {
                    var m = t.GetMethod(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (m == null) continue;
                    var ps = m.GetParameters();
                    try
                    {
                        if (ps.Length == 3 && ps[0].ParameterType == typeof(Vector3) && ps[1].ParameterType == typeof(int) && ps[2].ParameterType == typeof(float))
                        { m.Invoke(target, new object[] { near, count, scatter }); return true; }
                        if (ps.Length == 2 && ps[0].ParameterType == typeof(Vector3) && ps[1].ParameterType == typeof(int))
                        { m.Invoke(target, new object[] { near, count }); return true; }
                        if (ps.Length == 1 && ps[0].ParameterType == typeof(Vector3))
                        { for (int i=0;i<count;i++) m.Invoke(target, new object[] { near + UnityEngine.Random.insideUnitSphere * scatter }); return true; }
                    }
                    catch {}
                }
            }
            DAHLog.Warning(LogCategory.COMMON, "No spawn method found via reflection");
            return false;
        }

        private void TryLoadKeysFromGameConfig()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    PrevKey = GetKey(cfg, "scenarioPrevKey", PrevKey);
                    NextKey = GetKey(cfg, "scenarioNextKey", NextKey);
                    LoadKey = GetKey(cfg, "scenarioLoadKey", LoadKey);
                    DefaultScenario = GetString(cfg, "defaultScenarioName", DefaultScenario);
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