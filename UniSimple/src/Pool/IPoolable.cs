namespace UniSimple.Pool
{
    // 可池化对象接口
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
        void OnDestroy();
    }

    // 对象池接口
    public interface IObjectPool<T> where T : class
    {
        T Get();
        void Release(T item);
        void Prewarm(int count);
        void Clear();
        int TotalCount { get; }
        int ActiveCount { get; }
        int InactiveCount { get; }
    }
}