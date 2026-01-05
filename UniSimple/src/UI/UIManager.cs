using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.UI
{
    public class UIManager
    {
        // 负责加载/实例化/销毁
        private readonly UIAssetLoader _assetLoader;

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

        private readonly GameObject _uiRoot;

        // 打开事件
        public event Action<Type> OnOpened;

        // 关闭事件
        public event Action<Type> OnClosed;

        public UIManager(GameObject uiRoot, GameObject mask)
        {
            _assetLoader = new UIAssetLoader();
            _layerController = new UILayerController();
            _modalController = new UIModalController(uiRoot, mask);
            _navigation = new UINavigation();
            _windowCache = new UIWindowCache();
            _uiRoot = uiRoot;
        }

        /// <summary>
        /// 预加载
        /// </summary>
        public async UniTask PreloadAsync<T>(int count = 1) where T : UIBase, new()
        {
            var instances = new List<T>(count);

            for (var i = 0; i < count; i++)
            {
                var instance = await _assetLoader.GetOrCreateAsync<T>();
                if (instance != null)
                {
                    instances.Add(instance);
                }
            }

            // 预加载完成后放入池中
            foreach (var instance in instances)
            {
                _assetLoader.Recycle(instance);
            }
        }

        #region Window

        /// <summary>
        /// 打开一个窗口
        /// </summary>
        public async UniTask<T> OpenAsync<T>(UIParam param = null) where T : UIWindow, new()
        {
            var type = typeof(T);
            T window;

            if (_windowCache.Opened.TryGetValue(type, out var opened))
            {
                window = (T)opened;
            }

            if (_windowCache.Cached.Remove(type, out var cached))
            {
                window = (T)cached;
            }
            else
            {
                window = await _assetLoader.GetOrCreateAsync<T>();
                if (window == null)
                {
                    throw new Exception($"Open error: {type.Name} is null");
                }

                window.Transform.SetParent(_uiRoot.transform, false);
                window.CloseAction = () => CloseAsync<T>().Forget();

                if (window is IUpdatable updatable)
                {
                    _updateList.Add(updatable);
                }
            }

            if (window.State == UIState.Opening)
            {
                await UniTask.WaitUntil(() => window.State == UIState.Opened);
                _layerController.MoveToTop(window);
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
            }
            catch (Exception e)
            {
                Debug.Log($"{type.Name} open animation error: {e}");
            }

            window.Interactable = true;
            window.State = UIState.Opened;

            _navigation.Push(window); // 处理堆栈
            _windowCache.Opened.TryAdd(type, window); // 缓存打开的

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
            if (_assetLoader.TryCancelLoading(type))
            {
                return;
            }

            if (_windowCache.Opened.Remove(type, out var window))
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
                    _assetLoader.Destroy(window);
                }
                else
                {
                    _windowCache.Cached.TryAdd(type, window); // 缓存关闭的
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

        /// <summary>
        /// 获取一个窗口
        /// </summary>
        public T Get<T>() where T : UIWindow
        {
            if (_windowCache.Opened.TryGetValue(typeof(T), out var window))
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

        #endregion

        #region Widget

        public async UniTask<T> CreateWidgetAsync<T>(GameObject owner, UIParam param = null) where T : UIWidget, new()
        {
            if (owner == null) throw new ArgumentNullException(nameof(owner));

            var widget = await _assetLoader.GetOrCreateAsync<T>();
            widget.Transform.SetParent(owner.transform, false);
            widget.OnOpen(param);
            widget.State = UIState.Opened;

            // 绑定生命周期
            widget.RecycleAction = () => RecycleWidget(widget);
            widget.Bind(owner);
            return widget;
        }

        public void RecycleWidget(UIWidget widget)
        {
            if (widget == null) return;

            widget.InternalRecycle();
            widget.OnClose();
            widget.OnRecycle();
            widget.State = UIState.Closed;

            _assetLoader.Recycle(widget);
        }

        public void DestroyWidget(UIWidget widget)
        {
            _assetLoader.Destroy(widget);
        }

        #endregion
    }
}