using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWheelWindow : MonoBehaviour
    {
        [SerializeField] private GameObject window;
        [SerializeField] private RectTransform content;
        [SerializeField] private RecoveredWheelRotor rotor;
        [SerializeField] private RecoveredRegionAnimator frame, pointer;
        [SerializeField] private RecoveredJackpotMeter[] meters;
        [SerializeField] private RecoveredBonusRewardPopup cashPopup;
        [SerializeField] private RecoveredJackpotPopup jackpotPopup;
        [SerializeField] private float windowDuration, openingDelay, winDelay, pulseDuration, pulseScale;
        [SerializeField] private AnimationCurve enterEase, exitEase, pulseEase;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private IAdFacade ads;
        private RecoveredCashFlightPresenter cash;
        private RecoveredSpinPlayfield playfield;
        private Transform main;
        private bool isA;
        private int language, scalePhase, pulsePhase;
        private float scaleElapsed, pulseElapsed, reward;
        private Vector3 pulseFrom;
        private Transform winningItem;
        private RecoveredWheelType resultType;
        private RecoveredJackpotType jackpotType;
        private Action<float> completed;
        private RecoveredReelWait wait;
        public bool IsRunning { get; private set; }
        public Exception Error { get; private set; }
        public GameObject Window => window;
        public RecoveredWheelRotor Rotor => rotor;
        public RecoveredRegionAnimator Frame => frame;
        public RecoveredRegionAnimator Pointer => pointer;
        public RecoveredBonusRewardPopup CashPopup => cashPopup;
        public RecoveredJackpotPopup JackpotPopup => jackpotPopup;
        public RecoveredJackpotMeter Meter(int index) => meters[index];
        public event Action<string> SoundRequested, Sound1Requested;
        public event Action PauseMusicRequested, ResumeMusicRequested, StopSound1Requested;
        public event Action<int, int> CashOutTaskRefreshRequested;

        private void Awake()
        {
            rotor.SoundRequested += Sound;
            cashPopup.FlyCoinRequested += Fly; cashPopup.SoundRequested += Sound;
            jackpotPopup.FlyCoinRequested += Fly; jackpotPopup.SoundRequested += Sound;
            jackpotPopup.Sound1Requested += Sound1; jackpotPopup.PauseMusicRequested += PauseMusic;
            jackpotPopup.ResumeMusicRequested += ResumeMusic; jackpotPopup.StopSound1Requested += StopSound1;
            jackpotPopup.HideWheelRequested += HideWheel;
            jackpotPopup.CashOutTaskRefreshRequested += RefreshCashOut;
        }
        public void Bind(RecoveredPlayerProgress player, RecoveredGameplayRules config, IAdFacade facade,
            RecoveredCashFlightPresenter flights, Transform mainWindow, RecoveredSpinPlayfield mainPlayfield, bool versionA, int languageType)
        {
            progress = player; rules = config; ads = facade; cash = flights; main = mainWindow;
            playfield = mainPlayfield; isA = versionA; language = languageType;
            var camera = main.GetComponentInParent<Canvas>().worldCamera;
            foreach (var canvas in GetComponentsInChildren<Canvas>(true)) canvas.worldCamera = camera;
        }
        public void Show(Action<float> callback)
        {
            if (IsRunning) throw new InvalidOperationException("Wheel is already running.");
            Error = null; IsRunning = true; completed = callback;
            window.SetActive(true); Sound("jump"); frame.Play(0); pointer.Play(0);
            rotor.Prepare(rules, isA, language);
            var fans = rules.GetJackPot();
            for (int i = 0; i < fans.Count; i++) meters[i == 0 ? 0 : i == 1 ? 1 : 2].Initialize(i, fans[i], progress, rules, ReadBet, language);
            content.localScale = Vector3.zero; scaleElapsed = 0; scalePhase = 1;
        }
        private int ReadBet() => playfield.Bet;
        private void Update()
        {
            if (scalePhase != 0)
            {
                scaleElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(scaleElapsed / windowDuration); bool exiting = scalePhase == 2;
                content.localScale = Vector3.one * (exiting ? 1 - exitEase.Evaluate(t) : enterEase.Evaluate(t));
                if (t >= 1)
                {
                    scalePhase = 0;
                    if (exiting) window.SetActive(false);
                    else Delay(openingDelay, () => rotor.StartSpin(SpinCompleted));
                }
            }
            if (pulsePhase == 0) return;
            pulseElapsed += Time.deltaTime;
            float p = Mathf.Clamp01(pulseElapsed / pulseDuration);
            winningItem.localScale = Vector3.LerpUnclamped(pulseFrom, Vector3.one * (pulsePhase == 1 ? pulseScale : 1), pulseEase.Evaluate(p));
            if (p < 1) return;
            if (pulsePhase == 1) { pulseFrom = winningItem.localScale; pulseElapsed = 0; pulsePhase = 2; }
            else { pulsePhase = 0; OpenReward(); }
        }
        private void SpinCompleted(RecoveredWheelType type, float amount)
        {
            resultType = type; reward = amount;
            if (type == RecoveredWheelType.Cash) Sound("wheelWin");
            else
            {
                progress.SetTaskData(2, 1); PauseMusic(); Sound1("ring");
                jackpotType = type == RecoveredWheelType.Grand ? RecoveredJackpotType.Grand :
                    type == RecoveredWheelType.Mini ? RecoveredJackpotType.Minor : RecoveredJackpotType.Major;
                // Native event registration order: Main first, then this window.
                // Main's animation callback resets JpAddCount before refreshing.
                playfield.PlayJackpotWin(jackpotType);
                int index = jackpotType == RecoveredJackpotType.Grand ? 0 : jackpotType == RecoveredJackpotType.Major ? 1 : 2;
                meters[index].PlayAnim(null);
            }
            frame.Play(1); pointer.Play(1);
            Delay(winDelay, () => {
                winningItem = rotor.Item(rotor.ResultIndex).transform;
                pulseFrom = winningItem.localScale; pulseElapsed = 0; pulsePhase = 1;
            });
        }
        private void OpenReward()
        {
            if (resultType == RecoveredWheelType.Cash)
            {
                HideWheel();
                cashPopup.Show(reward, progress, rules, ads, isA, language, Paid);
            }
            else
            {
                float amount = jackpotType == RecoveredJackpotType.Grand ? progress.GrandJackPotReward :
                    jackpotType == RecoveredJackpotType.Major ? progress.MajorJackPotReward : progress.MiniJackPotReward;
                HideWheel(); StopSound1();
                jackpotPopup.Show(jackpotType, amount, progress, rules, ads, isA, language, Paid);
            }
        }
        private void HideWheel()
        {
            if (!window.activeSelf) return;
            content.localScale = Vector3.one; scaleElapsed = 0; scalePhase = 2;
        }
        private void Paid(float amount) { IsRunning = false; var callback = completed; completed = null; callback?.Invoke(amount); }
        private void Delay(float seconds, Action action) => wait = RecoveredReelWait.Delay(seconds, () => { wait = null; action(); }, Fail);
        private void Fail(Exception error) { Error = error; IsRunning = false; }
        private void Fly(float amount, Action callback) => cash.Begin(amount, callback, main, true);
        private void Sound(string name) => SoundRequested?.Invoke(name);
        private void Sound1(string name) => Sound1Requested?.Invoke(name);
        private void PauseMusic() => PauseMusicRequested?.Invoke();
        private void ResumeMusic() => ResumeMusicRequested?.Invoke();
        private void StopSound1() => StopSound1Requested?.Invoke();
        private void RefreshCashOut(int task, int amount) => CashOutTaskRefreshRequested?.Invoke(task, amount);
        private void OnDestroy()
        {
            wait?.Cancel();
            if (rotor != null) rotor.SoundRequested -= Sound;
            if (cashPopup != null) { cashPopup.FlyCoinRequested -= Fly; cashPopup.SoundRequested -= Sound; }
            if (jackpotPopup != null)
            {
                jackpotPopup.FlyCoinRequested -= Fly; jackpotPopup.SoundRequested -= Sound;
                jackpotPopup.Sound1Requested -= Sound1; jackpotPopup.PauseMusicRequested -= PauseMusic;
                jackpotPopup.ResumeMusicRequested -= ResumeMusic; jackpotPopup.StopSound1Requested -= StopSound1;
                jackpotPopup.HideWheelRequested -= HideWheel; jackpotPopup.CashOutTaskRefreshRequested -= RefreshCashOut;
            }
        }
    }
}
