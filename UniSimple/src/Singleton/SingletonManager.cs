using System;
using System.Collections.Generic;

namespace UniSimple.Singleton
{
    /// <summary>
    /// 单例管理器
    /// </summary>
    public static class SingletonManager
    {
        private static readonly List<ISingleton> AllList = new();
        private static readonly List<IUpdatable> UpdatableList = new();

        // 标记是否在清理中
        public static bool IsShuttingDown { get; private set; }
        public static bool IsInitialized { get; private set; }

        public static void Initialize()
        {
            if (IsInitialized) return;

            var go = new UnityEngine.GameObject("SingletonDriver");
            go.AddComponent<SingletonDriver>();
            IsInitialized = true;
        }

        internal static void Register(ISingleton singleton)
        {
            if (IsShuttingDown)
                return;

            if (AllList.Contains(singleton))
                return;

            AllList.Add(singleton);
            AllList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            if (singleton is IUpdatable updatable)
            {
                UpdatableList.Add(updatable);
                UpdatableList.Sort((a, b) => ((ISingleton)b).Priority.CompareTo(((ISingleton)a).Priority));
            }
        }

        internal static void Update(float deltaTime)
        {
            if (IsShuttingDown)
                return;

            for (var i = 0; i < UpdatableList.Count; i++)
            {
                try
                {
                    UpdatableList[i].OnUpdate(deltaTime);
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[SingletonManager] Error updating {AllList[i].GetType().Name}: {e}");
                }
            }
        }

        internal static void DestroyAll()
        {
            IsShuttingDown = true;

            for (var i = AllList.Count - 1; i >= 0; i--)
            {
                try
                {
                    AllList[i].OnDestroy();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[SingletonManager] Error destroying {AllList[i].GetType().Name}: {e}");
                }
            }

            AllList.Clear();
            UpdatableList.Clear();
            IsShuttingDown = false;
            IsInitialized = false;
        }
    }
}