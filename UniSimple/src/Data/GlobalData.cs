using System;
using System.Collections.Generic;
using System.Text;

namespace UniSimple.Data
{
    internal interface IContainer
    {
    }

    internal class Container<T> : IContainer
    {
        public T Value;

        public Container(T v)
        {
            Value = v;
        }
    }

    /// <summary>
    /// 全局数据存储
    /// </summary>
    public static class GlobalData
    {
        private static readonly Dictionary<Type, IContainer> Store = new();

        // 泛型存储
        public static void Set<T>(T value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var type = typeof(T);
            if (Store.TryGetValue(type, out var container))
            {
                ((Container<T>)container).Value = value;
            }
            else
            {
                Store[type] = new Container<T>(value);
            }
        }

        // 泛型读取
        public static T Get<T>()
        {
            var type = typeof(T);
            if (Store.TryGetValue(type, out var container))
            {
                return ((Container<T>)container).Value;
            }

            throw new KeyNotFoundException($"Type {type.FullName} not found");
        }

        // 尝试读取
        public static bool TryGet<T>(out T value)
        {
            if (Store.TryGetValue(typeof(T), out var container))
            {
                value = ((Container<T>)container).Value;
                return true;
            }

            value = default;
            return false;
        }

        public static T GetOrNew<T>() where T : new()
        {
            var type = typeof(T);
            if (Store.TryGetValue(type, out var container))
            {
                return ((Container<T>)container).Value;
            }

            var val = new T();
            Store[type] = new Container<T>(val);
            return val;
        }

        public static bool Remove<T>()
        {
            return Store.Remove(typeof(T));
        }

        public static void Clear()
        {
            Store.Clear();
        }

        public static string DebugInfo()
        {
            var builder = new StringBuilder();
            builder.Append($"Stored Count: {Store.Count} | Types:");
            foreach (var kvp in Store)
            {
                builder.Append($" [{kvp.Key.Name}]");
            }

            return builder.ToString();
        }
    }
}