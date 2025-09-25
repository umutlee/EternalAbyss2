using UnityEngine;
using DeepAbyssHive.Core.Economy;
using DeepAbyssHive.Buildings.Components;
using QA.Smoke.Dev.HUD;
using DeepAbyssHive.Core.Logging;
using System.Collections.Generic;

namespace DeepAbyssHive.QA.Smoke
{
    /// <summary>
    /// M5-T02 建築放置成本檢查測試腳本
    /// 用於驗證成本檢查系統是否正常工作
    /// </summary>
    public class M5T02CostCheckTest : MonoBehaviour
    {
        [Header("Test Configuration")]
        [SerializeField] private KeyCode testKey = KeyCode.F8;
        [SerializeField] private GameObject testBuildingPrefab;

        void Start()
        {
            DAHLog.Info(LogCategory.COMMON, "M5-T02 Cost Check Test initialized. Press F8 to run tests.");
        }

        void Update()
        {
            if (Input.GetKeyDown(testKey))
            {
                RunCostCheckTests();
            }
        }

        private void RunCostCheckTests()
        {
            DAHLog.Info(LogCategory.COMMON, "=== M5-T02 Cost Check Tests ===");
            
            // Test 1: Check if ResourceService is available
            bool hasResourceService = ResourceService.Instance != null;
            DAHLog.Info(LogCategory.COMMON, $"Test 1 - ResourceService Available: {hasResourceService}");
            
            // Test 2: Test ResourceServiceAdapter with List<ResourceCost>
            var testCosts = new List<ResourceCost>
            {
                new ResourceCost("Energy", 100),
                new ResourceCost("Materials", 50)
            };
            
            bool canAfford = ResourceServiceAdapter.CanAfford(testCosts);
            DAHLog.Info(LogCategory.COMMON, $"Test 2 - Can Afford Test Costs: {canAfford}");
            
            // Test 3: Check BuildingCostTag on test prefab
            if (testBuildingPrefab != null)
            {
                var costTag = testBuildingPrefab.GetComponent<BuildingCostTag>();
                bool hasCostTag = costTag != null;
                DAHLog.Info(LogCategory.COMMON, $"Test 3 - Test Prefab Has Cost Tag: {hasCostTag}");
                
                if (hasCostTag)
                {
                    var costs = costTag.GetCosts();
                    DAHLog.Info(LogCategory.COMMON, $"Test 3 - Cost Tag has {costs?.Count ?? 0} cost entries");
                    DAHLog.Info(LogCategory.COMMON, $"Test 3 - Cost Summary: {costTag.GetCostSummary()}");
                }
            }
            
            // Test 4: Toast notification test
            HUDToastRunner.ShowInsufficientResourcesToast("Energy", 100, 50);
            DAHLog.Info(LogCategory.COMMON, "Test 4 - Toast notification triggered");
            
            // Test 5: Resource deduction test (only if we can afford)
            if (canAfford)
            {
                bool deducted = ResourceServiceAdapter.DeductResources(testCosts);
                DAHLog.Info(LogCategory.COMMON, $"Test 5 - Resource Deduction: {deducted}");
            }
            else
            {
                DAHLog.Info(LogCategory.COMMON, "Test 5 - Skipped resource deduction (insufficient resources)");
            }
            
            // Test 6: Building cost check
            if (testBuildingPrefab != null)
            {
                string shortageInfo;
                bool canAffordBuilding = ResourceServiceAdapter.CanAffordBuilding(testBuildingPrefab, out shortageInfo);
                DAHLog.Info(LogCategory.COMMON, $"Test 6 - Can Afford Building: {canAffordBuilding}");
                if (!canAffordBuilding)
                {
                    DAHLog.Info(LogCategory.COMMON, $"Test 6 - Shortage Info: {shortageInfo}");
                }
            }
            
            DAHLog.Info(LogCategory.COMMON, "=== M5-T02 Tests Complete ===");
        }
    }
}