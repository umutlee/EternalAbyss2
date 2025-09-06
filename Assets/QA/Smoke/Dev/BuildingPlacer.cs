using UnityEngine;
using System.Reflection;
using DeepAbyssHive.Creep.Managers; // 若沒有這命名空間可刪掉這行
using DeepAbyssHive.Common.Placement;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Dev
{
    public class BuildingPlacer : MonoBehaviour
    {
        // 預覽顏色透明度（固定值，避免預覽不透明）
        private const float PREVIEW_ALPHA = 0.35f;
        
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
                
                var cfg = GameConfigProvider.Current;
                var worldPoint = hit.point;
                // 先量化中心與旋轉（與放置相同順序）
                var center = SnapXZ(worldPoint, cfg.snapSize);
                rotation = SnapRotationY(rotation, cfg.rotationStepDegrees);
                center.y = hit.point.y + previewHeight;

                // 統一遮罩：使用工具方法，避免各處手寫不一致
                int includeMask = PlacementLayerUtil.GetPlacementBlockMask();

                // 建立 Bounds（以量化後中心/半徑）
                var half = CalcHalfExtents();
                var worldBounds = new Bounds(center - new Vector3(0, previewHeight, 0), half * 2f);
                
                // 預覽即時驗證（有向版；旋轉參與 OverlapBox）
                var result = PlacementValidator.ValidateByConfig(worldBounds.center, half, rotation, includeMask, blockPadding);

                // 記錄給 PlaceNow() 重用，避免再次計算造成微差
                _lastPreviewCenter = worldBounds.center;
                _lastPreviewRotation = rotation;
                _lastPreviewBounds = worldBounds;
                _lastPreviewResult = result;

                // 依結果上色（OK=綠／Collision=紅／RequireCreep=黃／OutOfBounds=品紅／InvalidType=青）
                SetPreviewTint(GetTintFromResult(result));

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
            // 優先重用預覽快取；確保與 Update() 完全一致
            Vector3 center = _lastPreviewCenter;
            Quaternion rotation = _lastPreviewRotation;
            Bounds worldBounds = _lastPreviewBounds;
            var cached = _lastPreviewResult;

            // 若快取尚未建立（或外部呼叫 PlaceNow），才重算一次
            if (cached == null)
            {
                var cfg = GameConfigProvider.Current;
                var worldPoint = lastHit.point;
                center = SnapXZ(worldPoint, cfg.snapSize);
                rotation = SnapRotationY(rotation, cfg.rotationStepDegrees);

                int includeMask = PlacementLayerUtil.GetPlacementBlockMask();

                var half = CalcHalfExtents();
                worldBounds = new Bounds(center, half * 2f);
                cached = PlacementValidator.ValidateByConfig(center, half, rotation, includeMask, blockPadding);
            }

            if (!cached.ok)
            {
                Debug.Log($"[PLACE] blocked: {cached.code} {cached.message}");
                return;
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
            var bounds = CalcWorldBounds(temp, useColliderBoundsForBlocking);
            Destroy(temp);
            
            return bounds.extents;
        }

        private static Color GetTintFromResult(Result<Bounds> r)
        {
            Color c;
            if (r == null) c = Color.white;
            else if (r.ok) c = Color.green;
            else
            {
                switch (r.code)
                {
                    case PlaceResultCode.E_PLACE_COLLISION: c = Color.red; break;
                    case PlaceResultCode.E_REQUIRE_CREEP:   c = Color.yellow; break;
                    case PlaceResultCode.E_OUT_OF_BOUNDS:   c = new Color(1f, 0f, 1f); break; // magenta
                    case PlaceResultCode.E_INVALID_TYPE:    c = Color.cyan; break;
                    default:                                c = Color.white; break;
                }
            }
            c.a = PREVIEW_ALPHA; // 統一預覽透明度
            return c;
        }
    }
}
