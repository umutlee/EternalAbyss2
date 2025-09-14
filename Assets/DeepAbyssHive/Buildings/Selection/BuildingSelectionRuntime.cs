using UnityEngine;
using DeepAbyssHive.Core.Config;

namespace DeepAbyssHive.Buildings.Selection
{
    /// <summary>
    /// 運行期驅動：監聽 GameConfig 的 Next/Prev 熱鍵，驅動 Provider。
    /// </summary>
    public class BuildingSelectionRuntime : MonoBehaviour
    {
        private static bool _booted;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (_booted) return;
            _booted = true;
            BuildingSelectionProvider.InitializeFromConfig();
            var go = new GameObject("BuildingSelectionRuntime");
            go.hideFlags = HideFlags.HideAndDontSave;
            DontDestroyOnLoad(go);
            go.AddComponent<BuildingSelectionRuntime>();
        }

        private void Update()
        {
            var cfg = GameConfigProvider.Current;
            if (cfg == null) return;
            if (Input.GetKeyDown(cfg.buildingCycleNextKey))
                BuildingSelectionProvider.CycleNext();
            if (Input.GetKeyDown(cfg.buildingCyclePrevKey))
                BuildingSelectionProvider.CyclePrev();
        }
    }
}