using UnityEngine;
using DeepAbyssHive.Creep.Managers;

namespace QA.Smoke.Dev
{
    public class CreepStatsHUD : MonoBehaviour
    {
        public bool topLeft = true;

        void OnGUI()
        {
            var cm = CreepManager.GetActive();
            if (!cm) return;

            cm.GetTotals(out int total, out int covered);
            float cov = total > 0 ? (covered * 100f / total) : 0f;
            var rect = topLeft ? new Rect(10, 40, 520, 22) : new Rect(Screen.width - 530, 40, 520, 22);
            cm.GetLastPerf(out int stepCells, out float stepMs);
            GUI.Label(rect, $"Creep: cells={covered}/{total} ({cov:0.0}%) | last: {stepCells} cells, {stepMs:0.00} ms");
        }
    }
}