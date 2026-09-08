using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // CheckSmallGame Lucky branch: RewardPrefab pulse, then the shared UIRewardView.
    public sealed class RecoveredFreeLuckyGame : MonoBehaviour
    {
        [SerializeField] private RectTransform icon;
        [SerializeField] private RecoveredBonusRewardPopup popup;
        [SerializeField] private float scaleMultiplier,scaleDuration,popupDelay;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private IAdFacade ads;
        private RecoveredCashFlightPresenter cash;
        private Transform mainWindow;
        private bool versionA;
        private int language,phase;
        private float elapsed;
        private Vector3 initial,from;
        private RecoveredReelWait wait;
        private Action<float> callback;
        public bool IsRunning {get;private set;}
        public Exception Error {get;private set;}
        public RectTransform Icon=>icon;
        public RecoveredBonusRewardPopup Popup=>popup;
        public event Action<string> SoundRequested;
        private void Awake(){popup.FlyCoinRequested+=Fly;popup.SoundRequested+=Sound;}
        public void Bind(RecoveredPlayerProgress player,RecoveredGameplayRules config,IAdFacade adFacade,
            RecoveredCashFlightPresenter flights,Transform main,bool isA,int languageType)
        {
            progress=player;rules=config;ads=adFacade;cash=flights;mainWindow=main;versionA=isA;language=languageType;
            var camera=main.GetComponentInParent<Canvas>().worldCamera;
            foreach(var canvas in popup.GetComponentsInChildren<Canvas>(true))canvas.worldCamera=camera;
        }
        public void Begin(Vector3 source,Action<float> completed)
        {
            if(IsRunning)throw new InvalidOperationException("Lucky reward is already running.");
            IsRunning=true;Error=null;callback=completed;icon.position=source;icon.gameObject.SetActive(true);
            initial=icon.localScale;from=initial;elapsed=0;phase=1;
            wait=RecoveredReelWait.Delay(popupDelay,ShowPopup,error=>{Error=error;IsRunning=false;});
        }
        private void ShowPopup()
        {
            wait=null;icon.gameObject.SetActive(false);
            float reward=rules.GetLuckyReward();
            popup.Show(reward,progress,rules,ads,versionA,language,value=>{
                IsRunning=false;var completed=callback;callback=null;completed?.Invoke(value);
            });
        }
        private void Update()
        {
            if(phase==0)return;
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/scaleDuration),ease=1-(1-t)*(1-t);
            var target=new Vector3(initial.x,initial.y,0);if(phase==1)target*=scaleMultiplier;
            icon.localScale=Vector3.LerpUnclamped(from,target,ease);
            if(t<1)return;
            if(phase==1){phase=2;from=icon.localScale;elapsed=0;}else phase=0;
        }
        private void Fly(float amount,Action completed)=>cash.Begin(amount,completed,mainWindow,true);
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void OnDestroy(){wait?.Cancel();if(popup!=null){popup.FlyCoinRequested-=Fly;popup.SoundRequested-=Sound;}}
    }
}
