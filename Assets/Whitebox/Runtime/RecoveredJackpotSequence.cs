using System;

namespace DragonLegend.Whitebox
{
    // UIMainView.CheckJackPot 23caa64. Icon animation and the popup run concurrently;
    // the awaited boundary is the popup's fly-coin callback, not its close animation.
    public sealed class RecoveredJackpotSequence
    {
        private readonly RecoveredRewardBranches branches;
        private readonly float delay;
        private RecoveredReelWait wait;
        private int generation;
        private bool popupCompleted;
        private Action continuation;
        public bool IsRunning { get; private set; }
        public RecoveredJackpotType Type { get; private set; }
        public float CapturedReward { get; private set; }
        public event Action<RecoveredJackpotType> WinRequested;
        public event Action PauseMusicRequested, StopSound1Requested;
        public event Action<string> Sound1Requested;
        public event Action<Exception> Failed;

        public RecoveredJackpotSequence(RecoveredRewardBranches branches, float delay)
        { this.branches = branches ?? throw new ArgumentNullException(nameof(branches)); this.delay = delay; }

        public void Begin(int wildColumns, Action<RecoveredJackpotType,float,Action<float>> show, Action completed)
        {
            if(IsRunning)throw new InvalidOperationException("Jackpot sequence is already running.");
            int token=++generation;IsRunning=true;popupCompleted=false;continuation=completed;
            try
            {
                Type=branches.CheckJackpot(wildColumns,out float reward);CapturedReward=reward;
                if(Type==RecoveredJackpotType.None){Finish();return;}
                WinRequested?.Invoke(Type);
                if(token!=generation)return;
                PauseMusicRequested?.Invoke();Sound1Requested?.Invoke("dragon3");
                if(token!=generation)return;
                wait=RecoveredReelWait.Delay(delay,()=>{
                    wait=null;StopSound1Requested?.Invoke();
                    if(token!=generation)return;
                    show(Type,CapturedReward,_=>{if(token==generation)popupCompleted=true;});
                    if(token==generation)wait=RecoveredReelWait.Until(()=>popupCompleted,Finish,Fail);
                },Fail);
            }
            catch(Exception error){Fail(error);}
        }
        private void Finish(){wait=null;IsRunning=false;var completed=continuation;continuation=null;completed?.Invoke();}
        private void Fail(Exception error){Cancel();Failed?.Invoke(error);}
        public void Cancel(){generation++;wait?.Cancel();wait=null;continuation=null;IsRunning=false;popupCompleted=false;}
    }
}
