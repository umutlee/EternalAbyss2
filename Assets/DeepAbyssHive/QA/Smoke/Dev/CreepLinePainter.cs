using UnityEngine;
using System.Collections.Generic;
using DeepAbyssHive.Creep.Managers;

/// <summary>
/// DEV ONLY：兩點成線畫菌毯。點 A → 點 B，沿線以指定半徑撒種子。
/// 建議與 CreepBrushAndRunner 搭配使用（把 Budget 設為 0 可避免擴張）。
/// </summary>
public class CreepLinePainter : MonoBehaviour
{
    [Header("Raycast")]
    public LayerMask rayMask;                 // 建議指向 Terrain；miss 時會自動 fallback 到 ~0
    public float maxRayDist = 5000f;

    [Header("Paint")]
    [Tooltip("畫線的半徑（世界單位）。")]
    public float radius = 1.0f;
    [Tooltip("步進比例（0.3~0.8）。間距 = radius * stepRatio。")]
    public float stepRatio = 0.6f;

    [Header("Gizmos")]
    public bool drawGizmos = true;
    private readonly Queue<Vector3> _lastPaint = new Queue<Vector3>(256);
    private bool _warnedMaskOnce;

    private Vector3? _pA;

    void Reset()
    {
        int terrain = LayerMask.NameToLayer("Terrain");
        rayMask = (terrain >= 0) ? (1 << terrain) : ~0;
        
        // 設置合理的預設值
        if (radius <= 0f) radius = 1.0f;
        if (stepRatio <= 0f) stepRatio = 0.6f;
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
        var cm = CreepManager.GetActive();
        if (!cm) 
        { 
            Debug.LogWarning("[CreepLinePainter] CreepManager not found or not active."); 
            return; 
        }

        float dist = Vector3.Distance(a, b);
        if (dist < 0.001f) 
        { 
            cm.SeedWorld(a); 
            Debug.Log($"[DEV] CreepLinePainter: single seed at {a}");
            return; 
        }

        float step = Mathf.Max(0.05f, radius * Mathf.Clamp(stepRatio, 0.3f, 0.8f));
        int n = Mathf.CeilToInt(dist / step);
        
        for (int i = 0; i <= n; i++)
        {
            float t = (n == 0) ? 0f : (i / (float)n);
            Vector3 p = Vector3.Lerp(a, b, t);
            cm.SeedWorld(p);
            if (_lastPaint.Count >= 256) _lastPaint.Dequeue();
            _lastPaint.Enqueue(p);
        }
        
        Debug.Log($"[DEV] CreepLinePainter: painted {n+1} seeds, radius={radius:0.##}, from {a} to {b}");
    }

    private bool TryRayToPoint(out RaycastHit hit)
    {
        var cam = Camera.main; if (!cam) { hit = default; return false; }
        var ray = cam.ScreenPointToRay(Input.mousePosition);
        // 先用指定遮罩；miss 再用全遮罩一次（提示一次）
        if (Physics.Raycast(ray, out hit, maxRayDist, rayMask, QueryTriggerInteraction.Ignore))
            return true;
        if (!_warnedMaskOnce)
        {
            _warnedMaskOnce = true;
            Debug.LogWarning("[CreepLinePainter] Ray miss with current mask — falling back to ~0 once. Consider setting rayMask=Terrain.");
        }
        return Physics.Raycast(ray, out hit, maxRayDist, ~0, QueryTriggerInteraction.Ignore);
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || _pA == null) return;
        Gizmos.color = new Color(0f, 1f, 0.6f, 0.75f);
        Gizmos.DrawWireSphere(_pA.Value, Mathf.Max(0.05f, radius));
        // 畫出最近一次的落點序列，讓你看得到「真的有在畫」
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f);
        foreach (var p in _lastPaint) Gizmos.DrawSphere(p + Vector3.up * 0.05f, 0.08f);
    }
}