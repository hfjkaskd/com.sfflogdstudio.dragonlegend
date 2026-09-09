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
        [SerializeField] private RecoveredBalancePanel balancePanelPrefab;
        private RecoveredBalancePanel balancePanel;
        [SerializeField] private RecoveredSpinPlayfield playfieldPrefab;
        [SerializeField] private RecoveredAdSimulationControls adControls;
        [SerializeField] private RecoveredCashFlightPresenter cashFlightPrefab;
        public RecoveredCashFlightPresenter CashFlight {get;private set;}
        [SerializeField] private RecoveredBonusFlow bonusFlowPrefab;
        public RecoveredBonusFlow BonusFlow {get;private set;}
        [SerializeField] private RecoveredCollectEntry collectEntryPrefab;
        public RecoveredCollectEntry CollectEntry { get; private set; }
        [SerializeField] private RecoveredMainBackground backgroundPrefab;
        public RecoveredMainBackground Background { get; private set; }
        public RecoveredBalancePanel BalancePanel=>balancePanel;
        public LocalAdFacade Ads {get;private set;}
        public RecoveredAdSimulationControls AdControls=>adControls;
        public RecoveredSpinPlayfield Playfield { get; private set; }
        private void ReleasePlayfield()
        {
            if (Background != null) { Destroy(Background.gameObject); Background = null; }
            if (CollectEntry != null) { Destroy(CollectEntry.gameObject); CollectEntry = null; }
            if(BonusFlow!=null){BonusFlow.Unbind();Destroy(BonusFlow.gameObject);BonusFlow=null;}
            if(CashFlight!=null){CashFlight.Unbind();Destroy(CashFlight.gameObject);CashFlight=null;}
            Ads?.Complete(AdOutcome.Cancelled);Ads=null;
            if(adControls!=null)adControls.Bind(null);
            if (Playfield == null) return;
            Playfield.FlyCoinRequested-=FlyCoin;
            Playfield.Unbind(); Destroy(Playfield.gameObject); Playfield = null;
        }
        private void FlyCoin(float amount,Action completed)=>CashFlight.Begin(amount,completed,transform,true);
        private Coroutine loading;
        private IEnumerator activeLoad;
        public RecoveredGameplayRules Rules { get; private set; }
        public RecoveredSlotSettlement Settlement { get; private set; }
        public RecoveredSpinResult SpinResult { get; private set; }
        public RecoveredPlayerStore PlayerStore { get; private set; }
        public RecoveredFreeSpinResult FreeSpinResult { get; private set; }
        public RecoveredPlayerProgress PlayerProgress { get; private set; }
        public RecoveredFreeSpinExit FreeSpinExit { get; private set; }
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
            if (balancePanel != null) { balancePanel.Unbind(); Destroy(balancePanel.gameObject); balancePanel = null; }
            ReleasePlayfield();
            Rules = null;
            Settlement = null;
            SpinResult = null;
            PlayerProgress = null;
            FreeSpinResult = null;
            FreeSpinEntry = null;
            FreeSpinExit = null;
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
            if (balancePanel != null) { balancePanel.Unbind(); Destroy(balancePanel.gameObject); balancePanel = null; }
            ReleasePlayfield();
            Rules = null;
            Settlement = null;
            SpinResult = null;
            PlayerProgress = null;
            FreeSpinResult = null;
            FreeSpinEntry = null;
            FreeSpinExit = null;
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
            // Native Main creates its initial tween sequence before any popup can open.
            RecoveredTreasureCardRunner.EnsureCreated();
            Rules = new RecoveredGameplayRules(loader.Value);
            Settlement = new RecoveredSlotSettlement(Rules);
            PlayerStore = new RecoveredPlayerStore();
            PlayerStore.Load(OnPlayerLoaded);
            PlayerProgress = new RecoveredPlayerProgress(Rules, PlayerStore.Save, PlayerStore.Data);
            SpinResult = new RecoveredSpinResult(Rules, Settlement, PlayerProgress);
            Ads=new LocalAdFacade();if(adControls!=null)adControls.Bind(Ads);
            SpinEntry = new RecoveredSpinEntry(Rules, PlayerStore.Data, PlayerProgress, SpinResult, PlayerStore.Save);
            RewardBranches = new RecoveredRewardBranches(Rules, PlayerProgress);
            FreeSpinResult = new RecoveredFreeSpinResult(Rules);
            FreeSpinEntry = new RecoveredFreeSpinEntry(PlayerProgress, FreeSpinResult);
            FreeSpinExit = new RecoveredFreeSpinExit(PlayerProgress, FreeSpinEntry);
            status.text = profile.countryCode + " / " + profile.profileId + "\nSpins: " + PlayerProgress.SpinCount
                + "\nLines: " + Rules.GetLines() + "\nCash tiers: " + Rules.GetCashOutCount()
                + "\n\n" + profile.evidenceNote;
            loading = null;
            if (backgroundPrefab != null) {
                Background = Instantiate(backgroundPrefab, transform, false);
                Background.Bind(GetComponent<Canvas>());
                Background.transform.SetAsFirstSibling();
                Background.Apply(PlayerProgress.GameSlotType);
            }
            if (balancePanelPrefab != null) {
                balancePanel = Instantiate(balancePanelPrefab, transform, false);
                balancePanel.Bind(PlayerProgress, Rules, profile.languageType);
            }
            if (playfieldPrefab != null) {
                Playfield = Instantiate(playfieldPrefab, transform, false);
                Playfield.Bind(SpinEntry, SpinResult, PlayerProgress, Rules, profile.isA, profile.languageType, Ads);
            }
            if(cashFlightPrefab!=null) {
                Canvas.ForceUpdateCanvases();
                balancePanel.InitializePlacement((RectTransform)transform,GetComponent<Canvas>().worldCamera);
                CashFlight=Instantiate(cashFlightPrefab,transform,false);
                CashFlight.Bind(RewardBranches,balancePanel,profile.isA);
                Playfield.FlyCoinRequested+=FlyCoin;
            }
            if(bonusFlowPrefab!=null){
                BonusFlow=Instantiate(bonusFlowPrefab,transform,false);
                BonusFlow.Bind(Playfield,PlayerProgress,Rules,CashFlight,Ads,profile.isA,profile.languageType);
            }
            if (collectEntryPrefab != null) {
                CollectEntry = Instantiate(collectEntryPrefab, transform, false);
                CollectEntry.Bind(PlayerProgress, Rules, transform, profile.isA, profile.languageType);
            }
            Ready?.Invoke(Rules);
        }
    }
}
