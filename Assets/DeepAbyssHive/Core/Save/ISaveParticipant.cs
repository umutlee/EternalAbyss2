using UnityEngine;

namespace DeepAbyssHive.Core.Save
{
    /// <summary>
    /// 任何想被存讀的系統可掛一個 ISaveParticipant（通常是 MonoBehaviour）。
    /// CaptureState 回傳一個可被 JsonUtility 序列化的 POCO；RestoreState 以相同型別的物件還原。
    /// </summary>
    public interface ISaveParticipant
    {
        /// <summary>全域唯一鍵（建議：模組.子系統，如 "time.service"、"resources.core"）。</summary>
        string Key { get; }
        /// <summary>擷取狀態（POCO）。</summary>
        object CaptureState();
        /// <summary>還原狀態（與擷取型別一致）。</summary>
        void RestoreState(object state);
    }
}