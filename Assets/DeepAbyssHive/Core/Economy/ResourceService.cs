using UnityEngine;
using System.Collections.Generic;

namespace DeepAbyssHive.Core.Economy
{
    /// <summary>
    /// 最小資源系統服務 - 管理遊戲內資源（避免與 Unity.Resources 衝突）
    /// </summary>
    public class ResourceService : MonoBehaviour
    {
        private static ResourceService _instance;
        public static ResourceService Instance => _instance;

        // 資源存儲
        private Dictionary<string, float> _gameResources = new Dictionary<string, float>();
        
        // 配置引用
        private object _gameConfig;
        private bool _resourcesEnabled = true;
        private KeyCode _devTestKey = KeyCode.F5;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoStartup()
        {
            if (_instance != null) return;

            var go = new GameObject("ResourceService");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ResourceService>();
            
            Debug.Log("[ECONOMY] ResourceService 自動啟動完成");
        }

        private void Start()
        {
            LoadConfiguration();
            InitializeDefaultResources();
            
            Debug.Log($"[ECONOMY] ResourceService 初始化完成 - 啟用: {_resourcesEnabled}, 測試鍵: {_devTestKey}");
        }

        private void LoadConfiguration()
        {
            try
            {
                // 反射方式取得 GameConfig，避免硬相依
                var providerType = System.Type.GetType("DeepAbyssHive.Core.Config.GameConfigProvider");
                if (providerType != null)
                {
                    var configProperty = providerType.GetProperty("Config", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    _gameConfig = configProperty?.GetValue(null);
                    
                    if (_gameConfig != null)
                    {
                        // 嘗試讀取經濟相關配置
                        var enabledField = _gameConfig.GetType().GetField("resourcesEnabled");
                        if (enabledField != null)
                            _resourcesEnabled = (bool)enabledField.GetValue(_gameConfig);
                            
                        var testKeyField = _gameConfig.GetType().GetField("resourcesDevTestKey");
                        if (testKeyField != null)
                            _devTestKey = (KeyCode)testKeyField.GetValue(_gameConfig);
                            
                        Debug.Log($"[ECONOMY] 配置載入成功 - resourcesEnabled: {_resourcesEnabled}, devTestKey: {_devTestKey}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[ECONOMY] 配置載入失敗，使用預設值: {ex.Message}");
            }
        }

        private void InitializeDefaultResources()
        {
            if (!_resourcesEnabled) return;

            // 初始化基礎資源
            _gameResources["Energy"] = 100f;
            _gameResources["Biomass"] = 50f;
            _gameResources["Minerals"] = 25f;
            
            Debug.Log("[ECONOMY] 預設資源初始化完成");
        }

        private void Update()
        {
            if (!_resourcesEnabled) return;
            
            // Dev 測試熱鍵
            if (Input.GetKeyDown(_devTestKey))
            {
                TestResourceOperations();
            }
        }

        private void TestResourceOperations()
        {
            Debug.Log("[ECONOMY] === 資源系統測試開始 ===");
            
            // 測試資源累加
            AddResource("Energy", 10f);
            AddResource("Biomass", 5f);
            
            // 輸出當前資源狀態
            foreach (var kvp in _gameResources)
            {
                Debug.Log($"[ECONOMY] {kvp.Key}: {kvp.Value}");
            }
            
            Debug.Log("[ECONOMY] === 資源系統測試完成 ===");
        }

        /// <summary>
        /// 添加資源
        /// </summary>
        public bool AddResource(string resourceType, float amount)
        {
            if (!_resourcesEnabled) return false;
            
            if (!_gameResources.ContainsKey(resourceType))
                _gameResources[resourceType] = 0f;
                
            _gameResources[resourceType] += amount;
            Debug.Log($"[ECONOMY] 添加資源 {resourceType}: +{amount} (總計: {_gameResources[resourceType]})");
            
            return true;
        }

        /// <summary>
        /// 消耗資源
        /// </summary>
        public bool ConsumeResource(string resourceType, float amount)
        {
            if (!_resourcesEnabled) return false;
            
            if (!_gameResources.ContainsKey(resourceType) || _gameResources[resourceType] < amount)
            {
                Debug.LogWarning($"[ECONOMY] 資源不足 {resourceType}: 需要 {amount}, 擁有 {GetResource(resourceType)}");
                return false;
            }
            
            _gameResources[resourceType] -= amount;
            Debug.Log($"[ECONOMY] 消耗資源 {resourceType}: -{amount} (剩餘: {_gameResources[resourceType]})");
            
            return true;
        }

        /// <summary>
        /// 獲取資源數量
        /// </summary>
        public float GetResource(string resourceType)
        {
            return _gameResources.ContainsKey(resourceType) ? _gameResources[resourceType] : 0f;
        }

        /// <summary>
        /// 檢查資源是否足夠
        /// </summary>
        public bool HasResource(string resourceType, float amount)
        {
            return GetResource(resourceType) >= amount;
        }
    }
}