using UnityEngine;
using DeepAbyssHive.Creep.Managers;

namespace QA.Smoke.Dev
{
    public class CreepStatsHUD : MonoBehaviour
    {
        public bool topLeft = true;
        private Rect _rect;

        void Start()
        {
            var def = topLeft ? new Rect(10, 40, 520, 60) : new Rect(Screen.width - 530, 40, 520, 60);
            _rect = HudDragUtil.GetRect("HUD.CreepStats", def);
        }

        void OnGUI()
        {
            var cm = CreepManager.GetActive();
            if (!cm) return;

            cm.GetTotals(out int total, out int covered);
            float cov = total > 0 ? (covered * 100f / total) : 0f;
            cm.GetLastPerf(out int stepCells, out float stepMs);

            _rect = HudDragUtil.DraggableWindow("HUD.CreepStats", _rect, "Creep Stats", () =>
            {
                GUILayout.Label($"Cells: {covered}/{total} ({cov:0.0}%)");
                GUILayout.Label($"Last: {stepCells} cells, {stepMs:0.00} ms");
            });
        }
    }
}