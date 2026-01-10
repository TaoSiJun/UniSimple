using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.UI
{
    public interface IAssetLoader
    {
        /// <summary>
        /// 返回 Handle
        /// </summary>
        UniTask<object> LoadAsync(string path);

        /// <summary>
        /// 通过 Handle 返回资源对象
        /// </summary>
        GameObject GetAssetObject(object handle);

        /// <summary>
        /// 通过 Handle 卸载
        /// </summary>
        /// <param name="handle"></param>
        void Unload(object handle);
    }

    /// <summary>
    /// UI 资源管理
    /// </summary>
    internal class UIAssetProvider
    {
        private class AssetCacheInfo
        {
            public object Handle { set; get; }

            public int RefCount { set; get; }

            public float AccessTime { set; get; }
        }

        // 资源加载器
        private readonly IAssetLoader _assetLoader;

        // 缓存信息
        private readonly Dictionary<string, AssetCacheInfo> _assetCacheInfo = new();

        // 加载中
        private readonly Dictionary<string, UniTask> _loadingTasks = new();

        public UIAssetProvider(IAssetLoader loader)
        {
            _assetLoader = loader;
        }

        /// <summary>
        /// 预加载
        /// </summary>
        public async UniTask PreloadAsset(string path)
        {
            if (_assetCacheInfo.ContainsKey(path))
            {
                return;
            }

            if (_loadingTasks.TryGetValue(path, out var existingTask))
            {
                await existingTask;
                return;
            }

            try
            {
                var task = LoadAsyncInternal(path).Preserve();
                _loadingTasks[path] = task;
                await task;
            }
            catch (Exception e)
            {
                Debug.LogError($"Preload {path} asset error: {e}");
            }
            finally
            {
                _loadingTasks.Remove(path);
            }
        }

        /// <summary>
        /// 异步加载
        /// </summary>
        public async UniTask<GameObject> LoadAssetAsync(string path)
        {
            if (_assetCacheInfo.TryGetValue(path, out var info))
            {
                return GetAssetObjectInternal(info);
            }

            if (_loadingTasks.TryGetValue(path, out var existingTask))
            {
                await existingTask;
                if (_assetCacheInfo.TryGetValue(path, out info))
                {
                    return GetAssetObjectInternal(info);
                }
            }

            var task = LoadAsyncInternal(path).Preserve();
            _loadingTasks[path] = task;

            try
            {
                await task;
                if (_assetCacheInfo.TryGetValue(path, out info))
                {
                    return GetAssetObjectInternal(info);
                }
            }
            catch (Exception)
            {
                Debug.LogError($"Failed to load asset: {path}");
            }
            finally
            {
                _loadingTasks.Remove(path);
            }

            return null;
        }

        /// <summary>
        /// 卸载
        /// </summary>
        public void UnloadAsset(string path)
        {
            if (_assetCacheInfo.TryGetValue(path, out var info))
            {
                info.RefCount--;

                if (info.RefCount <= 0)
                {
                    _assetLoader.Unload(info.Handle);
                    _assetCacheInfo.Remove(path);
                }
            }
        }

        private async UniTask LoadAsyncInternal(string path)
        {
            var handle = await _assetLoader.LoadAsync(path);
            if (handle == null)
            {
                throw new Exception($"Failed to load asset: {_assetLoader.GetType()}");
            }

            var asset = _assetLoader.GetAssetObject(handle);
            if (asset == null)
            {
                _assetLoader.Unload(handle);
                throw new Exception($"Can't get the asset object from handle: {_assetLoader.GetType()}");
            }

            var newInfo = new AssetCacheInfo()
            {
                Handle = handle
            };

            if (!_assetCacheInfo.TryAdd(path, newInfo))
            {
                // 如果存在缓存则把新加载的卸载了
                _assetLoader.Unload(handle);
            }
        }

        private GameObject GetAssetObjectInternal(AssetCacheInfo info)
        {
            var obj = _assetLoader.GetAssetObject(info.Handle);
            if (obj != null)
            {
                info.RefCount++;
                info.AccessTime = Time.realtimeSinceStartup;
            }
            else
            {
                Debug.LogError("Handle get asset object error.");
            }

            return obj;
        }
    }
}