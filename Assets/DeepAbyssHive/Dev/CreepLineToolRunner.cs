using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Dev
{
    /// <summary>
    /// Creep Line 定義工具：C 鍵切換，滑鼠拖曳畫線定義菌毯覆蓋路徑
    /// </summary>
    public class CreepLineToolRunner : MonoBehaviour
    {
        private bool _toolActive = false;
        private bool _drawing = false;
        private List<Vector3> _currentLine = new List<Vector3>();
        private Camera _camera;
        private LayerMask _raycastMask;
        private float _radius = 2.0f;
        private float _step = 0.5f;
        private KeyCode _toggleKey = KeyCode.C;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<CreepLineToolRunner>() != null) return;
            var go = new GameObject("CreepLineToolRunner");
            go.AddComponent<CreepLineToolRunner>();
            var managers = GameObject.Find("Managers");
            if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            DAHLog.Info(LogCategory.SERVICE, "CreepLineToolRunner created");
        }

        private void Start()
        {
            _camera = Camera.main ?? FindObjectOfType<Camera>();
            LoadConfigFromGameConfig();
        }

        private void Update()
        {
            HandleToggle();
            if (_toolActive) HandleDrawing();
        }

        private void OnGUI()
        {
            if (_toolActive)
            {
                GUI.color = Color.green;
                GUI.Label(new Rect(10, 50, 200, 20), "DEV: CreepLine Tool ON");
                GUI.color = Color.white;
            }
        }

        private void HandleToggle()
        {
            if (Input.GetKeyDown(_toggleKey))
            {
                _toolActive = !_toolActive;
                if (!_toolActive)
                {
                    _drawing = false;
                    _currentLine.Clear();
                }
                DAHLog.Info(LogCategory.SERVICE, $"CreepLine Tool: {(_toolActive ? "ON" : "OFF")}");
            }
        }

        private void HandleDrawing()
        {
            if (Input.GetMouseButtonDown(0))
            {
                _drawing = true;
                _currentLine.Clear();
                AddPointFromMouse();
            }
            else if (Input.GetMouseButton(0) && _drawing)
            {
                AddPointFromMouse();
            }
            else if (Input.GetMouseButtonUp(0) && _drawing)
            {
                _drawing = false;
                SubmitLine();
            }
        }

        private void AddPointFromMouse()
        {
            if (_camera == null) return;
            
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _raycastMask == 0 ? -1 : _raycastMask))
            {
                if (_currentLine.Count == 0 || Vector3.Distance(_currentLine[_currentLine.Count - 1], hit.point) > 0.1f)
                {
                    _currentLine.Add(hit.point);
                }
            }
        }

        private void SubmitLine()
        {
            if (_currentLine.Count < 2) return;

            var samples = SampleLine(_currentLine, _step);
            DAHLog.Info(LogCategory.SERVICE, $"[DEV] CreepLine submit: points={_currentLine.Count}, step={_step:0.#}, radius={_radius:0.#} -> samples={samples.Count}");

            bool painted = TryPaintCreepCircles(samples, _radius);
            if (painted)
            {
                DAHLog.Info(LogCategory.SERVICE, $"[CREEP] Painted {samples.Count} circles");
            }
            else
            {
                DAHLog.Warning(LogCategory.SERVICE, "[CREEP] No paint method found");
            }
        }

        private List<Vector3> SampleLine(List<Vector3> points, float step)
        {
            var samples = new List<Vector3>();
            if (points.Count < 2) return samples;

            for (int i = 0; i < points.Count - 1; i++)
            {
                var start = points[i];
                var end = points[i + 1];
                var distance = Vector3.Distance(start, end);
                var direction = (end - start).normalized;

                float currentDist = 0f;
                while (currentDist <= distance)
                {
                    samples.Add(start + direction * currentDist);
                    currentDist += step;
                }
            }
            
            // 確保終點被包含
            samples.Add(points[points.Count - 1]);
            return samples;
        }

        private bool TryPaintCreepCircles(List<Vector3> positions, float radius)
        {
            // 嘗試通過反射呼叫 CreepBrushRunner 或 CreepManager 的畫筆方法
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                // 嘗試 CreepBrushRunner
                var brushType = asm.GetType("CreepBrushRunner") ?? asm.GetType("DeepAbyssHive.Creep.Dev.CreepBrushRunner");
                if (brushType != null)
                {
                    var instance = FindObjectOfType(brushType);
                    if (instance != null)
                    {
                        var method = brushType.GetMethod("PaintCircle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            foreach (var pos in positions)
                            {
                                try { method.Invoke(instance, new object[] { pos, radius }); }
                                catch { }
                            }
                            return true;
                        }
                    }
                }

                // 嘗試 CreepManager
                var managerType = asm.GetType("CreepManager") ?? asm.GetType("DeepAbyssHive.Creep.Managers.CreepManager");
                if (managerType != null)
                {
                    var instance = FindObjectOfType(managerType);
                    if (instance != null)
                    {
                        var method = managerType.GetMethod("AddCreepSource", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) ??
                                   managerType.GetMethod("PlantSeed", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (method != null)
                        {
                            foreach (var pos in positions)
                            {
                                try { method.Invoke(instance, new object[] { pos }); }
                                catch { }
                            }
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private void LoadConfigFromGameConfig()
        {
            try
            {
                var cfg = GameConfigProvider.Current;
                if (cfg != null)
                {
                    _toggleKey = cfg.creepLineToolToggleKey;
                    _radius = cfg.creepLineRadius;
                    _step = cfg.creepLineStep;
                    _raycastMask = cfg.terrainRaycastMask;
                }
            }
            catch { }
        }

        private void OnDrawGizmos()
        {
            if (_toolActive && _currentLine.Count > 1)
            {
                Gizmos.color = Color.green;
                for (int i = 0; i < _currentLine.Count - 1; i++)
                {
                    Gizmos.DrawLine(_currentLine[i], _currentLine[i + 1]);
                }
                
                // 顯示取樣點
                Gizmos.color = Color.yellow;
                var samples = SampleLine(_currentLine, _step);
                foreach (var sample in samples)
                {
                    Gizmos.DrawWireSphere(sample, _radius * 0.1f);
                }
            }
        }
    }
}