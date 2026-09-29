using System;
using System.Threading.Tasks;
using Bizza.Sdk;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public static class WkyRuntime
    {
        private static Task initialization;
        private static Exception languageError;
        public static int LanguageType { get; private set; }
        public static event Action<int> LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            BizzaEventSystem.Off(EventDefine.Login.InitContentByCountry, SynchronizeLanguage);
            initialization = null;
            languageError = null;
            LanguageChanged = null;
            LanguageType = PlayerPrefs.GetString("SelectedLanguage", "en-US") == "pt-BR" ? 1 : 0;
            GameLocalization.SetLanguage(LanguageType);
        }

        public static Task InitializeAsync()
        {
            if (initialization == null) initialization = InitializeCore().AsTask();
            return initialization;
        }

        private static async UniTask InitializeCore()
        {
            BizzaEventSystem.On(EventDefine.Login.InitContentByCountry, SynchronizeLanguage);
            var flow = new Boots_Flow();
            await flow.Init();
            if (!WKY_Flow.isInit) throw new InvalidOperationException("WKY initialization did not complete.");
            SynchronizeLanguage();
            if (languageError != null) throw languageError;
            AdjustAttributionAdapter.AttributeStart("GameEntry");
            await UniTask.WaitUntil(() => BizzaSdk.Ad.Inited);
        }

        public static bool TryGetLanguage(string country, out int language, out string locale)
        {
            if (string.Equals(country, "BR", StringComparison.OrdinalIgnoreCase))
            { language = 1; locale = "pt-BR"; return true; }
            if (string.Equals(country, "US", StringComparison.OrdinalIgnoreCase))
            { language = 0; locale = "en-US"; return true; }
            language = 0; locale = null; return false;
        }

        private static void SynchronizeLanguage()
        {
            // This SDK's user response supplies Os_Cty; it has no separate language field.
            string country = AccountModule.Instance?.Os_Current_Uso?.Os_Cty;
            if (!TryResolveLanguage(country, ChannelConfig.Instance, out int language, out string locale))
            {
                languageError = new InvalidOperationException(
                    "No supported country is available from the server or ChannelConfig defaults.");
                Debug.LogException(languageError);
                return;
            }
            languageError = null;
            LanguageType = language;
            PlayerPrefs.SetString("SelectedLanguage", locale);
            PlayerPrefs.Save();
            GameLocalization.SetLanguage(language);
            LanguageChanged?.Invoke(language);
        }

        public static bool TryResolveLanguage(string serverCountry, ChannelConfig config,
            out int language, out string locale)
        {
            if (!string.IsNullOrWhiteSpace(serverCountry))
            {
                if (TryGetLanguage(serverCountry.Trim(), out language, out locale)) return true;
                // Country support must not be confused with available UI translations.
                language = 0; locale = "en-US"; return true;
            }

            // Read the configured enum directly; do not depend on obfuscated enum names.
            if (config != null)
            {
                switch (config.real_CustomConfig.DefaultCountry)
                {
                    case AccountModule.E_CountryType.US:
                        language = 0; locale = "en-US"; return true;
                    case AccountModule.E_CountryType.BR:
                        language = 1; locale = "pt-BR"; return true;
                    default:
                        language = 0; locale = "en-US"; return true;
                }
            }
            language = 0; locale = null; return false;
        }
    }
}
