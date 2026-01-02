using UnityEngine;

namespace UniSimple
{
    /// <summary>
    /// 使用了 companyName 和 productName 作为前缀
    /// </summary>
    public static class SimplePlayerPrefs
    {
        private static string GetKey(string key)
        {
            return $"{Application.companyName}_{Application.productName}_{key}";
        }

        public static void SetInt(string key, int value)
        {
            PlayerPrefs.SetInt(GetKey(key), value);
        }

        public static void SetFloat(string key, float value)
        {
            PlayerPrefs.SetFloat(GetKey(key), value);
        }

        public static void SetString(string key, string value)
        {
            PlayerPrefs.SetString(GetKey(key), value);
        }

        public static int GetInt(string key, int defaultValue = 0)
        {
            return PlayerPrefs.GetInt(GetKey(key), defaultValue);
        }

        public static float GetFloat(string key, float defaultValue = 0f)
        {
            return PlayerPrefs.GetFloat(GetKey(key), defaultValue);
        }

        public static string GetString(string key, string defaultValue = "")
        {
            return PlayerPrefs.GetString(GetKey(key), defaultValue);
        }

        public static bool HasKey(string key)
        {
            return PlayerPrefs.HasKey(GetKey(key));
        }

        public static void DeleteKey(string key)
        {
            PlayerPrefs.DeleteKey(GetKey(key));
        }

        public static void Save()
        {
            PlayerPrefs.Save();
        }

        public static void DeleteAll()
        {
            PlayerPrefs.DeleteAll();
        }
    }
}