using UnityEngine;
using DeepAbyssHive.Common.Placement;
using DeepAbyssHive.Core.Config;
using DeepAbyssHive.Core.Economy;
using DeepAbyssHive.Buildings.Components;
using QA.Smoke.Dev.HUD;

namespace DeepAbyssHive.Dev
{
    public class BuildingPlacer : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private Camera sceneCamera;
        [SerializeField] private LayerMask groundMask;
        [SerializeField] private GameObject placePrefab;
        [SerializeField] private Material previewMaterial;
        [SerializeField] private float previewHeight = 0.5f;
        [SerializeField] private float terrainSampleHeight = 100f;
        [SerializeField] private float blockPadding = 0.02f;

        private GameObject previewInstance;
        private Material previewRuntimeMat;
        private bool isPlacing;
        private RaycastHit lastHit;
        private Quaternion rotation = Quaternion.identity;
        
        // Cost checking components
        private HUDToastRunner _toastRunner;

        void Awake()
        {
            if (!sceneCamera) sceneCamera = Camera.main;
            
            // Create default preview material if not set
            if (!previewMaterial)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.SetColor("_Color", new Color(0f, 1f, 0f, 0.35f));
                mat.SetFloat("_Mode", 2); // Fade
                mat.EnableKeyword("_ALPHABLEND_ON");
                previewMaterial = mat;
            }
            
            // Initialize cost checking
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
            if (!isPlacing || !placePrefab || !sceneCamera)
                return;

            // Get GameConfig once for the entire method
            var cfg = GameConfigProvider.Current;
            
            // Handle rotation input from GameConfig
            if (cfg != null)
            {
                if (cfg.buildingRotateLeftKey != KeyCode.None && Input.GetKeyDown(cfg.buildingRotateLeftKey))
                {
                    rotation *= Quaternion.Euler(0, -90f, 0);
                }
                if (cfg.buildingRotateRightKey != KeyCode.None && Input.GetKeyDown(cfg.buildingRotateRightKey))
                {
                    rotation *= Quaternion.Euler(0, 90f, 0);
                }
            }

            // Raycast to ground
            var ray = sceneCamera.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out var hit, 5000f, groundMask))
            {
                lastHit = hit;
                
                var center = SnapXZ(hit.point, cfg?.snapSize ?? 0f);
                
                // Sample terrain height
                var terrainHeight = SampleTerrainHeight(center);
                center.y = terrainHeight + previewHeight;
                
                // Validate placement with rotation
                var half = CalcHalfExtents();
                var worldBounds = new Bounds(center - new Vector3(0, previewHeight, 0), half * 2f);
                int includeMask = PlacementLayerUtil.GetPlacementBlockMask();
                var result = PlacementValidator.ValidateByConfig(worldBounds.center, half, rotation, includeMask, blockPadding, placePrefab);
                
                // Update preview with rotation
                EnsurePreview();
                previewInstance.transform.SetPositionAndRotation(center, rotation);
                SetPreviewTint(result.ok ? Color.green : Color.red);
                
                // Place building on left click
                if (Input.GetMouseButtonDown(0) && result.ok)
                {
                    PlaceBuilding(center, terrainHeight);
                }
            }

            // Cancel on right click or escape
            if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            {
                CancelPlacing();
            }
        }

        private void PlaceBuilding(Vector3 center, float terrainHeight)
        {
            // Check cost
            var costTag = placePrefab.GetComponent<BuildingCostTag>();
            if (costTag != null && !ResourceServiceAdapter.CanAfford(costTag.GetCosts()))
            {
                HUDToastRunner.ShowInsufficientResourcesToast("Energy", 0, 0);
                return;
            }

            // Since prefab pivot is at bottom, just place at terrain height + small padding
            var finalY = terrainHeight + 0.02f;
            var finalPosition = new Vector3(center.x, finalY, center.z);
            
            // Create building at the calculated position with rotation
            var placed = Instantiate(placePrefab, finalPosition, rotation);
            
            // Set to Building layer
            int buildingLayer = LayerMask.NameToLayer("Building");
            if (buildingLayer >= 0) 
                SetLayerRecursively(placed, buildingLayer);
            
            Debug.Log($"[PLACE] Building placed: prefab={placePrefab.name}, terrainHeight={terrainHeight:F2}, finalPosition={finalPosition}");
            
            // Deduct cost
            if (costTag != null)
                ResourceServiceAdapter.DeductResources(costTag.GetCosts());
            
            Debug.Log($"[PLACE] Building placed: {placePrefab.name} at {placed.transform.position}");
            
            // Exit placing mode (single placement)
            isPlacing = false;
            DestroyPreview();
        }

        private float SampleTerrainHeight(Vector3 center)
        {
            var rayStart = new Vector3(center.x, center.y + terrainSampleHeight, center.z);
            if (Physics.Raycast(rayStart, Vector3.down, out var hit, terrainSampleHeight * 2f, groundMask))
            {
                return hit.point.y;
            }
            return center.y; // Fallback
        }

        private void EnsurePreview()
        {
            if (previewInstance) return;

            previewInstance = Instantiate(placePrefab);
            previewInstance.name = "[Preview] " + placePrefab.name;

            // Set to Ignore Raycast layer
            int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (ignoreLayer >= 0) 
                SetLayerRecursively(previewInstance, ignoreLayer);
        }

        private void SetPreviewTint(Color color)
        {
            if (!previewInstance) return;

            var renderers = previewInstance.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!previewRuntimeMat)
                {
                    previewRuntimeMat = new Material(previewMaterial);
                }
                previewRuntimeMat.color = color;
                r.material = previewRuntimeMat;
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

        private Vector3 CalcHalfExtents()
        {
            var temp = Instantiate(placePrefab);
            var bounds = GetBounds(temp);
            Destroy(temp);
            return bounds.extents;
        }

        private Bounds GetBounds(GameObject go)
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



        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private Vector3 SnapXZ(Vector3 pos, float snapSize)
        {
            if (snapSize <= 0) return pos;
            return new Vector3(
                Mathf.Round(pos.x / snapSize) * snapSize,
                pos.y,
                Mathf.Round(pos.z / snapSize) * snapSize
            );
        }

        // Public API for external systems to trigger placement
        public void StartPlacing()
        {
            if (placePrefab != null)
            {
                isPlacing = true;
                Debug.Log($"[PLACE] Started placing: {placePrefab.name}");
            }
        }

        public void SetPrefab(GameObject prefab)
        {
            placePrefab = prefab;
        }

        // Property for external access
        public GameObject PrefabToPlace 
        { 
            get => placePrefab; 
            set => placePrefab = value; 
        }
    }
}