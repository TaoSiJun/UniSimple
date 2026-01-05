using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace UniSimple.UI
{
    public class UIAssetLoader
    {
        private readonly Dictionary<Type, AssetHandle> _handles = new();
        private readonly Dictionary<Type, UniTask<UIWindow>> _loading = new();
        private readonly Dictionary<Type, CancellationTokenSource> _cancelTokenSource = new();
        private readonly Dictionary<string, Stack<GameObject>> _pool = new();

        public async UniTask<T> GetOrCreateAsync<T>(Type type) where T : UIWindow, new()
        {
            // 先检查有没有加载任务
            if (_loading.TryGetValue(type, out var loading))
            {
                return await loading as T;
            }

            T newWindow = null;

            // 创建取消
            var cts = new CancellationTokenSource();
            _cancelTokenSource[type] = cts;

            // 创建加载任务
            var task = InternalCreateAsync<T>(cts.Token);
            _loading[type] = task;

            try
            {
                newWindow = await task as T;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning($"{type.Name} is canceled.");
                return null;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
            finally
            {
                cts.Dispose();
                _cancelTokenSource.Remove(type);
                _loading.Remove(type);
            }

            return newWindow;
        }

        public bool TryCancelLoading(Type type)
        {
            if (_cancelTokenSource.TryGetValue(type, out var cts))
            {
                cts.Cancel();
                return true;
            }

            return false;
        }

        public void Recycle(UIWindow window)
        {
        }

        public void Destroy(UIWindow window)
        {
            var type = window.GetType();
            _handles[type].Release();
            _handles.Remove(type);
            Object.Destroy(window.GameObject);
            window.InternalDestroy();
        }

        private async UniTask<UIWindow> InternalCreateAsync<TWindow>(CancellationToken token) where TWindow : UIWindow, new()
        {
            var window = new TWindow
            {
                State = UIState.Loading
            };

            var attr = window.Setting;
            var handle = YooAssets.LoadAssetAsync<GameObject>(attr.Address);
            await handle.ToUniTask(cancellationToken: token);
            if (handle.Status != EOperationStatus.Succeed)
            {
                handle.Release();
                throw new Exception($"Failed to load {window.Setting.Name}.");
            }

            // 这里只做实例化 GameObject
            window.GameObject = Object.Instantiate(handle.AssetObject as GameObject);
            window.InternalCreate();

            _handles[typeof(TWindow)] = handle;
            return window;
        }
    }
}