using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredLuckySpinWindow : MonoBehaviour
    {
        [SerializeField] private GameObject window;
        [SerializeField] private RectTransform content;
        [SerializeField] private RecoveredRegionAnimator art;
        [SerializeField] private RecoveredLuckySpinColumn[] columns;
        [SerializeField] private RecoveredBoardShake startShake,bounceShake;
        [SerializeField] private RecoveredBonusRewardPopup popup;
        [SerializeField] private float windowDuration,openingDelay,startDelay,columnStartInterval,spinDelay,columnStopInterval,rewardDelay;
        [SerializeField] private AnimationCurve enterEase,exitEase;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private IAdFacade ads;
        private RecoveredCashFlightPresenter cash;
        private Transform main;
        private bool isA;
        private int language,scalePhase,index;
        private float scaleElapsed,reward,stopBegan;
        private Action<float> completed;
        private RecoveredReelWait wait;
        private readonly List<int> digits=new List<int>(4);
        public bool IsRunning {get;private set;}
        public bool IsSpinning {get;private set;}
        public int StoppedCount {get;private set;}
        public Exception Error {get;private set;}
        public GameObject Window=>window;
        public RecoveredBonusRewardPopup Popup=>popup;
        public RecoveredRegionAnimator Art=>art;
        public RecoveredLuckySpinColumn Column(int i)=>columns[i];
        public IReadOnlyList<int> Digits=>digits;
        public event Action<string> SoundRequested;
        public event Action<Component> WindowShowRequested;
        private void Awake()
        {
            startShake.Initialize();bounceShake.Initialize();
            popup.FlyCoinRequested+=Fly;popup.SoundRequested+=Sound;
        }
        public void Bind(RecoveredPlayerProgress player,RecoveredGameplayRules config,IAdFacade facade,
            RecoveredCashFlightPresenter flights,Transform mainWindow,bool versionA,int languageType)
        {
            progress=player;rules=config;ads=facade;cash=flights;main=mainWindow;isA=versionA;language=languageType;
            var camera=main.GetComponentInParent<Canvas>().worldCamera;
            foreach(var canvas in GetComponentsInChildren<Canvas>(true))canvas.worldCamera=camera;
        }
        public void Show(float amount,Action<float> callback)
        {
            if(IsRunning)throw new InvalidOperationException("Lucky Spin is already running.");
            Error=null;IsRunning=true;reward=amount;completed=callback;IsSpinning=false;StoppedCount=0;
            WindowShowRequested?.Invoke(window.transform);window.SetActive(true);Sound("jump");
            for(int i=0;i<columns.Length;i++)if(columns[i]!=null)columns[i].Initialize(i,9);
            art.Play(0);content.localScale=Vector3.zero;scaleElapsed=0;scalePhase=1;
        }
        private void Update()
        {
            if(scalePhase==0)return;
            scaleElapsed+=Time.deltaTime;float t=Mathf.Clamp01(scaleElapsed/windowDuration);bool exiting=scalePhase==2;
            content.localScale=Vector3.one*(exiting?1-exitEase.Evaluate(t):enterEase.Evaluate(t));
            if(t<1)return;
            scalePhase=0;
            if(exiting)window.SetActive(false);else Delay(openingDelay,StartMachine);
        }
        private void StartMachine(){art.PlayOnce(1,null);Sound("slotsSpin");Delay(startDelay,StartColumns);}
        private void StartColumns()
        {
            if(IsSpinning)return;
            IsSpinning=true;StoppedCount=0;digits.Clear();
            string text=(reward/100f).ToString("0.00",CultureInfo.InvariantCulture);
            for(int i=0;i<text.Length;i++)digits.Add(text[i]>='0'&&text[i]<='9'?text[i]-'0':10);
            startShake.Begin();index=0;StartNextColumn();
        }
        private void StartNextColumn()
        {
            while(index<columns.Length) {
                int current=index++;var column=columns[current];if(column==null||column.IsDot)continue;
                column.StartSpin();column.SetTarget(digits[current]);Delay(columnStartInterval,StartNextColumn);return;
            }
            Delay(spinDelay,BeginStops);
        }
        private void BeginStops(){stopBegan=Time.time;index=0;StopNextColumn();}
        private void StopNextColumn()
        {
            while(index<columns.Length) {
                int current=index++;var column=columns[current];if(column==null)continue;
                float delay=Mathf.Max(0,columnStopInterval*(current+1)-(Time.time-stopBegan));
                Delay(delay,()=>{column.StopRoll(Stopped,bounceShake.Begin);StopNextColumn();});return;
            }
            wait=RecoveredReelWait.Until(()=>StoppedCount>=columns.Length,()=>Delay(rewardDelay,OpenReward),Fail);
        }
        private void Stopped(RecoveredLuckySpinColumn column){StoppedCount++;Sound("reelstop2");}
        private void OpenReward()
        {
            IsSpinning=false;content.localScale=Vector3.one;scaleElapsed=0;scalePhase=2;
            // The native Hide call starts exit; showing UIRewardView does not await that exit.
            popup.Show(reward,progress,rules,ads,isA,language,value=>{IsRunning=false;var callback=completed;completed=null;callback?.Invoke(value);});
        }
        private void Delay(float seconds,Action action)=>wait=RecoveredReelWait.Delay(seconds,()=>{wait=null;action();},Fail);
        private void Fly(float amount,Action callback)=>cash.Begin(amount,callback,main,true);
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void Fail(Exception error){Error=error;IsRunning=false;}
        private void OnDestroy(){wait?.Cancel();if(popup!=null){popup.FlyCoinRequested-=Fly;popup.SoundRequested-=Sound;}}
    }
}
