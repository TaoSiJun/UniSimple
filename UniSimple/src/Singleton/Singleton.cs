namespace UniSimple.Singleton
{
    public interface ISingleton
    {
        void OnCreate();

        void OnDestroy();

        /// <summary>
        /// 优先级
        /// 越大越先 Update 越后 Destroy
        /// </summary>
        int Priority { get; }
    }


    /// <summary>
    /// 抽象单例基类
    /// </summary>
    public abstract class Singleton<T> : ISingleton where T : Singleton<T>, new()
    {
        private static T _instance;
        private static readonly object _lock = new();

        public static T Instance
        {
            get
            {
                if (SingletonManager.IsShuttingDown)
                {
                    UnityEngine.Debug.LogWarning($"[Singleton] Trying to access {typeof(T).Name} during shutdown.");
                    return null;
                }

                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new T();
                            SingletonManager.Register(_instance);
                            _instance.OnCreate();
                        }
                    }
                }

                return _instance;
            }
        }

        public virtual int Priority => 0;

        public virtual void OnCreate()
        {
        }

        public virtual void OnDestroy()
        {
            _instance = null;
        }
    }
}