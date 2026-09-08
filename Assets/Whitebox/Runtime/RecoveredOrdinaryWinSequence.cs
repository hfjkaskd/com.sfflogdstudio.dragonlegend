using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // CheckPlaySymbolAnim's no-Big-Win branch and final b__5 transfer callback.
    public sealed class RecoveredOrdinaryWinSequence : MonoBehaviour
    {
        [SerializeField] private RecoveredSymbolWinAmount amount;
        [SerializeField] private RecoveredDownWinText downWin;
        [SerializeField] private float flightDuration,flightHeight,bottomCountDuration,completionDelay;
        [SerializeField] private AnimationCurve flightEase,countEase;
        private RecoveredReelWait wait;
        private Action continuation;
        private Transform labelTransform;
        private Vector3 origin,end,control;
        private float target,flightElapsed,countElapsed,countStart;
        private int generation,flightFrame,countFrame;
        private bool flying,counting,countStarted;
        public bool IsRunning {get;private set;}
        public bool IsFlying=>flying;
        public bool IsCounting=>counting;
        public Vector3 Origin=>origin;
        public Vector3 Destination=>end;
        public Vector3 Control=>control;
        public Exception Error {get;private set;}
        public event Action FlightArrived;
        public event Action<Exception> Failed;
        public void Begin(float totalWin,Func<float> readBonus,RecoveredPlayerProgress progress,Action completed)
        {
            Cancel();int token=++generation;IsRunning=true;continuation=completed;target=totalWin;
            try {
                if(totalWin==0){Complete();return;}
                labelTransform=amount.Label.transform;origin=labelTransform.position;
                // Native credit precedes the later presentation, using the latest balance.
                progress.SetGreenCount(progress.GreenCount+totalWin);
                if(token!=generation)return;
                downWin.SetTemporaryTotal(totalWin);
                if(readBonus()<totalWin) {
                    amount.Cancel();end=downWin.Label.transform.position;
                    control=(origin+end)*.5f+Vector3.up*flightHeight;
                    flightElapsed=0;flightFrame=Time.frameCount;flying=true;
                }
                wait=RecoveredReelWait.Delay(completionDelay,Complete,Fail);
            } catch(Exception error) {Fail(error);}
        }
        private void Update()
        {
            if(flying&&Time.frameCount!=flightFrame) {
                flightElapsed+=Time.deltaTime;float t=Mathf.Clamp01(flightElapsed/flightDuration),e=flightEase.Evaluate(t),u=1-e;
                labelTransform.position=origin*(u*u)+control*(2*u*e)+end*(e*e);
                if(t>=1) {
                    flying=false;labelTransform.position=origin;amount.Label.text=string.Empty;
                    // Native compares the stored DownWinCount; its tween setter changes only text.
                    if(downWin.Total!=target){counting=true;countStarted=false;countElapsed=0;countFrame=Time.frameCount;}
                    FlightArrived?.Invoke();
                }
            }
            if(counting&&Time.frameCount!=countFrame) {
                if(!countStarted){countStart=downWin.Total;countStarted=true;}
                countElapsed+=Time.deltaTime;float t=Mathf.Clamp01(countElapsed/bottomCountDuration);
                downWin.ShowAmountOnly(Mathf.LerpUnclamped(countStart,target,countEase.Evaluate(t)));
                if(t>=1)counting=false;
            }
        }
        private void Complete(){wait=null;IsRunning=false;var completed=continuation;continuation=null;completed?.Invoke();}
        private void Fail(Exception error){Cancel();Error=error;Failed?.Invoke(error);}
        public void Cancel()
        {
            generation++;wait?.Cancel();wait=null;continuation=null;
            if(flying&&labelTransform!=null)labelTransform.position=origin;
            flying=false;counting=false;IsRunning=false;Error=null;
        }
        private void OnDisable()=>Cancel();
    }
}
