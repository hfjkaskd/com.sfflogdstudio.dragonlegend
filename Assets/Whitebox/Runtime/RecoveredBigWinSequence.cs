using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // CheckPlaySymbolAnim: pre-window b__7, WaitUntil callback, adjusted count,
    // then common b__5 transfer. Cash credit belongs to the popup's event-1 flight.
    public sealed class RecoveredBigWinSequence : MonoBehaviour
    {
        [SerializeField] private RecoveredSymbolWinAmount amount;
        [SerializeField] private RecoveredDownWinText downWin;
        [SerializeField] private RecoveredDownWinFlight bursts;
        [SerializeField] private RecoveredOrdinaryWinSequence finalTransfer;
        [SerializeField] private RecoveredBigWinPopup popup;
        [SerializeField] private float flightDuration,flightHeight,countDuration,lineDelay,noLineDelay;
        [SerializeField] private AnimationCurve flightEase,countEase;
        private RecoveredReelWait wait;
        private RecoveredPlayerProgress progress;
        private RecoveredGameplayRules rules;
        private IAdFacade ads;
        private Func<float> bonus;
        private Action continuation;
        private RecoveredSlotWinType type;
        private bool isA,flying,counting,awaitingWindow;
        private int language,initialFrame,countFrame,generation;
        private float total,awarded,elapsed,countElapsed;
        private Vector3 origin,end,control;
        public bool IsRunning {get;private set;}
        public bool IsFlying=>flying;
        public bool AwaitingWindow=>awaitingWindow;
        public Exception Error {get;private set;}
        public event Action<Exception> Failed;
        public void Begin(RecoveredSlotWinType winType,float lineWin,float totalWin,Func<float> readBonus,
            RecoveredPlayerProgress player,RecoveredGameplayRules gameplayRules,IAdFacade adFacade,
            bool alternative,int languageType,Action completed)
        {
            Cancel();int token=++generation;IsRunning=true;type=winType;total=awarded=totalWin;
            progress=player;rules=gameplayRules;ads=adFacade;isA=alternative;language=languageType;
            bonus=readBonus;continuation=completed;origin=amount.Label.transform.position;
            try {
                awaitingWindow=true;progress.SetTaskData(1,1);
                if(token!=generation)return;
                if(lineWin!=0) {
                    amount.Cancel();end=downWin.Label.transform.position;
                    control=(origin+end)*.5f+Vector3.up*flightHeight;
                    flying=true;elapsed=0;initialFrame=Time.frameCount;
                }
                wait=RecoveredReelWait.Delay(lineWin==0?noLineDelay:lineDelay,Show,Fail);
            } catch(Exception error){Fail(error);}
        }
        private void Update()
        {
            if(flying&&Time.frameCount!=initialFrame) {
                elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/flightDuration),e=flightEase.Evaluate(t),u=1-e;
                amount.Label.transform.position=origin*(u*u)+control*(2*u*e)+end*(e*e);
                if(t>=1) {
                    flying=false;amount.Label.transform.position=origin;amount.Label.text=string.Empty;
                    bursts.PlayBurst();counting=true;countElapsed=0;countFrame=Time.frameCount;
                }
            }
            if(counting&&Time.frameCount!=countFrame) {
                countElapsed+=Time.deltaTime;float t=Mathf.Clamp01(countElapsed/countDuration);
                // b__117_10 returns zero; setter does not assign stored DownWinCount.
                downWin.ShowAmountOnly(total*countEase.Evaluate(t));if(t>=1)counting=false;
            }
        }
        private void Show()
        {
            wait=null;popup.Show(type,total,progress,rules,ads,isA,language,Returned);
            wait=RecoveredReelWait.Until(WindowReturned,Adjust,Fail);
        }
        private void Returned(float value){awarded=value;awaitingWindow=false;}
        private bool WindowReturned()=>!awaitingWindow;
        private void Adjust(){wait=null;amount.BeginAdjusted(total,awarded,Transfer);}
        private void Transfer()=>finalTransfer.BeginAfterBigWin(awarded,bonus,Complete);
        private void Complete(){IsRunning=false;var completed=continuation;continuation=null;completed?.Invoke();}
        private void Fail(Exception error){Cancel();Error=error;Failed?.Invoke(error);}
        public void Cancel()
        {
            generation++;wait?.Cancel();wait=null;
            if(flying&&amount!=null)amount.Label.transform.position=origin;
            flying=false;counting=false;awaitingWindow=false;IsRunning=false;continuation=null;Error=null;
            if(amount!=null)amount.Cancel();if(finalTransfer!=null)finalTransfer.Cancel();
            if(popup!=null)popup.gameObject.SetActive(false);
        }
        private void OnDisable()=>Cancel();
    }
}
