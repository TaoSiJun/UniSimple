using System;
using Cysharp.Threading.Tasks;
using YooAsset;

namespace UniSimple.YooAsset
{
    /// <summary>
    /// YooAsset Handle扩展方法
    /// </summary>
    public static class YooAssetHandleExtensions
    {
        public static async UniTask ToUniTask(this AssetHandle handle)
        {
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status == EOperationStatus.Failed)
            {
                throw new Exception($"Asset load failed: {handle.LastError}");
            }
        }

        public static async UniTask ToUniTask(this SceneHandle handle)
        {
            while (!handle.IsDone)
            {
                await UniTask.Yield();
            }

            if (handle.Status == EOperationStatus.Failed)
            {
                throw new Exception($"Scene load failed: {handle.LastError}");
            }
        }
    }
}