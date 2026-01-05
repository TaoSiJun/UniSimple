using System;
using System.Collections.Generic;

namespace UniSimple.UI
{
    public class UIWindowCache
    {
        private readonly Dictionary<Type, UIWindow> _opened = new(100);
        private readonly Dictionary<Type, UIWindow> _cached = new(100);

        public Dictionary<Type, UIWindow> Opened => _opened;
        public Dictionary<Type, UIWindow> Cached => _cached;

        // ---------- public ----------

        public bool TryGetOpened(Type type, out UIWindow window)
        {
            return InternalTryGet(_opened, type, out window);
        }

        public bool TryAddOpened(Type type, UIWindow window)
        {
            return InternalTryAdd(_opened, type, window);
        }

        public bool TryRemoveOpened(Type type, out UIWindow window)
        {
            return InternalTryRemove(_opened, type, out window);
        }

        public bool TryGetCached(Type type, out UIWindow window)
        {
            return InternalTryGet(_cached, type, out window);
        }

        public bool TryAddCached(Type type, UIWindow window)
        {
            return InternalTryAdd(_cached, type, window);
        }

        public bool TryRemoveCached(Type type, out UIWindow window)
        {
            return InternalTryRemove(_cached, type, out window);
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