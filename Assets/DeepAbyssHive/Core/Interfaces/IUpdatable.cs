namespace DeepAbyssHive.Core.Interfaces
{
    /// <summary>
    /// 可更新接口 - 用于需要在Update中更新的对象
    /// </summary>
    public interface IUpdatable
    {
        /// <summary>
        /// 更新方法，在每帧调用
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间间隔</param>
        void Update(float deltaTime);
    }
    
    /// <summary>
    /// 固定更新接口 - 用于需要在FixedUpdate中更新的对象
    /// </summary>
    public interface IFixedUpdatable
    {
        /// <summary>
        /// 固定更新方法，在固定时间间隔调用
        /// </summary>
        /// <param name="fixedDeltaTime">固定时间间隔</param>
        void FixedUpdate(float fixedDeltaTime);
    }
    
    /// <summary>
    /// 延迟更新接口 - 用于需要在LateUpdate中更新的对象
    /// </summary>
    public interface ILateUpdatable
    {
        /// <summary>
        /// 延迟更新方法，在所有Update之后调用
        /// </summary>
        /// <param name="deltaTime">距离上一帧的时间间隔</param>
        void LateUpdate(float deltaTime);
    }
}