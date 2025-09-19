using UnityEngine;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.UI
{
    /// <summary>
    /// 主遊戲 HUD 管理器
    /// 統一管理所有 UI 面板的顯示/隱藏
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("UI Panels")]
        [SerializeField] private GameObject resourcePanel;
        [SerializeField] private GameObject buildingPanel;
        [SerializeField] private GameObject minimapPanel;
        
        [Header("Toggle Keys")]
        [SerializeField] private KeyCode resourceToggleKey = KeyCode.F2;
        [SerializeField] private KeyCode minimapToggleKey = KeyCode.F3;
        
        public static GameHUD Instance { get; private set; }
        
        private ITimeService _timeService;
        private readonly float[] _timeScales = { 1f, 2f, 4f };
        private int _currentTimeScaleIndex = 0;
        
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
        }
        
        private void Start()
        {
            // 初始化 UI 狀態
            ShowResourcePanel(true);
            ShowBuildingPanel(false);
            ShowMinimapPanel(true);
            
            // 初始化時間服務
            InitializeTimeService();
            
            DAHLog.Info(LogCategory.UI, "GameHUD initialized");
        }
        
        private void Update()
        {
            // 熱鍵控制
            if (Input.GetKeyDown(resourceToggleKey))
            {
                ToggleResourcePanel();
            }
            
            if (Input.GetKeyDown(minimapToggleKey))
            {
                ToggleMinimapPanel();
            }
            
            // 建築面板由 GameConfig 控制
            var config = GameConfigProvider.Current;
            if (config != null && Input.GetKeyDown(config.buildingHudToggleKey))
            {
                ToggleBuildingPanel();
            }
            
            // 時間控制熱鍵
            HandleTimeControlInput(config);
        }
        
        public void ShowResourcePanel(bool show)
        {
            if (resourcePanel != null)
                resourcePanel.SetActive(show);
        }
        
        public void ShowBuildingPanel(bool show)
        {
            if (buildingPanel != null)
                buildingPanel.SetActive(show);
        }
        
        public void ShowMinimapPanel(bool show)
        {
            if (minimapPanel != null)
                minimapPanel.SetActive(show);
        }
        
        public void ToggleResourcePanel()
        {
            if (resourcePanel != null)
                resourcePanel.SetActive(!resourcePanel.activeInHierarchy);
        }
        
        public void ToggleBuildingPanel()
        {
            if (buildingPanel != null)
                buildingPanel.SetActive(!buildingPanel.activeInHierarchy);
        }
        
        public void ToggleMinimapPanel()
        {
            if (minimapPanel != null)
                minimapPanel.SetActive(!minimapPanel.activeInHierarchy);
        }
        
        private void InitializeTimeService()
        {
            try
            {
                _timeService = ServiceManager.Instance.GetService<ITimeService>();
                DAHLog.Info(LogCategory.UI, "[GameHUD] TimeService initialized");
            }
            catch (System.Exception ex)
            {
                DAHLog.Warning(LogCategory.UI, $"[GameHUD] Failed to get TimeService: {ex.Message}");
            }
        }
        
        private void HandleTimeControlInput(GameConfigSO config)
        {
            if (_timeService == null || config == null) return;
            
            // 暫停/恢復
            if (config.pauseToggleKey != KeyCode.None && Input.GetKeyDown(config.pauseToggleKey))
            {
                _timeService.TogglePause();
            }
            
            // 時間倍率循環
            if (config.timeScaleCycleKey != KeyCode.None && Input.GetKeyDown(config.timeScaleCycleKey))
            {
                CycleTimeScale();
            }
        }
        
        private void CycleTimeScale()
        {
            if (_timeService == null) return;
            
            _currentTimeScaleIndex = (_currentTimeScaleIndex + 1) % _timeScales.Length;
            _timeService.SetTimeScale(_timeScales[_currentTimeScaleIndex]);
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("GameHUD");
                go.AddComponent<GameHUD>();
            }
        }
    }
}