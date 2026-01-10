using System;
using System.Collections.Generic;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.U2D;
using YooAsset;
using Object = UnityEngine.Object;

namespace UniSimple.YooAsset
{
    public static class YooAssetManager
    {
        /// <summary>
        /// YooAsset AssetHandle 包装
        /// </summary>
        private class AssetHandleWrapper
        {
            public AssetHandle Handle { get; set; }
            public int RefCount { get; set; }
            public float LastAccessTime { get; set; }

            public AssetHandleWrapper(AssetHandle handle)
            {
                Handle = handle;
                RefCount = 1;
                LastAccessTime = Time.realtimeSinceStartup;
            }

            public void UpdateAccessTime()
            {
                LastAccessTime = Time.realtimeSinceStartup;
            }
        }

        // 普通资源
        private static readonly Dictionary<string, AssetHandleWrapper> HandleCache = new();
        private static readonly Dictionary<string, UniTask<AssetHandleWrapper>> LoadingAssetTasks = new();

        // ---------- 配置数据 ----------
        private static float _lastLruCheckTime;
        private const int MAX_CACHE_SIZE = 200;
        private const float LRU_CHECK_INTERVAL = 10f;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        public static async UniTask<T> LoadAssetAsync<T>(string assetPath) where T : Object
        {
            CheckLru();

            // 已缓存
            if (HandleCache.TryGetValue(assetPath, out var wrapper))
            {
                wrapper.RefCount++;
                wrapper.UpdateAccessTime();

                if (wrapper.Handle.IsDone)
                {
                    return wrapper.Handle.GetAssetObject<T>();
                }

                await wrapper.Handle.ToUniTask();
                if (wrapper.Handle.Status == EOperationStatus.Succeed)
                {
                    return wrapper.Handle.GetAssetObject<T>();
                }

                wrapper.RefCount--;
                return null;
            }

            // 正在加载中
            if (LoadingAssetTasks.TryGetValue(assetPath, out var loadingTask))
            {
                var existingWrapper = await loadingTask;
                if (existingWrapper == null)
                {
                    return null;
                }

                existingWrapper.RefCount++;
                existingWrapper.UpdateAccessTime();
                return existingWrapper.Handle.GetAssetObject<T>();
            }

            // 开始新的加载
            var task = InternalLoadAssetAsync<T>(assetPath);
            LoadingAssetTasks[assetPath] = task.Preserve();

            try
            {
                var newWrapper = await task;
                return newWrapper?.Handle.GetAssetObject<T>();
            }
            finally
            {
                LoadingAssetTasks.Remove(assetPath);
            }
        }

        private static async UniTask<AssetHandleWrapper> InternalLoadAssetAsync<T>(string assetPath) where T : Object
        {
            var handle = YooAssets.LoadAssetAsync<T>(assetPath);
            var newWrapper = new AssetHandleWrapper(handle);
            HandleCache[assetPath] = newWrapper;

            await handle.ToUniTask();
            if (handle.Status == EOperationStatus.Succeed)
            {
                return newWrapper;
            }

            HandleCache.Remove(assetPath);
            handle.Release();
            return null;
        }

        /// <summary>
        /// 异步加载场景
        /// </summary>
        public static async UniTask<SceneHandle> LoadSceneAsync(string scenePath, LoadSceneMode loadMode, bool suspendLoad = false)
        {
            const LocalPhysicsMode model = LocalPhysicsMode.None;
            var handle = YooAssets.LoadSceneAsync(scenePath, loadMode, model, suspendLoad);
            await handle.ToUniTask();
            return handle;
        }

        /// <summary>
        /// 异步加载GameObject
        /// </summary>
        public static async UniTask<GameObject> LoadGameObjectAsync(string assetPath)
        {
            return await LoadAssetAsync<GameObject>(assetPath);
        }

        /// <summary>
        /// 异步加载并实例化GameObject
        /// </summary>
        public static async UniTask<GameObject> LoadAndInstantiateAsync(string assetPath, Transform parent = null)
        {
            var prefab = await LoadAssetAsync<GameObject>(assetPath);
            if (prefab != null)
            {
                return parent != null ? Object.Instantiate(prefab, parent) : Object.Instantiate(prefab);
            }

            return null;
        }

        /// <summary>
        /// 异步加载ScriptableObject
        /// </summary>
        public static async UniTask<T> LoadScriptableObjectAsync<T>(string assetPath) where T : ScriptableObject
        {
            return await LoadAssetAsync<T>(assetPath);
        }

        /// <summary>
        /// 异步加载TextAsset
        /// </summary>
        public static async UniTask<TextAsset> LoadTextAssetAsync(string assetPath)
        {
            return await LoadAssetAsync<TextAsset>(assetPath);
        }

        /// <summary>
        /// 异步加载AudioClip
        /// </summary>
        public static async UniTask<AudioClip> LoadAudioClipAsync(string assetPath)
        {
            return await LoadAssetAsync<AudioClip>(assetPath);
        }

        /// <summary>
        /// 异步加载Material
        /// </summary>
        public static async UniTask<Material> LoadMaterialAsync(string assetPath)
        {
            return await LoadAssetAsync<Material>(assetPath);
        }

        /// <summary>
        /// 异步加载图集
        /// </summary>
        public static async UniTask<SpriteAtlas> LoadSpriteAtlasAsync(string assetPath)
        {
            return await LoadAssetAsync<SpriteAtlas>(assetPath);
        }

        /// <summary>
        /// 异步加载Sprite
        /// </summary>
        public static async UniTask<Sprite> LoadSpriteAsync(string assetPath)
        {
            return await LoadAssetAsync<Sprite>(assetPath);
        }

        /// <summary>
        /// 异步加载Texture2D
        /// </summary>
        public static async UniTask<Texture2D> LoadTexture2DAsync(string assetPath)
        {
            return await LoadAssetAsync<Texture2D>(assetPath);
        }

        /// <summary>
        /// 异步加载Texture
        /// </summary>
        public static async UniTask<Texture> LoadTextureAsync(string assetPath)
        {
            return await LoadAssetAsync<Texture>(assetPath);
        }

        /// <summary>
        /// 卸载指定资源
        /// </summary>
        public static void UnloadAsset(string assetPath)
        {
            if (HandleCache.TryGetValue(assetPath, out var wrapper))
            {
                wrapper.RefCount--;

                if (wrapper.RefCount <= 0)
                {
                    wrapper.Handle.Release();
                    HandleCache.Remove(assetPath);
                }
            }
        }

        /// <summary>
        /// 强制卸载指定资源（无视引用计数）
        /// </summary>
        public static void ForceUnloadAsset(string assetPath)
        {
            if (HandleCache.TryGetValue(assetPath, out var wrapper))
            {
                wrapper.Handle.Release();
                HandleCache.Remove(assetPath);
            }
        }

        /// <summary>
        /// 卸载所有未使用的资源
        /// </summary>
        public static void UnloadUnusedAssets()
        {
            var keysToRemove = new List<string>();

            foreach (var kvp in HandleCache)
            {
                if (kvp.Value.RefCount <= 0)
                {
                    kvp.Value.Handle.Release();
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                HandleCache.Remove(key);
            }
        }

        /// <summary>
        /// 卸载所有资源
        /// </summary>
        public static void UnloadAllAssets()
        {
            foreach (var wrapper in HandleCache.Values)
            {
                wrapper.Handle.Release();
            }

            HandleCache.Clear();
        }

        /// <summary>
        /// 检查并执行LRU清理
        /// </summary>
        private static void CheckLru()
        {
            var currentTime = Time.realtimeSinceStartup;

            // 首次初始化
            if (_lastLruCheckTime < 0)
            {
                _lastLruCheckTime = currentTime;
                return;
            }

            if (currentTime - _lastLruCheckTime < LRU_CHECK_INTERVAL)
            {
                return;
            }

            _lastLruCheckTime = currentTime;

            // 如果缓存未超过最大值，不执行LRU
            if (HandleCache.Count <= MAX_CACHE_SIZE)
            {
                return;
            }

            // 收集可以清理的资源
            var candidates = new List<KeyValuePair<string, AssetHandleWrapper>>();
            foreach (var kvp in HandleCache)
            {
                if (kvp.Value.RefCount <= 0)
                {
                    candidates.Add(kvp);
                }
            }

            // 按最后访问时间排序
            candidates.Sort((a, b) => a.Value.LastAccessTime.CompareTo(b.Value.LastAccessTime));

            // 清理最久未使用的资源
            var removeCount = Math.Min(candidates.Count, HandleCache.Count - MAX_CACHE_SIZE + 10); // 多清理10个，留出空间
            for (var i = 0; i < removeCount; i++)
            {
                var kvp = candidates[i];
                kvp.Value.Handle.Release();
                HandleCache.Remove(kvp.Key);
            }
        }

        public static void TryClear()
        {
            _lastLruCheckTime = -1f; // 强制下次检查执行清理
            CheckLru();
        }

        /// <summary>
        /// 检查资源是否已加载
        /// </summary>
        public static bool IsAssetLoaded(string assetPath)
        {
            return HandleCache.TryGetValue(assetPath, out var wrapper) && wrapper.Handle.IsDone;
        }

        /// <summary>
        /// 获取资源引用计数
        /// </summary>
        public static int GetAssetRefCount(string assetPath)
        {
            if (HandleCache.TryGetValue(assetPath, out var wrapper))
            {
                return wrapper.RefCount;
            }

            return 0;
        }

        public static string DebugInfo()
        {
            var sb = new StringBuilder($"YooAsset handle cache: Total={HandleCache.Count} |");
            foreach (var kvp in HandleCache)
            {
                sb.Append($" {kvp.Key}={kvp.Value.RefCount}");
            }

            return sb.ToString();
        }
    }
}