using DeepAbyssHive.Core.Interfaces;

namespace DeepAbyssHive.Terrain.Managers
{
    /// <summary>
    /// 將三個「帶參數」更新介面顯式實作，轉呼叫內部 Tick*，以滿足 IManager/ I*Updatable 而不違反 Unity 無參數規範
    /// </summary>
    public partial class TerrainManager : IUpdatable, IFixedUpdatable, ILateUpdatable
    {
        // IUpdatable 實現
        void IUpdatable.Update(float deltaTime) => TickUpdate(deltaTime);
        void IFixedUpdatable.FixedUpdate(float fixedDeltaTime) => TickFixedUpdate(fixedDeltaTime);
        void ILateUpdatable.LateUpdate(float deltaTime) => TickLateUpdate(deltaTime);
        
        // IManager 的 new 方法實現
        void IManager.Update(float deltaTime) => TickUpdate(deltaTime);
        void IManager.FixedUpdate(float fixedDeltaTime) => TickFixedUpdate(fixedDeltaTime);
        void IManager.LateUpdate(float deltaTime) => TickLateUpdate(deltaTime);
    }
}