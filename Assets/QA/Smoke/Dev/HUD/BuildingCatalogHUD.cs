using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DeepAbyssHive.QA.Smoke.Dev.HUD
{
    /// <summary>
    /// 建築選單 HUD（IMGUI Minimal）
    /// - 讀取 Resources/Configs/BuildingCatalog.asset（或 GameConfig.buildingCatalog）
    /// - 顯示水平按鈕列：點選即切換預覽到對應 prefab
    /// - 支援以 GameConfig.buildingHudToggleKey（字串對應 KeyCode）顯示/隱藏；預設 F8
    /// - Runtime/Editor 皆可顯示（供日後客戶端沿用）
    /// - 日誌分類：HUD / PLACEMENT / CONFIG（透過 DAHLog / DLog 橋接）
    /// </summary>
    public sealed class BuildingCatalogHUD : MonoBehaviour
    {
        private const string PrefKeyRect = "DAH.BuildingCatalogHUD.Rect";
        private static BuildingCatalogHUD s_instance;

        private Rect _rect = new Rect(12, 12, 800, 300);
        private bool _visible = true;
        private KeyCode _toggle = KeyCode.Z;
        
        // 允許拖曳/調整大小
        private bool _resizing;
        private Vector2 _resizeStartMouse;
        private Rect _resizeStartRect;
        private Vector2 _scroll;

        // Catalog 緩存
        private UnityEngine.Object _catalogAsset;
        private int _count;
        private string[] _ids;
        private GameObject[] _prefabs;
        private int _current = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (s_instance != null) return;
            var go = new GameObject("DevHUD_BuildingCatalog");
            UnityEngine.Object.DontDestroyOnLoad(go);
            s_instance = go.AddComponent<BuildingCatalogHUD>();
        }

        private void Awake()
        {
            LoadRect();
            ReadConfig(out var showHUD, out var toggleKey, out var fromCfgCatalog);
            _visible = showHUD ?? true;
            if (toggleKey.HasValue) _toggle = toggleKey.Value;

            _catalogAsset = fromCfgCatalog ?? Resources.Load("Configs/BuildingCatalog");
            if (_catalogAsset == null)
            {
                Log("CONFIG", "BuildingCatalog not found (Resources/Configs/BuildingCatalog). HUD disabled.");
                _visible = false;
                return;
            }
            ExtractCatalog(_catalogAsset);
            Log("CONFIG", $"BuildingCatalogHUD ready: entries={_count}, toggle={_toggle}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggle))
            {
                _visible = !_visible;
                Log("HUD", $"BuildingCatalogHUD visible = {_visible}");
                if (!_visible) SaveRect();
            }
        }

        private void OnGUI()
        {
            if (!_visible || _count == 0) return;
            
            // 顯示主視窗
            _rect = GUI.Window(0x17001, _rect, DrawWindow, "Building Catalog");

            // 右下角 resize handle（12x12）
            var handle = new Rect(_rect.xMax - 14, _rect.yMax - 14, 12, 12);
            var e = Event.current;
#if UNITY_EDITOR
            UnityEditor.EditorGUIUtility.AddCursorRect(handle, UnityEditor.MouseCursor.ResizeUpLeft);
#endif
            if (e.type == EventType.MouseDown && handle.Contains(e.mousePosition))
            {
                _resizing = true;
                _resizeStartMouse = e.mousePosition;
                _resizeStartRect = _rect;
                e.Use();
            }
            if (_resizing && e.type == EventType.MouseDrag)
            {
                var delta = e.mousePosition - _resizeStartMouse;
                _rect.width = Mathf.Max(600f, _resizeStartRect.width + delta.x);
                _rect.height = Mathf.Max(250f, _resizeStartRect.height + delta.y);
                e.Use();
            }
            if (_resizing && (e.type == EventType.MouseUp || e.rawType == EventType.MouseUp))
            {
                _resizing = false;
                SaveRect();
                e.Use();
            }

            // 夾在螢幕範圍內
            var oldRect = _rect;
            _rect.x = Mathf.Clamp(_rect.x, 0, Screen.width - _rect.width);
            _rect.y = Mathf.Clamp(_rect.y, 0, Screen.height - _rect.height);
            
            // 如果位置改變了，保存
            if (oldRect.x != _rect.x || oldRect.y != _rect.y || oldRect.width != _rect.width || oldRect.height != _rect.height)
            {
                SaveRect();
            }
        }

        private void DrawWindow(int id)
        {
            if (_ids == null) return;

            // 讓上方 20px 成為拖曳列（避免只能拖一次的問題）
            GUI.DragWindow(new Rect(0, 0, _rect.width, 20));

            GUILayout.Space(2);
            GUILayout.Label("Click a building to select. (Tab/BackQuote hotkeys still work)");

            // 動態高度：扣掉標題/邊距後的剩餘空間，至少 120px
            float contentH = Mathf.Max(120f, _rect.height - 140f);
            _scroll = GUILayout.BeginScrollView(_scroll, true, true, GUILayout.Height(contentH));
            
            // 網格列（每行 5 個按鈕）
            var style = new GUIStyle(GUI.skin.button){ alignment = TextAnchor.MiddleCenter, padding = new RectOffset(4,4,4,4)};
            int cols = 5;
            for (int row = 0; row < Mathf.CeilToInt((float)_ids.Length / cols); row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < cols; col++)
                {
                    int i = row * cols + col;
                    if (i >= _ids.Length) break;
                    
                    bool isCurrent = (i == _current);
                    var color = isCurrent ? Color.yellow : Color.white;
                    GUI.backgroundColor = color;
                    
                    if (GUILayout.Button($"{i:00}\n{_ids[i]}", style, GUILayout.Width(140), GUILayout.Height(50)))
                    {
                        Select(i);
                    }
                }
                GUI.backgroundColor = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();

            // 當前
            string curName = (_current >= 0 && _current < _ids.Length) ? _ids[_current] : "(none)";
            GUILayout.Space(6);
            GUILayout.Label($"Current: {_current:00}  {curName}", Mini());

            GUILayout.Space(4);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("◀ Prev", GUILayout.Width(80), GUILayout.Height(24))) Cycle(-1);
            if (GUILayout.Button("Next ▶", GUILayout.Width(80), GUILayout.Height(24))) Cycle(+1);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(72), GUILayout.Height(22))) { _visible = false; SaveRect(); }
            GUILayout.EndHorizontal();

            // 底部畫一個簡單的 ↘ 提示
            var bottom = GUILayoutUtility.GetRect(16, 12);
            var hint = new Rect(_rect.width - 18, bottom.yMin, 14, 12);
            GUI.Label(hint, "↘");
        }

        private GUIStyle Mini()
        {
            var s = new GUIStyle(GUI.skin.label);
            s.fontSize = 11; s.normal.textColor = Color.gray;
            return s;
        }

        private void Cycle(int dir)
        {
            if (_count == 0) return;
            int next = (_current + dir + _count) % _count;
            Select(next);
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _count) return;
            var prefab = _prefabs[index];
            if (prefab == null) { Log("PLACEMENT", $"Catalog entry {index} has null prefab."); return; }

            _current = index;
            // 優先呼叫 T17f Binder
            var binder = Type.GetType("DeepAbyssHive.Buildings.Runtime.BuildingCatalogBinder, Assembly-CSharp");
            var m = binder?.GetMethod("ApplyPrefabToPlacer", BindingFlags.Public|BindingFlags.Static);
            if (m != null) { m.Invoke(null, new object[]{ prefab, _ids[index], index }); }
            else { TryApplyToPlacer(prefab); }

            Log("PLACEMENT", $"Select → idx={index:00}, id={_ids[index]}, prefab={prefab.name}");
        }

        private void TryApplyToPlacer(GameObject prefab)
        {
            try
            {
                var placer = FindObjectOfTypeByNameContains("BuildingPlacer");
                if (placer == null) { Log("PLACEMENT", "No BuildingPlacer found in scene."); return; }
                var t = placer.GetType();
                var f = t.GetField("previewPrefab") ?? t.GetField("currentPrefab") ?? t.GetField("prefab");
                var p = t.GetProperty("PreviewPrefab") ?? t.GetProperty("CurrentPrefab") ?? t.GetProperty("Prefab");
                if (f != null) f.SetValue(placer, prefab);
                else if (p != null && p.CanWrite) p.SetValue(placer, prefab);
                var refresh = t.GetMethod("RefreshPreview") ?? t.GetMethod("RebuildPreview") ?? t.GetMethod("RecreatePreview");
                refresh?.Invoke(placer, null);
            }
            catch (Exception e)
            {
                Log("PLACEMENT", "Apply to placer failed: " + e.Message);
            }
        }

        private static Component FindObjectOfTypeByNameContains(string namePart)
        {
            var all = FindObjectsByType<Component>(FindObjectsSortMode.None);
            for (int i=0;i<all.Length;i++)
            {
                if (all[i] == null) continue;
                var t = all[i].GetType();
                if (t.Name.IndexOf(namePart, StringComparison.OrdinalIgnoreCase) >= 0) return all[i];
            }
            return null;
        }

        private void ExtractCatalog(UnityEngine.Object asset)
        {
            var t = asset.GetType();
            var entriesField = t.GetField("entries", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                             ?? t.GetField("items", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)
                             ?? t.GetField("list", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (entriesField == null)
            {
                Log("CONFIG", "Catalog has no 'entries' field. HUD disabled.");
                _count = 0; return;
            }
            var listObj = entriesField.GetValue(asset) as System.Collections.IEnumerable;
            if (listObj == null) { _count = 0; return; }

            var ids = new List<string>();
            var prefabs = new List<GameObject>();
            foreach (var e in listObj)
            {
                if (e == null) continue;
                var et = e.GetType();
                string id = (et.GetField("id")?.GetValue(e) as string)
                         ?? (et.GetProperty("id")?.GetValue(e) as string)
                         ?? (et.GetProperty("Id")?.GetValue(e) as string) ?? string.Empty;
                var pf  = (et.GetField("prefab")?.GetValue(e) as GameObject)
                       ?? (et.GetProperty("prefab")?.GetValue(e) as GameObject)
                       ?? (et.GetProperty("Prefab")?.GetValue(e) as GameObject);
                if (pf != null)
                {
                    ids.Add(string.IsNullOrEmpty(id) ? pf.name : id);
                    prefabs.Add(pf);
                }
            }
            _ids = ids.ToArray();
            _prefabs = prefabs.ToArray();
            _count = _ids.Length;
            if (_count > 0) _current = Mathf.Clamp(_current, 0, _count-1);
        }

        private void LoadRect()
        {
#if UNITY_EDITOR
            string s = UnityEditor.EditorPrefs.GetString(PrefKeyRect, string.Empty);
#else
            string s = PlayerPrefs.GetString(PrefKeyRect, string.Empty);
#endif
            if (string.IsNullOrEmpty(s)) return;
            var p = s.Split(',');
            if (p.Length==4 &&
                float.TryParse(p[0], out var x) &&
                float.TryParse(p[1], out var y) &&
                float.TryParse(p[2], out var w) &&
                float.TryParse(p[3], out var h))
            {
                _rect = new Rect(x,y,w,h);
            }
        }
        private void SaveRect()
        {
            string s = $"{_rect.x:F0},{_rect.y:F0},{_rect.width:F0},{_rect.height:F0}";
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(PrefKeyRect, s);
#else
            PlayerPrefs.SetString(PrefKeyRect, s); PlayerPrefs.Save();
#endif
        }

        #region Config & Logging helpers
        private static void ReadConfig(out bool? showHud, out KeyCode? toggleKey, out UnityEngine.Object catalog)
        {
            showHud = null; toggleKey = null; catalog = null;
            var cfg = Resources.Load("Configs/GameConfig"); if (cfg == null) return;
            var t = cfg.GetType();
            showHud = ReadBool(t, cfg, "showBuildingHUD", "buildingHudVisible", "showBuildingHud");
            
            // 優先讀取 KeyCode 欄位，回退到字串解析
            var keyCode = ReadKeyCode(t, cfg, "buildingHudToggleKey", "buildingHUDKey");
            if (keyCode.HasValue) { toggleKey = keyCode; }
            else { 
                var keyStr = ReadString(t, cfg, "buildingHudToggleKey", "buildingHUDKey");
                if (!string.IsNullOrEmpty(keyStr) && Enum.TryParse<KeyCode>(keyStr, true, out var key)) toggleKey = key;
            }
            catalog = ReadObject(t, cfg, "buildingCatalog", "BuildingCatalog") as UnityEngine.Object;
        }

        private static bool? ReadBool(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && fi.FieldType == typeof(bool)) return (bool)fi.GetValue(o);
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && pi.PropertyType == typeof(bool)) return (bool)pi.GetValue(o, null);
            }
            return null;
        }
        private static string ReadString(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && fi.FieldType == typeof(string)) return (string)fi.GetValue(o);
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && pi.PropertyType == typeof(string)) return (string)pi.GetValue(o, null);
            }
            return null;
        }
        private static KeyCode? ReadKeyCode(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && fi.FieldType == typeof(KeyCode)) return (KeyCode)fi.GetValue(o);
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && pi.PropertyType == typeof(KeyCode)) return (KeyCode)pi.GetValue(o, null);
            }
            return null;
        }
        
        private static UnityEngine.Object ReadObject(Type t, object o, params string[] names)
        {
            foreach (var n in names)
            {
                var fi = t.GetField(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (fi != null && typeof(UnityEngine.Object).IsAssignableFrom(fi.FieldType)) return (UnityEngine.Object)fi.GetValue(o);
                var pi = t.GetProperty(n, BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.IgnoreCase);
                if (pi != null && typeof(UnityEngine.Object).IsAssignableFrom(pi.PropertyType)) return (UnityEngine.Object)pi.GetValue(o, null);
            }
            return null;
        }

        private static void Log(string category, string message)
        {
            if (!_tryBound) TryBind();
            if (_bound != null) { _bound(category, message); return; }
            if (!_warned) { _warned = true; Debug.Log($"[{category}] {message}"); }
        }

        private static bool _tryBound, _warned;
        private static Action<string,string> _bound;
        private static void TryBind()
        {
            _tryBound = true;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types; try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    if (t.Name == "DAHLog" || t.Name == "DLog" || t.Name.Contains("SmartConsole"))
                    {
                        var m = t.GetMethod("Info", BindingFlags.Public|BindingFlags.Static, null, new[] { typeof(object), typeof(string) }, null);
                        if (m != null) { _bound = (c,mx) => m.Invoke(null, new object[] { c, mx }); return; }
                        m = t.GetMethod("DLog", BindingFlags.Public|BindingFlags.Static);
                        if (m != null) { _bound = (c,mx) => m.Invoke(null, new object[] { c, mx, null }); return; }
                        m = t.GetMethod("Log", BindingFlags.Public|BindingFlags.Static, null, new[] { typeof(string), typeof(string) }, null);
                        if (m != null) { _bound = (c,mx) => m.Invoke(null, new object[] { c, mx }); return; }
                    }
                }
            }
        }
        #endregion
    }
}