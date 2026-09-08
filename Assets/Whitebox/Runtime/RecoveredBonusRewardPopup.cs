using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBonusRewardPopup : MonoBehaviour, IRecoveredBonusRewardClaimView
    {
        [Serializable] private sealed class Reveal
        {
            public RectTransform target;
            public float delay, duration;
            public AnimationCurve ease;
            private float elapsed;
            private int phase;
            public void Begin() { target.gameObject.SetActive(false); elapsed = 0; phase = 1; }
            public void Cancel() => phase = 0;
            public void Tick(float delta)
            {
                if (phase == 0) return;
                elapsed += delta;
                if (phase == 1)
                {
                    if (elapsed < delay) return;
                    elapsed -= delay; phase = 2;
                    target.gameObject.SetActive(true); target.localScale = Vector3.zero;
                }
                float t = Mathf.Clamp01(elapsed / duration);
                target.localScale = Vector3.one * ease.Evaluate(t);
                if (t >= 1) phase = 0;
            }
        }
        [SerializeField] private RectTransform content;
        [SerializeField] private float advertisedMultiplier=2,plainMultiplier=.5f;
        [SerializeField] private Text rewardText;
        [SerializeField] private TMP_Text advertisedText, plainText;
        [SerializeField] private Button claimButton, plainButton;
        [SerializeField] private RecoveredCashOutTip cashOutTip;
        [SerializeField] private Reveal plainReveal, tipReveal;
        [SerializeField] private float windowDuration, countDuration, fromScale, toScale;
        [SerializeField] private AnimationCurve enterEase, exitEase, countEase;
        private RecoveredBonusRewardClaim claim;
        private int language, scalePhase;
        private float scaleElapsed, countElapsed, countFrom, countTo;
        private Action countCompleted;
        public event Action HideWheelRequested;
        public event Action<string> SoundRequested;
        public event Action<float, Action> FlyCoinRequested;
        public RectTransform Content => content;
        public Text RewardText => rewardText;
        public TMP_Text AdvertisedText => advertisedText;
        public TMP_Text PlainText => plainText;
        public Button ClaimButton => claimButton;
        public Button PlainButton => plainButton;
        public RecoveredCashOutTip CashOutTip => cashOutTip;
        public RecoveredBonusRewardClaim Claim => claim;
        public bool IsTransitioning => scalePhase != 0;
        private void Awake()
        {
            claimButton.onClick.AddListener(ClickClaim);
            plainButton.onClick.AddListener(ClickPlain);
        }
        private void ClickClaim() => claim.OnClickButton("ClaimBtn");
        private void ClickPlain() => claim.OnClickButton("UnPlayBtn");

        public void Show(float amount, RecoveredPlayerProgress progress,
            RecoveredGameplayRules rules, IAdFacade ads, bool isA, int languageType, Action<float> completed)
        {
            gameObject.SetActive(true);
            language=languageType;countCompleted=null;plainReveal.Cancel();tipReveal.Cancel();
            claim=new RecoveredBonusRewardClaim(ads,this,advertisedMultiplier,plainMultiplier);
            claim.BeforeShow(amount,completed);
            PlaySound("jump");
            rewardText.text=RecoveredCurrency.Format(amount,language,2);
            advertisedText.text=string.Format("<sprite name=\"tc_btn_bofang\">CLAIMx{0}",claim.AdvertisedMultiplier);
            plainText.text="Only "+RecoveredCurrency.Format(amount*claim.UnadvertisedMultiplier,language,2);
            plainText.gameObject.SetActive(true);plainReveal.Begin();
            if(cashOutTip.Initialize(progress,rules,isA,language))tipReveal.Begin();
            content.localScale=Vector3.one*fromScale;scaleElapsed=0;scalePhase=1;
        }
        private void Update()
        {
            float delta = Time.deltaTime;
            plainReveal.Tick(delta); tipReveal.Tick(delta);
            // Scale is advanced before count; a hide requested at count completion
            // starts its own next-frame tween rather than consuming delta twice.
            if (scalePhase != 0)
            {
                scaleElapsed += delta; float t = Mathf.Clamp01(scaleElapsed / windowDuration);
                bool exiting = scalePhase == 2;
                content.localScale = Vector3.one * Mathf.LerpUnclamped(exiting ? toScale : fromScale,
                    exiting ? fromScale : toScale, (exiting ? exitEase : enterEase).Evaluate(t));
                if (t >= 1)
                {
                    scalePhase = 0;
                    if (exiting) { gameObject.SetActive(false); claim.AfterHide(); return; }
                }
            }
            if (countCompleted != null)
            {
                countElapsed += delta; float t = Mathf.Clamp01(countElapsed / countDuration);
                rewardText.text = RecoveredCurrency.Format(Mathf.LerpUnclamped(countFrom, countTo, countEase.Evaluate(t)), language, 2);
                if (t >= 1) { var completed = countCompleted; countCompleted = null; completed(); }
            }
        }
        public void PlaySound(string name) => SoundRequested?.Invoke(name);
        public void CountReward(float from, float to, Action completed)
        { countFrom = from; countTo = to; countElapsed = 0; countCompleted = completed; }
        public void HideWheelIfShown() => HideWheelRequested?.Invoke();
        public void HideReward()
        { content.localScale = Vector3.one * toScale; scaleElapsed = 0; scalePhase = 2; }
        public void FlyCoin(float amount, Action completed) => FlyCoinRequested?.Invoke(amount, completed);
        private void OnDisable()
        { plainReveal.Cancel(); tipReveal.Cancel(); countCompleted = null; scalePhase = 0; }
    }
}
