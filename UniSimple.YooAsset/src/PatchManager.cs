using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YooAsset;

namespace UniSimple.YooAsset
{
    public static class PatchManager
    {
        // 管理已加载的包
        private static readonly Dictionary<string, ResourcePackage> Packages = new();

        /// <summary>
        /// 初始化资源包
        /// </summary>
        public static IEnumerator InitializePackage(PackageConfig config)
        {
            if (Packages.ContainsKey(config.PackageName))
            {
                Debug.LogWarning($"Package {config.PackageName} is already initialized.");
                yield break;
            }

            if (!YooAssets.Initialized)
            {
                YooAssets.Initialize();
            }

            var package = YooAssets.TryGetPackage(config.PackageName);
            if (package == null)
            {
                package = YooAssets.CreatePackage(config.PackageName);
            }

            // 设置默认包（如果是主包）
            if (config.PackageName == "DefaultPackage")
            {
                YooAssets.SetDefaultPackage(package);
            }

            InitializationOperation operation = null;

            switch (config.PlayMode)
            {
                // 编辑器模拟模式
                case EPlayMode.EditorSimulateMode:
                    var buildResult = EditorSimulateModeHelper.SimulateBuild(config.PackageName);
                    var packageRoot = buildResult.PackageRootDirectory;
                    var editorFileSystem = FileSystemParameters.CreateDefaultEditorFileSystemParameters(packageRoot);
                    var editorParams = new EditorSimulateModeParameters
                    {
                        EditorFileSystemParameters = editorFileSystem
                    };
                    operation = package.InitializeAsync(editorParams);
                    break;

                // 本地单机模式
                case EPlayMode.OfflinePlayMode:
                    var offlineFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    var offlineParams = new OfflinePlayModeParameters
                    {
                        BuildinFileSystemParameters = offlineFileSystem
                    };
                    operation = package.InitializeAsync(offlineParams);
                    break;

                // 远程联机模式
                case EPlayMode.HostPlayMode:
                    var remoteServices = new RemoteServices(config.GetHostServerURL(), config.GetFallbackServerURL());
                    var cacheFileSystem = FileSystemParameters.CreateDefaultCacheFileSystemParameters(remoteServices);
                    var buildinFileSystem = FileSystemParameters.CreateDefaultBuildinFileSystemParameters();
                    var hostParams = new HostPlayModeParameters
                    {
                        BuildinFileSystemParameters = buildinFileSystem,
                        CacheFileSystemParameters = cacheFileSystem
                    };
                    operation = package.InitializeAsync(hostParams);
                    break;
            }

            if (operation == null)
            {
                Debug.LogError($"Initialize Package Operation is null: {config.PackageName}");
                yield break;
            }

            yield return operation;

            if (operation.Status == EOperationStatus.Succeed)
            {
                Packages.TryAdd(config.PackageName, package);
                Debug.Log($"[{config.PackageName}] Initialize Success Mode: {config.PlayMode}");
            }
            else
            {
                Debug.LogError($"[{config.PackageName}] Initialize Failed: {operation.Error}");
            }
        }

        /// <summary>
        /// 检查并更新资源清单
        /// </summary>
        public static IEnumerator UpdatePackageManifest(string packageName)
        {
            var package = GetPackage(packageName);
            if (package == null)
            {
                yield break;
            }

            // 1. 请求版本
            var versionOp = package.RequestPackageVersionAsync();
            yield return versionOp;

            if (versionOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"Request Version Failed: {versionOp.Error}");
                yield break;
            }

            Debug.Log($"Found New Version: {versionOp.PackageVersion}");

            // 2. 更新清单
            var manifestOp = package.UpdatePackageManifestAsync(versionOp.PackageVersion);
            yield return manifestOp;

            if (manifestOp.Status != EOperationStatus.Succeed)
            {
                Debug.LogError($"Update Manifest Failed: {manifestOp.Error}");
                yield break;
            }

            Debug.Log("Manifest Updated.");
        }

        /// <summary>
        /// 创建资源下载器
        /// </summary>
        public static ResourceDownloaderOperation CreateDownloader(string packageName, int downloadingMaxNum = 10, int failedTryAgain = 3)
        {
            var package = GetPackage(packageName);
            var downloader = package?.CreateResourceDownloader(downloadingMaxNum, failedTryAgain);
            return downloader;
        }

        public static ResourceDownloaderOperation CreateDownloader(string packageName, string tag, int downloadingMaxNum = 10, int failedTryAgain = 3)
        {
            var package = GetPackage(packageName);
            var downloader = package?.CreateResourceDownloader(tag, downloadingMaxNum, failedTryAgain);
            return downloader;
        }

        public static ResourcePackage GetPackage(string packageName)
        {
            if (Packages.TryGetValue(packageName, out var package))
                return package;

            Debug.LogError($"Package not found or not initialized: {packageName}");
            return null;
        }

        public static void UnloadPackage(string packageName)
        {
            if (Packages.TryGetValue(packageName, out var package))
            {
                package.DestroyAsync();
                Packages.Remove(packageName);
                YooAssets.RemovePackage(package);
                Debug.Log($"Package {packageName} unloaded.");
            }
        }

        /// <summary>
        /// 远端地址服务
        /// </summary>
        private class RemoteServices : IRemoteServices
        {
            private readonly string _defaultHost;
            private readonly string _fallbackHost;

            public RemoteServices(string defaultHost, string fallbackHost)
            {
                _defaultHost = defaultHost;
                _fallbackHost = fallbackHost;
            }

            public string GetRemoteMainURL(string fileName)
            {
                return $"{_defaultHost}/{fileName}";
            }

            public string GetRemoteFallbackURL(string fileName)
            {
                return $"{_fallbackHost}/{fileName}";
            }
        }
    }
}