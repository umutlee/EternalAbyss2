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
            var config = GameConfigProvider.Current;
            _catalog = config?.buildingCatalog;
            
            if (_catalog == null)
            {
                Debug.LogWarning("[BuildingCatalogHUD] No catalog found");
                enabled = false;
                return;
            }
            
            Debug.Log($"[BuildingCatalogHUD] Ready: {_catalog.Count} buildings, toggle={_toggle}");
        }

        private void Update()
        {
            if (Input.GetKeyDown(_toggle))
            {
                _visible = !_visible;
            }
        }

        private void OnGUI()
        {
            if (!_visible || _catalog == null || _catalog.Count == 0) return;
            
            _rect = GUI.Window(0x17001, _rect, DrawWindow, "Building Catalog");
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
    }
}