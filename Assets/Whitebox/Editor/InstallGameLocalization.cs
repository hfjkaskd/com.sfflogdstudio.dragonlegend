using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DragonLegend.Whitebox.EditorTools
{
    // Asset authoring only. Runtime localization uses the components saved in prefabs.
    public static class InstallGameLocalization
    {
        private const string CatalogPath = "Assets/Resources/Localization/GameText.json";
        private const string ReportPath = "Artifacts/Localization/static-text-audit.txt";
        private static readonly string[] PrefabRoots = {
            "Assets/Resources/RecoveredUI", "Assets/Resources/RecoveredSymbols",
            "Assets/Resources/Whitebox", "Assets/Resources/MainSkin",
            "Assets/Resources/Loading", "Assets/Whitebox/Prefabs"
        };

        [Serializable] private sealed class Catalog { public Entry[] entries; }
        [Serializable] private sealed class Entry { public string key, english, portuguese; }

        private sealed class Context
        {
            public readonly Dictionary<string, Entry> english = new Dictionary<string, Entry>(StringComparer.Ordinal);
            public readonly Dictionary<string, Entry> keys = new Dictionary<string, Entry>(StringComparer.Ordinal);
            public readonly HashSet<string> dynamicIds = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> dynamicLocations = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> reportedGlyphs = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> reportedImages = new HashSet<string>(StringComparer.Ordinal);
            public readonly StringBuilder report = new StringBuilder();
            public int attached, configured, dynamic, unmatched, missingGlyphs, changedAssets;
        }

        [MenuItem("Dragon Legend/Localization/Install Static Game Text")]
        public static void Install() { Run(true); }

        [MenuItem("Dragon Legend/Localization/Install All Game Localization")]
        public static void InstallAll()
        {
            // Validate the table before authoring either category of assets.
            LoadCatalog();
            InstallLocalizedArtwork.Install();
            Install();
        }

        [MenuItem("Dragon Legend/Localization/Audit Static Game Text")]
        public static void Audit() { Run(false); }

        private static void Run(bool write)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Leave Play Mode before authoring localization assets.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Save open scene changes before running localization authoring.");

            var context = LoadCatalog();
            var prefabs = FindPrefabs();
            var scenes = FindScenes();
            context.report.AppendLine("Static game localization audit (catalog exact English matches)")
                .AppendLine("Dynamic text is excluded when referenced by an authored game runtime component.")
                .AppendLine("Existing fonts, materials, colors, hierarchy and click targets are preserved.");

            // Scan every owning asset before changing any leaf prefab. A parent may write
            // text on a nested prefab; its source must be excluded in every other instance.
            foreach (string path in prefabs) VisitPrefab(path, root => ScanDynamic(context, root, path));
            foreach (string path in scenes) VisitScene(path, false, root => ScanDynamic(context, root, path));

            foreach (string path in prefabs)
            {
                bool changed = false;
                VisitPrefab(path, root => {
                    changed = ProcessRoot(context, root, path, write);
                    if (write && changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                });
                if (changed) context.changedAssets++;
            }
            foreach (string path in scenes)
            {
                bool changed = VisitScene(path, write, root => ProcessRoot(context, root, path, write));
                if (changed) context.changedAssets++;
            }
            if (write) AssetDatabase.SaveAssets();
            context.report.Insert(0, "Mode=" + (write ? "install" : "audit") + ", attached=" + context.attached
                + ", configured=" + context.configured + ", dynamicSkipped=" + context.dynamic
                + ", unmatched=" + context.unmatched + ", missingGlyphs=" + context.missingGlyphs
                + ", changedAssets=" + context.changedAssets + "\n\n");
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
            File.WriteAllText(ReportPath, context.report.ToString(), new UTF8Encoding(false));
            Debug.Log("GAME_LOCALIZATION_" + (write ? "INSTALLED" : "AUDITED") + ": " + ReportPath
                + " (attached=" + context.attached + ", missingGlyphs=" + context.missingGlyphs + ")");
        }

        private static Context LoadCatalog()
        {
            var catalog = JsonUtility.FromJson<Catalog>(File.ReadAllText(CatalogPath));
            if (catalog == null || catalog.entries == null || catalog.entries.Length == 0)
                throw new InvalidDataException("Localization catalog is empty: " + CatalogPath);
            var context = new Context();
            foreach (var entry in catalog.entries)
            {
                if (string.IsNullOrWhiteSpace(entry.key) || string.IsNullOrWhiteSpace(entry.english)
                    || string.IsNullOrWhiteSpace(entry.portuguese))
                    throw new InvalidDataException("Incomplete localization entry: " + entry.key);
                context.keys.Add(entry.key, entry);
                string english = Normalize(entry.english);
                if (context.english.TryGetValue(english, out var duplicate))
                {
                    if (Normalize(duplicate.portuguese) != Normalize(entry.portuguese))
                        throw new InvalidDataException("Ambiguous static English translation: " + english);
                }
                else context.english.Add(english, entry);
            }
            return context;
        }

        private static List<string> FindPrefabs()
        {
            var roots = new List<string>();
            foreach (string root in PrefabRoots) if (AssetDatabase.IsValidFolder(root)) roots.Add(root);
            var candidates = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", roots.ToArray()))
                candidates.Add(AssetDatabase.GUIDToAssetPath(guid));
            var result = new List<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in candidates) OrderPrefab(path, candidates, visited, result);
            return result;
        }

        private static void OrderPrefab(string path, SortedSet<string> candidates,
            HashSet<string> visited, List<string> result)
        {
            if (!visited.Add(path)) return;
            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                if (dependency != path && candidates.Contains(dependency)) OrderPrefab(dependency, candidates, visited, result);
            result.Add(path);
        }

        private static List<string> FindScenes()
        {
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Whitebox/Scenes" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            return new List<string>(paths);
        }

        private static void VisitPrefab(string path, Action<GameObject> visit)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try { visit(root); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static bool VisitScene(string path, bool save, Func<GameObject, bool> visit)
        {
            var scene = SceneManager.GetSceneByPath(path);
            bool wasLoaded = scene.IsValid() && scene.isLoaded;
            if (!wasLoaded) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            bool changed = false;
            try
            {
                foreach (var root in scene.GetRootGameObjects()) changed |= visit(root);
                if (save && changed) EditorSceneManager.SaveScene(scene);
                return changed;
            }
            finally { if (!wasLoaded) EditorSceneManager.CloseScene(scene, true); }
        }

        private static bool ScanDynamic(Context context, GameObject root, string assetPath)
        {
            foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || behaviour is LocalizedGameText) continue;
                var script = MonoScript.FromMonoBehaviour(behaviour);
                if (script == null || !AssetDatabase.GetAssetPath(script).StartsWith("Assets/Whitebox/Runtime/", StringComparison.Ordinal)) continue;
                var serialized = new SerializedObject(behaviour);
                var iterator = serialized.GetIterator();
                while (iterator.Next(true))
                {
                    if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                    // These labels are only shown/hidden; their owners never write the text.
                    if (behaviour is RecoveredCashOutItem && iterator.propertyPath == "detailText") continue;
                    if (behaviour is RecoveredMoreWildEntry && iterator.propertyPath == "word") continue;
                    var component = iterator.objectReferenceValue as Component;
                    if (!(component is TMP_Text) && !(component is Text)) continue;
                    context.dynamicLocations.Add(Location(assetPath, component));
                    foreach (string id in SourceIds(component)) context.dynamicIds.Add(id);
                }
            }
            return false;
        }

        private static IEnumerable<string> SourceIds(Component component)
        {
            Object current = component;
            var visited = new HashSet<int>();
            while (current != null && visited.Add(current.GetInstanceID()))
            {
                if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(current, out string guid, out long id)
                    && !string.IsNullOrEmpty(guid)) yield return guid + ":" + id;
                current = PrefabUtility.GetCorrespondingObjectFromSource(current);
            }
        }

        private static bool ProcessRoot(Context context, GameObject root, string assetPath, bool write)
        {
            bool changed = false;
            foreach (var text in root.GetComponentsInChildren<TMP_Text>(true))
                changed |= ProcessText(context, text, null, assetPath, write);
            foreach (var text in root.GetComponentsInChildren<Text>(true))
                changed |= ProcessText(context, null, text, assetPath, write);
            foreach (var image in root.GetComponentsInChildren<Image>(true))
            {
                if (image.sprite == null) continue;
                string spritePath = AssetDatabase.GetAssetPath(image.sprite);
                string name = image.sprite.name.ToLowerInvariant();
                if (!(name.Contains("_txt") || name.Contains("_title") || name.StartsWith("bz_bg", StringComparison.Ordinal))) continue;
                if (context.reportedImages.Add(spritePath + ":" + name))
                    context.report.AppendLine("IMAGE_TEXT_REVIEW\t" + spritePath + "\t" + Location(assetPath, image));
            }
            return changed;
        }

        private static bool ProcessText(Context context, TMP_Text tmp, Text legacy, string path, bool write)
        {
            Component text = tmp != null ? (Component)tmp : legacy;
            string location = Location(path, text);
            string english = tmp != null ? tmp.text : legacy.text;
            bool dynamic = context.dynamicLocations.Contains(location);
            foreach (string id in SourceIds(text)) dynamic |= context.dynamicIds.Contains(id);
            if (dynamic)
            {
                context.dynamic++;
                context.report.AppendLine("DYNAMIC\t" + location + "\t" + OneLine(english));
                return false;
            }

            var localizer = text.GetComponent<LocalizedGameText>();
            Entry entry = null;
            if (localizer != null)
            {
                var existing = new SerializedObject(localizer);
                context.keys.TryGetValue(existing.FindProperty("key").stringValue, out entry);
                english = existing.FindProperty("englishFallback").stringValue;
            }
            if (entry == null) context.english.TryGetValue(Normalize(english), out entry);
            // Formats and numeric previews belong to their owning runtime presenter.
            if (entry == null || entry.english.IndexOf('{') >= 0)
            {
                if (Regex.IsMatch(StripTags(english), "[A-Za-z]{2}"))
                {
                    context.unmatched++;
                    context.report.AppendLine("UNMATCHED\t" + location + "\t" + OneLine(english));
                }
                return false;
            }

            CheckGlyphs(context, tmp, legacy, entry.portuguese, location);
            if (!write)
            {
                CheckFit(context, tmp, entry, location);
                context.report.AppendLine((localizer == null ? "NEEDS_COMPONENT\t" : "LOCALIZED\t") + location + "\t" + entry.key);
                return false;
            }
            bool changed = false;
            if (localizer == null)
            {
                localizer = text.gameObject.AddComponent<LocalizedGameText>();
                context.attached++;
                changed = true;
            }
            var properties = new SerializedObject(localizer);
            if (properties.FindProperty("tmpText").objectReferenceValue != tmp
                || properties.FindProperty("legacyText").objectReferenceValue != legacy
                || properties.FindProperty("key").stringValue != entry.key
                || properties.FindProperty("englishFallback").stringValue != english)
            {
                localizer.Configure(tmp, legacy, entry.key, english);
                EditorUtility.SetDirty(localizer);
                if (PrefabUtility.IsPartOfPrefabInstance(localizer)) PrefabUtility.RecordPrefabInstancePropertyModifications(localizer);
                context.configured++;
                changed = true;
            }
            changed |= RestoreInstallerAutoSizing(tmp, legacy);
            CheckFit(context, tmp, entry, location);
            context.report.AppendLine("LOCALIZED\t" + location + "\t" + entry.key);
            return changed;
        }

        private static bool RestoreInstallerAutoSizing(TMP_Text tmp, Text legacy)
        {
            // Earlier installs enabled auto size using this exact .72 fingerprint.
            // Many original labels intentionally overflow a small nominal text box;
            // changing their fit shrinks even English. Preserve authored layout and
            // leave Portuguese adjustments to the verified presentation regions.
            bool changed = false;
            if (tmp != null && tmp.enableAutoSizing && tmp.name != "LocalizedArtworkText"
                && Mathf.Abs(tmp.fontSizeMin - Mathf.Min(tmp.fontSizeMax, Mathf.Max(18f, tmp.fontSizeMax * .72f))) < .01f)
            {
                tmp.enableAutoSizing = false;
                tmp.fontSize = tmp.fontSizeMax;
                EditorUtility.SetDirty(tmp);
                if (PrefabUtility.IsPartOfPrefabInstance(tmp)) PrefabUtility.RecordPrefabInstancePropertyModifications(tmp);
                changed = true;
            }
            if (legacy != null && legacy.resizeTextForBestFit && legacy.name != "LocalizedArtworkText"
                && legacy.resizeTextMinSize == Mathf.Min(legacy.resizeTextMaxSize, Mathf.Max(18, Mathf.RoundToInt(legacy.resizeTextMaxSize * .72f))))
            {
                legacy.resizeTextForBestFit = false;
                legacy.fontSize = legacy.resizeTextMaxSize;
                EditorUtility.SetDirty(legacy);
                if (PrefabUtility.IsPartOfPrefabInstance(legacy)) PrefabUtility.RecordPrefabInstancePropertyModifications(legacy);
                changed = true;
            }
            return changed;
        }

        private static void CheckGlyphs(Context context, TMP_Text tmp, Text legacy, string translated, string location)
        {
            string plain = StripTags(translated);
            foreach (char character in plain)
            {
                if (char.IsWhiteSpace(character) || char.IsControl(character)) continue;
                bool exists = tmp != null
                    ? tmp.font != null && tmp.font.HasCharacter(character, true, false)
                    : legacy.font != null && legacy.font.HasCharacter(character);
                if (exists) continue;
                string font = tmp != null ? AssetDatabase.GetAssetPath(tmp.font) : AssetDatabase.GetAssetPath(legacy.font);
                if (!context.reportedGlyphs.Add(font + ":" + (int)character)) continue;
                context.missingGlyphs++;
                context.report.AppendLine("MISSING_GLYPH\t" + font + "\tU+" + ((int)character).ToString("X4")
                    + " " + character + "\t" + location);
            }
        }

        private static void CheckFit(Context context, TMP_Text tmp, Entry entry, string location)
        {
            if (tmp == null || tmp.font == null) return;
            var rect = tmp.rectTransform.rect;
            if (rect.width <= 1 || rect.height <= 1) return;
            var preferred = tmp.GetPreferredValues(entry.portuguese, rect.width, float.PositiveInfinity);
            if (preferred.x > rect.width + 2 || preferred.y > rect.height + 2)
                context.report.AppendLine("FIT_REVIEW\t" + location + "\tbox=" + rect.width + "x" + rect.height
                    + "\tpreferred=" + preferred.x + "x" + preferred.y + "\tkey=" + entry.key);
        }

        private static string Location(string asset, Component text)
        {
            var path = new StringBuilder();
            for (var node = text.transform; node != null; node = node.parent)
                path.Insert(0, "/" + node.name + "[" + node.GetSiblingIndex() + "]");
            return asset + path + (text is TMP_Text ? "#TMP" : "#Text");
        }
        private static string Normalize(string text) { return (text ?? string.Empty).Replace("\r\n", "\n").Trim(); }
        private static string StripTags(string text) { return Regex.Replace(text ?? string.Empty, "<[^>]+>", string.Empty); }
        private static string OneLine(string text) { return (text ?? string.Empty).Replace("\r", string.Empty).Replace("\n", "\\n"); }
    }
}
