using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.UI
{
    public class UIManager
    {
        // 负责加载/实例化/销毁
        private readonly UIAssetLoader _loader;

        // 负责层级/排序
        private readonly UILayerController _layerController;

        // 负责模态/遮罩
        private readonly UIModalController _modalController;

        // 负责堆栈/Back逻辑
        private readonly UINavigation _navigation;

        // 负责已打开/缓存列表的管理
        private readonly UIWindowCache _windowCache;

        // 需要更新的列表
        private readonly List<IUpdatable> _updateList = new(100);

        // 打开事件
        public event Action<Type> OnOpened;

        // 关闭事件
        public event Action<Type> OnClosed;

        public UIManager(GameObject uiRoot, GameObject mask)
        {
            _loader = new UIAssetLoader();
            _layerController = new UILayerController();
            _modalController = new UIModalController(uiRoot, mask);
            _navigation = new UINavigation();
            _windowCache = new UIWindowCache();
        }

        /// <summary>
        /// 预加载
        /// </summary>
        public async UniTask PreloadAsync<T>() where T : UIWindow, new()
        {
            var type = typeof(T);
            var window = await _loader.GetOrCreateAsync<T>(type);
            window.Visible = false;
            window.Interactable = false;
            _windowCache.TryAddCached(type, window);
        }

        /// <summary>
        /// 打开一个窗口
        /// </summary>
        public async UniTask<T> OpenAsync<T>(UIParam param = null) where T : UIWindow, new()
        {
            var type = typeof(T);
            T window;

            if (_windowCache.TryGetOpened(type, out var opened))
            {
                window = (T)opened;
            }
            else if (_windowCache.TryRemoveCached(type, out var cached))
            {
                window = (T)cached;
            }
            else
            {
                window = await _loader.GetOrCreateAsync<T>(type);
                window.CloseAction = () => CloseAsync<T>().Forget();

                if (window is IUpdatable updatable)
                {
                    _updateList.Add(updatable);
                }
            }

            if (window == null)
            {
                throw new Exception($"Open error: {type.Name} is null");
            }

            if (window.State == UIState.Opened)
            {
                window.OnOpen(param);
                _layerController.AddToLayer(window);
                _modalController.ShowMask(window);
                return window;
            }

            if (window.State == UIState.Opening)
            {
                await UniTask.WaitUntil(() => window.State == UIState.Opened);
                window.OnOpen(param);
                return window;
            }

            window.Visible = true;
            window.Interactable = false;
            window.State = UIState.Opening;
            window.OnOpen(param);

            _layerController.AddToLayer(window); // 处理层级关系
            _modalController.ShowMask(window); // 显示模态的遮罩

            try
            {
                await window.OpenAnimationAsync();
                await UniTask.Yield();
            }
            catch (Exception e)
            {
                Debug.Log($"{type.Name} open animation error: {e}");
            }

            window.Interactable = true;
            window.State = UIState.Opened;

            _navigation.Push(window); // 处理堆栈
            _windowCache.TryAddOpened(type, window); // 缓存打开的

            OnOpened?.Invoke(type);
            return window;
        }

        /// <summary>
        /// 关掉一个窗口
        /// </summary>
        public async UniTask CloseAsync<T>() where T : UIWindow
        {
            await InternalCloseAsync(typeof(T), true);
        }

        public async UniTask CloseAllAsync()
        {
            var toClose = new List<Type>(_windowCache.Opened.Keys);
            foreach (var type in toClose)
            {
                try
                {
                    await InternalCloseAsync(type, true);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Close error:{e}");
                }
            }
        }

        /// <summary>
        /// 立即关掉一个窗口 无动画
        /// </summary>
        public void CloseImmediate<T>() where T : UIWindow
        {
            InternalCloseAsync(typeof(T), false).Forget(e => Debug.LogError($"Close error:{e}"));
        }

        public void CloseAllImmediate()
        {
            var toClose = new List<Type>(_windowCache.Opened.Keys);
            foreach (var type in toClose)
            {
                InternalCloseAsync(type, false).Forget(e => Debug.LogError($"Close error:{e}"));
            }
        }

        private async UniTask InternalCloseAsync(Type type, bool playAnimation)
        {
            if (_loader.TryCancelLoading(type))
            {
                return;
            }

            if (_windowCache.TryRemoveOpened(type, out var window))
            {
                if (window is IUpdatable updatable)
                {
                    _updateList.Remove(updatable);
                }

                window.Interactable = false;

                if (playAnimation)
                {
                    window.State = UIState.Closing;
                    try
                    {
                        await window.CloseAnimationAsync();
                        await UniTask.Yield();
                    }
                    catch (Exception e)
                    {
                        Debug.Log($"{type.Name} close animation error: {e}");
                    }
                }

                window.Visible = false;
                window.State = UIState.Closed;
                window.OnClose();

                _navigation.Pop(window);
                _layerController.RemoveFromLayer(window);
                _modalController.HideMask(window);

                OnClosed?.Invoke(type);

                // 处理是否销毁
                if (window.DestroyWhenClose)
                {
                    _loader.Destroy(window);
                    window.InternalDestroy();
                }
                else
                {
                    _windowCache.TryAddCached(type, window); // 缓存关闭的
                }
            }
        }

        public void Update(float deltaTime)
        {
            for (var i = _updateList.Count - 1; i >= 0; i--)
            {
                _updateList[i].OnUpdate(deltaTime);
            }
        }

        /// <summary>
        /// 后退一个窗口
        /// </summary>
        /// <returns>是否成功</returns>
        public bool Back()
        {
            if (_navigation.Stack.Count > 0)
            {
                var top = _navigation.Stack[^1];
                InternalCloseAsync(top.GetType(), false).Forget(Debug.LogError);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 后退到指定的窗口
        /// </summary>
        /// <returns>是否成功</returns>
        public bool BackTo<T>() where T : UIWindow
        {
            var stack = _navigation.Stack;
            if (stack.Count > 0)
            {
                var index = -1;
                for (var i = stack.Count - 1; i > -1; i--)
                {
                    if (stack[i].GetType() == typeof(T))
                    {
                        index = i;
                        break;
                    }
                }

                if (index == -1)
                {
                    return false;
                }

                var toClose = new List<UIWindow>(stack);
                for (var i = index + 1; i < toClose.Count; i++)
                {
                    InternalCloseAsync(toClose[i].GetType(), false).Forget(e => Debug.LogError($"Back to error:{e}"));
                }

                return true;
            }

            return false;
        }

        public T Get<T>() where T : UIWindow
        {
            if (_windowCache.TryGetOpened(typeof(T), out var window))
            {
                return (T)window;
            }

            return null;
        }

        public UIWindow GetTop()
        {
            UIWindow top = null;
            var order = -1;
            foreach (var opened in _windowCache.Opened.Values)
            {
                if (opened.Canvas.sortingOrder > order)
                {
                    top = opened;
                    order = opened.Canvas.sortingOrder;
                }
            }

            return top;
        }
    }
}