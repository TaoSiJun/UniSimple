using UnityEngine;
using YooAsset;

namespace UniSimple.YooAsset
{
    public struct PackageConfig
    {
        public EPlayMode PlayMode;
        public string PackageName;
        public string DefaultHostServer;
        public string FallbackHostServer;
        public string Platform;
        public string Version; // APP版本

        public static string GetPlatformName()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.IPhonePlayer:
                    return "iOS";
                case RuntimePlatform.Android:
                    return "Android";
                case RuntimePlatform.WebGLPlayer:
                    return "WebGL";
                default:
                    return "StandaloneWindows64";
            }
        }

        // http://127.0.0.1/CDN/Android/DefaultPackage/v1.0/
        public string GetHostServerURL()
        {
            return $"{DefaultHostServer}/{Platform}/{PackageName}/{Version}";
        }

        // http://127.0.0.1/CDN/Android/DefaultPackage/v1.0/
        public string GetFallbackServerURL()
        {
            return $"{FallbackHostServer}/{Platform}/{PackageName}/{Version}";
        }
    }
}