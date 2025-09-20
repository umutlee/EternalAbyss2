using UnityEngine;
using DeepAbyssHive.Core.Interfaces;
using DeepAbyssHive.Terrain.Interfaces;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// TerrainManager Unity 生命周期回調
    /// 統一管理所有 Unity 生命周期方法，避免參數衝突
    /// EA-M1-T01: 實現 ITerrainManager 接口
    /// </summary>
    public partial class TerrainManager : MonoBehaviour, ITerrainManager
    {
        private void Start()
        {
            // 初始化隨機種子（必須在 Unity 生命週期方法中呼叫）
            UnityEngine.Random.InitState(ConfigSeed);
            Initialize();
        }

        private void Update()      => TickUpdate(UnityEngine.Time.deltaTime);
        private void LateUpdate()  => TickLateUpdate(UnityEngine.Time.deltaTime);
        private void FixedUpdate() => TickFixedUpdate(UnityEngine.Time.fixedDeltaTime);
    }
}