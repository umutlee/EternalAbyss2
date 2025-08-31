using System.Collections;
namespace DeepAbyssHive.Core.Utils
{
    /// <summary> No-op runner for places that are not MonoBehaviour but called StartCoroutine. </summary>
    public static class CoroutineStub
    {
        public static void Start(IEnumerator routine) { /* no-op */ }
    }
}