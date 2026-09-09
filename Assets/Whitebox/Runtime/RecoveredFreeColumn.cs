using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // FreeRoll 0x23b85c0 / 0x23b87c4 and callbacks 0x23b8c7c / 0x23b8d68.
    public sealed class RecoveredFreeColumn : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeReelMotion[] rows;
        [SerializeField] private int column;
        [SerializeField] private float stopDelay,stopDelayPerColumn;
        [SerializeField] private RecoveredFreeSpecials specials;
        [SerializeField] private Transform resultLayer;
        private readonly RecoveredFreeReelSpinOperation[] starts=new RecoveredFreeReelSpinOperation[3];
        private readonly RecoveredReelStopOperation[] stops=new RecoveredReelStopOperation[3];
        public int Index=>column;
        public Transform ResultLayer=>resultLayer;
        public void StartSpin(float seconds,Action<int> acceleration,Action<int> stopped)
        {
            // The original acc parameter is unused. First child AccCall schedules
            // all three stops before the other two child spins have started.
            bool issued=false;
            Action<int> beginStop=index=>{if(issued)return;issued=true;StopSpin(stopDelay+index*stopDelayPerColumn,stopped);};
            for(int i=0;i<rows.Length;i++) {
                starts[i]=rows[i].StartSpin(seconds,beginStop,null);
                starts[i].Forget();
            }
        }
        public void StopSpin(float seconds,Action<int> completed)
        {
            int count=0;
            Action<RecoveredReelView> afterRow=reel=>{
                count++;ShowFreeEffects();
                if(count==3)completed?.Invoke(column);
            };
            for(int i=0;i<rows.Length;i++) {
                var operation=rows[i].SetStop(seconds,afterRow);
                stops[i]=operation;
                operation.Continuation=()=>{if(operation.Error!=null)Debug.LogException(operation.Error);};
            }
        }
        public void PlayStopAnimation()
        {for(int i=0;i<rows.Length;i++)specials.PlayStopAnimation(rows[i].Reel);}
        public void ShowFreeEffects()
        {for(int i=0;i<rows.Length;i++)specials.ShowStoppedEffect(rows[i].Reel,resultLayer);}
        internal void AbortForProfileChange()
        {
            for(int i=0;i<starts.Length;i++) {
                starts[i]?.CancelForProfileChange();starts[i]=null;
                stops[i]?.CancelForProfileChange();stops[i]=null;
            }
            if(rows==null)return;
            for(int i=0;i<rows.Length;i++)
                if(rows[i]!=null&&rows[i].Movement!=null)rows[i].Movement.AbortForProfileChange();
        }
        private void OnDestroy()=>AbortForProfileChange();
    }
}
