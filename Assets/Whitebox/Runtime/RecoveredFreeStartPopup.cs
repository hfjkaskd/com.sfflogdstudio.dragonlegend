using System;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // UIFreeSpinStart 23b1478 / 23b17f0 / 23b1bf8 / 23b1af4.
    public sealed class RecoveredFreeStartPopup : MonoBehaviour
    {
        private struct AdCount { public float elapsed; public bool counted; }
        [SerializeField] private RectTransform content;
        [SerializeField] private RectTransform fingerPrefab;
        private RectTransform finger;
        private RecoveredRegionAnimator fingerAnimator;
        public RectTransform Finger=>finger;
        [SerializeField] private RecoveredRegionAnimator dragon;
        [SerializeField] private Text countText;
        [SerializeField] private TMP_Text advertisedText,plainText;
        [SerializeField] private Button claimButton,plainButton;
        [SerializeField] private float windowDuration,countDuration,adHideDelay;
        [SerializeField] private AnimationCurve enterEase,exitEase,countEase;
        [SerializeField] private string advertisedFormat,plainCaption;
        private readonly List<AdCount> adCounts=new List<AdCount>();
        private RecoveredPlayerProgress progress;
        private IAdFacade ads;
        private Action completed;
        private int spinCount,extraCount,scalePhase;
        private float scaleElapsed;
        public event Action<string> SoundRequested;
        public event Action<Transform> FingerRequested;
        public event Action FingerHideRequested;
        public RectTransform Content=>content;
        public Text CountText=>countText;
        public TMP_Text AdvertisedText=>advertisedText;
        public TMP_Text PlainText=>plainText;
        public Button ClaimButton=>claimButton;
        public Button PlainButton=>plainButton;
        public int InitialCount=>spinCount;
        public bool IsClicked {get;private set;}
        public bool IsTransitioning=>scalePhase!=0;
        private void Awake()
        {claimButton.onClick.AddListener(Claim);plainButton.onClick.AddListener(Plain);}
        public void Show(int initialCount,RecoveredPlayerProgress player,RecoveredGameplayRules rules,IAdFacade facade,Action sourceCompleted)
        {
            progress=player;ads=facade;completed=sourceCompleted;spinCount=initialCount;
            extraCount=rules.GetExtraFreeSpins();IsClicked=false;adCounts.Clear();
            gameObject.SetActive(true);SoundRequested?.Invoke("fsstart");
            countText.text=spinCount.ToString(CultureInfo.InvariantCulture);dragon.Play(0);
            advertisedText.text=string.Format(CultureInfo.InvariantCulture,advertisedFormat,extraCount);
            plainText.text=plainCaption;ShowFinger();
            content.localScale=Vector3.zero;scaleElapsed=0;scalePhase=1;
        }
        private void Claim()
        {
            if(IsClicked)return;
            IsClicked=false;HideFinger();SoundRequested?.Invoke("click");
            ads.PlayRewardAd(AdSucceeded,()=>IsClicked=false,"freespin","freespin");
        }
        private void Plain()
        {
            if(IsClicked)return;
            IsClicked=false;HideFinger();SoundRequested?.Invoke("click");Hide();
        }
        private void ShowFinger()
        {
            if(finger==null){finger=Instantiate(fingerPrefab,advertisedText.transform,false);fingerAnimator=finger.GetComponentInChildren<RecoveredRegionAnimator>();}
            else finger.SetParent(advertisedText.transform,false);
            finger.localScale=Vector3.one;finger.anchoredPosition=Vector2.zero;finger.gameObject.SetActive(true);
            fingerAnimator.Play(0);FingerRequested?.Invoke(advertisedText.transform);
        }
        private void HideFinger(){if(finger!=null)finger.gameObject.SetActive(false);FingerHideRequested?.Invoke();}
        private void AdSucceeded()
        {
            progress.FreeSpinCount=spinCount+extraCount;
            // Each successful ad starts its own tween and independent .5-second wait.
            // The original getter still returns spinCount, never the animated label.
            adCounts.Add(new AdCount());
        }
        private void Hide()
        {content.localScale=Vector3.one;scaleElapsed=0;scalePhase=2;}
        private void Update()
        {
            float delta=Time.deltaTime;
            if(scalePhase!=0) {
                scaleElapsed+=delta;float t=Mathf.Clamp01(scaleElapsed/windowDuration);bool exiting=scalePhase==2;
                content.localScale=Vector3.one*(exiting?1-exitEase.Evaluate(t):enterEase.Evaluate(t));
                if(t>=1) {
                    scalePhase=0;
                    if(exiting) {var callback=completed;completed=null;gameObject.SetActive(false);callback?.Invoke();return;}
                }
            }
            for(int i=0;i<adCounts.Count;) {
                var job=adCounts[i];job.elapsed+=delta;
                if(!job.counted) {
                    float t=Mathf.Clamp01(job.elapsed/countDuration);
                    float value=spinCount+extraCount*countEase.Evaluate(t);
                    // IntPlugin 2430d00: float interpolation followed by round-to-even.
                    countText.text=((int)Math.Round((double)value,MidpointRounding.ToEven)).ToString(CultureInfo.InvariantCulture);
                    job.counted=t>=1;
                }
                if(job.elapsed>=adHideDelay){adCounts.RemoveAt(i);Hide();}
                else {adCounts[i]=job;i++;}
            }
        }
        private void OnDisable(){adCounts.Clear();scalePhase=0;}
    }
}
