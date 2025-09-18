using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.UI
{
    /// <summary>
    /// 基於 uGUI 的建築選擇 UI
    /// 替代 IMGUI 版本，提供更好的遊戲體驗
    /// </summary>
    public class BuildingSelectionUI : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private GameObject buttonPrefab;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private Button closeButton;
        
        [Header("Settings")]
        [SerializeField] private int buttonsPerRow = 4;
        [SerializeField] private float buttonSpacing = 10f;
        
        private BuildingCatalogSO _catalog;
        private List<BuildingButton> _buttons = new List<BuildingButton>();
        private int _currentSelection = -1;
        
        public static BuildingSelectionUI Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }
            
            // 初始化 UI
            if (closeButton != null)
                closeButton.onClick.AddListener(Hide);
        }
        
        private void Start()
        {
            LoadCatalog();
            CreateButtons();
            Hide(); // 預設隱藏
        }
        
        private void Update()
        {
            // 熱鍵控制
            var config = GameConfigProvider.Current;
            if (config != null && Input.GetKeyDown(config.buildingHudToggleKey))
            {
                Toggle();
            }
        }
        
        private void LoadCatalog()
        {
            var config = GameConfigProvider.Current;
            _catalog = config?.buildingCatalog;
            
            if (_catalog == null)
            {
                DAHLog.Warn(LogCategory.UI, "BuildingSelectionUI: No catalog found in GameConfig");
            }
        }
        
        private void CreateButtons()
        {
            if (_catalog == null || buttonPrefab == null || buttonContainer == null)
                return;
                
            // 清除現有按鈕
            foreach (var btn in _buttons)
            {
                if (btn != null && btn.gameObject != null)
                    Destroy(btn.gameObject);
            }
            _buttons.Clear();
            
            // 創建新按鈕
            for (int i = 0; i < _catalog.Count; i++)
            {
                var entry = _catalog.Get(i);
                if (entry?.prefab == null) continue;
                
                var buttonObj = Instantiate(buttonPrefab, buttonContainer);
                var buildingButton = buttonObj.GetComponent<BuildingButton>();
                
                if (buildingButton == null)
                    buildingButton = buttonObj.AddComponent<BuildingButton>();
                
                buildingButton.Initialize(entry, i, OnBuildingSelected);
                _buttons.Add(buildingButton);
            }
            
            DAHLog.Info(LogCategory.UI, $"BuildingSelectionUI: Created {_buttons.Count} building buttons");
        }
        
        private void OnBuildingSelected(int index)
        {
            _currentSelection = index;
            
            // 更新按鈕選中狀態
            for (int i = 0; i < _buttons.Count; i++)
            {
                _buttons[i].SetSelected(i == index);
            }
            
            // 應用到 BuildingPlacer
            var entry = _catalog.Get(index);
            if (entry?.prefab != null)
            {
                ApplyToPlacer(entry.prefab, entry.id, index);
            }
            
            DAHLog.Info(LogCategory.PLACEMENT, $"BuildingSelectionUI: Selected building {index} - {entry?.id}");
        }
        
        private void ApplyToPlacer(GameObject prefab, string id, int index)
        {
            // 使用與 IMGUI 版本相同的邏輯
            try
            {
                var binderType = Type.GetType("DeepAbyssHive.Buildings.Runtime.BuildingCatalogBinder, Assembly-CSharp");
                if (binderType != null)
                {
                    var method = binderType.GetMethod("ApplyPrefabToPlacer", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    if (method != null)
                    {
                        method.Invoke(null, new object[] { prefab, id, index });
                        return;
                    }
                }
                
                // 回退到直接設置
                DAHLog.Warn(LogCategory.PLACEMENT, "BuildingSelectionUI: BuildingCatalogBinder not found, using fallback");
            }
            catch (Exception e)
            {
                DAHLog.Error(LogCategory.PLACEMENT, $"BuildingSelectionUI: Failed to apply prefab - {e.Message}");
            }
        }
        
        public void Show()
        {
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void Toggle()
        {
            if (gameObject.activeInHierarchy)
                Hide();
            else
                Show();
        }
    }
    
    /// <summary>
    /// 建築按鈕組件
    /// </summary>
    public class BuildingButton : MonoBehaviour
    {
        private Button _button;
        private Image _image;
        private Text _label;
        private BuildingCatalogEntry _entry;
        private int _index;
        private Action<int> _onSelected;
        
        [Header("Visual Settings")]
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color selectedColor = Color.yellow;
        
        private void Awake()
        {
            _button = GetComponent<Button>();
            _image = GetComponent<Image>();
            _label = GetComponentInChildren<Text>();
            
            if (_button != null)
                _button.onClick.AddListener(OnClick);
        }
        
        public void Initialize(BuildingCatalogEntry entry, int index, Action<int> onSelected)
        {
            _entry = entry;
            _index = index;
            _onSelected = onSelected;
            
            // 設置按鈕文字
            if (_label != null)
                _label.text = $"{index:00}\n{entry.id}";
            
            // TODO: 設置建築圖標（如果有的話）
        }
        
        public void SetSelected(bool selected)
        {
            if (_image != null)
                _image.color = selected ? selectedColor : normalColor;
        }
        
        private void OnClick()
        {
            _onSelected?.Invoke(_index);
        }
    }
}