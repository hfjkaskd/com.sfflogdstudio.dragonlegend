using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using Object = UnityEngine.Object;

namespace DragonLegend.Whitebox.EditorTools
{
    // Editor authoring: preserve recovered atlas/material assets and bake language glyphs once.
    public static class PrepareLocalizationFonts
    {
        private const string CatalogPath = "Assets/Resources/Localization/GameText.json";
        private const string OutputFolder = "Assets/Localization/Fonts";
        private const string BackupSource = "Assets/Resources/RecoveredArt/Res/Font/msyhbd.ttf";
        private const string ReportPath = "Artifacts/Localization/font-preparation.txt";
        private static readonly string[] Roots = {
            "Assets/Resources/RecoveredUI", "Assets/Resources/RecoveredSymbols",
            "Assets/Resources/Whitebox", "Assets/Resources/MainSkin",
            "Assets/Resources/Loading", "Assets/Whitebox/Prefabs"
        };
        private static readonly Regex Markup = new Regex(@"<[^>]*>|\{[0-9]+(?::[^}]*)?\}");

        [MenuItem("Dragon Legend/Localization/Prepare Font Glyphs")]
        public static void Prepare()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Leave Play Mode before preparing font assets.");
            var report = new StringBuilder("Localization font preparation\n");
            int atlases = 0, unresolved = 0;
            try
            {
                var catalog = JsonUtility.FromJson<GameTextCatalog>(File.ReadAllText(CatalogPath));
                if (catalog == null || catalog.entries == null) throw new InvalidDataException("Missing localization entries.");
                var characters = new SortedSet<uint>();
                foreach (var entry in catalog.entries)
                {
                    AddCharacters(characters, entry.english);
                    AddCharacters(characters, entry.portuguese);
                }
                AddCharacters(characters, "0123456789$R%:/.,-+()");
                unresolved += AuditLegacySource(characters, report);
                var fonts = FindTextFonts(report);
                EnsureFolder(OutputFolder);
                foreach (var font in fonts)
                {
                    var path = AssetDatabase.GetAssetPath(font);
                    var missing = Missing(font, characters);
                    var source = SourceFont(font);
                    report.AppendLine(path + ": " + missing.Count + " missing catalog glyphs before preparation.");
                    bool writable = IsWritableDynamic(font);
                    if (writable && source != null && missing.Count != 0)
                    {
                        font.TryAddCharacters(missing.ToArray(), out _, true);
                        atlases += PersistNewAtlases(font, path);
                        font.ReadFontAssetDefinition();
                        EditorUtility.SetDirty(font);
                        missing = Missing(font, characters);
                        report.AppendLine("  Pre-baked writable Alpha8 owner; Dynamic mode retained for input text.");
                    }
                    if (missing.Count != 0 && source != null)
                        AddFallback(font, source, missing, report, ref atlases);
                    missing = Missing(font, characters);
                    if (missing.Count != 0)
                    {
                        var backup = AssetDatabase.LoadAssetAtPath<Font>(BackupSource);
                        if (backup == null) throw new InvalidDataException("Missing fallback source: " + BackupSource);
                        AddFallback(font, backup, missing, report, ref atlases);
                    }
                    missing = Missing(font, characters);
                    if (missing.Count != 0)
                    {
                        unresolved += missing.Count;
                        report.AppendLine("  UNRESOLVED: " + CodePoints(missing));
                        continue;
                    }
                    if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic && !writable)
                    {
                        // TMP attempts Dynamic insertion before consulting fallbacks. Imported,
                        // compressed atlases must not receive that runtime write attempt.
                        if (source != null)
                        {
                            var settings = font.creationSettings;
                            settings.sourceFontFileGUID = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
                            font.creationSettings = settings;
                        }
                        font.atlasPopulationMode = AtlasPopulationMode.Static;
                        EditorUtility.SetDirty(font);
                        report.AppendLine("  FROZEN: imported/non-writable atlas; existing glyphs, atlas and material GUIDs preserved.");
                    }
                    report.AppendLine("  Coverage: complete; no catalog glyph requires runtime generation.");
                }
                AssetDatabase.SaveAssets();
                report.AppendLine("Fonts checked: " + fonts.Count + "; new atlases: " + atlases + "; unresolved glyphs: " + unresolved);
                if (unresolved != 0) throw new InvalidDataException("Localization fonts still have " + unresolved + " missing glyphs. See " + ReportPath);
                Debug.Log("LOCALIZATION_FONTS_PREPARED: " + fonts.Count + " fonts; " + atlases + " new atlases; 0 missing glyphs.");
            }
            finally
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
                File.WriteAllText(ReportPath, report.ToString(), new UTF8Encoding(false));
            }
        }

        private static List<TMP_FontAsset> FindTextFonts(StringBuilder report)
        {
            var found = new HashSet<TMP_FontAsset>();
            var roots = new List<string>();
            foreach (var path in Roots) if (AssetDatabase.IsValidFolder(path)) roots.Add(path);
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", roots.ToArray()))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab == null) continue;
                foreach (var text in prefab.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (text.font == null) continue;
                    var plain = Markup.Replace(text.text ?? string.Empty, string.Empty);
                    bool letters = false;
                    foreach (char value in plain) if (char.IsLetter(value)) { letters = true; break; }
                    if (letters || text.GetComponent<LocalizedGameText>() != null) found.Add(text.font);
                }
            }
            // These also render dynamically supplied descriptions and input placeholders.
            foreach (var path in new[] {
                "Assets/Resources/RecoveredText/Res/Font/QuorumStd-Black_zitidi.com SDF.asset",
                "Assets/Resources/RecoveredUI/CashOutItemArt/msyhbd SDF.asset" })
            {
                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font != null) found.Add(font);
            }
            var result = new List<TMP_FontAsset>(found);
            result.Sort((a, b) => string.CompareOrdinal(AssetDatabase.GetAssetPath(a), AssetDatabase.GetAssetPath(b)));
            report.AppendLine("Only fonts used by authored letter text or localized labels are selected; number-only effect fonts are excluded.");
            return result;
        }

        private static int AuditLegacySource(IEnumerable<uint> required, StringBuilder report)
        {
            const string path = "Assets/Resources/RecoveredArt/Font/SourceCodePro-Regular.otf";
            var source = AssetDatabase.LoadAssetAtPath<Font>(path);
            if (source == null || FontEngine.LoadFontFace(source, 64) != FontEngineError.Success)
                throw new InvalidDataException("Cannot inspect legacy UI font: " + path);
            var missing = new List<uint>();
            foreach (uint value in required)
                if (!FontEngine.TryGetGlyphWithUnicodeValue(value, GlyphLoadFlags.LOAD_NO_BITMAP, out _)) missing.Add(value);
            report.AppendLine("Legacy Font source " + path + ": " + missing.Count + " missing glyphs; font asset left unchanged.");
            if (missing.Count != 0) report.AppendLine("  UNRESOLVED: " + CodePoints(missing));
            return missing.Count;
        }

        private static void AddCharacters(SortedSet<uint> output, string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = Markup.Replace(text, string.Empty);
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsControl(text[i])) continue;
                uint value = char.IsSurrogatePair(text, i) ? (uint)char.ConvertToUtf32(text, i++) : text[i];
                output.Add(value);
            }
        }

        private static List<uint> Missing(TMP_FontAsset font, IEnumerable<uint> required)
        {
            var available = new HashSet<uint>();
            CollectGlyphs(font, available, new HashSet<TMP_FontAsset>());
            var missing = new List<uint>();
            foreach (uint value in required) if (!available.Contains(value)) missing.Add(value);
            return missing;
        }

        private static void CollectGlyphs(TMP_FontAsset font, HashSet<uint> output, HashSet<TMP_FontAsset> seen)
        {
            if (font == null || !seen.Add(font)) return;
            foreach (var character in font.characterTable) output.Add(character.unicode);
            if (font.fallbackFontAssetTable != null)
                foreach (var fallback in font.fallbackFontAssetTable) CollectGlyphs(fallback, output, seen);
        }

        private static Font SourceFont(TMP_FontAsset font)
        {
            if (font.sourceFontFile != null) return font.sourceFontFile;
            return AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(font.creationSettings.sourceFontFileGUID));
        }

        private static bool IsWritableDynamic(TMP_FontAsset font)
        {
            if (font.atlasPopulationMode != AtlasPopulationMode.Dynamic || font.atlasTextures == null || font.atlasTextures.Length == 0) return false;
            foreach (var texture in font.atlasTextures)
            {
                if (texture == null) continue;
                if (texture.format != TextureFormat.Alpha8 || !texture.isReadable) return false;
                // Pixel edits to imported PNGs are not persisted by SaveAssets.
                if (AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) is TextureImporter) return false;
            }
            return true;
        }

        private static void AddFallback(TMP_FontAsset owner, Font source, List<uint> missing, StringBuilder report, ref int atlasCount)
        {
            int pointSize = Mathf.Clamp(Mathf.RoundToInt(owner.faceInfo.pointSize), 32, 90);
            if (FontEngine.LoadFontFace(source, pointSize) != FontEngineError.Success)
                throw new InvalidDataException("Cannot load font source: " + AssetDatabase.GetAssetPath(source));
            var supported = new List<uint>();
            foreach (uint value in missing)
                if (FontEngine.TryGetGlyphWithUnicodeValue(value, GlyphLoadFlags.LOAD_NO_BITMAP, out _)) supported.Add(value);
            if (supported.Count == 0) return;
            string ownerGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(owner));
            string sourceGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source));
            string suffix = Hash128.Compute(CodePoints(supported)).ToString().Substring(0, 8);
            string path = OutputFolder + "/" + ownerGuid + "-" + sourceGuid + "-" + suffix + ".asset";
            var fallback = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (fallback == null)
            {
                fallback = Bake(source, supported.ToArray(), pointSize);
                fallback.name = owner.name + " Portuguese Glyphs";
                fallback.normalStyle = owner.normalStyle; fallback.normalSpacingOffset = owner.normalSpacingOffset;
                fallback.boldStyle = owner.boldStyle; fallback.boldSpacing = owner.boldSpacing;
                fallback.italicStyle = owner.italicStyle; fallback.tabSize = owner.tabSize;
                fallback.atlasPopulationMode = AtlasPopulationMode.Static;
                AssetDatabase.CreateAsset(fallback, path);
                fallback.material.name = fallback.name + " Material";
                AssetDatabase.AddObjectToAsset(fallback.material, fallback);
                atlasCount += PersistNewAtlases(fallback, path);
                EditorUtility.SetDirty(fallback);
            }
            if (owner.fallbackFontAssetTable == null) owner.fallbackFontAssetTable = new List<TMP_FontAsset>();
            if (!owner.fallbackFontAssetTable.Contains(fallback)) owner.fallbackFontAssetTable.Add(fallback);
            EditorUtility.SetDirty(owner);
            report.AppendLine("  Static fallback: " + supported.Count + " glyphs from " + AssetDatabase.GetAssetPath(source)
                + "; " + fallback.atlasWidth + "x" + fallback.atlasHeight + "; " + path);
        }

        private static TMP_FontAsset Bake(Font source, uint[] characters, int pointSize)
        {
            foreach (int size in new[] { 512, 1024 })
            {
                var font = TMP_FontAsset.CreateFontAsset(source, pointSize, 6, GlyphRenderMode.SDFAA, size, size, AtlasPopulationMode.Dynamic, false);
                if (font == null) throw new InvalidDataException("Unable to create localized font: " + source.name);
                if (font.TryAddCharacters(characters, out _, true)) return font;
                foreach (var texture in font.atlasTextures) if (texture != null) Object.DestroyImmediate(texture);
                Object.DestroyImmediate(font.material); Object.DestroyImmediate(font);
            }
            throw new InvalidDataException("Localized glyphs exceed a single 1024 atlas for " + source.name);
        }

        private static int PersistNewAtlases(TMP_FontAsset font, string path)
        {
            int count = 0;
            foreach (var texture in font.atlasTextures)
            {
                if (texture == null) continue;
                EditorUtility.SetDirty(texture);
                if (AssetDatabase.Contains(texture)) continue;
                texture.name = font.name + " Atlas " + count;
                texture.filterMode = FilterMode.Bilinear; texture.wrapMode = TextureWrapMode.Clamp;
                AssetDatabase.AddObjectToAsset(texture, path); count++;
            }
            return count;
        }

        private static string CodePoints(IEnumerable<uint> values)
        {
            var text = new StringBuilder();
            foreach (uint value in values) text.Append("U+").Append(value.ToString("X4")).Append(' ');
            return text.ToString();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
