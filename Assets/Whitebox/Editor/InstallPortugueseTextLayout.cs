using System;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace DragonLegend.Whitebox.EditorTools
{
    public static class InstallPortugueseTextLayout
    {
        [MenuItem("Dragon Legend/Localization/Install Verified Portuguese Layouts")]
        public static void Install()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Leave Play Mode before editing prefabs.");
            var candidates = new SortedSet<string>(StringComparer.Ordinal);
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Resources/RecoveredUI" }))
                candidates.Add(AssetDatabase.GUIDToAssetPath(guid));
            var ordered = new List<string>(); var visited = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in candidates) Order(path, candidates, visited, ordered);
            int count = 0;
            foreach (string path in ordered)
            {
                var root = PrefabUtility.LoadPrefabContents(path);
                int changes = 0;
                try
                {
                    foreach (var label in root.GetComponentsInChildren<LocalizedGameText>(true))
                    {
                        var text = label.GetComponent<TMP_Text>();
                        if (text == null || text.GetComponent<LocalizedTextLayout>() != null) continue;
                        if (path.EndsWith("/MainUtility/Settings.prefab", StringComparison.Ordinal)
                            && (label.Key == "settings.on" || label.Key == "settings.off"))
                        {
                            Configure(text, 52, text.rectTransform.sizeDelta, false); changes++;
                        }
                        else if (label.Key == "reward.watch_ad" && text.GetComponentInParent<RecoveredBonusRewardPopup>(true) != null)
                        {
                            Configure(text, 44, new Vector2(900, 100), true); changes++;
                        }
                        else if (label.Key == "button.claim"
                            && path.EndsWith("/DailyTasks/Item.prefab", StringComparison.Ordinal))
                        {
                            // RESGATAR is wider than CLAIM: keep 20 px padding per side.
                            var bounds = text.rectTransform.sizeDelta;
                            bounds.x = -40;
                            Configure(text, 44, bounds, false); changes++;
                        }
                    }
                    foreach (var reward in root.GetComponentsInChildren<RecoveredBonusRewardPopup>(true))
                        changes += ConfigureRewardClaim(reward.AdvertisedText);
                    foreach (var reward in root.GetComponentsInChildren<RecoveredBigWinPopup>(true))
                        changes += ConfigureRewardClaim(reward.AdvertisedText);
                    foreach (var reward in root.GetComponentsInChildren<RecoveredJackpotPopup>(true))
                        changes += ConfigureRewardClaim(reward.AdvertisedText);
                    if (changes != 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                    count += changes;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
            Debug.Log("PORTUGUESE_TEXT_LAYOUTS_INSTALLED: " + count);
        }

        private static int ConfigureRewardClaim(TMP_Text text)
        {
            if (text == null || text.GetComponent<LocalizedTextLayout>() != null) return 0;
            // The three reward popups share this authored button size and sprite.
            // Keep the icon, RESGATAR and multiplier on one line with 24 px padding.
            var bounds = text.rectTransform.sizeDelta;
            if (text.rectTransform.anchorMin.x == 0 && text.rectTransform.anchorMax.x == 1) bounds.x = -48;
            Configure(text, 50, bounds, false);
            return 1;
        }

        private static void Configure(TMP_Text text, float size, Vector2 bounds, bool wrapping)
        {
            var layout = text.gameObject.AddComponent<LocalizedTextLayout>();
            // Configure only serializes the alternatives; it does not apply Portuguese
            // to the authored English prefab or recapture an existing English baseline.
            layout.Configure(text, size, bounds, wrapping);
            EditorUtility.SetDirty(layout);
        }
        private static void Order(string path, SortedSet<string> candidates, HashSet<string> visited, List<string> result)
        {
            if (!visited.Add(path)) return;
            foreach (string dependency in AssetDatabase.GetDependencies(path, false))
                if (dependency != path && candidates.Contains(dependency)) Order(dependency, candidates, visited, result);
            result.Add(path);
        }
    }
}
