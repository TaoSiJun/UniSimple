using System;

namespace UniSimple.Pool
{
    // C#类对象池
    public class ClassPool<T> : ObjectPoolBase<T> where T : class, new()
    {
        private readonly Func<T> _createFunc;
        private readonly Action<T> _resetAction;

        public ClassPool(
            Func<T> createFunc = null,
            Action<T> resetAction = null,
            int initialSize = 10,
            int limitSize = 100,
            bool autoExpand = true)
            : base(limitSize, autoExpand)
        {
            _createFunc = createFunc ?? (() => new T());
            _resetAction = resetAction;
            Prewarm(initialSize);
        }

        protected override T CreateItem()
        {
            return _createFunc.Invoke();
        }

        protected override void OnGetItem(T item)
        {
            // 获取时不需要特殊处理
        }

        protected override void OnReleaseItem(T item)
        {
            // 调用自定义重置方法
            _resetAction?.Invoke(item);
        }

        protected override void DestroyItem(T item)
        {
            // C#对象由GC处理，这里可以做清理工作
            if (item is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}