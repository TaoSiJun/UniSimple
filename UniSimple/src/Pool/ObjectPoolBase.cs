using System.Collections.Generic;

namespace UniSimple.Pool
{
    // 对象池接口
    public interface IObjectPool<T> where T : class
    {
        T Get();
        void Release(T item);
        void Prewarm(int count);
        int TotalCount { get; }
        int ActiveCount { get; }
        int InactiveCount { get; }
        void Clear();
    }

    // 对象池接口 非泛型
    public interface IObjectPool
    {
        void Clear();
    }

    // 对象池基类
    public abstract class ObjectPoolBase<T> : IObjectPool<T>, IObjectPool where T : class
    {
        private readonly Stack<T> _inactiveItems = new();
        private readonly HashSet<T> _activeItems = new();

        private readonly int _limitSize;
        private readonly bool _autoExpand;

        public int TotalCount => _activeItems.Count + _inactiveItems.Count;
        public int ActiveCount => _activeItems.Count;
        public int InactiveCount => _inactiveItems.Count;

        protected ObjectPoolBase(int limitSize = 100, bool autoExpand = true)
        {
            _limitSize = limitSize;
            _autoExpand = autoExpand;
        }

        protected abstract T CreateItem();
        protected abstract void OnGetItem(T item);
        protected abstract void OnReleaseItem(T item);
        protected abstract void DestroyItem(T item);

        public virtual T Get()
        {
            T item = null;

            if (_inactiveItems.Count > 0)
            {
                item = _inactiveItems.Pop();
            }
            else if (_autoExpand || TotalCount < _limitSize)
            {
                item = CreateItem();
            }

            if (item != null)
            {
                _activeItems.Add(item);
                OnGetItem(item);

                if (item is IPoolable poolable)
                {
                    poolable.OnSpawn();
                }
            }

            return item;
        }

        public virtual void Release(T item)
        {
            if (item == null)
                return;

            if (!_activeItems.Remove(item))
            {
                UnityEngine.Debug.LogWarning($"Release {item} not in pool");
                return;
            }

            if (item is IPoolable poolable1)
            {
                poolable1.OnDespawn();
            }

            OnReleaseItem(item);

            if (_inactiveItems.Count < _limitSize)
            {
                _inactiveItems.Push(item);
            }
            else
            {
                if (item is IPoolable poolable2)
                {
                    poolable2.OnDestroy();
                }

                DestroyItem(item);
            }
        }

        public virtual void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (TotalCount >= _limitSize)
                    break;

                var item = CreateItem();
                if (item == null)
                    continue;

                OnReleaseItem(item);
                _inactiveItems.Push(item);
            }
        }

        public virtual void Clear()
        {
            foreach (var item in _activeItems)
            {
                if (item is IPoolable poolable)
                {
                    poolable.OnDestroy();
                }

                DestroyItem(item);
            }

            foreach (var item in _inactiveItems)
            {
                if (item is IPoolable poolable)
                {
                    poolable.OnDestroy();
                }

                DestroyItem(item);
            }

            _activeItems.Clear();
            _inactiveItems.Clear();
        }
    }
}