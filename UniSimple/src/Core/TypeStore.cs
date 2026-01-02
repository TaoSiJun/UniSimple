using System;
using System.Collections.Generic;
using System.Text;

namespace UniSimple.Core
{
    internal interface IValueContainer
    {
    }

    internal class ValueContainer<T> : IValueContainer
    {
        public T Value { get; private set; }

        public ValueContainer(T v)
        {
            Value = v;
        }

        public void Set(T v)
        {
            Value = v;
        }
    }

    /// <summary>
    /// 类型存储
    /// </summary>
    public class TypeStore
    {
        private readonly Dictionary<Type, IValueContainer> _store = new();

        // 泛型存储
        public void Set<T>(T value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var type = typeof(T);
            if (_store.TryGetValue(type, out var existing))
            {
                ((ValueContainer<T>)existing).Set(value);
            }
            else
            {
                _store[type] = new ValueContainer<T>(value);
            }
        }

        // 泛型读取
        public T Get<T>()
        {
            var type = typeof(T);
            if (_store.TryGetValue(type, out var container))
            {
                return ((ValueContainer<T>)container).Value;
            }

            throw new KeyNotFoundException($"Type {type.FullName} not found");
        }

        public bool TryGet<T>(out T value)
        {
            if (_store.TryGetValue(typeof(T), out var container))
            {
                value = ((ValueContainer<T>)container).Value;
                return true;
            }

            value = default;
            return false;
        }

        public bool Has<T>()
        {
            return _store.ContainsKey(typeof(T));
        }

        public void Remove<T>()
        {
            _store.Remove(typeof(T));
        }

        public void Clear()
        {
            _store.Clear();
        }

        public string DebugInfo()
        {
            var builder = new StringBuilder();
            builder.Append($"Stored Count: {_store.Count} | Types:");
            foreach (var kvp in _store)
            {
                builder.Append($" [{kvp.Key.Name}]");
            }

            return builder.ToString();
        }
    }
}