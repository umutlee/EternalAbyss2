using System;
using System.Collections.Generic;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Game.Resources
{
    /// <summary>
    /// 簡單的資源管理系統
    /// </summary>
    public class ResourceManager : MonoBehaviour
    {
        public static ResourceManager Instance { get; private set; }
        
        [Header("Starting Resources")]
        [SerializeField] private int startingBiomass = 100;   // 生物質為主要資源
        [SerializeField] private int startingEnergy = 50;     // 能量為次要資源
        [SerializeField] private int startingMinerals = 25;   // 礦物為稀有資源
        
        private Dictionary<ResourceType, int> _resources = new Dictionary<ResourceType, int>();
        
        public event Action<ResourceType, int, int> OnResourceChanged; // type, oldValue, newValue
        
        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeResources();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeResources()
        {
            _resources[ResourceType.Biomass] = startingBiomass;
            _resources[ResourceType.Energy] = startingEnergy;
            _resources[ResourceType.Minerals] = startingMinerals;
            
            DAHLog.Info(LogCategory.MANAGER, $"ResourceManager initialized: Biomass={startingBiomass}, Energy={startingEnergy}, Minerals={startingMinerals}");
        }
        
        public int GetResource(ResourceType type)
        {
            return _resources.GetValueOrDefault(type, 0);
        }
        
        public bool HasResource(ResourceType type, int amount)
        {
            return GetResource(type) >= amount;
        }
        
        public bool SpendResource(ResourceType type, int amount)
        {
            if (!HasResource(type, amount))
                return false;
                
            int oldValue = _resources[type];
            _resources[type] -= amount;
            OnResourceChanged?.Invoke(type, oldValue, _resources[type]);
            
            DAHLog.Dev(LogCategory.MANAGER, $"Spent {amount} {type}: {oldValue} -> {_resources[type]}");
            return true;
        }
        
        public void AddResource(ResourceType type, int amount)
        {
            int oldValue = _resources.GetValueOrDefault(type, 0);
            _resources[type] = oldValue + amount;
            OnResourceChanged?.Invoke(type, oldValue, _resources[type]);
            
            DAHLog.Dev(LogCategory.MANAGER, $"Added {amount} {type}: {oldValue} -> {_resources[type]}");
        }
        
        public void SetResource(ResourceType type, int amount)
        {
            int oldValue = _resources.GetValueOrDefault(type, 0);
            _resources[type] = amount;
            OnResourceChanged?.Invoke(type, oldValue, _resources[type]);
        }
        
        // 開發用：增加資源的熱鍵
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1))
            {
                AddResource(ResourceType.Biomass, 50);   // 主要資源
                AddResource(ResourceType.Energy, 25);    // 次要資源
                AddResource(ResourceType.Minerals, 10);  // 稀有資源
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance == null)
            {
                var go = new GameObject("ResourceManager");
                go.AddComponent<ResourceManager>();
            }
        }
    }
    
    public enum ResourceType
    {
        Biomass,    // 生物質 - 主要資源，用於基礎建築和單位
        Energy,     // 能量 - 用於高級科技和特殊能力
        Minerals    // 礦物 - 用於建築升級和防禦設施
    }
}