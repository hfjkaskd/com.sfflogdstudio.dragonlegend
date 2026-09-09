using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Connect CheckRewardCollect -> CheckFreeSpinEnd to the actual window/transition.
    public sealed class RecoveredFreeExitFlow : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeEndWindow window;
        [SerializeField] private RecoveredSceneTransition transitionPrefab;
        [SerializeField] private int generationStepsPerFrame;
        private RecoveredSceneTransition transition;
        private RecoveredFreeReels reels;
        private RecoveredFreeSpinResult result;
        private RecoveredFreeSpinEntry entry;
        private RecoveredFreeSpinExit exit;
        private IReadOnlyList<int> symbols;
        private Func<int> initialCount;
        private bool generating;
        public RecoveredFreeEndWindow Window => window;
        public RecoveredSceneTransition Transition => transition;
        public RecoveredFreeSpinExit Exit => exit;
        public Exception Error { get; private set; }
        public event Action Completed, PauseMusicRequested, BaseViewResetRequested, BaseReelsInitRequested, FreeEndFlagClearRequested;
        public event Action<string> SoundRequested, ChangeMusicRequested;
        public void Bind(RecoveredFreeReels freeReels, RecoveredPlayerProgress player, RecoveredFreeSpinResult source,
            RecoveredFreeSpinEntry spinEntry, IReadOnlyList<int> symbolIds, Func<int> originalSpinCount, Transform main, Func<int> language)
        {
            Unbind(); reels=freeReels;result=source;entry=spinEntry;symbols=symbolIds;initialCount=originalSpinCount;Error=null;
            window.Bind(player,main,language); window.SoundRequested+=Sound;window.Failed+=Fail;
            transition=Instantiate(transitionPrefab);transition.Failed+=Fail;
            exit=new RecoveredFreeSpinExit(player,entry);
            exit.PauseMusicRequested+=Pause;
            exit.EndViewRequested+=ShowEnd;
            exit.TransitionSoundRequested+=TransitionSound;
            exit.TransitionRequested+=LaunchTransition;
            exit.BaseViewResetRequested+=ResetView;exit.BaseReelsInitRequested+=ResetReels;
            exit.FreeEndFlagClearRequested+=ClearFlag;exit.BaseMusicRequested+=BaseMusic;
            reels.RewardCollect.Completed+=Check;entry.PresentationRequested+=SpinReady;
        }
        public void Check()
        {
            try { exit.Check(initialCount(),symbols);generating=result.IsGenerating;if(generating)AdvanceGeneration();else if(!exit.IsWaiting)Completed?.Invoke(); }
            catch(Exception error){Fail(error);}
        }
        private void SpinReady()=>reels.Controller.Begin();
        private void Update(){if(generating)AdvanceGeneration();}
        private void AdvanceGeneration()
        {
            try {
                for(int i=0;i<generationStepsPerFrame&&result.IsGenerating;i++)result.Step();
                if(result.IsGenerating)return;
                generating=false;Completed?.Invoke();
            }catch(Exception error){Fail(error);}
        }
        private void ShowEnd(int count)=>window.Show(count,exit.CompleteEndView);
        private void LaunchTransition(){transition.Play(exit.OnTransitionEvent,exit.OnTransitionComplete);Completed?.Invoke();}
        private void Pause()=>PauseMusicRequested?.Invoke();
        private void TransitionSound()=>Sound("transform");
        private void Sound(string name)=>SoundRequested?.Invoke(name);
        private void ResetView()=>BaseViewResetRequested?.Invoke();
        private void ResetReels()=>BaseReelsInitRequested?.Invoke();
        private void ClearFlag()=>FreeEndFlagClearRequested?.Invoke();
        private void BaseMusic()=>ChangeMusicRequested?.Invoke("normalBg");
        private void Fail(Exception error){Error=error;generating=false;}
        public void Unbind()
        {
            generating=false;
            if(reels!=null&&reels.RewardCollect!=null)reels.RewardCollect.Completed-=Check;
            if(entry!=null)entry.PresentationRequested-=SpinReady;
            if(window!=null){window.SoundRequested-=Sound;window.Failed-=Fail;window.CancelForProfileChange();}
            if(transition!=null){transition.Failed-=Fail;transition.Cancel();Destroy(transition.gameObject);}
            transition=null;reels=null;entry=null;exit=null;result=null;symbols=null;initialCount=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
