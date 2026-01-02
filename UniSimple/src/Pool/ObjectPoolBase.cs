using System.Collections.Generic;

namespace UniSimple.Pool
{
    // 对象池基类
    public abstract class ObjectPoolBase<T> : IObjectPool<T> where T : class
    {
        protected readonly Stack<T> InactiveItems = new Stack<T>();
        protected readonly HashSet<T> ActiveItems = new HashSet<T>();

        private readonly int _limitSize;
        private readonly bool _autoExpand;

        public int TotalCount => ActiveItems.Count + InactiveItems.Count;
        public int ActiveCount => ActiveItems.Count;
        public int InactiveCount => InactiveItems.Count;

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

            if (InactiveItems.Count > 0)
            {
                item = InactiveItems.Pop();
            }
            else if (_autoExpand || TotalCount < _limitSize)
            {
                item = CreateItem();
            }

            if (item != null)
            {
                ActiveItems.Add(item);
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

            if (item is IPoolable poolable)
            {
                poolable.OnDespawn();
            }

            OnReleaseItem(item);
            ActiveItems.Remove(item);

            if (InactiveItems.Count < _limitSize)
            {
                InactiveItems.Push(item);
            }
            else
            {
                DestroyItem(item);
            }
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (TotalCount >= _limitSize)
                    break;

                var item = CreateItem();
                OnReleaseItem(item);
                InactiveItems.Push(item);
            }
        }

        public virtual void Clear()
        {
            foreach (var item in ActiveItems)
            {
                if (item is IPoolable poolable)
                {
                    poolable.OnDestroy();
                }

                DestroyItem(item);
            }

            foreach (var item in InactiveItems)
            {
                if (item is IPoolable poolable)
                {
                    poolable.OnDestroy();
                }

                DestroyItem(item);
            }

            ActiveItems.Clear();
            InactiveItems.Clear();
        }
    }
}