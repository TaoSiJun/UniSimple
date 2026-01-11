using System.Collections;
using UnityEngine;
using YooAsset;

namespace UniSimple.YooAsset.Example
{
    public class ExampleYooAsset
    {
        private IEnumerator Start()
        {
            // 1. 定义配置
            var config = new PackageConfig
            {
                PackageName = "DefaultPackage",
                PlayMode = EPlayMode.EditorSimulateMode,
                Platform = PackageConfig.GetPlatformName(),
                DefaultHostServer = "https:/127.0.0.1/CDN",
                FallbackHostServer = "https:127.0.0.1/CDN",
                Version = "v1.0"
            };

            // 2. 初始化
            yield return PatchManager.InitializePackage(config);

            // 如果是单机或模拟模式
            if (config.PlayMode == EPlayMode.EditorSimulateMode || config.PlayMode == EPlayMode.OfflinePlayMode)
            {
                Debug.Log("进入游戏");
                yield break;
            }

            // 3. (仅联机模式) 检查版本和清单
            yield return PatchManager.UpdatePackageManifest("DefaultPackage");

            // 4. (仅联机模式) 创建下载器
            var downloader = PatchManager.CreateDownloader("DefaultPackage");

            // 如果有需要下载的文件
            if (downloader.TotalDownloadCount > 0)
            {
                Debug.Log($"需要下载: {downloader.TotalDownloadBytes / 1024.0f / 1024.0f:f2} MB");

                // 这里可以弹出 UI 让用户确认
                // yield return ShowConfirmUI();

                // 注册回调来驱动 UI 进度条
                downloader.DownloadUpdateCallback = (data) => { Debug.Log($"进度: {data.CurrentDownloadCount}/{data.TotalDownloadCount}"); };

                // 开始下载
                downloader.BeginDownload();
                yield return downloader;

                if (downloader.Status != EOperationStatus.Succeed)
                {
                    Debug.LogError("下载失败，请重试");
                    yield break;
                }
            }

            Debug.Log("更新完成，进入游戏");
        }
    }
}