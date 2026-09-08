using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Native startup and GM entry. Profiles are explicit local test selections;
    // they do not invent a server's unknown country-routing rules.
    public sealed class GameEntry : MonoBehaviour
    {
        [SerializeField] private LaunchProfile defaultProfile;
        [SerializeField] private LaunchProfile alternativeProfile;
        [SerializeField] private Button selectDefault;
        [SerializeField] private Button selectAlternative;
        [SerializeField] private Text status;
        private Coroutine loading;
        private IEnumerator activeLoad;
        public RecoveredGameplayRules Rules { get; private set; }
        public LaunchProfile CurrentProfile { get; private set; }
        public event Action<RecoveredGameplayRules> Ready;

        public void Configure(LaunchProfile primary, LaunchProfile alternative, Button primaryButton, Button alternativeButton, Text label)
        { defaultProfile = primary; alternativeProfile = alternative; selectDefault = primaryButton; selectAlternative = alternativeButton; status = label; }

        private void OnEnable()
        {
            selectDefault.onClick.AddListener(SelectDefault);
            selectAlternative.onClick.AddListener(SelectAlternative);
            SelectDefault();
        }
        private void OnDisable()
        {
            selectDefault.onClick.RemoveListener(SelectDefault);
            selectAlternative.onClick.RemoveListener(SelectAlternative);
            if (loading != null) StopCoroutine(loading);
            (activeLoad as IDisposable)?.Dispose();
            activeLoad = null;
            loading = null;
            Rules = null;
            CurrentProfile = null;
        }
        private void SelectDefault() => Select(defaultProfile);
        private void SelectAlternative() => Select(alternativeProfile);
        public void Select(LaunchProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (loading != null) StopCoroutine(loading);
            (activeLoad as IDisposable)?.Dispose();
            activeLoad = null;
            Rules = null;
            CurrentProfile = null;
            loading = StartCoroutine(Load(profile));
        }
        private IEnumerator Load(LaunchProfile profile)
        {
            status.text = "Loading configuration...";
            var loader = new ConfigSnapshotLoader();
            // Manually step the loader to report faults consistently on device and Editor.
            var operation = loader.Load(profile.snapshotPath);
            activeLoad = operation;
            while (true)
            {
                object pending;
                bool more;
                try { more = operation.MoveNext(); pending = more ? operation.Current : null; }
                catch (Exception error)
                {
                    status.text = "Configuration failed: " + error.Message;
                    loading = null;
                    (operation as IDisposable)?.Dispose();
                    activeLoad = null;
                    yield break;
                }
                if (!more) break;
                yield return pending;
            }
            (operation as IDisposable)?.Dispose();
            activeLoad = null;
            CurrentProfile = profile;
            Rules = new RecoveredGameplayRules(loader.Value);
            status.text = profile.countryCode + " / " + profile.profileId + "\nSpins: " + Rules.GetInitSpinCount()
                + "\nLines: " + Rules.GetLines() + "\nCash tiers: " + Rules.GetCashOutCount()
                + "\n\n" + profile.evidenceNote;
            loading = null;
            Ready?.Invoke(Rules);
        }
    }
}
