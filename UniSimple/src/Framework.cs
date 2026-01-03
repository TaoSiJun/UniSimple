using UniSimple.Singleton;

namespace UniSimple
{
    public static class Framework
    {
        public static void Initialize()
        {
        }

        public static void Update(float deltaTime)
        {
            SingletonManager.Update(deltaTime);
        }
    }
}