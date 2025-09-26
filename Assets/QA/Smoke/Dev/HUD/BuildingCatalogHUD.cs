using System;
using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Buildings.Runtime;

namespace DeepAbyssHive.QA.Smoke.Dev.HUD
{
    /// <summary>
    /// 簡化的建築選單 HUD
    /// </summary>
    public sealed class BuildingCatalogHUD : MonoBehaviour
    {
        private const string PrefKeyRect = "DAH.BuildingCatalogHUD.Rect";
        private const string PrefKeyVisible = "DAH.BuildingCatalogHUD.Visible";
        
        private Rect _rect = new Rect(12, 12, 600, 200);
        private bool _visible = false;
        private KeyCode _toggle = KeyCode.Z;
        
        private BuildingCatalogSO _catalog;
        private int _current = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (FindObjectOfType<BuildingCatalogHUD>() != null) return;
            
            var go = new GameObject("DevHUD_BuildingCatalog");
            DontDestroyOnLoad(go);
            go.AddComponent<BuildingCatalogHUD>();
        }

        private void Awake()
        {
            LoadSettings();
            
            var config = GameConfigProvider.Current;
            _catalog = config?.buildingCatalog;
            
            if (_catalog == null)
            {
                Debug.LogWarning("[BuildingCatalogHUD] No catalog found");
                enabled = false;
                return;
            }
            
            Debug.Log($"[BuildingCatalogHUD] Ready: {_catalog.Count} buildings, toggle={_toggle}, visible={_visible}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggle))
            {
                _visible = !_visible;
                SaveSettings();
            }
        }

        private void OnGUI()
        {
            if (!_visible || _catalog == null || _catalog.Count == 0) return;
            
            var newRect = GUI.Window(0x17001, _rect, DrawWindow, "Building Catalog");
            
            // 如果窗口位置改變了，保存新位置
            if (newRect != _rect)
            {
                _rect = newRect;
                SaveSettings();
            }
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();
            
            // 建築按鈕網格
            int buttonsPerRow = 4;
            int rows = Mathf.CeilToInt((float)_catalog.Count / buttonsPerRow);
            
            for (int row = 0; row < rows; row++)
            {
                GUILayout.BeginHorizontal();
                
                for (int col = 0; col < buttonsPerRow; col++)
                {
                    int index = row * buttonsPerRow + col;
                    if (index >= _catalog.Count) break;
                    
                    var entry = _catalog.Get(index);
                    if (entry?.prefab == null) continue;
                    
                    // 按鈕樣式
                    var style = GUI.skin.button;
                    if (index == _current)
                    {
                        var oldColor = GUI.backgroundColor;
                        GUI.backgroundColor = Color.yellow;
                        if (GUILayout.Button($"{index:00}\n{entry.id}", style, GUILayout.Width(120), GUILayout.Height(60)))
                        {
                            SelectBuilding(index);
                        }
                        GUI.backgroundColor = oldColor;
                    }
                    else
                    {
                        if (GUILayout.Button($"{index:00}\n{entry.id}", style, GUILayout.Width(120), GUILayout.Height(60)))
                        {
                            SelectBuilding(index);
                        }
                    }
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(10);
            
            // 控制按鈕
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                _visible = false;
                SaveSettings();
            }
            GUILayout.FlexibleSpace();
            GUILayout.Label($"Selected: {(_current >= 0 ? _catalog.Get(_current)?.id : "None")}");
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            // 允許拖動窗口
            GUI.DragWindow();
        }

        private void SelectBuilding(int index)
        {
            if (index < 0 || index >= _catalog.Count) return;
            
            var entry = _catalog.Get(index);
            if (entry?.prefab == null) return;
            
            _current = index;
            
            Debug.Log($"[BuildingCatalogHUD] Selected: [{index}] {entry.id}");
            
            // 通過 BuildingCatalogBinder 應用到 BuildingPlacer
            var binder = FindObjectOfType<BuildingCatalogBinder>();
            if (binder != null)
            {
                binder.SelectBuilding(index);
            }
            else
            {
                // 直接調用靜態方法
                BuildingCatalogBinder.ApplyPrefabToPlacer(entry.prefab, entry.id, index);
            }
        }

        private void LoadSettings()
        {
            // 載入窗口位置
            if (PlayerPrefs.HasKey(PrefKeyRect))
            {
                var rectStr = PlayerPrefs.GetString(PrefKeyRect);
                if (TryParseRect(rectStr, out var savedRect))
                {
                    _rect = savedRect;
                }
            }
            
            // 載入顯示狀態
            _visible = PlayerPrefs.GetInt(PrefKeyVisible, 0) == 1;
        }

        private void SaveSettings()
        {
            // 保存窗口位置
            var rectStr = $"{_rect.x},{_rect.y},{_rect.width},{_rect.height}";
            PlayerPrefs.SetString(PrefKeyRect, rectStr);
            
            // 保存顯示狀態
            PlayerPrefs.SetInt(PrefKeyVisible, _visible ? 1 : 0);
            
            PlayerPrefs.Save();
        }

        private bool TryParseRect(string rectStr, out Rect rect)
        {
            rect = new Rect();
            try
            {
                var parts = rectStr.Split(',');
                if (parts.Length == 4)
                {
                    rect = new Rect(
                        float.Parse(parts[0]),
                        float.Parse(parts[1]),
                        float.Parse(parts[2]),
                        float.Parse(parts[3])
                    );
                    return true;
                }
            }
            catch (System.Exception)
            {
                // 解析失敗，使用默認值
            }
            return false;
        }

        private void OnDestroy()
        {
            SaveSettings();
        }
    }
}