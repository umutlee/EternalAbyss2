using UnityEngine;
using DeepAbyssHive.Creep.Managers;
using DeepAbyssHive.Units.Agents;

/// <summary>
/// DEV ONLY：將 UnitAgent.OnCreepPredicate 綁到 CreepManager.IsSetWorld()。
/// 掛在 Managers 或任意物件即可；找不到 CreepManager 會印一次警告。
/// </summary>
public class UnitCreepBinder : MonoBehaviour
{
    void OnEnable()
    {
        var cm = FindObjectOfType<CreepManager>();
        if (!cm) { Debug.LogWarning("[UnitCreepBinder] CreepManager not found."); return; }
        UnitAgent.OnCreepPredicate = (pos) => cm.HasCreepAt(pos);
        Debug.Log("[UnitCreepBinder] Bound UnitAgent.OnCreepPredicate -> CreepManager.IsSetWorld()");
    }

    void OnDisable()
    {
        UnitAgent.OnCreepPredicate = null;
        Debug.Log("[UnitCreepBinder] Unbound UnitAgent.OnCreepPredicate");
    }
}