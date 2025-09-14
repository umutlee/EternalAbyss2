using UnityEngine;
using DeepAbyssHive.Buildings.Selection;
using DeepAbyssHive.Buildings.Config;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.QA.Smoke.DevHUD
{
    /// <summary>
    /// 簡易顯示目前的建築選取（與其他 HUD 一樣可關閉/拖拽的機制留給既有 HUD 管理）。
    /// </summary>
    public class BuildingSelectionHUD : MonoBehaviour
    {
        private string _label = "(none)";
        private Rect _win = new Rect(10, 80, 260, 58);

        private void OnEnable()
        {
            BuildingSelectionProvider.OnSelectionChanged += OnSel;
            var e = BuildingSelectionProvider.CurrentEntry;
            OnSel(e);
        }
        private void OnDisable()
        {
            BuildingSelectionProvider.OnSelectionChanged -= OnSel;
        }

        private void OnSel(BuildingCatalogEntry e)
        {
            if (e == null || e.prefab == null) { _label = "(none)"; return; }
            _label = $"{e.id}  half=({e.footprintHalfExtents.x:F1},{e.footprintHalfExtents.y:F1},{e.footprintHalfExtents.z:F1})";
        }

        private void OnGUI()
        {
            _win = GUI.Window(0xDABB1D, _win, id =>
            {
                GUILayout.Label("Building Selection");
                GUILayout.Label(_label);
                GUI.DragWindow();
            }, "DEV • BUILDINGS");
        }
    }
}