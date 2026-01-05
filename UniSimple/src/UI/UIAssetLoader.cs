using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace UniSimple.UI
{
    internal sealed class UIAssetLoader
    {
        private readonly Dictionary<UIBase, AssetHandle> _handle = new();
        private readonly Dictionary<Type, UniTask<UIBase>> _loading = new();
        private readonly Dictionary<Type, CancellationTokenSource> _cancelTokenSource = new();
        private readonly Dictionary<Type, Stack<UIBase>> _pool = new();

        private static GameObject _root;

        public async UniTask<T> GetOrCreateAsync<T>() where T : UIBase, new()
        {
            var type = typeof(T);

            // 先从池中获取
            var newUI = InternalGet(type);
            if (newUI != null)
            {
                newUI.GameObject.SetActive(true);
                return (T)newUI;
            }

            // 检查并发加载
            if (_loading.TryGetValue(type, out var loading))
            {
                return await loading as T;
            }

            // 创建加载任务
            var cts = new CancellationTokenSource();
            _cancelTokenSource[type] = cts;

            var task = InternalCreateAsync<T>(cts.Token);
            _loading[type] = task;

            try
            {
                newUI = await task as T;
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

            return (T)newUI;
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

        public void Recycle(UIBase ui)
        {
            if (ui == null || ui.GameObject == null) return;

            var type = ui.GetType();
            if (!_pool.TryGetValue(type, out var stack))
            {
                stack = new Stack<UIBase>(2000);
                _pool[type] = stack;
            }

            if (stack.Count < 2000)
            {
                stack.Push(ui);
                ui.GameObject.SetActive(false);
                ui.Transform.SetParent(_root.transform, false); // 归还到回收节点
            }
            else
            {
                // 池子满了直接销毁
                Destroy(ui);
            }
        }

        public void Destroy(UIBase ui)
        {
            if (ui == null) return;

            if (_handle.Remove(ui, out var handle))
            {
                handle.Release();
            }

            ui.InternalDestroy();
            ui.OnDestroy();

            if (ui.GameObject != null)
            {
                Object.Destroy(ui.GameObject);
            }
        }

        // ---------- Internal ----------

        private UIBase InternalGet(Type type)
        {
            if (_pool.TryGetValue(type, out var stack))
            {
                if (stack.Count > 0)
                {
                    return stack.Pop();
                }
            }

            return null;
        }

        private async UniTask<UIBase> InternalCreateAsync<T>(CancellationToken token) where T : UIBase, new()
        {
            InternalCreateRoot();

            var ui = new T()
            {
                State = UIState.Loading
            };
            var handle = YooAssets.LoadAssetAsync<GameObject>(ui.Setting.Address);
            await handle.ToUniTask(cancellationToken: token);
            if (handle.Status != EOperationStatus.Succeed)
            {
                handle.Release();
                throw new Exception($"Failed to load {ui.Setting.Address}.");
            }

            // 这里只做实例化 GameObject
            ui.GameObject = Object.Instantiate(handle.AssetObject as GameObject);
            ui.InternalCreate();
            ui.OnCreate();

            _handle[ui] = handle;
            return ui;
        }

        private static void InternalCreateRoot()
        {
            if (_root == null)
            {
                _root = new GameObject("UIAssetLoader_Pool");
                Object.DontDestroyOnLoad(_root);
            }
        }
    }
}