using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One-shot validation only; this file is never included in a player build.
[InitializeOnLoad]
public static class ValidateApprovedArtRuntime
{
    private const string Request = "Artifacts/ApprovedArt/runtime.request";
    private static double next;
    [Serializable] private sealed class Hit
    {
        public string path, topHit;
        public Vector2 screenCenter, size;
        public bool ownsHit, onScreen;
    }
    [Serializable] private sealed class Report
    {
        public string utc, balance, level, progress;
        public List<string> jackpots = new List<string>();
        public bool completed, helpOpenedAndClosed, settingsOpenedAndClosed;
        public List<Hit> buttons = new List<Hit>();
        public List<string> errors = new List<string>();
    }
    static ValidateApprovedArtRuntime() { EditorApplication.update += Tick; }
    private static void Tick()
    {
        if (!EditorApplication.isPlaying || EditorApplication.isCompiling || EditorApplication.timeSinceStartup < next) return;
        next = EditorApplication.timeSinceStartup + 0.5;
        if (!File.Exists(Request)) return;
        var entry = UnityEngine.Object.FindObjectOfType<GameEntry>();
        if (entry == null || entry.MainUtility == null || entry.Playfield == null || entry.BalancePanel == null) return;
        // GameEntry creates the UI one frame before dismissing the startup overlay.
        // Inspect the normal interactive screen only after that overlay has gone.
        if (UnityEngine.Object.FindObjectOfType<StartupLoadingView>() != null) return;
        File.Delete(Request);
        var report = new Report { utc = DateTime.UtcNow.ToString("o") };
        try
        {
            if (entry.Playfield.Error != null) throw entry.Playfield.Error;
            report.balance = entry.BalancePanel.BalanceText;
            report.level = entry.BalancePanel.LevelText;
            report.progress = entry.BalancePanel.ProgressText;
            for (int i = 0; i < 3; i++) {
                var meter = entry.Playfield.JackpotMeters.At(i);
                if (!meter.Label.gameObject.activeInHierarchy || !meter.Label.enabled) throw new InvalidOperationException("Jackpot amount is hidden: " + i);
                report.jackpots.Add(meter.Label.text);
            }
            if (report.balance != RecoveredCurrency.Format(entry.PlayerProgress.GreenCount, entry.CurrentProfile.languageType)) throw new InvalidOperationException("Balance display does not match player data.");
            entry.MainUtility.HelpButton.onClick.Invoke();
            if (entry.MainUtility.Help == null || !entry.MainUtility.Help.gameObject.activeInHierarchy) throw new InvalidOperationException("Help button did not open its window.");
            entry.MainUtility.Help.CloseButton.onClick.Invoke();
            report.helpOpenedAndClosed = !entry.MainUtility.Help.gameObject.activeSelf;
            entry.MainUtility.SettingsButton.onClick.Invoke();
            if (entry.MainUtility.Settings == null || !entry.MainUtility.Settings.gameObject.activeInHierarchy) throw new InvalidOperationException("Settings button did not open its window.");
            entry.MainUtility.Settings.CloseButton.onClick.Invoke();
            report.settingsOpenedAndClosed = !entry.MainUtility.Settings.gameObject.activeSelf;
            Canvas.ForceUpdateCanvases();
            var eventSystem = EventSystem.current;
            if (eventSystem == null) throw new InvalidOperationException("Missing EventSystem.");
            var results = new List<RaycastResult>(32);
            var pointer = new PointerEventData(eventSystem);
            foreach (var button in entry.GetComponentsInChildren<Button>(false))
            {
                if (!button.IsActive() || !button.IsInteractable()) continue;
                var rect = (RectTransform)button.transform;
                var canvas = button.GetComponentInParent<Canvas>();
                var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                var center = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(rect.rect.center));
                pointer.position = center;
                results.Clear();
                eventSystem.RaycastAll(pointer, results);
                var hit = new Hit {
                    path = AnimationUtility.CalculateTransformPath(button.transform, entry.transform),
                    screenCenter = center, size = rect.rect.size,
                    onScreen = center.x >= 0 && center.x <= Screen.width && center.y >= 0 && center.y <= Screen.height,
                    topHit = results.Count == 0 ? "" : results[0].gameObject.name,
                    ownsHit = results.Count > 0 && results[0].gameObject.GetComponentInParent<Button>() == button
                };
                report.buttons.Add(hit);
            }
            if (report.balance.Length == 0 || report.level.Length == 0 || report.progress.Length == 0) throw new InvalidOperationException("A dynamic header value is blank.");
        }
        catch (Exception error) { report.errors.Add(error.ToString()); Debug.LogException(error); }
        report.completed = true;
        File.WriteAllText("Artifacts/ApprovedArt/runtime-validation.json", JsonUtility.ToJson(report, true));
    }
}
