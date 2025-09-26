using UnityEngine;
using System.Reflection;
using DeepAbyssHive.Creep.Managers; // 若沒有這命名空間可刪掉這行
using DeepAbyssHive.Common.Placement;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Economy;
using DeepAbyssHive.Buildings.Components;
using QA.Smoke.Dev.HUD;

namespace DeepAbyssHive.Dev
{
    public class BuildingPlacer : MonoBehaviour
    {
        // --- T07: 預覽/放置一致性快取 ---
        private Vector3 _lastPreviewCenter;
        private Quaternion _lastPreviewRotation;
        private Bounds _lastPreviewBounds;
        private Result<Bounds> _lastPreviewResult;
        
        [Header("Basics")]
        [SerializeField] private Camera sceneCamera;                 // 不填就用 Camera.main
        [SerializeField] private LayerMask groundMask;               // 指定「Ground」Layer
        [SerializeField] private GameObject placePrefab;             // 要放置的預製物
        [SerializeField] private Material previewMaterial;           // 預覽用透明材質（可空）

        [Header("Footprint / Grid")]
        [SerializeField, Min(1)] private int footprintSize = 1;      // 格子尺寸（單位：公尺）

        [Header("Preview")]
        [SerializeField] private float previewHeight = 0.5f;         // 預覽抬高一點避免穿地
        [SerializeField] private Vector3 placedScale = Vector3.one;  // 放下去後的縮放
        [SerializeField] private bool requireCreep = false;          // 勾選時，只允許在菌毯上放置（IsOnCreep）
        
        // 放置尺寸倍率（可於 Inspector 調整，預設 1 = 不變）
        [SerializeField] private float spawnScale = 1f;

        [Header("Terrain Sampling")]
        [SerializeField] private bool enableMultiPointSampling = true;  // 啟用多點地形採樣
        [SerializeField] private bool enableSlopeCheck = true;          // 啟用坡度檢查
        [SerializeField] private float maxTerrainSlope = 2.0f;          // 最大允許地形高度差（米）
        [SerializeField] private float terrainSampleHeight = 100f;      // 地形採樣射線起始高度

        [Header("Blocking Settings")]
        [Tooltip("擋重疊時優先使用 Collider.bounds；若 prefab 沒有 Collider 才退回 Renderer.bounds")]
        [SerializeField] private bool useColliderBoundsForBlocking = true;
        [Tooltip("額外外擴/縮小的邊界（世界單位）。正值＝更寬鬆容易擋，負值＝允許更靠近。")]
        [SerializeField] private float blockPadding = 0.02f;

        [SerializeField] private KeyCode toggleKey = KeyCode.B;
        [SerializeField] private KeyCode rotateCWKey = KeyCode.E;
        [SerializeField] private KeyCode rotateCCWKey = KeyCode.Q;
        [SerializeField] private KeyCode cancelKeyPrimary = KeyCode.Escape;
        [SerializeField] private KeyCode cancelKeyAlt = KeyCode.C;
        [SerializeField] private float rotateStep = 90f;

        private GameObject previewInstance;
        private Material previewRuntimeMat;
        private Quaternion rotation = Quaternion.identity;
        private bool isPlacing;
        private Vector3 lastValidPos;
        private RaycastHit lastHit; // 保存最後一次有效的射線命中資訊
        
        // M5-T02: Cost checking components
        private ResourceServiceAdapter _resourceAdapter;
        private HUDToastRunner _toastRunner;

        void Awake()
        {
            if (!sceneCamera) sceneCamera = Camera.main;
            if (!previewMaterial)
            {
                // 動態做一個半透明材質（Standard/Fade）
                var mat = new Material(Shader.Find("Standard"));
                var col = new Color(0f, 1f, 0f, 0.35f);
                mat.SetColor("_Color", col);
                mat.SetFloat("_Mode", 2); // Fade
                mat.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial = mat;
            }
            
            // M5-T02: Initialize cost checking components
            _resourceAdapter = new ResourceServiceAdapter();
            _toastRunner = FindObjectOfType<HUDToastRunner>();
            if (_toastRunner == null)
            {
                var toastGO = new GameObject("HUDToastRunner");
                _toastRunner = toastGO.AddComponent<HUDToastRunner>();
                DontDestroyOnLoad(toastGO);
            }
        }

        void Update()
        {
            // 進出建築模式
            if (Input.GetKeyDown(toggleKey))
                TogglePlacing();

            if (!isPlacing || !placePrefab || !sceneCamera)
                return;

            // 旋轉（Shift 變成 15° 微調）＋滑鼠滾輪也可旋轉
            float step = Input.GetKey(KeyCode.LeftShift) ? 15f : rotateStep;
            if (Input.GetKeyDown(rotateCCWKey)) { rotation *= Quaternion.Euler(0, -step, 0); }
            if (Input.GetKeyDown(rotateCWKey))  { rotation *= Quaternion.Euler(0,  step, 0); }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)       { rotation *= Quaternion.Euler(0, wheel * step, 0); }

            // 射線打地面（Ground Mask）
            var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 5000f, groundMask))
            {
                lastHit = hit; // 保存 hit 資訊供 PlaceNow() 使用
                
                var cfg = GameConfigProvider.Current;
                var worldPoint = hit.point;
                // 先量化中心與旋轉（與放置相同順序）
                var center = SnapXZ(worldPoint, cfg.snapSize);
                rotation = SnapRotationY(rotation, cfg.rotationStepDegrees);

                // 多點地形採樣
                var half = CalcHalfExtents();
                var terrainResult = SampleTerrainMultiPoint(center, half);
                
                Result<Bounds> result;
                Bounds worldBounds; // 提前聲明，避免作用域問題
                
                if (!terrainResult.isValid)
                {
                    result = PlacementResults.OutOfBounds("無法檢測到地形");
                    worldBounds = new Bounds(center, half * 2f); // 提供默認值
                }
                else if (!IsTerrainSuitableForBuilding(terrainResult))
                {
                    result = PlacementResults.TerrainTooSteep($"地形坡度過大 ({terrainResult.heightDifference:F2}m > {maxTerrainSlope:F2}m)");
                    worldBounds = new Bounds(center, half * 2f); // 提供默認值
                }
                else
                {
                    // 使用地形採樣的結果設置建築高度
                    center.y = terrainResult.groundHeight + previewHeight;

                    // 統一遮罩：使用工具方法，避免各處手寫不一致
                    int includeMask = PlacementLayerUtil.GetPlacementBlockMask();

                    // 建立 Bounds（以量化後中心/半徑）
                    worldBounds = new Bounds(center - new Vector3(0, previewHeight, 0), half * 2f);
                    
                    // 預覽即時驗證（有向版；旋轉參與 OverlapBox）
                    result = PlacementValidator.ValidateByConfig(worldBounds.center, half, rotation, includeMask, blockPadding, placePrefab);
                }

                // 記錄給 PlaceNow() 重用，避免再次計算造成微差
                _lastPreviewCenter = worldBounds.center;
                _lastPreviewRotation = rotation;
                _lastPreviewBounds = worldBounds;
                _lastPreviewResult = result;

                // 依結果上色（共用色表/透明度）
                SetPreviewTint(PlacementUiUtil.ColorFor(result, true));

                EnsurePreview();
                previewInstance.transform.SetPositionAndRotation(center, rotation);
                lastValidPos = center;
                
                if (Input.GetMouseButtonDown(0))
                    PlaceNow();
            }

            // 取消（右鍵 或 Esc 或 C）
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(cancelKeyPrimary) || Input.GetKeyDown(cancelKeyAlt))
            {
                CancelPlacing();
            }
        }

        private void TogglePlacing()
        {
            isPlacing = !isPlacing;
            if (!isPlacing) DestroyPreview();
        }

        private void EnsurePreview()
        {
            if (previewInstance) return;

            previewInstance = Instantiate(placePrefab);
            previewInstance.name = "[Preview] " + placePrefab.name;

            // 預覽體一律放到 Ignore Raycast（整樹），避免被 Ray/Physics 命中
            int ignore = LayerMask.NameToLayer("Ignore Raycast");
            if (ignore >= 0) SetLayerRecursively(previewInstance, ignore);
            // 以 prefab 原始縮放為基礎，再乘上倍率（placedScale × spawnScale），與放置/驗證一致
            {
                var baseScale = placePrefab ? placePrefab.transform.localScale : Vector3.one;
                var s = new Vector3(
                    baseScale.x * placedScale.x * spawnScale,
                    baseScale.y * placedScale.y * spawnScale,
                    baseScale.z * placedScale.z * spawnScale
                );
                previewInstance.transform.localScale = s;
            }

            // 關碰撞 + 套透明材質
            foreach (var col in previewInstance.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            foreach (var r in previewInstance.GetComponentsInChildren<Renderer>(true))
            {
                if (!previewRuntimeMat)
                    previewRuntimeMat = new Material(previewMaterial); // 各自一份，改色不影響資產
                r.sharedMaterial = previewRuntimeMat;
            }
        }

        private void SetPreviewTint(Color c)
        {
            if (previewRuntimeMat)
                previewRuntimeMat.color = c;
        }

        private void PlaceNow()
        {
            // 優先重用預覽快取；確保與 Update() 完全一致
            Vector3 center = _lastPreviewCenter;
            Quaternion rotation = _lastPreviewRotation;
            Bounds worldBounds = _lastPreviewBounds;
            var cached = _lastPreviewResult;

            // 若快取尚未建立（或外部呼叫 PlaceNow），才重算一次（與預覽一致的順序與 Y 偏移）
            if (cached == null)
            {
                var cfg = GameConfigProvider.Current;
                var worldPoint = lastHit.point;
                center = SnapXZ(worldPoint, cfg.snapSize);
                rotation = SnapRotationY(rotation, cfg.rotationStepDegrees);

                // 使用與 Update() 相同的地形採樣邏輯
                var halfExtents = CalcHalfExtents();
                var terrainSample = SampleTerrainMultiPoint(center, halfExtents);
                
                if (!terrainSample.isValid)
                {
                    cached = PlacementResults.OutOfBounds("無法檢測到地形");
                    worldBounds = new Bounds(center, halfExtents * 2f); // 提供默認值
                }
                else if (!IsTerrainSuitableForBuilding(terrainSample))
                {
                    cached = PlacementResults.TerrainTooSteep($"地形坡度過大 ({terrainSample.heightDifference:F2}m > {maxTerrainSlope:F2}m)");
                    worldBounds = new Bounds(center, halfExtents * 2f); // 提供默認值
                }
                else
                {
                    center.y = terrainSample.groundHeight + previewHeight;
                    int includeMask = PlacementLayerUtil.GetPlacementBlockMask();
                    var bounds = new Bounds(center - new Vector3(0, previewHeight, 0), halfExtents * 2f);
                    cached = PlacementValidator.ValidateByConfig(bounds.center, halfExtents, rotation, includeMask, blockPadding, placePrefab);
                    worldBounds = bounds; // 設置 worldBounds 供後續使用
                }
            }

            if (!cached.ok)
            {
                Debug.Log($"[PLACE] blocked: {cached.code} {cached.message}");
                
                // M5-T02: Show toast notification for cost-related failures
                if (cached.code == PlaceResultCode.E_INSUFFICIENT_RESOURCES && _toastRunner != null)
                {
                    // Extract resource info from message if available
                    HUDToastRunner.ShowInsufficientResourcesToast("Energy", 0, 0);
                }
                return;
            }

            // M5-T02: Deduct resources after successful placement
            var costTag = placePrefab.GetComponent<BuildingCostTag>();
            if (costTag != null)
            {
                ResourceServiceAdapter.DeductResources(costTag.GetCosts());
            }

            // 實例化放置物（在 center/rotation 生成建築實例）
            var placed = Instantiate(placePrefab, center, rotation);
            // 放置實體同樣採用「原比例 × 倍率」
            {
                var baseScale = placePrefab ? placePrefab.transform.localScale : Vector3.one;
                placed.transform.localScale = new Vector3(
                    baseScale.x * placedScale.x,
                    baseScale.y * placedScale.y,
                    baseScale.z * placedScale.z
                );
            }
            // 放大/縮小新放置的物件，便於辨識（1 = 不變）
            placed.transform.localScale *= spawnScale;

            // 成品一律放到 Building 層（整樹），使刪除/碰撞/查詢規則一致
            int building = LayerMask.NameToLayer("Building");
            if (building >= 0) SetLayerRecursively(placed, building);
            else Debug.LogWarning("[Placement] 'Building' layer not found — delete tool may not hit.");

            // —— 改進的貼地邏輯：使用多點地形採樣結果 —— 
            // 取 Collider 或 Renderer bounds（以 Collider 為優先）
            Bounds GetBounds(GameObject go)
            {
                var cols = go.GetComponentsInChildren<Collider>();
                if (cols != null && cols.Length > 0)
                {
                    var b = cols[0].bounds;
                    for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                    return b;
                }
                var rends = go.GetComponentsInChildren<Renderer>();
                if (rends != null && rends.Length > 0)
                {
                    var b = rends[0].bounds;
                    for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                    return b;
                }
                return new Bounds(go.transform.position, Vector3.one * 0.5f);
            }

            var half = CalcHalfExtents();
            var terrainResult = SampleTerrainMultiPoint(new Vector3(center.x, 0, center.z), half);
            
            if (terrainResult.isValid)
            {
                var placedBounds = GetBounds(placed);
                var halfHeight = placedBounds.extents.y;
                const float padding = 0.02f; // 稍微離地避免 z-fight
                
                // 使用地形採樣的最高點作為基準，確保建築不會埋入地下或懸浮
                Vector3 up = terrainResult.groundNormal;
                placed.transform.position = new Vector3(center.x, terrainResult.groundHeight + halfHeight + padding, center.z);
                
                Debug.Log($"[PLACE] 建築貼地：地形高度={terrainResult.groundHeight:F2}m, 高度差={terrainResult.heightDifference:F2}m, 最終位置Y={placed.transform.position.y:F2}m");
            }
            else
            {
                // 回退到原始邏輯
                var placedBounds = GetBounds(placed);
                var halfHeight = placedBounds.extents.y;
                const float padding = 0.02f;
                Vector3 up = lastHit.normal.normalized;
                placed.transform.position = lastHit.point + up * (halfHeight + padding);
                
                Debug.LogWarning("[PLACE] 地形採樣失敗，使用回退邏輯");
            }
        }

        private void CancelPlacing()
        {
            isPlacing = false;
            DestroyPreview();
        }

        private void DestroyPreview()
        {
            if (previewInstance) Destroy(previewInstance);
            previewInstance = null;
            if (previewRuntimeMat) Destroy(previewRuntimeMat);
            previewRuntimeMat = null;
        }

        private Bounds CalcWorldBounds(GameObject go, bool preferCollider)
        {
            var cols = go.GetComponentsInChildren<Collider>(true);
            var rends = go.GetComponentsInChildren<Renderer>(true);
            if (preferCollider && cols != null && cols.Length > 0)
            {
                var b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b;
            }
            if (rends != null && rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                return b;
            }
            if (!preferCollider && cols != null && cols.Length > 0)
            {
                var b = cols[0].bounds;
                for (int i = 1; i < cols.Length; i++) b.Encapsulate(cols[i].bounds);
                return b;
            }
            return new Bounds(go.transform.position, Vector3.one * 0.5f);
        }

        private static Vector3 SnapXZ(Vector3 v, float step)
        {
            if (step <= 0f) return v;
            float sx = Mathf.Round(v.x / step) * step;
            float sz = Mathf.Round(v.z / step) * step;
            return new Vector3(sx, v.y, sz);
        }

        private static Quaternion SnapRotationY(Quaternion rot, float stepDeg)
        {
            if (stepDeg <= 0f) return rot;
            var e = rot.eulerAngles;
            e.x = 0f; // 直立建築：避免意外傾斜
            e.z = 0f;
            e.y = Mathf.Round(e.y / stepDeg) * stepDeg;
            return Quaternion.Euler(e);
        }

        private Vector3 CalcHalfExtents()
        {
            if (!placePrefab) return Vector3.one * 0.5f;
            
            // 建立臨時物件來計算 bounds
            var temp = Instantiate(placePrefab);
            // 統一縮放：以 prefab 原始比例 × placedScale × spawnScale
            {
                var bs = temp.transform.localScale;
                temp.transform.localScale = new Vector3(
                    bs.x * placedScale.x * spawnScale,
                    bs.y * placedScale.y * spawnScale,
                    bs.z * placedScale.z * spawnScale
                );
            }
            var bounds = CalcWorldBounds(temp, useColliderBoundsForBlocking);
            Destroy(temp);
            
            return bounds.extents;
        }

        /// <summary>
        /// 多點地形採樣：對建築四個角+中心點進行raycast，返回地形信息
        /// </summary>
        private TerrainSampleResult SampleTerrainMultiPoint(Vector3 center, Vector3 halfExtents)
        {
            if (!enableMultiPointSampling)
            {
                // 回退到單點採樣
                if (Physics.Raycast(center + Vector3.up * terrainSampleHeight, Vector3.down, out var hit, terrainSampleHeight * 2f, groundMask))
                {
                    return new TerrainSampleResult { isValid = true, groundHeight = hit.point.y, minHeight = hit.point.y, maxHeight = hit.point.y, groundNormal = hit.normal };
                }
                return new TerrainSampleResult { isValid = false };
            }

            // 多點採樣：四個角 + 中心點
            Vector3[] samplePoints = {
                center + new Vector3(-halfExtents.x, 0, -halfExtents.z), // 左下角
                center + new Vector3(halfExtents.x, 0, -halfExtents.z),  // 右下角
                center + new Vector3(-halfExtents.x, 0, halfExtents.z),  // 左上角
                center + new Vector3(halfExtents.x, 0, halfExtents.z),   // 右上角
                center                                                   // 中心點
            };

            float minHeight = float.MaxValue;
            float maxHeight = float.MinValue;
            Vector3 avgNormal = Vector3.zero;
            int validHits = 0;

            foreach (var point in samplePoints)
            {
                Vector3 rayStart = point + Vector3.up * terrainSampleHeight;
                if (Physics.Raycast(rayStart, Vector3.down, out var hit, terrainSampleHeight * 2f, groundMask))
                {
                    minHeight = Mathf.Min(minHeight, hit.point.y);
                    maxHeight = Mathf.Max(maxHeight, hit.point.y);
                    avgNormal += hit.normal;
                    validHits++;
                }
            }

            if (validHits == 0)
            {
                return new TerrainSampleResult { isValid = false };
            }

            avgNormal = (avgNormal / validHits).normalized;
            
            // 使用最高點作為建築底部高度，避免埋入地下
            float groundHeight = maxHeight;

            return new TerrainSampleResult 
            { 
                isValid = true, 
                groundHeight = groundHeight, 
                minHeight = minHeight, 
                maxHeight = maxHeight, 
                groundNormal = avgNormal,
                heightDifference = maxHeight - minHeight
            };
        }

        /// <summary>
        /// 檢查地形坡度是否適合建築放置
        /// </summary>
        private bool IsTerrainSuitableForBuilding(TerrainSampleResult terrainResult)
        {
            if (!terrainResult.isValid) return false;
            
            if (enableSlopeCheck && terrainResult.heightDifference > maxTerrainSlope)
            {
                return false;
            }
            
            return true;
        }

        /// <summary>
        /// 地形採樣結果
        /// </summary>
        private struct TerrainSampleResult
        {
            public bool isValid;
            public float groundHeight;      // 建築應該放置的高度（通常是最高點）
            public float minHeight;         // 採樣區域最低點
            public float maxHeight;         // 採樣區域最高點
            public Vector3 groundNormal;    // 平均地面法線
            public float heightDifference;  // 高度差 (maxHeight - minHeight)
        }

        // 將整棵樹設為指定層
        private static void SetLayerRecursively(GameObject root, int layer)
        {
            if (!root) return;
            var trs = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < trs.Length; i++) trs[i].gameObject.layer = layer;
        }
    }
}