using UnityEngine;
using UnityEngine.UI;
using DeepAbyssHive.Core.Logging;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Services;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.UI
{
    /// <summary>
    /// 左上角顯示 Time 狀態（Paused / Nx）。如場景中沒有對應 UI，將自動創建一個最小 Canvas + Text。
    /// </summary>
    public class HUDTimeStatusRunner : MonoBehaviour
    {
        private Text _text;
        private float _acc;
        private ITimeService _timeService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<HUDTimeStatusRunner>() != null) return;
            var go = new GameObject("HUD-TimeStatus");
            var r = go.AddComponent<HUDTimeStatusRunner>();
            var managers = GameObject.Find("Managers"); 
            if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
            DAHLog.Info(LogCategory.SERVICE, "HUDTimeStatusRunner created");
        }

        private void Awake()
        {
            EnsureText();
            InitializeTimeService();
            
            int fontSize = TryGetIntFromGameConfig("hudTimeFontSize", 16);
            _text.fontSize = fontSize;
            _text.alignment = TextAnchor.UpperLeft;
            _text.raycastTarget = false;
            UpdateNow(true);
            DAHLog.Info(LogCategory.CONFIG, $"HUDTime: fontSize={fontSize}");
        }

        private void Update()
        {
            _acc += UnityEngine.Time.unscaledDeltaTime;
            if (_acc >= 0.2f) { _acc = 0f; UpdateNow(false); }
        }

        private void InitializeTimeService()
        {
            try
            {
                _timeService = ServiceManager.Instance.GetService<ITimeService>();
            }
            catch (System.Exception ex)
            {
                DAHLog.Warning(LogCategory.UI, $"Failed to get TimeService: {ex.Message}");
            }
        }

        private void UpdateNow(bool force)
        {
            if (_text == null) return;
            
            bool paused;
            float scale;
            
            if (_timeService != null)
            {
                paused = _timeService.IsPaused;
                scale = _timeService.TimeScale;
            }
            else
            {
                paused = UnityEngine.Time.timeScale == 0f;
                scale = UnityEngine.Time.timeScale;
            }
            
            _text.text = paused ? "Time: Paused" : (scale == 1f ? "Time: 1x" : $"Time: {scale:0.#}x");
        }

        private void EnsureText()
        {
            var existing = GameObject.Find("TimeStatusText"); 
            if (existing != null) { _text = existing.GetComponent<Text>(); if (_text != null) return; }

            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var cgo = new GameObject("HUD-Canvas");
                canvas = cgo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                cgo.AddComponent<CanvasScaler>();
                cgo.AddComponent<GraphicRaycaster>();
                DontDestroyOnLoad(cgo);
            }
            var textGO = new GameObject("TimeStatusText"); 
            textGO.transform.SetParent(canvas.transform, false);
            var rect = textGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); 
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1); 
            rect.anchoredPosition = new Vector2(12, -12);
            _text = textGO.AddComponent<Text>();
            _text.color = Color.white;
            _text.text = "Time: 1x";
        }

        private int TryGetIntFromGameConfig(string name, int fallback)
        {
            try
            {
                var cfg = GameConfigProvider.Current;
                if (cfg == null) return fallback;
                var t = cfg.GetType();
                var f = t.GetField(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                if (f != null) return System.Convert.ToInt32(f.GetValue(cfg));
                var pr = t.GetProperty(name, System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
                if (pr != null) return System.Convert.ToInt32(pr.GetValue(cfg));
            } 
            catch {}
            return fallback;
        }
    }
}