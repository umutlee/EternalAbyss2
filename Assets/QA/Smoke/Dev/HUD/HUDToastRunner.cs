using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace QA.Smoke.Dev.HUD
{
    /// <summary>
    /// Toast 通知類型
    /// </summary>
    public enum ToastType
    {
        Info,
        Warning,
        Error,
        Success
    }
    
    /// <summary>
    /// Toast 消息數據
    /// </summary>
    [System.Serializable]
    public class ToastMessage
    {
        public string text;
        public ToastType type;
        public float duration;
        public float createdTime;
        
        public ToastMessage(string msg, ToastType toastType, float dur)
        {
            text = msg;
            type = toastType;
            duration = dur;
            createdTime = Time.time;
        }
        
        public bool IsExpired()
        {
            return Time.time - createdTime >= duration;
        }
        
        public float GetAlpha()
        {
            float elapsed = Time.time - createdTime;
            if (elapsed < duration * 0.8f)
                return 1.0f;
            
            // 最後 20% 時間淡出
            float fadeTime = duration * 0.2f;
            float fadeElapsed = elapsed - (duration * 0.8f);
            return 1.0f - (fadeElapsed / fadeTime);
        }
    }
    
    /// <summary>
    /// HUD Toast 通知運行器，顯示臨時通知消息
    /// </summary>
    public class HUDToastRunner : MonoBehaviour
    {
        [Header("Toast 配置")]
        [SerializeField] private int maxToasts = 5;
        [SerializeField] private float defaultDuration = 3.0f;
        [SerializeField] private Vector2 toastSize = new Vector2(300, 50);
        [SerializeField] private float toastSpacing = 10f;
        
        [Header("顏色配置")]
        [SerializeField] private Color infoColor = Color.white;
        [SerializeField] private Color warningColor = Color.yellow;
        [SerializeField] private Color errorColor = Color.red;
        [SerializeField] private Color successColor = Color.green;
        
        private List<ToastMessage> activeToasts = new List<ToastMessage>();
        private static HUDToastRunner _instance;
        
        public static HUDToastRunner Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<HUDToastRunner>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("HUDToastRunner");
                        _instance = go.AddComponent<HUDToastRunner>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        /// <summary>
        /// 顯示 Toast 通知
        /// </summary>
        public static void ShowToast(string message, ToastType type = ToastType.Info, float duration = 0f)
        {
            if (string.IsNullOrEmpty(message))
                return;
                
            float actualDuration = duration > 0 ? duration : Instance.defaultDuration;
            var toast = new ToastMessage(message, type, actualDuration);
            
            Instance.activeToasts.Add(toast);
            
            // 限制最大數量
            while (Instance.activeToasts.Count > Instance.maxToasts)
            {
                Instance.activeToasts.RemoveAt(0);
            }
            
            Debug.Log($"[HUDToast] {type}: {message}");
        }
        
        /// <summary>
        /// 顯示資源不足通知
        /// </summary>
        public static void ShowResourceShortage(string resourceType, int required, int available)
        {
            string message = $"資源不足: {resourceType} (需要:{required}, 擁有:{available})";
            ShowToast(message, ToastType.Error, 4.0f);
        }
        
        /// <summary>
        /// 顯示資源不足 Toast（M5-T02 API 兼容）
        /// </summary>
        public static void ShowInsufficientResourcesToast(string resourceType, int required, int available)
        {
            ShowResourceShortage(resourceType, required, available);
        }
        
        /// <summary>
        /// 顯示建築放置成功通知
        /// </summary>
        public static void ShowBuildingPlaced(string buildingName, string costSummary)
        {
            string message = $"建築已放置: {buildingName} (消耗: {costSummary})";
            ShowToast(message, ToastType.Success, 2.0f);
        }
        
        private void Update()
        {
            // 清理過期的 Toast
            for (int i = activeToasts.Count - 1; i >= 0; i--)
            {
                if (activeToasts[i].IsExpired())
                {
                    activeToasts.RemoveAt(i);
                }
            }
        }
        
        private void OnGUI()
        {
            if (activeToasts.Count == 0)
                return;
            
            // 在螢幕右上角顯示 Toast
            float startY = 20f;
            
            for (int i = 0; i < activeToasts.Count; i++)
            {
                var toast = activeToasts[i];
                float alpha = toast.GetAlpha();
                
                if (alpha <= 0)
                    continue;
                
                Color bgColor = GetToastColor(toast.type);
                bgColor.a = alpha * 0.8f;
                
                Color textColor = Color.white;
                textColor.a = alpha;
                
                float x = Screen.width - toastSize.x - 20f;
                float y = startY + i * (toastSize.y + toastSpacing);
                
                Rect toastRect = new Rect(x, y, toastSize.x, toastSize.y);
                
                // 繪製背景
                GUI.color = bgColor;
                GUI.Box(toastRect, "");
                
                // 繪製文字
                GUI.color = textColor;
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.alignment = TextAnchor.MiddleCenter;
                style.wordWrap = true;
                GUI.Label(toastRect, toast.text, style);
            }
            
            GUI.color = Color.white;
        }
        
        private Color GetToastColor(ToastType type)
        {
            switch (type)
            {
                case ToastType.Info: return infoColor;
                case ToastType.Warning: return warningColor;
                case ToastType.Error: return errorColor;
                case ToastType.Success: return successColor;
                default: return infoColor;
            }
        }
    }
}