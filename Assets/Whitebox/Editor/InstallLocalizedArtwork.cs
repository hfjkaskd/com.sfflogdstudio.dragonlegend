using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox.EditorTools
{
    // Converts English-only artwork labels into authored TMP children. No bitmap is edited.
    public static class InstallLocalizedArtwork
    {
        private const string FontPath = "Assets/Resources/RecoveredText/Res/Font/QuorumStd-Black_zitidi.com SDF.asset";
        private const string MaterialPath = "Assets/Resources/Localization/GoldArtworkText.mat";
        private const string LabelName = "LocalizedArtworkText";
        private struct Label
        {
            public string key, english;
            public Label(string k, string e) { key = k; english = e; }
        }
        private static readonly Dictionary<string, Label> Labels = new Dictionary<string, Label>(StringComparer.Ordinal)
        {
            { "bz_txt_01", new Label("art.help.pay_table", "PAY TABLE") },
            { "bz_txt_02", new Label("art.help.free_game", "FREE GAME") },
            { "bz_txt_03", new Label("art.help.bonus_game", "BONUS GAME") },
            { "sz_settings_txt", new Label("art.settings", "SETTINGS") },
            { "b_luckybonus_txt", new Label("art.lucky_bonus", "LUCKY BONUS") },
            { "mfyx_congatulations01_txt", new Label("art.congratulations", "CONGRATULATIONS") },
            { "mfyx_congatulations02_txt", new Label("art.congratulations", "CONGRATULATIONS") },
            { "tx_9congratulations_txt_-3940978220648715811", new Label("art.congratulations", "CONGRATULATIONS") },
            { "mfyx_youwin_txt", new Label("art.you_win", "YOU WIN") },
            { "re_dailytask_txt", new Label("art.daily_task", "DAILY TASK") },
            { "tc_luckyreward_txt", new Label("art.lucky_reward", "LUCKY REWARD") },
            { "fortune_wheel_txt", new Label("art.fortune_wheel", "FORTUNE WHEEL") },
            { "sc_luckycard_txt", new Label("art.lucky_card", "LUCKY CARD") },
            { "t_treasures_txt_2411717305829263836", new Label("art.treasures", "TREASURES") },
            { "hp_reviewus_txt", new Label("art.review_us", "REVIEW US") },
            { "more_wild_txt", new Label("art.more_wild", "MORE WILD") },
            { "ty_spins_txt", new Label("art.spins", "SPINS") },
            { "ty_morespins_txt", new Label("art.more_spins", "MORE SPINS") },
            { "sltos_lucktspin_txt", new Label("art.lucky_spin", "LUCKY SPIN") }
        };

        [MenuItem("Dragon Legend/Localization/Install Artwork Labels")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode before editing prefabs.");
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
            if (font == null) throw new InvalidDataException("Missing authored Quorum font: " + FontPath);
            var material = GetMaterial(font);
            var paths = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] {
                "Assets/Resources/RecoveredUI", "Assets/Resources/Whitebox", "Assets/Resources/MainSkin" }))
                paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            var ordered = new List<string>();
            var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in paths) OrderPrefabs(path, paths, visited, ordered);
            int titles = 0, diagrams = 0;
            foreach (string path in ordered)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                bool changed = false;
                try
                {
                    foreach (var image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.sprite == null || image.gameObject.name == "CroppedDiagram") continue;
                        string spriteName = Path.GetFileNameWithoutExtension(AssetDatabase.GetAssetPath(image.sprite));
                        if (!Labels.TryGetValue(spriteName, out var label)) Labels.TryGetValue(image.sprite.name, out label);
                        if (!string.IsNullOrEmpty(label.key))
                        {
                            if (image.transform.Find(LabelName) != null) continue;
                            float size = Mathf.Clamp(image.rectTransform.rect.height * .62f, 26, 108);
                            AddLabel(image.transform, LabelName, label, font, material, Vector2.zero, Vector2.one, size, false);
                            image.enabled = false;
                            titles++; changed = true;
                        }
                        else if (path.EndsWith("/MainUtility/Help.prefab", StringComparison.Ordinal)
                            && (spriteName == "bz_bg02" || spriteName == "bz_bg03"))
                        {
                            if (image.transform.Find("LocalizedDiagramClip") != null) continue;
                            LocalizeDiagram(image, spriteName == "bz_bg02", font, material);
                            diagrams++; changed = true;
                        }
                    }
                    if (changed) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("LOCALIZED_ARTWORK_INSTALLED: titles=" + titles + ", helpDiagrams=" + diagrams
                + ". Original image files, WILD/SCATTER symbols and button components were retained.");
        }

        private static void OrderPrefabs(string path, SortedSet<string> paths, HashSet<string> visited, List<string> ordered)
        {
            if (!visited.Add(path)) return;
            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                if (dependency != path && paths.Contains(dependency)) OrderPrefabs(dependency, paths, visited, ordered);
            ordered.Add(path);
        }

        private static Material GetMaterial(TMP_FontAsset font)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material != null) return material;
            Directory.CreateDirectory(Path.GetDirectoryName(MaterialPath));
            AssetDatabase.Refresh();
            material = new Material(font.material) { name = "GoldArtworkText" };
            material.EnableKeyword("OUTLINE_ON");
            material.SetColor("_OutlineColor", new Color(.42f, .08f, .015f, 1));
            material.SetFloat("_OutlineWidth", .16f);
            material.EnableKeyword("UNDERLAY_ON");
            material.SetColor("_UnderlayColor", new Color(.31f, .015f, .005f, .9f));
            material.SetFloat("_UnderlayOffsetX", .25f);
            material.SetFloat("_UnderlayOffsetY", -.5f);
            material.SetFloat("_UnderlayDilate", .1f);
            AssetDatabase.CreateAsset(material, MaterialPath);
            return material;
        }

        private static void LocalizeDiagram(Image original, bool freeGame, TMP_FontAsset font, Material material)
        {
            // The source is 836 x 847. Keep the game illustration, crop only the
            // baked English banner/captions, and author translated text in that space.
            float bottom = freeGame ? 222f / 847f : 149f / 847f;
            float top = freeGame ? 732f / 847f : 1f;
            var clipObject = new GameObject("LocalizedDiagramClip", typeof(RectTransform), typeof(RectMask2D));
            clipObject.layer = original.gameObject.layer;
            var clip = (RectTransform)clipObject.transform;
            clip.SetParent(original.transform, false);
            clip.anchorMin = new Vector2(0, bottom); clip.anchorMax = new Vector2(1, top);
            clip.offsetMin = clip.offsetMax = Vector2.zero;
            var pictureObject = new GameObject("CroppedDiagram", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            pictureObject.layer = original.gameObject.layer;
            var picture = pictureObject.GetComponent<Image>();
            picture.sprite = original.sprite; picture.color = original.color; picture.material = original.material;
            picture.raycastTarget = false;
            var rect = picture.rectTransform;
            rect.SetParent(clip, false); rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f);
            rect.sizeDelta = original.rectTransform.rect.size;
            rect.anchoredPosition = new Vector2(0, (.5f - (top + bottom) * .5f) * rect.sizeDelta.y);
            if (freeGame)
            {
                AddLabel(original.transform, "LocalizedBonusBanner",
                    new Label("art.help.collect_all", "Collect all coins to trigger the bonus game."),
                    font, material, new Vector2(.035f, .87f), new Vector2(.965f, .985f), 34, true);
                AddLabel(original.transform, "LocalizedScatterHelp",
                    new Label("art.help.scatter", "Land 3 SCATTER symbols to activate Free Spins."),
                    font, material, new Vector2(.025f, .125f), new Vector2(.975f, .245f), 36, true);
                AddLabel(original.transform, "LocalizedOrbHelp",
                    new Label("art.help.orb", "The green orb triggers special rewards."),
                    font, material, new Vector2(.025f, .005f), new Vector2(.975f, .12f), 36, true);
            }
            else
            {
                AddLabel(original.transform, "LocalizedBonusHelp",
                    new Label("art.help.bonus_description", "Collect coins to trigger the Bonus game, which can award the Jackpot."),
                    font, material, new Vector2(.025f, .005f), new Vector2(.975f, .17f), 36, true);
            }
            original.enabled = false;
        }

        private static void AddLabel(Transform parent, string name, Label entry, TMP_FontAsset font,
            Material material, Vector2 min, Vector2 max, float size, bool wrapping)
        {
            var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LocalizedGameText));
            node.layer = parent.gameObject.layer;
            var rect = (RectTransform)node.transform; rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = node.GetComponent<TextMeshProUGUI>();
            text.font = font; text.fontSharedMaterial = material;
            text.text = entry.english;
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = wrapping;
            text.overflowMode = TextOverflowModes.Overflow;
            text.enableAutoSizing = true; text.fontSize = size; text.fontSizeMax = size;
            text.fontSizeMin = Mathf.Max(18, size * .55f);
            text.raycastTarget = false;
            text.enableVertexGradient = true;
            text.colorGradient = new VertexGradient(new Color(1, 1, .65f), new Color(1, 1, .65f),
                new Color(1, .66f, .05f), new Color(1, .66f, .05f));
            node.GetComponent<LocalizedGameText>().Configure(text, null, entry.key, entry.english);
        }
    }
}
