using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Free StarSlotSpin / SetStop / ConstantSpeedRoll use the shared native motion.
    public sealed class RecoveredFreeReelMotion : MonoBehaviour
    {
        [SerializeField] private RecoveredBaseReelMotion movement;
        [SerializeField] private RecoveredReelView reel;
        [SerializeField] private float speedPixels;
        private RecoveredFreeSpecials specials;
        private int column,row;
        private Action applyResult;
        public RecoveredBaseReelMotion Movement=>movement;
        public void Bind(RecoveredFreeSpecials source,int col,int reelRow)
        {specials=source;column=col;row=reelRow;applyResult=ApplyResult;}
        public bool StartSlotSpin(float accelerationSeconds)
        {reel.ResetPresentation();return movement.StartSlotSpin(speedPixels,accelerationSeconds);}
        public RecoveredReelStopOperation SetStop(float seconds,Action<RecoveredReelView> completed=null)
            => movement.SetResultStop(seconds,applyResult,completed);
        public void RequestStop()=>movement.RequestResultStop(applyResult);
        private void ApplyResult()=>specials.ApplyStoppedResult(reel,column,row);
    }
}
