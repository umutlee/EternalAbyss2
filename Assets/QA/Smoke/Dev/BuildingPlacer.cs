using UnityEngine;
using System.Reflection;
using DeepAbyssHive.Creep.Managers; // 若沒有這命名空間可刪掉這行
using DeepAbyssHive.Common.Placement;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Dev
{
    public class BuildingPlacer : MonoBehaviour
    {
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
        
        // 預覽狀態快取（避免重複計算）
        private Vector3 lastPreviewCenter;
        private Bounds lastPreviewBounds;
        private Result<Bounds> lastPreviewResult;

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
            if (Input.GetKeyDown(rotateCCWKey)) { rotation *= Quaternion.Euler(0, -step, 0); Debug.Log($"[Placer] Rotate -{step}"); }
            if (Input.GetKeyDown(rotateCWKey))  { rotation *= Quaternion.Euler(0,  step, 0); Debug.Log($"[Placer] Rotate +{step}"); }
            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Abs(wheel) > 0.01f)       { rotation *= Quaternion.Euler(0, wheel * step, 0); }

            // 射線打地面（Ground Mask）
            var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 5000f, groundMask))
            {
                lastHit = hit; // 保存 hit 資訊供 PlaceNow() 使用
                
                // 統一使用 GameConfig.snapSize 進行 Grid Snap
                var cfg = GameConfigProvider.Current;
                var worldPoint = hit.point;
                var snappedPoint = SnapXZ(worldPoint, cfg.snapSize);
                snappedPoint.y = hit.point.y + previewHeight;

                EnsurePreview();
                previewInstance.transform.SetPositionAndRotation(snappedPoint, rotation);
                lastValidPos = snappedPoint;

                // 實時驗證預覽位置
                UpdatePreviewValidation(snappedPoint);
                
                if (Input.GetMouseButtonDown(0))
                    PlaceNow();
            }

            // 取消（右鍵 或 Esc 或 C）
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(cancelKeyPrimary) || Input.GetKeyDown(cancelKeyAlt))
            {
                CancelPlacing();
                Debug.Log("[Placer] Cancel placing");
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
            // 以 prefab 原始縮放為基礎，再乘上倍率 placedScale（預設 1,1,1 = 保留原比例）
            {
                var baseScale = placePrefab ? placePrefab.transform.localScale : Vector3.one;
                previewInstance.transform.localScale = new Vector3(
                    baseScale.x * placedScale.x,
                    baseScale.y * placedScale.y,
                    baseScale.z * placedScale.z
                );
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
            // 重用預覽的驗證結果，避免重複計算
            if (lastPreviewResult == null || !lastPreviewResult.ok)
            {
                Debug.Log($"[PLACE] Cannot place: {lastPreviewResult?.message ?? "No preview validation"}");
                return;
            }

            var pos = lastValidPos;
            pos.y -= previewHeight; // 放回地面

            // 實例化放置物
            var placed = Instantiate(placePrefab, pos, rotation);
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

            // —— 最小修補：用放置物的實際高度把它「頂到」地表上，避免埋進去 —— 
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

            var placedBounds = GetBounds(placed);
            // extents 是世界座標的一半尺寸；我們沿著命中的法線（一般為 Vector3.up）把物件抬出地表
            var halfHeight = placedBounds.extents.y;
            const float padding = 0.02f; // 稍微離地避免 z-fight
            Vector3 up = lastHit.normal.normalized;
            placed.transform.position = lastHit.point + up * (halfHeight + padding);
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

        private void UpdatePreviewValidation(Vector3 previewPos)
        {
            if (!previewInstance) return;

            // 計算預覽物件的世界邊界（先 Snap 再算 bounds）
            var previewCenter = previewPos;
            previewCenter.y -= previewHeight; // 回到地面高度進行驗證

            // 建立臨時預覽物件來計算 bounds（與 PlaceNow 邏輯一致）
            var tempPreview = Instantiate(placePrefab, previewCenter, rotation);
            int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreRaycast != -1)
            {
                foreach (var t in tempPreview.GetComponentsInChildren<Transform>(true))
                    t.gameObject.layer = ignoreRaycast;
            }
            foreach (var c in tempPreview.GetComponentsInChildren<Collider>(true))
                c.enabled = false;

            Bounds wb = CalcWorldBounds(tempPreview, useColliderBoundsForBlocking);
            Destroy(tempPreview); // 立即銷毀臨時物件

            // 統一的掩碼設定（與 PlaceNow 一致）
            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int includeMask = ~0;
            if (terrainLayer != -1) includeMask &= ~(1 << terrainLayer);
            if (ignoreRaycast != -1) includeMask &= ~(1 << ignoreRaycast);

            // 防抖：只在位置或旋轉變化時才重新驗證
            if (Vector3.Distance(wb.center, lastPreviewCenter) < 0.01f && 
                lastPreviewBounds.size == wb.size)
            {
                return; // 位置沒有顯著變化，跳過驗證
            }

            // 執行驗證（這會更新 PlacementValidator.LastResult）
            lastPreviewCenter = wb.center;
            lastPreviewBounds = wb;
            lastPreviewResult = PlacementValidator.ValidateByConfig(wb, includeMask, blockPadding);

            // 根據驗證結果設定預覽顏色
            var previewColor = GetPreviewColor(lastPreviewResult.code, lastPreviewResult.ok);
            SetPreviewTint(previewColor);
        }

        private Color GetPreviewColor(PlaceResultCode code, bool ok)
        {
            if (ok) return new Color(0f, 1f, 0f, 0.35f); // 綠色
            
            switch (code)
            {
                case PlaceResultCode.E_PLACE_COLLISION: return new Color(1f, 0f, 0f, 0.35f); // 紅色
                case PlaceResultCode.E_REQUIRE_CREEP:   return new Color(1f, 1f, 0f, 0.35f); // 黃色
                case PlaceResultCode.E_OUT_OF_BOUNDS:   return new Color(1f, 0f, 1f, 0.35f); // 品紅
                case PlaceResultCode.E_INVALID_TYPE:    return new Color(0f, 1f, 1f, 0.35f); // 青色
                default:                                return new Color(0.5f, 0.5f, 0.5f, 0.35f); // 灰色
            }
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
    }
}
