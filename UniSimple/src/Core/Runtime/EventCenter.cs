using System;
using System.Collections.Generic;

namespace UniSimple.Core
{
    /// <summary>
    /// 事件中心
    /// </summary>
    public static class EventCenter
    {
        private static readonly Dictionary<string, Delegate> Events = new();
        private static readonly Dictionary<object, List<(string, Delegate)>> OwnerEvents = new();

        // 编辑器下用于调试：记录事件监听数量
#if UNITY_EDITOR
        public static int EventCount => Events.Count;
        public static int OwnerCount => OwnerEvents.Count;
#endif

        #region 无参数事件

        public static void AddListener(string eventType, Action callback, object owner = null)
        {
            AddListenerInternal(eventType, callback, owner);
        }

        public static void RemoveListener(string eventType, Action callback)
        {
            RemoveListenerInternal(eventType, callback);
        }

        public static void Broadcast(string eventType)
        {
            if (Events.TryGetValue(eventType, out var del))
            {
                if (del is Action callbacks)
                {
                    foreach (var callback in callbacks.GetInvocationList())
                    {
                        try
                        {
                            ((Action)callback).Invoke();
                        }
                        catch (Exception e)
                        {
                            LogError(eventType, e);
                        }
                    }
                }
            }
        }

        #endregion

        #region 单参数事件

        public static void AddListener<T>(string eventType, Action<T> callback, object owner = null)
        {
            AddListenerInternal(eventType, callback, owner);
        }

        public static void RemoveListener<T>(string eventType, Action<T> callback)
        {
            RemoveListenerInternal(eventType, callback);
        }

        public static void Broadcast<T>(string eventType, T arg)
        {
            if (Events.TryGetValue(eventType, out var del))
            {
                if (del is Action<T> callbacks)
                {
                    foreach (var callback in callbacks.GetInvocationList())
                    {
                        try
                        {
                            ((Action<T>)callback).Invoke(arg);
                        }
                        catch (Exception e)
                        {
                            LogError(eventType, e);
                        }
                    }
                }
            }
        }

        #endregion

        #region 双参数事件

        public static void AddListener<T1, T2>(string eventType, Action<T1, T2> callback, object owner = null)
        {
            AddListenerInternal(eventType, callback, owner);
        }

        public static void RemoveListener<T1, T2>(string eventType, Action<T1, T2> callback)
        {
            RemoveListenerInternal(eventType, callback);
        }

        public static void Broadcast<T1, T2>(string eventType, T1 arg1, T2 arg2)
        {
            if (Events.TryGetValue(eventType, out var del))
            {
                if (del is Action<T1, T2> callbacks)
                {
                    foreach (var callback in callbacks.GetInvocationList())
                    {
                        try
                        {
                            ((Action<T1, T2>)callback).Invoke(arg1, arg2);
                        }
                        catch (Exception e)
                        {
                            LogError(eventType, e);
                        }
                    }
                }
            }
        }

        #endregion

        #region 三参数事件

        public static void AddListener<T1, T2, T3>(string eventType, Action<T1, T2, T3> callback, object owner = null)
        {
            AddListenerInternal(eventType, callback, owner);
        }

        public static void RemoveListener<T1, T2, T3>(string eventType, Action<T1, T2, T3> callback)
        {
            RemoveListenerInternal(eventType, callback);
        }

        public static void Broadcast<T1, T2, T3>(string eventType, T1 arg1, T2 arg2, T3 arg3)
        {
            if (Events.TryGetValue(eventType, out var del))
            {
                if (del is Action<T1, T2, T3> callbacks)
                {
                    foreach (var callback in callbacks.GetInvocationList())
                    {
                        try
                        {
                            ((Action<T1, T2, T3>)callback).Invoke(arg1, arg2, arg3);
                        }
                        catch (Exception e)
                        {
                            LogError(eventType, e);
                        }
                    }
                }
            }
        }

        #endregion

        #region 四参数事件

        public static void AddListener<T1, T2, T3, T4>(string eventType, Action<T1, T2, T3, T4> callback, object owner = null)
        {
            AddListenerInternal(eventType, callback, owner);
        }

        public static void RemoveListener<T1, T2, T3, T4>(string eventType, Action<T1, T2, T3, T4> callback)
        {
            RemoveListenerInternal(eventType, callback);
        }

        public static void Broadcast<T1, T2, T3, T4>(string eventType, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            if (Events.TryGetValue(eventType, out var del))
            {
                if (del is Action<T1, T2, T3, T4> callbacks)
                {
                    foreach (var callback in callbacks.GetInvocationList())
                    {
                        try
                        {
                            ((Action<T1, T2, T3, T4>)callback).Invoke(arg1, arg2, arg3, arg4);
                        }
                        catch (Exception e)
                        {
                            LogError(eventType, e);
                        }
                    }
                }
            }
        }

        #endregion

        #region 内部方法

        private static void AddListenerInternal(string eventType, Delegate callback, object owner)
        {
            if (callback == null) return;

            // 添加到事件字典
            Events[eventType] = Events.TryGetValue(eventType, out var del)
                ? Delegate.Combine(del, callback)
                : callback;

            if (owner == null) return;

            // 记录owner关联
            if (!OwnerEvents.TryGetValue(owner, out var list))
            {
                list = new List<(string, Delegate)>();
                OwnerEvents[owner] = list;
            }

            list.Add((eventType, callback));
        }

        private static void RemoveListenerInternal(string eventType, Delegate callback)
        {
            if (callback == null) return;

            if (Events.TryGetValue(eventType, out var del))
            {
                var newDel = Delegate.Remove(del, callback);
                if (newDel == null)
                {
                    Events.Remove(eventType);
                }
                else
                {
                    Events[eventType] = newDel;
                }
            }
        }

        private static void LogError(string eventType, Exception e)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        UnityEngine.Debug.LogError($"[EventCenter] Broadcast '{eventType}' error: {e}");
#endif
        }

        #endregion

        #region 工具方法

        /// <summary>
        /// 移除指定对象的所有事件监听
        /// </summary>
        public static void RemoveListeners(object owner)
        {
            if (owner == null) return;

            if (OwnerEvents.TryGetValue(owner, out var list))
            {
                foreach (var (eventType, callback) in list)
                {
                    RemoveListenerInternal(eventType, callback);
                }

                OwnerEvents.Remove(owner);
            }
        }

        /// <summary>
        /// 移除指定事件的所有监听
        /// </summary>
        public static void RemoveListeners(string eventType)
        {
            Events.Remove(eventType);
        }

        /// <summary>
        /// 检查事件是否有监听者
        /// </summary>
        public static bool HasListener(string eventType)
        {
            return Events.ContainsKey(eventType);
        }

        /// <summary>
        /// 清除所有事件监听
        /// </summary>
        public static void ClearAll()
        {
            Events.Clear();
            OwnerEvents.Clear();
        }

        #endregion
    }
}