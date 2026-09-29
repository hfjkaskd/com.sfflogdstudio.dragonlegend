using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class GameLocalizationTests
{
    private int previousLanguage;
    [SetUp] public void RememberLanguage() { previousLanguage = GameLocalization.CurrentLanguage; }
    [TearDown] public void RestoreLanguage() { GameLocalization.SetLanguage(previousLanguage); }

    [Test]
    public void ShippedCatalogPreservesEveryFormatArgumentAndRichTextTag()
    {
        var text = Resources.Load<TextAsset>(GameLocalization.ResourcePath);
        Assert.IsNotNull(text);
        var catalog = JsonUtility.FromJson<GameTextCatalog>(text.text);
        Assert.Greater(catalog.entries.Length, 20, "The game requires complete screen text, not just currency labels.");
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var row in catalog.entries)
        {
            Assert.IsTrue(keys.Add(row.key), "Duplicate key: " + row.key);
            Assert.IsFalse(string.IsNullOrWhiteSpace(row.portuguese), row.key);
            CollectionAssert.AreEquivalent(Matches(row.english, @"(?<!\{)\{\d+[^}]*\}(?!\})"),
                Matches(row.portuguese, @"(?<!\{)\{\d+[^}]*\}(?!\})"), "Format arguments changed: " + row.key);
            CollectionAssert.AreEqual(Matches(row.english, "<[^>]+>"), Matches(row.portuguese, "<[^>]+>"),
                "Rich text markup changed: " + row.key);
            Assert.AreEqual(row.english, GameLocalization.Get(row.key, 0), row.key);
            Assert.AreEqual(row.portuguese, GameLocalization.Get(row.key, 1), row.key);
            Assert.AreEqual(row.portuguese, GameLocalization.Text(row.english, 1), row.key);
        }
    }

    [Test]
    public void MissingTextUsesTheVisibleEnglishSourceAndUnsupportedLanguagesUseEnglish()
    {
        Assert.AreEqual("Uncatalogued example", GameLocalization.Text("Uncatalogued example", 1));
        Assert.AreEqual("Safe fallback", GameLocalization.Get("missing.key", 1, "Safe fallback"));
        Assert.AreEqual("CLAIM", GameLocalization.Text("CLAIM", 5));
        Assert.AreEqual(string.Empty, GameLocalization.Text(null, 1));
    }

    [Test]
    public void PrefabTextBindingRefreshesOnLanguageChangeAndWhenReenabled()
    {
        var go = new GameObject("Localized label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.SetActive(false);
        try
        {
            var label = go.GetComponent<Text>();
            var binding = go.AddComponent<LocalizedGameText>();
            var data = JsonUtility.FromJson<GameTextCatalog>(Resources.Load<TextAsset>(GameLocalization.ResourcePath).text);
            GameTextEntry entry = null;
            foreach (var row in data.entries) if (row.english == "CLAIM") { entry = row; break; }
            Assert.IsNotNull(entry, "The actual reward claim button must be included.");
            binding.Configure(null, label, entry.key, entry.english);
            GameLocalization.SetLanguage(0);
            go.SetActive(true);
            Assert.AreEqual(entry.english, label.text);
            GameLocalization.SetLanguage(1);
            Assert.AreEqual(entry.portuguese, label.text);
            go.SetActive(false);
            GameLocalization.SetLanguage(0);
            Assert.AreEqual(entry.portuguese, label.text, "Inactive views must not stay subscribed.");
            go.SetActive(true);
            Assert.AreEqual(entry.english, label.text);
        }
        finally { Object.DestroyImmediate(go); }
    }

    private static string[] Matches(string value, string pattern)
    {
        var found = Regex.Matches(value, pattern);
        var result = new string[found.Count];
        for (int i = 0; i < result.Length; i++) result[i] = found[i].Value;
        return result;
    }
}
