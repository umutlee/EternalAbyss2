using UnityEngine;
using DeepAbyssHive.Creep.Managers;

/// <summary>
/// DEV ONLY：兩點成線畫菌毯。點 A → 點 B，沿線以指定半徑撒種子。
/// 建議與 CreepBrushAndRunner 搭配使用（把 Budget 設為 0 可避免擴張）。
/// </summary>
public class CreepLinePainter : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask rayMask;                 // 預設 Inspector 選 Terrain
    public float maxRayDist = 5000f;

    [Header("Paint")]
    [Tooltip("畫線的半徑（世界單位）。")]
    public float radius = 1.0f;
    [Tooltip("步進比例（0.3~0.8）。間距 = radius * stepRatio。")]
    public float stepRatio = 0.6f;

    [Header("Gizmos")]
    public bool drawGizmos = true;

    private Vector3? _pA;

    void Reset()
    {
        int terrain = LayerMask.NameToLayer("Terrain");
        rayMask = (terrain >= 0) ? (1 << terrain) : ~0;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (TryRayToPoint(out var hit))
            {
                if (_pA == null) _pA = hit.point;           // 設定 A
                else { PaintLine(_pA.Value, hit.point); _pA = null; } // A→B 畫線
            }
        }
        // 右鍵取消 A
        if (Input.GetMouseButtonDown(1)) _pA = null;
    }

    private void PaintLine(Vector3 a, Vector3 b)
    {
        var cm = FindObjectOfType<CreepManager>();
        if (!cm) { Debug.LogWarning("[CreepLinePainter] CreepManager not found."); return; }

        float dist = Vector3.Distance(a, b);
        if (dist < 0.001f) { cm.SeedWorld(a); return; }

        float step = Mathf.Max(0.05f, radius * Mathf.Clamp(stepRatio, 0.3f, 0.8f));
        int n = Mathf.CeilToInt(dist / step);
        for (int i = 0; i <= n; i++)
        {
            float t = (n == 0) ? 0f : (i / (float)n);
            Vector3 p = Vector3.Lerp(a, b, t);
            cm.SeedWorld(p);
        }
        Debug.Log($"[DEV] CreepLinePainter: painted {n+1} seeds, radius={radius:0.##}");
    }

    private bool TryRayToPoint(out RaycastHit hit)
    {
        var cam = Camera.main; if (!cam) { hit = default; return false; }
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        return Physics.Raycast(ray, out hit, maxRayDist, rayMask, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || _pA == null) return;
        Gizmos.color = new Color(0f, 1f, 0.6f, 0.75f);
        Gizmos.DrawWireSphere(_pA.Value, Mathf.Max(0.05f, radius));
    }
}