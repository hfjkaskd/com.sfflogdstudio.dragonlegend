using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredBonusFlow:MonoBehaviour
    {
        [SerializeField] private RecoveredBonusWindow window;
        [SerializeField] private RecoveredBonusExit exit;
        [SerializeField] private RecoveredSceneTransition transitionPrefab;
        [SerializeField] private float ringDuration=1.8f,afterSourceDelay=.5f;
        private RecoveredSceneTransition transition;
        private RecoveredSpinPlayfield playfield;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private RecoveredCashFlightPresenter flight;
        private IAdFacade ads;
        private bool isA;
        private int language;
        private RecoveredReelWait wait;
        public bool IsRunning {get;private set;}
        public RecoveredBonusWindow Window=>window;
        public RecoveredSceneTransition Transition=>transition;
        public event Action Completed,PauseMusicRequested,StopSound1Requested;
        public event Action<string> SoundRequested,Sound1Requested,ChangeMusicRequested;
        public event Action<Exception> Failed;
        private void Awake()
        {
            exit.PauseMusicRequested+=Pause;exit.SoundRequested+=Sound;exit.ChangeMusicRequested+=Music;exit.Failed+=Fail;
            window.Failed+=Fail;
        }
        public void Bind(RecoveredSpinPlayfield field,RecoveredPlayerProgress data,RecoveredGameplayRules config,
            RecoveredCashFlightPresenter cash,IAdFacade adFacade,bool versionA,int languageType)
        {
            Unbind();playfield=field;progress=data;rules=config;flight=cash;ads=adFacade;isA=versionA;language=languageType;
            transition=Instantiate(transitionPrefab);transition.Failed+=Fail;
            playfield.SymbolSequenceCompleted+=Begin;playfield.Npc.SoundRequested+=Sound;playfield.Npc.Failed+=Fail;
            var camera=GetComponentInParent<Canvas>().worldCamera;
            foreach(var canvas in window.GetComponentsInChildren<Canvas>(true))canvas.worldCamera=camera;
        }
        public void Begin()
        {
            if(IsRunning)return;
            if(!progress.PrepareBonusGame()){Completed?.Invoke();return;}
            IsRunning=true;playfield.Npc.Show(2);Pause();Sound1Requested?.Invoke("ring");
            wait=RecoveredReelWait.Delay(ringDuration,()=>{
                wait=null;StopSound1Requested?.Invoke();Sound("transform");
                exit.Bind(window,transition,progress,SourceCompleted);
                transition.Play(()=>window.Show(progress,rules,ads,flight,ReadBet,isA,language),()=>{
                    playfield.BonusCollection.Initialize(progress.BonusArea);Music("bonusBg");
                });
            },Fail);
        }
        private int ReadBet()=>playfield.Bet;
        private void SourceCompleted()=>wait=RecoveredReelWait.Delay(afterSourceDelay,()=>{
            wait=null;IsRunning=false;Completed?.Invoke();
        },Fail);
        private void Pause()=>PauseMusicRequested?.Invoke();
        private void Sound(string value)=>SoundRequested?.Invoke(value);
        private void Music(string value)=>ChangeMusicRequested?.Invoke(value);
        private void Fail(Exception error){Unbind();Failed?.Invoke(error);}
        public void Unbind()
        {
            wait?.Cancel();wait=null;exit.Unbind();window.gameObject.SetActive(false);
            if(playfield!=null){playfield.SymbolSequenceCompleted-=Begin;playfield.Npc.SoundRequested-=Sound;playfield.Npc.Failed-=Fail;playfield.Npc.Cancel();}
            if(transition!=null){transition.Failed-=Fail;transition.Cancel();Destroy(transition.gameObject);transition=null;}
            playfield=null;progress=null;rules=null;flight=null;ads=null;IsRunning=false;
        }
        private void OnDestroy()=>Unbind();
    }
}
