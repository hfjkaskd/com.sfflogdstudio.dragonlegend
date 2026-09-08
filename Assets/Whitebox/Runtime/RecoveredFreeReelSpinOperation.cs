using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Free StarSpin 0x23789bc: synchronous AccCall, then Update WaitUntil(!isStop).
    public sealed class RecoveredFreeReelSpinOperation : CustomYieldInstruction,IRecoveredReelUpdateItem
    {
        private readonly RecoveredBaseReelMotion movement;
        private readonly Action<int> stopped;
        private readonly int column;
        private bool forgotten;
        public bool IsCompleted {get;private set;}
        public Exception Error {get;private set;}
        public override bool keepWaiting {get {if(IsCompleted)GetResult();return !IsCompleted;}}
        internal RecoveredFreeReelSpinOperation(RecoveredFreeReelMotion motion,float seconds,int col,Action<int> acceleration,Action<int> stop)
        {
            movement=motion.Movement;column=col;stopped=stop;
            try {motion.StartSlotSpin(seconds);acceleration?.Invoke(column);RecoveredReelStopLoop.Requeue(this);}
            catch(Exception error){Error=error;IsCompleted=true;}
        }
        public void Forget(){forgotten=true;if(IsCompleted&&Error!=null)Debug.LogException(Error);}
        public void GetResult(){if(!IsCompleted)throw new InvalidOperationException("Free startup is pending.");if(Error!=null)throw Error;}
        bool IRecoveredReelUpdateItem.Step(int frame,float delta)
        {
            if(movement.StopRequested)return true;
            try {stopped?.Invoke(column);}catch(Exception error){Error=error;}
            IsCompleted=true;if(forgotten&&Error!=null)Debug.LogException(Error);return false;
        }
    }
}
