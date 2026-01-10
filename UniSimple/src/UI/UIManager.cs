using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UniSimple.UI
{
    public partial class UIManager
    {
        private readonly UIAssetProvider _assetProvider;
        private readonly UILayerController _layerController;
        private readonly UIModalController _modalController;

        private readonly Dictionary<Type, UIWindow> _openedWindows = new();
        private readonly Dictionary<Type, UIWindow> _cachedWindows = new();
        private readonly Dictionary<Type, UniTask<GameObject>> _loadingTasks = new();
        private readonly Dictionary<Type, CancellationTokenSource> _cancelTokenSource = new();
        private readonly List<UIWindow> _stack = new();

        public UIManager(IAssetLoader loader, GameObject root, GameObject mask)
        {
            if (loader == null || Root == null || Mask == null)
                throw new InvalidOperationException();

            Root = root;
            Mask = mask;

            _assetProvider = new UIAssetProvider(loader);
            _layerController = new UILayerController(root);
            _modalController = new UIModalController(root, mask);
        }

        #region Window

        public async UniTask OpenAsync<T>(UIParam param = null) where T : UIWindow, new()
        {
            UIWindow window;
            var type = typeof(T);

            if (_openedWindows.TryGetValue(type, out var opened))
            {
                opened.Visible = true;
                opened.OnOpen(param);
                opened.OnRefresh();
                _layerController.BringToFront(opened);
                _modalController.ShowMask(opened);
                _modalController.ClickMaskAction = Close<T>;
                return;
            }

            if (_cachedWindows.Remove(type, out var cached))
            {
                window = cached;
            }
            else if (_loadingTasks.TryGetValue(type, out var loadingTask))
            {
                await loadingTask;
                return;
            }
            else
            {
                window = new T();
                var cts = new CancellationTokenSource();
                var task = _assetProvider.LoadAssetAsync(window.WindowSetting.AssetPath).Preserve();
                try
                {
                    _cancelTokenSource[type] = cts;
                    _loadingTasks[type] = task;

                    var prefab = await task;
                    if (cts.IsCancellationRequested)
                        throw new OperationCanceledException();

                    window.GameObject = Object.Instantiate(prefab);
                    window.CreateInternal();
                }
                catch (OperationCanceledException)
                {
                    Debug.LogWarning($"{type.Name} is canceled.");
                    return;
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                    throw;
                }
                finally
                {
                    cts.Dispose();
                    _cancelTokenSource.Remove(type);
                    _loadingTasks.Remove(type);
                }
            }

            _openedWindows[type] = window;

            window.Visible = true;
            window.OnOpen(param);
            window.OnRefresh();
            _layerController.BringToFront(window);

            if (window.IsModal)
            {
                _modalController.ShowMask(window);
                _modalController.ClickMaskAction = Close<T>;
            }

            if (window.IsStack)
            {
                if (!_stack.Contains(window))
                {
                    _stack.Add(window);
                }
            }

            try
            {
                window.DoOpenAnimation();
            }
            catch (Exception e)
            {
                window.OpenImmediate();
                Debug.LogError($"Open animation error: {e}");
            }
        }

        public void Close<T>() where T : UIWindow
        {
            Close(typeof(T));
        }

        public void Close(Type type)
        {
            if (_cancelTokenSource.TryGetValue(type, out var cts))
            {
                cts.Cancel();
                return;
            }

            if (!_openedWindows.Remove(type, out var opened))
            {
                return;
            }

            opened.CloseAction = () =>
            {
                opened.Visible = false;
                opened.OnClose();
                _layerController.RemoveFromLayer(opened);

                if (opened.IsModal)
                {
                    _modalController.HideMask(opened);
                    _modalController.ClickMaskAction = null;
                }

                if (opened.IsStack)
                    _stack.Remove(opened);

                if (opened.DestroyOnClose)
                {
                    _assetProvider.UnloadAsset(opened.WindowSetting.AssetPath);
                    opened.DestroyInternal();
                }
                else
                {
                    _cachedWindows[type] = opened;
                }
            };

            try
            {
                opened.DoCloseAnimation();
            }
            catch (Exception e)
            {
                opened.CloseImmediate();
                Debug.LogError($"Close animation error: {e}");
            }
        }

        public bool Back()
        {
            if (_stack.Count > 0)
            {
                var top = _stack[^1];
                Close(top.GetType());
                return true;
            }

            return false;
        }

        public bool BackTo<T>() where T : UIWindow
        {
            var type = typeof(T);
            var index = -1;
            for (var i = 0; i < _stack.Count; i++)
            {
                if (_stack[i].GetType() == type)
                {
                    index = i;
                    break;
                }
            }

            if (index > -1)
            {
                for (var i = _stack.Count - 1; i > index; i--)
                {
                    var window = _stack[i];
                    Close(window.GetType());
                }

                return true;
            }

            return false;
        }

        #endregion
    }
}