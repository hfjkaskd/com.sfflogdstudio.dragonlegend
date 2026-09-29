using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // JSON field names are part of the shipped text asset, so preserve them during obfuscation.
    [Serializable, Obfuz.ObfuzIgnore]
    public sealed class GameTextEntry
    {
        public string key;
        public string english;
        public string portuguese;
    }

    [Serializable, Obfuz.ObfuzIgnore]
    public sealed class GameTextCatalog
    {
        public GameTextEntry[] entries;
    }

    public static class GameLocalization
    {
        public const string ResourcePath = "Localization/GameText";
        private static Dictionary<string, GameTextEntry> byKey;
        private static Dictionary<string, GameTextEntry> byEnglish;
        private static readonly CultureInfo EnglishCulture = CultureInfo.GetCultureInfo("en-US");
        private static readonly CultureInfo PortugueseCulture = CultureInfo.GetCultureInfo("pt-BR");
        public static int CurrentLanguage { get; private set; }
        public static event Action<int> LanguageChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            byKey = null;
            byEnglish = null;
            LanguageChanged = null;
            CurrentLanguage = PlayerPrefs.GetString("SelectedLanguage", "en-US") == "pt-BR" ? 1 : 0;
        }

        public static void SetLanguage(int language)
        {
            int next = language == 1 ? 1 : 0;
            if (CurrentLanguage == next) return;
            CurrentLanguage = next;
            LanguageChanged?.Invoke(next);
        }

        public static string Get(string key, string englishFallback = null)
        {
            return Get(key, CurrentLanguage, englishFallback);
        }

        public static string Get(string key, int language, string englishFallback = null)
        {
            EnsureLoaded();
            if (!string.IsNullOrEmpty(key) && byKey.TryGetValue(key, out var entry))
                return Translate(entry, language);
            return Text(englishFallback ?? key ?? string.Empty, language);
        }

        public static string Text(string englishSource)
        {
            return Text(englishSource, CurrentLanguage);
        }

        public static string Text(string englishSource, int language)
        {
            if (string.IsNullOrEmpty(englishSource) || language != 1) return englishSource ?? string.Empty;
            EnsureLoaded();
            return byEnglish.TryGetValue(englishSource, out var entry) ? Translate(entry, language) : englishSource;
        }

        // The language argument is explicit: an integer placeholder must never select an overload's language.
        public static string Format(string englishSource, int language, params object[] arguments)
        {
            return string.Format(language == 1 ? PortugueseCulture : EnglishCulture,
                Text(englishSource, language), arguments);
        }

        private static string Translate(GameTextEntry entry, int language)
        {
            return language == 1 && !string.IsNullOrEmpty(entry.portuguese) ? entry.portuguese : entry.english;
        }

        private static void EnsureLoaded()
        {
            if (byKey != null) return;
            var asset = Resources.Load<TextAsset>(ResourcePath);
            if (asset == null) throw new InvalidOperationException("Missing game localization catalog: " + ResourcePath);
            GameTextCatalog catalog;
            try { catalog = JsonUtility.FromJson<GameTextCatalog>(asset.text); }
            finally { Resources.UnloadAsset(asset); }
            if (catalog == null || catalog.entries == null)
                throw new InvalidOperationException("Invalid game localization catalog.");
            var keys = new Dictionary<string, GameTextEntry>(catalog.entries.Length, StringComparer.Ordinal);
            var sources = new Dictionary<string, GameTextEntry>(catalog.entries.Length, StringComparer.Ordinal);
            foreach (var entry in catalog.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key) || string.IsNullOrEmpty(entry.english))
                    throw new InvalidOperationException("Localization entry requires a key and English source.");
                if (keys.ContainsKey(entry.key)) throw new InvalidOperationException("Duplicate localization key: " + entry.key);
                keys.Add(entry.key, entry);
                if (sources.TryGetValue(entry.english, out var previous))
                {
                    if (previous.portuguese != entry.portuguese)
                        throw new InvalidOperationException("Ambiguous localization source: " + entry.english);
                }
                else sources.Add(entry.english, entry);
            }
            byEnglish = sources;
            byKey = keys;
        }
    }
}
