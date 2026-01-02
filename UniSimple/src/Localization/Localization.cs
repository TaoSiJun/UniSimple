using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace UniSimple.Localization
{
    /// <summary>
    /// 支持的语言
    /// </summary>
    public enum Language
    {
        English,
        Chinese,
    }

    public class Localization
    {
        private const string LANGUAGE_KEY = "Language";
        private Dictionary<string, string> _texts;
        private UniTaskCompletionSource _uniTaskCompletionSource;

        public bool IsInitialized { get; private set; }

        // 当前语言
        public Language CurrentLanguage { get; private set; } = Language.English;

        // 更改语言事件
        public static event Action<Language> OnLanguageChanged;

        // 语言数据
        public Func<Language, UniTask<string>> LanguageLoader { get; set; }

        /// <summary>
        /// 初始化
        /// </summary>
        public async UniTask InitializeAsync()
        {
            if (IsInitialized) return;

            if (_uniTaskCompletionSource != null)
            {
                await _uniTaskCompletionSource.Task;
                return;
            }

            _uniTaskCompletionSource = new UniTaskCompletionSource();

            try
            {
                // 确定语言
                var savedLang = SimplePlayerPrefs.GetInt(LANGUAGE_KEY, -1);
                CurrentLanguage = savedLang > -1 ? (Language)savedLang : GetSystemLanguage();

                // 加载数据
                await ReloadLanguageData(CurrentLanguage);
                IsInitialized = true;
                _uniTaskCompletionSource.TrySetResult();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                _uniTaskCompletionSource.TrySetException(e);
                throw;
            }
        }

        /// <summary>
        /// 重新加载语言数据
        /// </summary>
        private async UniTask ReloadLanguageData(Language lang)
        {
            _texts = new Dictionary<string, string>();

            // 加载当前语言
            var content = await LanguageLoader.Invoke(lang);
            LocalizationUtility.ParseCsv(content, _texts);
        }

        /// <summary>
        /// 获取本地化文本
        /// </summary>
        public string Get(string key)
        {
            if (!IsInitialized) return key;

            if (_texts.TryGetValue(key, out var text))
            {
                return text;
            }

#if UNITY_EDITOR
            Debug.LogWarning($"[Localization] Key completely missing: {key}");
#endif
            return key;
        }

        /// <summary>
        /// 获取带参数的本地化文本
        /// </summary>
        public string Get(string key, params object[] args)
        {
            var format = Get(key);

            if (args == null || args.Length == 0) return format;

            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogError($"[Localization] Format error: {key} in {CurrentLanguage}");
                return format;
            }
        }

        public async UniTask SetLanguage(Language language)
        {
            try
            {
                if (CurrentLanguage == language && IsInitialized) return;

                await ReloadLanguageData(language);

                CurrentLanguage = language;
                IsInitialized = true;

                SimplePlayerPrefs.SetInt(LANGUAGE_KEY, (int)language);
                SimplePlayerPrefs.Save();

                OnLanguageChanged?.Invoke(language);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }
        }

        private static Language GetSystemLanguage()
        {
            return Application.systemLanguage switch
            {
                SystemLanguage.Chinese => Language.Chinese,
                SystemLanguage.ChineseSimplified => Language.Chinese,
                SystemLanguage.ChineseTraditional => Language.Chinese,
                _ => Language.English
            };
        }
    }
}