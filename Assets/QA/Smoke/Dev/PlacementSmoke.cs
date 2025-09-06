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

    private int BuildPlacementMask()
    {
        int mask = ~0;
        int terrain = LayerMask.NameToLayer("Terrain");
        int ignore  = LayerMask.NameToLayer("Ignore Raycast");
        if (terrain >= 0) mask &= ~(1 << terrain);
        if (ignore  >= 0) mask &= ~(1 << ignore);
        return mask;
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
        int mask = BuildPlacementMask();

        if (_anchor != null) DestroyImmediate(_anchor);
        _anchor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _anchor.layer = buildingLayer;
        _anchor.name = "SMOKE_PlacedAnchor";
        _anchor.transform.position = testOrigin;
        var bc = _anchor.GetComponent<BoxCollider>() ?? _anchor.AddComponent<BoxCollider>();
        bc.size = halfExtents * 2f;

        // Case A：距離 < minSpacing（應該 Collision）
        Vector3 posA = testOrigin + new Vector3(ms * 0.4f, 0, 0);
        var rA = PlacementValidator.ValidateByConfig(posA, halfExtents, Quaternion.identity, mask, 0f);
        bool passA = (!rA.ok && rA.code == PlaceResultCode.E_PLACE_COLLISION);
        Debug.Log($"[SMOKE] A: expect COLLISION at d={ms*0.4f:0.###} -> {(passA ? "PASS" : "FAIL")} ({rA.code})");

        // Case B：距離 > minSpacing（應該 OK）
        Vector3 posB = testOrigin + new Vector3(ms * 1.5f + halfExtents.x * 2f, 0, 0); // 增加尺寸餘裕，避免 Physics 重疊
        var rB = PlacementValidator.ValidateByConfig(posB, halfExtents, Quaternion.identity, mask, 0f);
        bool passB = (rB.ok && rB.code == PlaceResultCode.OK);
        Debug.Log($"[SMOKE] B: expect OK at d~{(ms*1.5f + halfExtents.x*2f):0.###} -> {(passB ? "PASS" : "FAIL")} ({rB.code})");

        // Case C：旋轉 45° / 90° 在 minSpacing 邊界附近（應該 OK，不與 Physics 重疊）
        float edge = Mathf.Max(ms, halfExtents.x * 2f + 0.1f);
        Vector3 posC = testOrigin + new Vector3(edge, 0, 0);
        var rC1 = PlacementValidator.ValidateByConfig(posC, halfExtents, Quaternion.Euler(0, 45, 0), mask, 0f);
        var rC2 = PlacementValidator.ValidateByConfig(posC, halfExtents, Quaternion.Euler(0, 90, 0), mask, 0f);
        bool passC = rC1.ok && rC2.ok;
        Debug.Log($"[SMOKE] C: expect OK at edge~{edge:0.###} with rot 45/90 -> {(passC ? "PASS" : "FAIL")} ({rC1.code}/{rC2.code})");

        // Case D：刪除後同點重測，不應再回 Collision（刪除=移除鄰居/碰撞源）
        // 先在同點測一次（應碰 anchor → Collision）
        var rBefore = PlacementValidator.ValidateByConfig(testOrigin, halfExtents, Quaternion.identity, mask, 0f);
        // 立即刪除 anchor，再測一次（不應再是 Collision；其他規則可能阻擋，但不應為 Collision）
        DestroyImmediate(_anchor); _anchor = null;
        var rAfter = PlacementValidator.ValidateByConfig(testOrigin, halfExtents, Quaternion.identity, mask, 0f);
        bool passD = (rBefore.code == PlaceResultCode.E_PLACE_COLLISION) && (rAfter.code != PlaceResultCode.E_PLACE_COLLISION);
        Debug.Log($"[SMOKE] D: delete-and-rebuild at same pos -> {(passD ? "PASS" : "FAIL")} (before={rBefore.code}, after={rAfter.code})");
    }
}