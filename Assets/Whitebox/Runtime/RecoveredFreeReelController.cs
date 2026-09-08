using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // AutoFreeSpin's reel phase only: launch all columns, present each stop,
    // then wait for exactly five column callbacks before reward processing.
    public sealed class RecoveredFreeReelController : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeReels reels;
        [SerializeField] private float accelerationSeconds;
        [SerializeField] private string reelStopSound;
        private RecoveredReelWait wait;
        public bool IsRunning {get;private set;}
        public int StoppedCount {get;private set;}
        public Exception Error {get;private set;}
        public event Action<string> SoundRequested;
        public event Action ShakeRequested,ReelsStopped;
        public void Begin()
        {
            if(IsRunning)throw new InvalidOperationException("Free reels are already running.");
            IsRunning=true;StoppedCount=0;Error=null;
            try {
                for(int col=0;col<5;col++)reels.ColumnAt(col).StartSpin(accelerationSeconds,null,ColumnStopped);
                wait=RecoveredReelWait.Until(()=>StoppedCount==5,Complete,error=>Error=error);
            } catch(Exception error){Error=error;}
        }
        private void ColumnStopped(int column)
        {
            StoppedCount++;reels.ColumnAt(column).PlayStopAnimation();
            SoundRequested?.Invoke(reelStopSound);ShakeRequested?.Invoke();
        }
        private void Complete(){IsRunning=false;ReelsStopped?.Invoke();}
        private void OnDestroy()=>wait?.Cancel();
    }
}
