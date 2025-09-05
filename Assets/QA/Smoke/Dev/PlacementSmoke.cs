using UnityEngine;
using DeepAbyssHive.Common.Placement;
using DeepAbyssHive.Core.Config;

public class PlacementSmoke : MonoBehaviour
{
    public KeyCode triggerKey = KeyCode.F7;
    public Vector3 testOrigin = new Vector3(0, 0.5f, 0);
    public Vector3 halfExtents = new Vector3(0.5f, 0.5f, 0.5f); // 1x1x1 測試建築

    private GameObject _anchor;

    void Update()
    {
        if (Input.GetKeyDown(triggerKey))
        {
            RunSmoke();
        }
    }

    private void RunSmoke()
    {
        var cfg = GameConfigProvider.Current;
        float ms = Mathf.Max(0.5f, cfg.minSpacing > 0 ? cfg.minSpacing : 1.0f);

        // 建立一個基準「已放置建築」：放在 Building 層
        int buildingLayer = LayerMask.NameToLayer("Building");
        if (buildingLayer < 0)
        {
            Debug.LogWarning("[SMOKE] Building layer not found; skipping.");
            return;
        }
        int terrainLayer = LayerMask.NameToLayer("Terrain");
        int ignoreRaycast = LayerMask.NameToLayer("Ignore Raycast");

        var mask = ~0;
        if (terrainLayer >= 0)   mask &= ~(1 << terrainLayer);
        if (ignoreRaycast >= 0)  mask &= ~(1 << ignoreRaycast);

        if (_anchor != null) DestroyImmediate(_anchor);
        _anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _anchor.layer = buildingLayer;
        _anchor.name = "SMOKE_PlacedAnchor";
        _anchor.transform.position = testOrigin;
        var bc = _anchor.GetComponent<BoxCollider>();
        if (bc == null) bc = _anchor.AddComponent<BoxCollider>();
        bc.size = halfExtents * 2f;

        // Case A：距離 < minSpacing（應該 Collision）
        Vector3 posA = testOrigin + new Vector3(ms * 0.4f, 0, 0);
        var bA = new Bounds(posA, halfExtents * 2f);
        var rA = PlacementValidator.ValidateByConfig(bA, mask, 0f);
        bool passA = (!rA.ok && rA.code == PlaceResultCode.E_PLACE_COLLISION);
        Debug.Log($"[SMOKE] A: expect COLLISION at d={ms*0.4f:0.###} -> {(passA ? "PASS" : "FAIL")} ({rA.code})");

        // Case B：距離 > minSpacing（應該 OK）
        Vector3 posB = testOrigin + new Vector3(ms * 1.5f, 0, 0);
        var bB = new Bounds(posB, halfExtents * 2f);
        var rB = PlacementValidator.ValidateByConfig(bB, mask, 0f);
        bool passB = (rB.ok && rB.code == PlaceResultCode.OK);
        Debug.Log($"[SMOKE] B: expect OK at d={ms*1.5f:0.###} -> {(passB ? "PASS" : "FAIL")} ({rB.code})");

        // 清理
        Destroy(_anchor);
        _anchor = null;
    }
}