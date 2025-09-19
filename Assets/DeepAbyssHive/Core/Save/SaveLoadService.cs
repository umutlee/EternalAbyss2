using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using DeepAbyssHive.Core.Logging;

namespace DeepAbyssHive.Core.Save
{
    /// <summary>
    /// v1 存讀服務：版本化 JSON + slot；自動蒐集 ISaveParticipant。
    /// </summary>
    public static class SaveLoadService
    {
        [Serializable]
        public class SaveEntry
        {
            public string key;        // 參與者鍵
            public string type;       // 狀態型別全名（供除錯/前向相容）
            public string payload;    // JsonUtility.ToJson(state)
        }

        [Serializable]
        public class SaveGameV1
        {
            public string version = "v1";
            public string slot = "autosave";
            public string savedAt;        // ISO8601
            public List<SaveEntry> entries = new List<SaveEntry>();
            // 可擴充：全域欄位（例如地圖 seed、遊戲版本等）
        }

        public static string SavesDir =>
            Path.Combine(Application.persistentDataPath, "Saves");

        public static string GetSlotPath(string slot) =>
            Path.Combine(SavesDir, $"{slot}.json");

        public static void Save(string slot = "autosave")
        {
            try
            {
                Directory.CreateDirectory(SavesDir);
                var data = new SaveGameV1 { slot = slot, savedAt = DateTime.UtcNow.ToString("o") };

                // 蒐集所有參與者
                var participants = GameObject.FindObjectsOfType<MonoBehaviour>(true);
                foreach (var mb in participants)
                {
                    if (mb is ISaveParticipant p)
                    {
                        var state = p.CaptureState();
                        if (state == null) continue;
                        var entry = new SaveEntry
                        {
                            key = p.Key,
                            type = state.GetType().AssemblyQualifiedName,
                            payload = JsonUtility.ToJson(state)
                        };
                        data.entries.Add(entry);
                    }
                }

                var json = JsonUtility.ToJson(data, true);
                File.WriteAllText(GetSlotPath(slot), json);
                DAHLog.Info(LogCategory.SERVICE, $"Saved slot='{slot}' entries={data.entries.Count} -> {GetSlotPath(slot)}");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.SERVICE, "Save failed: " + ex.Message);
            }
        }

        public static void Load(string slot = "autosave")
        {
            try
            {
                var path = GetSlotPath(slot);
                if (!File.Exists(path)) { DAHLog.Info(LogCategory.SERVICE, $"No save file for slot='{slot}'"); return; }
                var json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveGameV1>(json);
                if (data == null || data.entries == null) { DAHLog.Warning(LogCategory.SERVICE, "Invalid save file (null entries)"); return; }

                // 掃描場景，按 key 分派
                var participants = GameObject.FindObjectsOfType<MonoBehaviour>(true);
                var map = new Dictionary<string, ISaveParticipant>();
                foreach (var mb in participants) if (mb is ISaveParticipant p) map[p.Key] = p; // 最後出現者覆蓋

                int applied = 0;
                foreach (var e in data.entries)
                {
                    if (!map.TryGetValue(e.key, out var p)) continue;
                    // 利用已知 type 名稱反序列化；若型別不存在/搬家，嘗試以匿名容器（同鍵名）做兼容
                    object stateObj = null;
                    try
                    {
                        var t = Type.GetType(e.type, false);
                        if (t != null) stateObj = JsonUtility.FromJson(e.payload, t);
                    }
                    catch { /* 忽略，stateObj 可能為 null */ }
                    if (stateObj == null) { DAHLog.Warning(LogCategory.SERVICE, $"Skip '{e.key}' – type missing: {e.type}"); continue; }
                    try { p.RestoreState(stateObj); applied++; }
                    catch (Exception rex) { DAHLog.Error(LogCategory.SERVICE, $"Restore '{e.key}' failed: {rex.Message}"); }
                }
                DAHLog.Info(LogCategory.SERVICE, $"Loaded slot='{slot}' applied={applied}/{data.entries.Count}");
            }
            catch (Exception ex)
            {
                DAHLog.Error(LogCategory.SERVICE, "Load failed: " + ex.Message);
            }
        }
    }
}