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
        private class AssetHandleWrapper
        {
            public AssetHandle Handle { get; set; }
            public int RefCount { get; set; }
            public float LastAccessTime { get; set; }

            public AssetHandleWrapper(AssetHandle handle)
            {
                Handle = handle;
                RefCount = 0;
                LastAccessTime = Time.realtimeSinceStartup;
            }
        }

        // 缓存
        private static readonly Dictionary<string, AssetHandleWrapper> HandleCache = new();

        // ---------- 配置数据 ----------
        private static float _lastLruCheckTime;
        private const int MAX_CACHE_SIZE = 50;
        private const float LRU_CHECK_INTERVAL = 10f;

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
        /// 异步加载资源
        /// </summary>
        public static async UniTask<T> LoadAssetAsync<T>(string assetPath) where T : Object
        {
            CheckLru();

            if (HandleCache.TryGetValue(assetPath, out var wrapper))
            {
                wrapper.LastAccessTime = Time.realtimeSinceStartup;
                wrapper.RefCount++;

                // 如果正在加载，等待结果
                if (!wrapper.Handle.IsDone)
                {
                    await wrapper.Handle.ToUniTask();
                }

                // 检查结果
                if (wrapper.Handle.Status == EOperationStatus.Succeed)
                {
                    return wrapper.Handle.GetAssetObject<T>();
                }

                // 加载失败
                wrapper.RefCount--;
                HandleCache.Remove(assetPath);
                Debug.LogError($"(Cache) Load failed: {assetPath}");
                return null;
            }

            var handle = YooAssets.LoadAssetAsync<T>(assetPath);
            var newWrapper = new AssetHandleWrapper(handle);

            // 立即加入缓存防止重复加载
            HandleCache[assetPath] = newWrapper;
            newWrapper.RefCount++;

            await handle.ToUniTask();

            if (handle.Status == EOperationStatus.Succeed)
            {
                newWrapper.LastAccessTime = Time.realtimeSinceStartup;
                return handle.GetAssetObject<T>();
            }

            handle.Release();
            HandleCache.Remove(assetPath);
            Debug.LogError($"(New) Load failed: {assetPath}");
            return null;
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
                    wrapper.RefCount = 0;
                    Debug.LogWarning($"Over-release detected: {assetPath}");
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
        /// 清除未使用的资源
        /// </summary>
        public static void ClearUnusedAssets()
        {
            var keys = new List<string>(HandleCache.Keys);
            foreach (var key in keys)
            {
                if (HandleCache[key].RefCount <= 0)
                {
                    HandleCache[key].Handle.Release();
                    HandleCache.Remove(key);
                }
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
            if (currentTime - _lastLruCheckTime < LRU_CHECK_INTERVAL) return;
            _lastLruCheckTime = currentTime;

            var candidates = new List<string>();
            foreach (var kvp in HandleCache)
            {
                if (kvp.Value.RefCount <= 0)
                {
                    candidates.Add(kvp.Key);
                }
            }

            // 没有超过上限就不
            if (candidates.Count < MAX_CACHE_SIZE) return;

            // 时间越久远越先删除 (LRU)
            candidates.Sort((keyA, keyB) => HandleCache[keyA].LastAccessTime.CompareTo(HandleCache[keyB].LastAccessTime));

            // 删除溢出的部分
            var removeCount = candidates.Count - MAX_CACHE_SIZE;
            for (var i = 0; i < removeCount; i++)
            {
                var key = candidates[i];
                if (HandleCache.TryGetValue(key, out var wrapper))
                {
                    wrapper.Handle.Release();
                    HandleCache.Remove(key);
                }
            }
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