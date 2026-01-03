using System;
using System.Collections.Generic;
using UnityEngine;

namespace UniSimple
{
    public class BindableProperty<T> : IDisposable
    {
        private T _value;
        private readonly List<Action<T>> _callbacks = new();
        private readonly List<Action<T, T>> _changeCallbacks = new();

        // 当前值
        public T Value
        {
            get => _value;
            set
            {
                if (EqualityComparer<T>.Default.Equals(_value, value))
                    return;

                var oldValue = _value;
                _value = value;
                NotifyValueChanged(oldValue, value);
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="initialValue">初始值</param>
        public BindableProperty(T initialValue = default)
        {
            _value = initialValue;
        }

        public void SetValue(T value, bool isNotify = false)
        {
            if (isNotify)
            {
                var oldValue = _value;
                _value = value;
                NotifyValueChanged(oldValue, value);
            }
            else
            {
                _value = value;
            }
        }

        public IDisposable Bind(Action<T> callback, bool callImmediately = false)
        {
            if (callback == null) return null;

            _callbacks.Add(callback);

            if (callImmediately)
                callback.Invoke(_value); // 立即触发一次，传递当前值

            return new BindingDisposer(() => Unbind(callback));
        }

        public void Unbind(Action<T> callback)
        {
            _callbacks.Remove(callback);
        }

        public IDisposable BindChange(Action<T, T> callback, bool callImmediately = false)
        {
            if (callback == null) return null;

            _changeCallbacks.Add(callback);

            if (callImmediately)
                callback.Invoke(_value, _value); // 立即触发一次，传递当前值

            return new BindingDisposer(() => UnbindChange(callback));
        }

        public void UnbindChange(Action<T, T> callback)
        {
            _changeCallbacks.Remove(callback);
        }

        public void UnbindAll()
        {
            _callbacks.Clear();
            _changeCallbacks.Clear();
        }

        private void NotifyValueChanged(T oldValue, T newValue)
        {
            if (_callbacks.Count > 0)
            {
                var callbacks = _callbacks.ToArray();
                foreach (var callback in callbacks)
                {
                    try
                    {
                        callback?.Invoke(newValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[BindableProperty] Callback error: {e}");
                    }
                }
            }

            if (_changeCallbacks.Count > 0)
            {
                var changeCallbacks = _changeCallbacks.ToArray();
                foreach (var callback in changeCallbacks)
                {
                    try
                    {
                        callback?.Invoke(oldValue, newValue);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"[BindableProperty] Change callback error: {e}");
                    }
                }
            }
        }

        /// <summary>
        /// 强制触发一次通知
        /// </summary>
        public void ForceNotify()
        {
            NotifyValueChanged(_value, _value);
        }

        // 重写 ToString 方法
        public override string ToString()
        {
            return _value?.ToString() ?? "null";
        }

        /// <summary>
        /// 隐式转换为 T
        /// </summary>
        public static implicit operator T(BindableProperty<T> property) => property.Value;

        public void Dispose()
        {
            UnbindAll();
        }

        /// <summary>
        /// 绑定释放器
        /// </summary>
        private class BindingDisposer : IDisposable
        {
            private Action _disposeAction;

            public BindingDisposer(Action disposeAction)
            {
                _disposeAction = disposeAction;
            }

            public void Dispose()
            {
                _disposeAction?.Invoke();
                _disposeAction = null;
            }
        }
    }
}