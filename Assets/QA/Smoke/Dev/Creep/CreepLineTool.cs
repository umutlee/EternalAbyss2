using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.QA.Dev.Creep
{
    /// <summary>
    /// 開發用「畫線定義菌毯」工具：C 切換；左鍵拖曳，放開提交；步長取樣並以半徑刷入菌毯。
    /// 以反射對接 Creep 畫筆，盡量匹配常見方法：
    ///  - PaintCircle(Vector3 pos, float radius)
    ///  - ApplyCircle(Vector3 pos, float radius)
    ///  - PaintAt(Vector3 pos, float radius)
    /// 找不到則只記錄警告，不崩潰。
    /// </summary>
    public class CreepLineTool : MonoBehaviour
    {
        internal static KeyCode ToggleKey = KeyCode.C;
        internal static float Radius = 2f;
        internal static float Step = 0.5f;
        internal static LayerMask RayMask = ~0; // default: everything

        private bool _enabled;
        private bool _dragging;
        private readonly List<Vector3> _points = new List<Vector3>(256);
        private LineRenderer _lr;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<CreepLineTool>() != null) return;
            var go = new GameObject("Dev-CreepLineTool"); var t = go.AddComponent<CreepLineTool>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            TryLoadFromGameConfig();
            DAHLog.Info(LogCategory.CONFIG, $"CreepLineTool: key={ToggleKey}, radius={Radius}, step={Step}, mask={RayMask.value}"); 
            DAHLog.Info(LogCategory.SERVICE, "CreepLineTool created");
        }

        private void Awake()
        {
            _lr = gameObject.AddComponent<LineRenderer>();
            _lr.positionCount = 0;
            _lr.material = new Material(Shader.Find("Sprites/Default"));
            _lr.widthMultiplier = 0.1f;
            _lr.startColor = _lr.endColor = Color.green;
            _lr.enabled = false;
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey))
            {
                _enabled = !_enabled;
                _lr.enabled = _enabled;
                _points.Clear(); _lr.positionCount = 0;
                DAHLog.Info(LogCategory.SERVICE, $"CreepLine Tool {(_enabled ? "ON" : "OFF")}");
            }
            if (!_enabled) return;

            // Begin drag
            if (!_dragging && Input.GetMouseButtonDown(0))
            {
                _dragging = true;
                _points.Clear();
                TryAddPointFromMouse();
            }
            // Dragging
            if (_dragging && Input.GetMouseButton(0))
            {
                TryAddPointFromMouse();
            }
            // End drag -> submit
            if (_dragging && Input.GetMouseButtonUp(0))
            {
                _dragging = false;
                SubmitStroke();
                _points.Clear(); _lr.positionCount = 0;
            }
        }

        private void OnGUI()
        {
            if (_enabled)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(10, 70, 200, 20), "DEV: CreepLine Tool ON");
                GUI.color = Color.white;
            }
        }

        private void TryAddPointFromMouse()
        {
            var ray = Camera.main != null ? Camera.main.ScreenPointToRay(Input.mousePosition) : new Ray();
            if (Physics.Raycast(ray, out var hit, 10000f, RayMask))
            {
                var p = hit.point;
                if (_points.Count == 0 || Vector3.Distance(_points[_points.Count - 1], p) > Mathf.Max(0.05f, Step * 0.5f))
                {
                    _points.Add(p);
                    _lr.positionCount = _points.Count;
                    _lr.SetPositions(_points.ToArray());
                }
            }
        }

        private void SubmitStroke()
        {
            if (_points.Count < 2) return;
            int samples = 0, painted = 0;
            for (int i = 0; i < _points.Count - 1; i++)
            {
                var a = _points[i]; var b = _points[i + 1];
                float dist = Vector3.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.CeilToInt(dist / Mathf.Max(0.001f, Step)));
                for (int s = 0; s <= steps; s++)
                {
                    var t = s / (float)steps;
                    var p = Vector3.Lerp(a, b, t);
                    samples++;
                    if (TryPaintAt(p, Radius)) painted++;
                }
            }
            DAHLog.Info(LogCategory.SERVICE, $"[DEV] CreepLine submit: points={_points.Count}, step={Step}, radius={Radius} -> samples={samples}");
            if (painted > 0) DAHLog.Info(LogCategory.SERVICE, $"[CREEP] Painted {painted} circles"); 
            else DAHLog.Warning(LogCategory.SERVICE, "[CREEP] No paint method found (reflection)"); 
        }

        private static bool TryPaintAt(Vector3 pos, float radius)
        {
            // 嘗試幾個常見類別與方法
            var candidatesTypes = new[] {
                "DeepAbyssHive.Creep.Tools.CreepBrushRunner",
                "DeepAbyssHive.Creep.Managers.CreepManager",
                "CreepBrushRunner",
                "CreepManager"
            };
            var candidatesMethods = new[] {
                "PaintCircle", "ApplyCircle", "PaintAt", "ApplyAt", "BrushCircle"
            };
            
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                foreach (var tn in candidatesTypes)
                {
                    var t = asm.GetType(tn);
                    if (t == null) continue;
                    var target = UnityEngine.Object.FindObjectOfType(t) as UnityEngine.Object;
                    if (target == null) continue;
                    
                    foreach (var mn in candidatesMethods)
                    {
                        var m = t.GetMethod(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (m == null) continue;
                        var ps = m.GetParameters();
                        
                        // Try (Vector3,float) signature first
                        try
                        {
                            if (ps.Length >= 2 && ps[0].ParameterType == typeof(Vector3) && ps[1].ParameterType == typeof(float))
                            {
                                var args = new object[ps.Length];
                                args[0] = pos; 
                                args[1] = radius;
                                for (int i = 2; i < ps.Length; i++) args[i] = null;
                                m.Invoke(target, args);
                                return true;
                            }
                        }
                        catch (Exception)
                        {
                            // Continue trying other methods
                        }
                    }
                }
            }
            return false;
        }

        private static void TryLoadFromGameConfig()
        {
            try
            {
                foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var p = asm.GetType("GameConfigProvider") ?? asm.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                    if (p == null) continue;
                    var cfg = p.GetProperty("Current", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);
                    if (cfg == null) continue;
                    
                    ToggleKey = GetKey(cfg, "creepLineToolToggleKey", ToggleKey);
                    Radius = GetFloat(cfg, "creepLineRadius", Radius);
                    Step = Mathf.Max(0.05f, GetFloat(cfg, "creepLineStep", Step));
                    RayMask = GetInt(cfg, "terrainRaycastMask", RayMask.value);
                    break;
                }
            }
            catch (Exception)
            {
                // Use defaults
            }
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
        
        private static float GetFloat(object cfg, string name, float fallback)
        {
            var t = cfg.GetType();
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null) return Convert.ToSingle(f.GetValue(cfg));
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null) return Convert.ToSingle(p.GetValue(cfg));
            return fallback;
        }
        
        private static int GetInt(object cfg, string name, int fallback)
        {
            var t = cfg.GetType();
            var f = t.GetField(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (f != null) return Convert.ToInt32(f.GetValue(cfg));
            var p = t.GetProperty(name, BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);
            if (p != null) return Convert.ToInt32(p.GetValue(cfg));
            return fallback;
        }
    }
}