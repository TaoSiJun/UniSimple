using System;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.Data
{
    public interface IBinarySerializer
    {
        /// <summary>
        /// 序列化接口
        /// </summary>
        byte[] Serialize<T>(T obj);

        /// <summary>
        /// 反序列化
        /// </summary>
        T Deserialize<T>(byte[] bytes);
    }

    /// <summary>
    /// 存档管理器
    /// </summary>
    public static class SaveDataManager
    {
        private static IBinarySerializer _serializer;
        private static string _saveRootPath;

        public static void Initialize(IBinarySerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));

            _saveRootPath = Path.Combine(Application.persistentDataPath, "SaveData");

            if (!Directory.Exists(_saveRootPath))
            {
                Directory.CreateDirectory(_saveRootPath);
            }
        }

        public static void Save<T>(string fileName, T data)
        {
            EnsureInitialized();

            try
            {
                var fullPath = GetFullPath(fileName);
                var bytes = _serializer.Serialize(data);

                // 防止写入中途崩溃导致坏档
                var tempPath = fullPath + ".temp";
                File.WriteAllBytes(tempPath, bytes);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                // 把临时数据移过去
                File.Move(tempPath, fullPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataManager] Save Failed: {fileName}. Error: {e}");
            }
        }

        public static T Load<T>(string fileName)
        {
            EnsureInitialized();

            var fullPath = GetFullPath(fileName);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[SaveDataManager] File not found: {fileName}");
                return default;
            }

            try
            {
                var bytes = File.ReadAllBytes(fullPath);
                return _serializer.Deserialize<T>(bytes);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataManager] Load Failed: {fileName}. Error: {e}");
                return default;
            }
        }

        public static async UniTask SaveAsync<T>(string fileName, T data)
        {
            EnsureInitialized();

            try
            {
                var fullPath = GetFullPath(fileName);

                // 放入线程处理池 防止主线程卡顿
                var bytes = await UniTask.RunOnThreadPool(() => _serializer.Serialize(data));

                // 防止写入中途崩溃导致坏档
                var tempPath = fullPath + ".temp";

                // 异步写入文件
                await File.WriteAllBytesAsync(tempPath, bytes);

                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                }

                File.Move(tempPath, fullPath);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataManager] SaveAsync Failed: {fileName}. Error: {e}");
            }
        }

        public static async UniTask<T> LoadAsync<T>(string fileName)
        {
            EnsureInitialized();

            var fullPath = GetFullPath(fileName);
            if (!File.Exists(fullPath))
            {
                return default;
            }

            try
            {
                var bytes = await File.ReadAllBytesAsync(fullPath);

                // 放到线程池反序列化
                return await UniTask.RunOnThreadPool(() => _serializer.Deserialize<T>(bytes));
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveDataManager] LoadAsync Failed: {fileName}. Error: {e}");
                return default;
            }
        }

        #region 辅助方法

        private static void EnsureInitialized()
        {
            if (_serializer == null || string.IsNullOrEmpty(_saveRootPath))
                throw new Exception("[SaveDataManager] Serializer not initialized!");
        }

        private static string GetFullPath(string fileName)
        {
            return Path.Combine(_saveRootPath, fileName + ".bin");
        }

        public static bool IsFileExist(string fileName)
        {
            return File.Exists(GetFullPath(fileName));
        }

        public static void DeleteFile(string fileName)
        {
            var path = GetFullPath(fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        #endregion
    }
}