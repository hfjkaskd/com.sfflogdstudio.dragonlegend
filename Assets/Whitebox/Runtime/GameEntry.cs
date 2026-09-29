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
        [SerializeField] private RecoveredDailyTaskEntry dailyTaskPrefab;
        [SerializeField] private RecoveredMainUtility mainUtilityPrefab;
        public RecoveredMainUtility MainUtility{get;private set;}
        public RecoveredDailyTaskEntry DailyTasks{get;private set;}
        [SerializeField] private LaunchProfile alternativeProfile;
        [SerializeField] private Button selectDefault;
        [SerializeField] private Button selectAlternative;
        [SerializeField] private Text status;
        [SerializeField] private StartupLoadingView loadingScreenPrefab;
        private StartupLoadingView loadingScreen;
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
        [SerializeField] private RecoveredCoreRoundFlow coreRoundPrefab;
        public RecoveredCoreRoundFlow CoreRound {get;private set;}
        [SerializeField] private RecoveredCoreAudio coreAudioPrefab;
        public RecoveredCoreAudio CoreAudio {get;private set;}
        public RecoveredBalancePanel BalancePanel=>balancePanel;
        public LocalAdFacade Ads {get;private set;}
        public RecoveredAdSimulationControls AdControls=>adControls;
        public RecoveredSpinPlayfield Playfield { get; private set; }
        private void ReleasePlayfield()
        {
            if(MainUtility!=null){
                var gm=GetComponent<RecoveredGmPanel>();
                if(gm!=null&&gm.ToggleButton.transform.parent==MainUtility.transform)gm.ToggleButton.transform.SetParent(transform,false);
                Destroy(MainUtility.gameObject);MainUtility=null;
            }
            if(DailyTasks!=null){Destroy(DailyTasks.gameObject);DailyTasks=null;}
            if(CoreAudio!=null){CoreAudio.Unbind();Destroy(CoreAudio.gameObject);CoreAudio=null;}
            if(CoreRound!=null){CoreRound.Unbind();Destroy(CoreRound.gameObject);CoreRound=null;}
            if (Background != null) { Destroy(Background.gameObject); Background = null; }
            if (CollectEntry != null) { Destroy(CollectEntry.gameObject); CollectEntry = null; }
            if(BonusFlow!=null){BonusFlow.Unbind();Destroy(BonusFlow.gameObject);BonusFlow=null;}
            if(CashFlight!=null){CashFlight.Unbind();Destroy(CashFlight.gameObject);CashFlight=null;}
            Ads?.Complete(AdOutcome.Cancelled);Ads?.CompleteInterstitial(false);Ads=null;
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
        private LaunchProfile runtimeProfile;
        public event Action<RecoveredGameplayRules> Ready;

        public void Configure(LaunchProfile primary, LaunchProfile alternative, Button primaryButton, Button alternativeButton, Text label)
        { defaultProfile = primary; alternativeProfile = alternative; selectDefault = primaryButton; selectAlternative = alternativeButton; status = label; }

        private void OnEnable()
        {
            WkyRuntime.LanguageChanged += OnSdkLanguageChanged;
            selectDefault.onClick.AddListener(SelectDefault);
            selectAlternative.onClick.AddListener(SelectAlternative);
            SelectDefault();
        }
        private void OnDisable()
        {
            WkyRuntime.LanguageChanged -= OnSdkLanguageChanged;
            selectDefault.onClick.RemoveListener(SelectDefault);
            selectAlternative.onClick.RemoveListener(SelectAlternative);
            if (loading != null) StopCoroutine(loading);
            (activeLoad as IDisposable)?.Dispose();
            activeLoad = null;
            loading = null;
            if (loadingScreen != null) { Destroy(loadingScreen.gameObject); loadingScreen = null; }
            if (balancePanel != null) { balancePanel.Unbind(); Destroy(balancePanel.gameObject); balancePanel = null; }
            ReleasePlayfield();
            if (runtimeProfile != null) { Destroy(runtimeProfile); runtimeProfile = null; }
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
        private void OnSdkLanguageChanged(int language)
        {
            if (CurrentProfile != null) CurrentProfile.languageType = language;
        }
        public void Select(LaunchProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (loading != null) StopCoroutine(loading);
            (activeLoad as IDisposable)?.Dispose();
            activeLoad = null;
            if (balancePanel != null) { balancePanel.Unbind(); Destroy(balancePanel.gameObject); balancePanel = null; }
            ReleasePlayfield();
            if (runtimeProfile != null) { Destroy(runtimeProfile); runtimeProfile = null; }
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
            loading = StartCoroutine(LoadWithScreen(profile));
        }
        private IEnumerator LoadWithScreen(LaunchProfile profile)
        {
            if (loadingScreen != null) { Destroy(loadingScreen.gameObject); loadingScreen = null; }
            if (loadingScreenPrefab != null)
            {
                loadingScreen = Instantiate(loadingScreenPrefab);
                yield return loadingScreen.Prepare();
            }
            var operation = Load(profile);
            try
            {
                while (true)
                {
                    bool more;
                    object pending;
                    try { more = operation.MoveNext(); pending = more ? operation.Current : null; }
                    catch (Exception error)
                    {
                        Debug.LogException(error);
                        status.text = GameLocalization.Text("Unable to start. Please restart the game.");
                        if (loadingScreen != null) loadingScreen.ShowFailure("Unable to start. Please restart the game.");
                        yield break;
                    }
                    if (!more) break;
                    yield return pending;
                }
            }
            finally { (operation as IDisposable)?.Dispose(); loading = null; }
            // Failure paths set no completed screen stage and keep the error visible.
            if (loadingScreen != null && startupComplete)
            {
                loadingScreen.SetStage("Ready", 1f);
                yield return null;
                Destroy(loadingScreen.gameObject);
                loadingScreen = null;
            }
        }
        private bool startupComplete;
        private void SetLoadingStage(string message, float progress)
        {
            status.text = GameLocalization.Text(message);
            if (loadingScreen != null) loadingScreen.SetStage(message, progress);
        }
        private IEnumerator Load(LaunchProfile profile)
        {
            startupComplete = false;
            SetLoadingStage("Connecting...", 0.1f);
            var sdkInitialization = WkyRuntime.InitializeAsync();
            while (!sdkInitialization.IsCompleted) yield return null;
            if (sdkInitialization.IsFaulted || sdkInitialization.IsCanceled)
            {
                status.text = GameLocalization.Text("Connection failed. Please restart the game.");
                if (loadingScreen != null) loadingScreen.ShowFailure("Connection failed. Please restart the game.");
                if (sdkInitialization.Exception != null) Debug.LogException(sdkInitialization.Exception);
                loading = null;
                yield break;
            }
            profile = Instantiate(profile);
            runtimeProfile = profile;
            profile.languageType = WkyRuntime.LanguageType;
            SetLoadingStage("Loading configuration...", 0.4f);
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
                    status.text = GameLocalization.Text("Unable to load configuration. Please restart the game.");
                    Debug.LogException(error);
                    if (loadingScreen != null) loadingScreen.ShowFailure("Unable to load configuration. Please restart the game.");
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
            SetLoadingStage("Preparing game...", 0.7f);
            yield return null;
            CurrentProfile = profile;
            // Native Main creates its initial tween sequence before any popup can open.
            RecoveredTreasureCardRunner.EnsureCreated();
            Rules = new RecoveredGameplayRules(loader.Value);
            Settlement = new RecoveredSlotSettlement(Rules);
            PlayerStore = new RecoveredPlayerStore();
            PlayerStore.Load(OnPlayerLoaded);
            PlayerProgress = new RecoveredPlayerProgress(Rules, PlayerStore.Save, PlayerStore.Data);
            SpinResult = new RecoveredSpinResult(Rules, Settlement, PlayerProgress);
            var adPlayer=PlayerProgress;
            Ads=new LocalAdFacade(new RecoveredInterstitialPolicy(Rules,()=>profile.isA,()=>adPlayer.Level,onShown:()=>adPlayer.RefreshCashOutTask(1,1)),new WkyAdTransport());
            if(adControls!=null){adControls.Bind(null);adControls.gameObject.SetActive(false);}
            Ads.RewardAdCompleted+=outcome=>{if(outcome==AdOutcome.Rewarded)adPlayer.RefreshCashOutTask(1,1);};
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
                Playfield.FreeBottom.Bind(PlayerProgress, FreeSpinEntry);
                Playfield.ModeView.Bind(Background, PlayerProgress, FreeSpinResult, Playfield.Symbols);
            }
            if(balancePanel!=null){
                balancePanel.transform.SetParent(Playfield.transform,false);
                balancePanel.transform.SetAsFirstSibling();
            }
            if(cashFlightPrefab!=null) {
                Canvas.ForceUpdateCanvases();
                balancePanel.InitializePlacement((RectTransform)Playfield.transform,GetComponent<Canvas>().worldCamera);
                CashFlight=Instantiate(cashFlightPrefab,transform,false);
                CashFlight.Bind(RewardBranches,balancePanel,profile.isA);
                Playfield.FlyCoinRequested+=FlyCoin;
            }
            if(coreRoundPrefab!=null)CoreRound=Instantiate(coreRoundPrefab,transform,false);
            if(bonusFlowPrefab!=null){
                BonusFlow=Instantiate(bonusFlowPrefab,CoreRound!=null?CoreRound.PopupRoot:transform,false);
                BonusFlow.Bind(Playfield,PlayerProgress,Rules,CashFlight,Ads,profile.isA,profile.languageType);
            }
            if (collectEntryPrefab != null) {
                CollectEntry = Instantiate(collectEntryPrefab, transform, false);
                CollectEntry.Bind(PlayerProgress, Rules, transform, profile.isA, profile.languageType,CoreRound!=null?CoreRound.PopupRoot:transform);
            }
            if(dailyTaskPrefab!=null){DailyTasks=Instantiate(dailyTaskPrefab,CoreRound.CashOutEntry.SideEntriesRoot,false);DailyTasks.Bind(this);}
            if(mainUtilityPrefab!=null){
                // Header controls share the playfield safe area, including the local GM toggle.
                MainUtility=Instantiate(mainUtilityPrefab,Playfield.transform,false);MainUtility.Bind(this);
                var gm=GetComponent<RecoveredGmPanel>();
                if(gm!=null)gm.ToggleButton.transform.SetParent(MainUtility.transform,false);
            }
            if(CoreRound!=null)CoreRound.Bind(this);
            if(coreAudioPrefab!=null){CoreAudio=Instantiate(coreAudioPrefab,transform,false);CoreAudio.Bind(this);}
            Ready?.Invoke(Rules);
            startupComplete = true;
        }
    }
}
