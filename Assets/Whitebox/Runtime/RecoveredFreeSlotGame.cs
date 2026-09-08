using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // CheckSmallGame Slot entry. Task 4 is updated by the router before this call.
    public sealed class RecoveredFreeSlotGame : MonoBehaviour
    {
        [SerializeField] private RectTransform icon;
        [SerializeField] private RecoveredLuckySpinWindow window;
        [SerializeField] private float scaleMultiplier,scaleDuration,windowDelay;
        private RecoveredGameplayRules rules;
        private Vector3 initial,from;
        private float elapsed;
        private int phase;
        private RecoveredReelWait wait;
        public bool IsRunning {get;private set;}
        public Exception Error {get;private set;}
        public RectTransform Icon=>icon;
        public RecoveredLuckySpinWindow Window=>window;
        public void Bind(RecoveredPlayerProgress player,RecoveredGameplayRules config,IAdFacade ads,
            RecoveredCashFlightPresenter cash,Transform main,bool isA,int language)
        {rules=config;window.Bind(player,config,ads,cash,main,isA,language);}
        public void Begin(Vector3 source,Action<float> completed)
        {
            if(IsRunning)throw new InvalidOperationException("Slot entry is already running.");
            IsRunning=true;Error=null;icon.position=source;icon.gameObject.SetActive(true);
            initial=icon.localScale;from=initial;elapsed=0;phase=1;
            wait=RecoveredReelWait.Delay(windowDelay,()=>{
                wait=null;icon.gameObject.SetActive(false);
                window.Show(rules.GetSlotReward(),value=>{IsRunning=false;completed?.Invoke(value);});
            },error=>{Error=error;IsRunning=false;});
        }
        private void Update()
        {
            if(phase==0)return;
            elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/scaleDuration),ease=1-(1-t)*(1-t);
            var target=new Vector3(initial.x,initial.y,0);if(phase==1)target*=scaleMultiplier;
            icon.localScale=Vector3.LerpUnclamped(from,target,ease);
            if(t<1)return;
            if(phase==1){phase=2;from=icon.localScale;elapsed=0;}else phase=0;
        }
        private void OnDestroy()=>wait?.Cancel();
    }
}
