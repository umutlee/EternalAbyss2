using UnityEngine;
using DeepAbyssHive.Buildings.Runtime;

namespace DeepAbyssHive.Buildings.UI
{
    /// <summary>極簡 HUD：顯示當前建築 id 與鍵提示（IMGUI，可拖拽）。</summary>
    public class BuildingSelectionHUD : MonoBehaviour
    {
        private Rect _rect = new Rect(12, 12, 320, 48);

        private void OnGUI()
        {
            var cat = BuildingCatalogRuntime.Instance;
            if (cat == null || cat.Count == 0) return;
            _rect = GUI.Window(0xDAH1701, _rect, Draw, "Building");
        }

        private void Draw(int id)
        {
            var cat = BuildingCatalogRuntime.Instance;
            GUILayout.Label($"Selected: {cat.CurrentId}  (Prev: '['  Next: ']')");
            GUI.DragWindow(new Rect(0,0,10000,20));
        }
    }
}