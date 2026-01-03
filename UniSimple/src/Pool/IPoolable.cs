namespace UniSimple.Pool
{
    // 可池化对象接口
    public interface IPoolable
    {
        void OnSpawn();
        void OnDespawn();
        void OnDestroy();
    }
}