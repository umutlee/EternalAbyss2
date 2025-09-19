using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Core.Services;

namespace DeepAbyssHive.Core.Save.Participants
{
    /// <summary>
    /// v1 範例參與者：保存/還原 TimeService 狀態（Paused/Scale）。
    /// 自動隨遊戲啟動創建並掛到 Managers。
    /// </summary>
    public class TimeServiceSaveParticipant : MonoBehaviour, ISaveParticipant
    {
        public string Key => "time.service";

        private ITimeService _timeService;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (FindObjectOfType<TimeServiceSaveParticipant>() != null) return;
            var go = new GameObject("SaveP-TimeService"); go.AddComponent<TimeServiceSaveParticipant>();
            var managers = GameObject.Find("Managers"); if (managers != null) go.transform.SetParent(managers.transform);
            DontDestroyOnLoad(go);
        }

        private void Start()
        {
            try
            {
                _timeService = ServiceManager.Instance.GetService<ITimeService>();
            }
            catch (System.Exception)
            {
                // TimeService 可能尚未註冊，使用 Unity.Time 作為後備
            }
        }

        [System.Serializable]
        public class State
        {
            public bool paused;
            public float scale;
        }

        public object CaptureState()
        {
            if (_timeService != null)
            {
                return new State { paused = _timeService.IsPaused, scale = _timeService.TimeScale };
            }
            else
            {
                // 後備：直接讀取 Unity.Time
                return new State { paused = UnityEngine.Time.timeScale == 0f, scale = UnityEngine.Time.timeScale };
            }
        }

        public void RestoreState(object state)
        {
            if (state is State s)
            {
                if (_timeService != null)
                {
                    _timeService.SetPaused(s.paused);
                    _timeService.SetTimeScale(Mathf.Max(0.0001f, s.scale));
                }
                else
                {
                    // 後備：直接設置 Unity.Time
                    UnityEngine.Time.timeScale = s.paused ? 0f : Mathf.Max(0.0001f, s.scale);
                }
            }
        }
    }
}