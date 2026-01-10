using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace UniSimple.YooAsset
{
    /// <summary>
    /// 图集管理器
    /// </summary>
    public static class AtlasManager
    {
        private class AtlasWrapper
        {
            public SpriteAtlas Atlas { set; get; }

            public int RefCount { set; get; }

            public AtlasWrapper(SpriteAtlas atlas)
            {
                Atlas = atlas;
            }

            public Sprite GetSprite(string spriteName)
            {
                return Atlas == null ? null : Atlas.GetSprite(spriteName);
            }
        }

        // 图集缓存
        private static readonly Dictionary<string, AtlasWrapper> AtlasCache = new();

        // 加载中的任务
        private static readonly Dictionary<string, UniTaskCompletionSource<AtlasWrapper>> LoadingUcs = new();

        // 精灵缓存
        private static readonly Dictionary<string, Dictionary<string, Sprite>> SpritesCache = new();

        /// <summary>
        /// 加载单个图集 引用计数+1
        /// </summary>
        public static async UniTask LoadAsync(string atlasName)
        {
            try
            {
                var atlasWrapper = await InternalLoadAtlas(atlasName);
                if (atlasWrapper != null)
                    atlasWrapper.RefCount++;
            }
            catch (OperationCanceledException)
            {
                // Clear 方法取消任务
            }
        }

        /// <summary>
        /// 批量加载图集
        /// </summary>
        public static async UniTask PreloadAsync(params string[] atlasNames)
        {
            var tasks = atlasNames.Select(LoadAsync);
            await UniTask.WhenAll(tasks);
        }

        /// <summary>
        /// 卸载图集 引用计数-1
        /// </summary>
        public static void Unload(string atlasName)
        {
            if (AtlasCache.TryGetValue(atlasName, out var atlasWrapper))
            {
                atlasWrapper.RefCount--;

                if (atlasWrapper.RefCount <= 0)
                {
                    AtlasCache.Remove(atlasName);
                    YooAssetManager.UnloadAsset(atlasName);
                    RemoveSprites(atlasName);
                    Debug.Log($"[AtlasManager] Unloaded: {atlasName}");
                }
            }
        }

        /// <summary>
        /// 移除精灵缓存
        /// </summary>
        public static void RemoveSprites(string atlasName)
        {
            if (SpritesCache.TryGetValue(atlasName, out var cacheDictionary))
            {
                foreach (var sprite in cacheDictionary.Values)
                {
                    if (sprite) Object.Destroy(sprite);
                }

                SpritesCache.Remove(atlasName);
            }
        }

        /// <summary>
        /// 清除所有图集
        /// </summary>
        public static void Clear()
        {
            Debug.Log("[AtlasManager] Clear all cache.");

            var ucsList = LoadingUcs.Values.ToList();
            foreach (var ucs in ucsList)
            {
                // 这会导致 await 的地方抛出 OperationCanceledException
                ucs.TrySetCanceled();
            }

            LoadingUcs.Clear();

            var atlasNames = AtlasCache.Keys.ToList();
            foreach (var atlasName in atlasNames)
            {
                YooAssetManager.UnloadAsset(atlasName);
            }

            AtlasCache.Clear();

            foreach (var dict in SpritesCache.Values)
            {
                foreach (var sprite in dict.Values)
                {
                    if (sprite) Object.Destroy(sprite);
                }
            }

            SpritesCache.Clear();
        }

        /// <summary>
        /// 拓展方法 - Image
        /// </summary>
        /// <param name="image"></param>
        /// <param name="atlasName"></param>
        /// <param name="spriteName"></param>
        public static async UniTaskVoid SetSprite(this Image image, string atlasName, string spriteName)
        {
            await SetSpriteAsync(image, atlasName, spriteName);
        }

        public static async UniTask SetSpriteAsync(Image image, string atlasName, string spriteName, bool setNativeSize = false)
        {
            try
            {
                if (image == null || string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
                    return;

                var sprite = await LoadSpriteAsync(atlasName, spriteName);
                if (image != null && sprite != null)
                {
                    image.sprite = sprite;
                    if (setNativeSize)
                        image.SetNativeSize();
                }
            }
            catch (OperationCanceledException)
            {
                // 忽略取消
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 拓展方法 - SpriteRenderer
        /// </summary>
        /// <param name="spriteRenderer"></param>
        /// <param name="atlasName"></param>
        /// <param name="spriteName"></param>
        public static async UniTaskVoid SetSprite(this SpriteRenderer spriteRenderer, string atlasName, string spriteName)
        {
            await SetSpriteAsync(spriteRenderer, atlasName, spriteName);
        }

        public static async UniTask SetSpriteAsync(SpriteRenderer spriteRenderer, string atlasName, string spriteName)
        {
            try
            {
                if (spriteRenderer == null || string.IsNullOrEmpty(atlasName) || string.IsNullOrEmpty(spriteName))
                    return;

                var sprite = await LoadSpriteAsync(atlasName, spriteName);
                if (spriteRenderer != null && sprite != null)
                    spriteRenderer.sprite = sprite;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        /// <summary>
        /// 加载一个图集和其中的精灵
        /// </summary>
        private static async UniTask<Sprite> LoadSpriteAsync(string atlasName, string spriteName)
        {
            if (SpritesCache.TryGetValue(atlasName, out var cachedDictionary))
            {
                if (cachedDictionary.TryGetValue(spriteName, out var cachedSprite))
                {
                    return cachedSprite;
                }
            }

            var atlasWrapper = await InternalLoadAtlas(atlasName);
            if (atlasWrapper == null)
            {
                Debug.LogError($"[AtlasManager] Failed to load atlas: {atlasName}");
                return null;
            }

            var sprite = atlasWrapper.GetSprite(spriteName);
            if (sprite == null)
            {
                Debug.LogWarning($"[AtlasManager] Sprite '{spriteName}' not found in atlas '{atlasName}'");
                return null;
            }

            var dict = new Dictionary<string, Sprite>
            {
                [spriteName] = sprite
            };
            SpritesCache[atlasName] = dict;
            return sprite;
        }

        private static async UniTask<AtlasWrapper> InternalLoadAtlas(string atlasName)
        {
            if (AtlasCache.TryGetValue(atlasName, out var atlasWrapper))
            {
                return atlasWrapper;
            }

            if (LoadingUcs.TryGetValue(atlasName, out var loading))
            {
                var existing = await loading.Task;
                return existing;
            }

            var ucs = new UniTaskCompletionSource<AtlasWrapper>();
            LoadingUcs.TryAdd(atlasName, ucs);

            try
            {
                var newAtlas = await YooAssetManager.LoadSpriteAtlasAsync(atlasName);

                if (ucs.Task.Status == UniTaskStatus.Canceled)
                {
                    return null;
                }

                if (newAtlas != null)
                {
                    var newWrapper = new AtlasWrapper(newAtlas);
                    AtlasCache[atlasName] = newWrapper;
                    ucs.TrySetResult(newWrapper);
                    return newWrapper;
                }

                AtlasCache.Remove(atlasName);
                ucs.TrySetResult(null);
                return null;
            }
            catch (Exception ex)
            {
                if (ucs.Task.Status != UniTaskStatus.Canceled)
                {
                    ucs.TrySetException(ex);
                }

                throw;
            }
            finally
            {
                LoadingUcs.Remove(atlasName);
            }
        }

        public static string DebugInfo()
        {
            var builder = new StringBuilder();
            builder.Append("Atlas:");
            foreach (var kvp in AtlasCache)
            {
                builder.Append($" {kvp.Key}={kvp.Value.RefCount}");
            }

            builder.Append(" Sprites:");
            foreach (var kvp in SpritesCache)
            {
                builder.Append($" {kvp.Key}=");
                foreach (var name in kvp.Value.Keys)
                {
                    builder.Append($"{name} ");
                }
            }

            builder.Append($" Loading: {LoadingUcs.Count}");
            return builder.ToString();
        }
    }
}