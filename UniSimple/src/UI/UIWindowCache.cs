using System;
using System.Collections.Generic;

namespace UniSimple.UI
{
    internal sealed class UIWindowCache
    {
        public Dictionary<Type, UIWindow> Opened { get; } = new(100);
        public Dictionary<Type, UIWindow> Cached { get; } = new(100);

        // ---------- public ----------

        public bool TryGetOpened(Type type, out UIWindow window)
        {
            return InternalTryGet(Opened, type, out window);
        }

        public bool TryAddOpened(Type type, UIWindow window)
        {
            return InternalTryAdd(Opened, type, window);
        }

        public bool TryRemoveOpened(Type type, out UIWindow window)
        {
            return InternalTryRemove(Opened, type, out window);
        }

        public bool TryGetCached(Type type, out UIWindow window)
        {
            return InternalTryGet(Cached, type, out window);
        }

        public bool TryAddCached(Type type, UIWindow window)
        {
            return InternalTryAdd(Cached, type, window);
        }

        public bool TryRemoveCached(Type type, out UIWindow window)
        {
            return InternalTryRemove(Cached, type, out window);
        }

        // ---------- Internal ----------

        private static bool InternalTryAdd(IDictionary<Type, UIWindow> dictionary, Type type, UIWindow window)
        {
            return dictionary.TryAdd(type, window);
        }

        private static bool InternalTryGet(IDictionary<Type, UIWindow> dictionary, Type type, out UIWindow window)
        {
            if (dictionary.TryGetValue(type, out var got))
            {
                window = got;
                return true;
            }

            window = null;
            return false;
        }

        private static bool InternalTryRemove(IDictionary<Type, UIWindow> dictionary, Type type, out UIWindow window)
        {
            if (dictionary.Remove(type, out var removed))
            {
                window = removed;
                return true;
            }

            window = null;
            return false;
        }
    }
}