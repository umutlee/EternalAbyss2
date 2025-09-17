using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace DeepAbyssHive.QA.Smoke.Dev.HUD
{
    /// <summary>
    /// 可拖拽的建築目錄 HUD（IMGUI）。
    /// - 自動尋找 BuildingCatalogBinder 與 Catalog。
    /// - 按鈕點選切換，並支援 Tab / BackQuote（`）熱鍵。
    /// - 位置持久化；視窗不會跑出螢幕外。
    /// </summary>
    public class BuildingCatalogHUD_IMGUI : MonoBehaviour
    {
        private const string PrefKey = "DAH.BuildingCatalogHUD.Rect";
        private Rect _rect = new Rect(320, 360, 660, 120);
        private Vector2 _scroll;
        private bool _visible = true;

        private UnityEngine.Object _binder;
        private ScriptableObject _catalog;
        private IList<UnityEngine.Object> _entries; // entries[i] 應為 Prefab
        private int _activeIndex = -1;

        // 熱鍵：Tab / BackQuote
        private KeyCode _nextKey = KeyCode.Tab;
        private KeyCode _prevKey = KeyCode.BackQuote;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            var go = new GameObject("HUD_BuildingCatalog_IMGUI");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildingCatalogHUD_IMGUI>();
        }

        private void Awake()
        {
            LoadRect();
            FindBinderAndCatalog();
        }

        private void Update()
        {
            if (!_visible) return;
            if (Input.GetKeyDown(_nextKey)) SelectRelative(+1);
            if (Input.GetKeyDown(_prevKey)) SelectRelative(-1);
        }

        private void OnGUI()
        {
            if (!_visible || _catalog == null || _entries == null) return;
            KeepInsideScreen();
            _rect = GUI.Window(0xDA17i, _rect, DrawWindow, "Building Catalog");
        }

        private void DrawWindow(int id)
        {
            GUILayout.Label("Click a building to select. (Tab/BackQuote hotkeys still work)");

            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUILayout.Height(56));
            GUILayout.BeginHorizontal();

            for (int i = 0; i < _entries.Count; i++)
            {
                var obj = _entries[i] as GameObject;
                if (obj == null) continue;
                GUI.enabled = true;
                var style = new GUIStyle(GUI.skin.button) { padding = new RectOffset(10, 10, 6, 6) };
                if (GUILayout.Button(i.ToString("00") + "  " + obj.name, style, GUILayout.Height(32)))
                {
                    Apply(i);
                }
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Prev", GUILayout.Width(90))) SelectRelative(-1);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(90))) _visible = false;
            GUILayout.EndHorizontal();

            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void SelectRelative(int delta)
        {
            if (_entries == null || _entries.Count == 0) return;
            _activeIndex = Mathf.Clamp((_activeIndex < 0 ? 0 : _activeIndex) + delta, 0, _entries.Count - 1);
            Apply(_activeIndex);
        }

        private void Apply(int index)
        {
            if (_binder == null || _entries == null || index < 0 || index >= _entries.Count) return;
            var go = _entries[index] as GameObject;
            if (go == null) return;

            // 呼叫 Binder 的 ApplyPrefabToPlacer(GameObject, string, int)（若存在）
            var t = _binder.GetType();
            var m = t.GetMethod("ApplyPrefabToPlacer", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
            if (m != null && m.GetParameters().Length == 3)
            {
                m.Invoke(_binder, new object[] { go, go.name, index });
            }
            else
            {
                // 退而求其次：嘗試 SetActiveIndex(int)
                var m2 = t.GetMethod("SetActiveIndex", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic);
                if (m2 != null) m2.Invoke(_binder, new object[] { index });
            }
            _activeIndex = index;
        }

        private void FindBinderAndCatalog()
        {
            _binder = FindObjectOfTypeByName("BuildingCatalogBinder");
            if (_binder == null) return;

            // 讀 binder.catalog（或同名屬性）
            var t = _binder.GetType();
            var cat = t.GetField("catalog", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(_binder)
                   ?? t.GetProperty("catalog", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(_binder, null);

            _catalog = cat as ScriptableObject;
            if (_catalog == null) return;

            // 取 catalog.entries / items / list
            var ct = _catalog.GetType();
            var entries = ct.GetField("entries", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(_catalog)
                       ?? ct.GetField("items", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(_catalog)
                       ?? ct.GetField("list", BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic)?.GetValue(_catalog);

            _entries = entries as IList<UnityEngine.Object>;
            if (_entries == null && entries is Array arr)
            {
                var list = new List<UnityEngine.Object>();
                foreach (var e in arr) if (e is UnityEngine.Object uo) list.Add(uo);
                _entries = list;
            }
        }

        private UnityEngine.Object FindObjectOfTypeByName(string typeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types; try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    if (t.Name != typeName) continue;
                    var comps = GameObject.FindObjectsOfType(t);
                    if (comps != null && comps.Length > 0) return comps[0] as Component;
                }
            }
            return null;
        }

        private void KeepInsideScreen()
        {
            float w = _rect.width, h = _rect.height;
            float x = Mathf.Clamp(_rect.x, 0, Screen.width - w);
            float y = Mathf.Clamp(_rect.y, 0, Screen.height - h);
            _rect = new Rect(x, y, w, h);
        }

        private void LoadRect()
        {
#if UNITY_EDITOR
            string s = UnityEditor.EditorPrefs.GetString(PrefKey, string.Empty);
#else
            string s = PlayerPrefs.GetString(PrefKey, string.Empty);
#endif
            if (!string.IsNullOrEmpty(s))
            {
                var p = s.Split(',');
                if (p.Length == 4 &&
                    float.TryParse(p[0], out var x) &&
                    float.TryParse(p[1], out var y) &&
                    float.TryParse(p[2], out var w) &&
                    float.TryParse(p[3], out var h))
                    _rect = new Rect(x, y, w, h);
            }
        }

        private void OnDisable() => SaveRect();
        private void OnDestroy() => SaveRect();
        private void SaveRect()
        {
            string s = $"{_rect.x:F0},{_rect.y:F0},{_rect.width:F0},{_rect.height:F0}";
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetString(PrefKey, s);
#else
            PlayerPrefs.SetString(PrefKey, s);
            PlayerPrefs.Save();
#endif
        }
    }
}