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
        public RecoveredSlotSettlement Settlement { get; private set; }
        public RecoveredSpinResult SpinResult { get; private set; }
        public RecoveredPlayerStore PlayerStore { get; private set; }
        public RecoveredFreeSpinResult FreeSpinResult { get; private set; }
        public RecoveredPlayerProgress PlayerProgress { get; private set; }
        public RecoveredFreeSpinEntry FreeSpinEntry { get; private set; }
        public RecoveredSpinEntry SpinEntry { get; private set; }
        public RecoveredRewardBranches RewardBranches { get; private set; }
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
            Settlement = null;
            SpinResult = null;
            PlayerProgress = null;
            FreeSpinResult = null;
            FreeSpinEntry = null;
            SpinEntry = null;
            RewardBranches = null;
            PlayerStore = null;
            CurrentProfile = null;
        }
        private void OnPlayerLoaded(int result)
        {
            if (result != 1) PlayerStore.Data.Init(Rules);
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
            Settlement = null;
            SpinResult = null;
            PlayerProgress = null;
            FreeSpinResult = null;
            FreeSpinEntry = null;
            SpinEntry = null;
            RewardBranches = null;
            PlayerStore = null;
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
            Settlement = new RecoveredSlotSettlement(Rules);
            SpinResult = new RecoveredSpinResult(Rules, Settlement);
            PlayerStore = new RecoveredPlayerStore();
            PlayerStore.Load(OnPlayerLoaded);
            PlayerProgress = new RecoveredPlayerProgress(Rules, PlayerStore.Save, PlayerStore.Data);
            SpinEntry = new RecoveredSpinEntry(Rules, PlayerStore.Data, PlayerProgress, SpinResult, PlayerStore.Save);
            RewardBranches = new RecoveredRewardBranches(Rules, PlayerProgress);
            FreeSpinResult = new RecoveredFreeSpinResult(Rules);
            FreeSpinEntry = new RecoveredFreeSpinEntry(PlayerProgress, FreeSpinResult);
            status.text = profile.countryCode + " / " + profile.profileId + "\nSpins: " + PlayerProgress.SpinCount
                + "\nLines: " + Rules.GetLines() + "\nCash tiers: " + Rules.GetCashOutCount()
                + "\n\n" + profile.evidenceNote;
            loading = null;
            Ready?.Invoke(Rules);
        }
    }
}
