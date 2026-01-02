namespace UniSimple
{
    public interface IUpdatable
    {
        void OnUpdate(float deltaTime);
    }

    public interface IFixedUpdatable
    {
        void OnFixedUpdate(float fixedDeltaTime);
    }

    public interface ILateUpdatable
    {
        void OnLateUpdate(float deltaTime);
    }
}