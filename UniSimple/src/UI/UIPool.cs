using System.Collections.Generic;
using UnityEngine;

namespace UniSimple.UI
{
    // 缓存关闭的实例
    // 减少重复创建的开销
    // 管理缓存容量
    public class UIPool
    {
        private readonly Dictionary<string, Stack<UIBase>> _pool = new();
        private GameObject _poolRoot;
        private const int LIMIT = 10;

        public UIPool(GameObject root)
        {
            var go = new GameObject("UIPool");
            go.transform.SetParent(root.transform, false);
            go.SetActive(false);
            _poolRoot = go;
        }

        public UIBase Get(string name)
        {
            if (!_pool.TryGetValue(name, out var stack) || stack.Count <= 0)
            {
                return null;
            }

            var top = stack.Pop();
            return top;
        }

        public void Recycle(string name, UIBase ui)
        {
            if (!_pool.TryGetValue(name, out var stack))
            {
                stack = new Stack<UIBase>();
                _pool[name] = stack;
            }

            if (stack.Count < LIMIT)
            {
                stack.Push(ui);
            }
            else
            {
                ui.DestroyInternal();
            }
        }

        public void Remove(string name)
        {
            if (_pool.TryGetValue(name, out var stack))
            {
                while (stack.Count > 0)
                {
                    var ui = stack.Pop();
                    ui.DestroyInternal();
                }
            }
        }

        public void Clear()
        {
            foreach (var kvp in _pool)
            {
                var stack = kvp.Value;
                if (stack.Count <= 0)
                {
                    continue;
                }

                var ui = stack.Pop();
                ui.DestroyInternal();
            }

            _pool.Clear();
        }
    }
}