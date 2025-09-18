using UnityEngine;
using UnityEngine.UI;
using DeepAbyssHive.Core.Resources;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.UI
{
    /// <summary>
    /// 資源顯示 UI
    /// </summary>
    public class ResourceDisplayUI : MonoBehaviour
    {
        [Header("Resource Text References")]
        [SerializeField] private Text biomassText;
        [SerializeField] private Text energyText;
        [SerializeField] private Text mineralsText;
        
        [Header("Settings")]
        [SerializeField] private string biomassFormat = "生物質: {0}";
        [SerializeField] private string energyFormat = "能量: {0}";
        [SerializeField] private string mineralsFormat = "礦物: {0}";
        
        private void Start()
        {
            // 訂閱資源變化事件
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged += OnResourceChanged;
                UpdateAllDisplays();
            }
            else
            {
                DAHLog.Warn(LogCategory.UI, "ResourceDisplayUI: ResourceManager not found");
            }
        }
        
        private void OnDestroy()
        {
            // 取消訂閱
            if (ResourceManager.Instance != null)
            {
                ResourceManager.Instance.OnResourceChanged -= OnResourceChanged;
            }
        }
        
        private void OnResourceChanged(ResourceType type, int oldValue, int newValue)
        {
            UpdateDisplay(type, newValue);
        }
        
        private void UpdateDisplay(ResourceType type, int value)
        {
            switch (type)
            {
                case ResourceType.Biomass:
                    if (biomassText != null)
                        biomassText.text = string.Format(biomassFormat, value);
                    break;
                    
                case ResourceType.Energy:
                    if (energyText != null)
                        energyText.text = string.Format(energyFormat, value);
                    break;
                    
                case ResourceType.Minerals:
                    if (mineralsText != null)
                        mineralsText.text = string.Format(mineralsFormat, value);
                    break;
            }
        }
        
        private void UpdateAllDisplays()
        {
            if (ResourceManager.Instance == null) return;
            
            UpdateDisplay(ResourceType.Biomass, ResourceManager.Instance.GetResource(ResourceType.Biomass));
            UpdateDisplay(ResourceType.Energy, ResourceManager.Instance.GetResource(ResourceType.Energy));
            UpdateDisplay(ResourceType.Minerals, ResourceManager.Instance.GetResource(ResourceType.Minerals));
        }
    }
}