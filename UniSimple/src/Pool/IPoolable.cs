namespace UniSimple.Pool
{
    public interface IPoolable
    {
        void OnSpawn();

        void OnDespawn();
    }
}