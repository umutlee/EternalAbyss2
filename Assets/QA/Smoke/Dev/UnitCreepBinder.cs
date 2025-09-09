using UnityEngine;
using DeepAbyssHive.Creep.Managers;
using DeepAbyssHive.Units.Agents;

/// <summary>
/// DEV ONLY：將 UnitAgent.OnCreepPredicate 綁到 CreepManager.IsSetWorld()。
/// 掛在 Managers 或任意物件即可；找不到 CreepManager 會印一次警告。
/// </summary>
public class UnitCreepBinder : MonoBehaviour
{
    private bool _bound;
    
    void OnEnable()
    {
        var cm = FindObjectOfType<CreepManager>();
        if (!cm) { Debug.LogWarning("[UnitCreepBinder] CreepManager not found (will retry once)."); _bound = false; return; }
        UnitAgent.OnCreepPredicate = (pos) => cm.HasCreepAt(pos);
        _bound = true;
        Debug.Log("[UnitCreepBinder] Bound UnitAgent.OnCreepPredicate -> CreepManager.IsSetWorld()");
    }

    void Update()
    {
        if (_bound || UnitAgent.OnCreepPredicate != null) return;
        var cm = FindObjectOfType<CreepManager>();
        if (!cm) return;
        UnitAgent.OnCreepPredicate = (pos) => cm.HasCreepAt(pos);
        _bound = true;
        Debug.Log("[UnitCreepBinder] Rebound predicate (late init)");
    }

    void OnDisable()
    {
        UnitAgent.OnCreepPredicate = null;
        Debug.Log("[UnitCreepBinder] Unbound UnitAgent.OnCreepPredicate");
    }
}