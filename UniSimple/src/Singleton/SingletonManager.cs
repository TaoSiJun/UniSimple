using System;
using System.Collections.Generic;

namespace UniSimple.Singleton
{
    /// <summary>
    /// 单例管理器
    /// </summary>
    public static class SingletonManager
    {
        private static readonly List<ISingleton> AllSingletonList = new();
        private static readonly List<IUpdatable> UpdatableList = new();

        // 标记是否在清理中
        public static bool IsShuttingDown { get; private set; }

        internal static void Register(ISingleton singleton)
        {
            if (IsShuttingDown)
                return;

            if (AllSingletonList.Contains(singleton))
                return;

            AllSingletonList.Add(singleton);
            AllSingletonList.Sort((a, b) => b.Priority.CompareTo(a.Priority));

            if (singleton is IUpdatable updatable)
            {
                UpdatableList.Add(updatable);
                UpdatableList.Sort((a, b) => (b as ISingleton).Priority.CompareTo((a as ISingleton).Priority));
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
                    UnityEngine.Debug.LogError($"[SingletonManager] Error updating {AllSingletonList[i].GetType().Name}: {e}");
                }
            }
        }

        internal static void DestroyAll()
        {
            IsShuttingDown = true;

            for (var i = AllSingletonList.Count - 1; i >= 0; i--)
            {
                try
                {
                    AllSingletonList[i].OnDestroy();
                }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogError($"[SingletonManager] Error destroying {AllSingletonList[i].GetType().Name}: {e}");
                }
            }

            AllSingletonList.Clear();
            UpdatableList.Clear();
            IsShuttingDown = false;
        }
    }
}