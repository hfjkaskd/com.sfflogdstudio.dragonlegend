using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    // Initial CheckPlaySymbolAnim tween: getter reads bonusCount; setter changes only TempWinTxt.
    public sealed class RecoveredSymbolWinAmount : MonoBehaviour
    {
        [SerializeField] private Text label;
        [SerializeField] private Canvas sortingCanvas;
        public void SetSortingOrder(int order)=>sortingCanvas.sortingOrder=order;
        [SerializeField] private float countDuration,waitDuration;
        [SerializeField] private AnimationCurve countEase;
        private RecoveredReelWait wait;
        private Func<float> readBonus;
        private Action continuation;
        private float elapsed,start,target;
        private int language,initialFrame;
        private bool counting,started;
        public Text Label=>label;
        public bool IsRunning {get;private set;}
        public bool IsCounting=>counting;
        public float DisplayedAmount {get;private set;}
        public Exception Error {get;private set;}
        public void Bind(int languageType) {Cancel();language=languageType;label.text=string.Empty;}
        public void Begin(float lineWin,float totalWin,Func<float> bonusGetter,Action completed)
        {
            Cancel();IsRunning=true;continuation=completed;
            if(totalWin==0||lineWin==0){Finish();return;}
            readBonus=bonusGetter??throw new ArgumentNullException(nameof(bonusGetter));target=lineWin;
            elapsed=0;started=false;counting=true;initialFrame=Time.frameCount;
            wait=RecoveredReelWait.Delay(waitDuration,Finish,Failed);
        }
        private void Update()
        {
            if(!counting||Time.frameCount==initialFrame)return;
            if(!started){start=readBonus();readBonus=null;started=true;}
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/countDuration);
            DisplayedAmount=Mathf.LerpUnclamped(start,target,countEase.Evaluate(t));
            label.text=RecoveredCurrency.Format(DisplayedAmount,language,2);
            if(t>=1)counting=false;
        }
        private void Finish(){wait=null;IsRunning=false;var completed=continuation;continuation=null;completed?.Invoke();}
        private void Failed(Exception error){Cancel();Error=error;}
        public void Cancel(){wait?.Cancel();wait=null;continuation=null;readBonus=null;counting=false;IsRunning=false;Error=null;}
        private void OnDisable()=>Cancel();
    }
}
