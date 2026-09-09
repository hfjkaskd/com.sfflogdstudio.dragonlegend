using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // CheckFreeGame 23c90bc. Completed follows transition launch, not its callbacks.
    public sealed class RecoveredFreeEntryFlow : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeStartPopup window;
        [SerializeField] private RecoveredSceneTransition transitionPrefab;
        [SerializeField] private float ringDuration;
        [SerializeField] private int generationStepsPerFrame;
        private RecoveredSceneTransition transition;
        private RecoveredSpinPlayfield field;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private RecoveredRewardBranches branches;
        private RecoveredFreeSpinResult result;
        private RecoveredFreeSpinEntry entry;
        private IReadOnlyList<int> symbolIds;
        private IAdFacade ads;
        private RecoveredReelWait wait;
        private int generationPhase;
        public bool IsRunning {get;private set;}
        public bool IsFreeSpinEnd {get;private set;}
        public void ClearFreeEndFlag()=>IsFreeSpinEnd=false;
        public int InitialSpinCount {get;private set;}
        public RecoveredFreeStartPopup Window=>window;
        public RecoveredSceneTransition Transition=>transition;
        public event Action Completed,PauseMusicRequested,StopSound1Requested,RewardCountersResetRequested;
        public event Action InitShowRequested,InitFreeReelsRequested;
        public event Action<int> InitFreeSpinTimesRequested;
        public event Action<int,int> CashOutTaskRefreshRequested;
        public event Action<string> SoundRequested,Sound1Requested,ChangeMusicRequested;
        public event Action<Exception> Failed;
        public event Action<Component> WindowShowRequested;
        public void Bind(RecoveredSpinPlayfield playfield,RecoveredPlayerProgress player,RecoveredGameplayRules config,
            RecoveredRewardBranches rewardBranches,RecoveredFreeSpinResult freeResult,RecoveredFreeSpinEntry freeEntry,
            IReadOnlyList<int> freeSymbols,IAdFacade adFacade)
        {
            Unbind();field=playfield;progress=player;rules=config;branches=rewardBranches;result=freeResult;entry=freeEntry;symbolIds=freeSymbols;ads=adFacade;
            transition=Instantiate(transitionPrefab);transition.Failed+=Fail;
            branches.CashOutTaskRefreshRequested+=TaskRefresh;
            window.SoundRequested+=Sound;
            foreach(var canvas in window.GetComponentsInChildren<Canvas>(true))canvas.worldCamera=GetComponentInParent<Canvas>().worldCamera;
        }
        public void Begin(int scatterCount)
        {
            if(IsRunning)return;
            int count=branches.CheckFreeGame(scatterCount);
            if(count<=0){Completed?.Invoke();return;}
            IsRunning=true;IsFreeSpinEnd=true;InitialSpinCount=count;RewardCountersResetRequested?.Invoke();
            field.Scatters.PlayScatterAnim();field.Npc.Show(2);PauseMusicRequested?.Invoke();Sound1Requested?.Invoke("ring");
            wait=RecoveredReelWait.Delay(ringDuration,()=>{
                wait=null;StopSound1Requested?.Invoke();WindowShowRequested?.Invoke(window);window.Show(count,progress,rules,ads,AfterWindow);
            },Fail);
        }
        private void AfterWindow()
        {
            Sound("transform");result.Begin(symbolIds);generationPhase=1;AdvanceGeneration();
        }
        private void Update(){if(generationPhase!=0)AdvanceGeneration();}
        private void AdvanceGeneration()
        {
            try {
                for(int i=0;i<generationStepsPerFrame&&result.IsGenerating;i++)result.Step();
                if(result.IsGenerating)return;
                int phase=generationPhase;generationPhase=0;
                if(phase==1){transition.Play(Cover,AfterTransition);Completed?.Invoke();}
                else IsRunning=false;
            } catch(Exception error){Fail(error);}
        }
        private void Cover()
        {
            // Source callback 23bf73c: exact ordering under the transition cover.
            InitShowRequested?.Invoke();InitFreeReelsRequested?.Invoke();InitFreeSpinTimesRequested?.Invoke(progress.FreeSpinCount);
        }
        private void AfterTransition()
        {
            ChangeMusicRequested?.Invoke("freeBg");
            // Source 23bf75c → FreeAutoSpin: consumes one spin and generates a NEW result.
            if(entry.TryBegin(symbolIds)){generationPhase=2;AdvanceGeneration();}
            else IsRunning=false;
        }
        private void TaskRefresh(int task,int value)=>CashOutTaskRefreshRequested?.Invoke(task,value);
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void Fail(Exception error){Unbind();Failed?.Invoke(error);}
        public void Unbind()
        {
            wait?.Cancel();wait=null;generationPhase=0;
            if(branches!=null)branches.CashOutTaskRefreshRequested-=TaskRefresh;
            if(window!=null){window.SoundRequested-=Sound;window.gameObject.SetActive(false);}
            if(transition!=null){transition.Failed-=Fail;transition.Cancel();Destroy(transition.gameObject);transition=null;}
            field=null;progress=null;rules=null;branches=null;result=null;entry=null;ads=null;symbolIds=null;IsRunning=false;
        }
        private void OnDestroy()=>Unbind();
    }
}
