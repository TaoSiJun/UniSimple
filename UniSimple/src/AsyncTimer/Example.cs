using UniSimple.AsyncTimer;

namespace UniSimple.Timer
{
    /*
    public class Example : MonoBehaviour
    {
        void Start()
        {
            // 1. 延迟 2 秒，并绑定到当前 GameObject
            // 如果 GameObject 在 2 秒内销毁，Timer 自动停止并回收
            AsyncTimerManager.Delay(2f, () =>
            {
                Debug.Log("2秒到了");
            }).AttachTo(this.gameObject);

            // 2. 等待直到某个变量为 true
            bool isReady = false;
            AsyncTimerManager.WaitUntil(() => isReady, () =>
            {
                Debug.Log("Ready!");
            }).AttachTo(this);

            // 3. 模拟销毁
            // Destroy(this.gameObject, 1f);
            // 上面的 Timer 将在 1秒后自动停止，不会报错，也不会执行回调。
        }

        void OnDestroy()
        {
            // 建议：如果你不是把 Timer 绑定在 GameObject 上，而是作为全局逻辑
            // 可以在这里手动清理，或者在场景管理器里调用 AsyncTimerManager.DisposeAll();
        }
    }
    */

    public class Example
    {
        public void Main()
        {
            AsyncTimerManager.Delay(1f, () => { }).WithTag("Test");
        }
    }
}