using System.Collections;
using TMPro;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // Main.ShowCashOutTip 23bc108; independent of reward-popup CashOutTip.
    public sealed class RecoveredMainCashOutStatus : MonoBehaviour
    {
        [SerializeField] private RectTransform panel;
        [SerializeField] private TMP_Text label;
        [SerializeField] private string readyText,remainingFormat;
        [SerializeField] private float delay,scaleDuration,holdDuration;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private int language,generation;
        public RectTransform Panel=>panel;
        public TMP_Text Label=>label;
        public bool IsScheduled {get;private set;}
        public void Bind(RecoveredPlayerProgress progress,RecoveredGameplayRules config,int locale)
        {Unbind();player=progress;rules=config;language=locale;ApplyCurrent();}
        public void ApplyCurrent()
        {
            generation++;IsScheduled=false;panel.gameObject.SetActive(false);
            if(player.GameSlotType==RecoveredSlotType.Base)Schedule();
        }
        private void Schedule()
        {
            int selected=-1;
            for(int i=0;i<rules.GetCashOutCount();i++){
                bool found=false;
                for(int j=0;j<player.CashOutRecords.Count;j++)if(player.CashOutRecords[j].id==i){found=true;break;}
                if(!found){selected=i;break;}
            }
            if(selected<0){IsScheduled=false;return;}
            float goal=rules.GetCashOutCash(selected);
            label.text=goal<=player.GreenCount?readyText:string.Format(remainingFormat,
                RecoveredCurrency.Format(goal-player.GreenCount,language,2),RecoveredCurrency.Format(goal,language,0));
            IsScheduled=true;RecoveredTreasureCardRunner.Run(Animate(generation));
        }
        private IEnumerator Animate(int token)
        {
            if(this==null||token!=generation)yield break;
            float time=0;bool revealed=false;Vector3 start=panel.localScale;
            while(this!=null&&token==generation){
                time+=Time.deltaTime;
                if(time>=delay){
                    if(!revealed){revealed=true;panel.gameObject.SetActive(true);}
                    float t=time-delay;
                    if(t<scaleDuration)panel.localScale=Vector3.LerpUnclamped(start,Vector3.one,OutQuad(t/scaleDuration));
                    else if(t<scaleDuration+holdDuration)panel.localScale=Vector3.one;
                    else if(t<2*scaleDuration+holdDuration)panel.localScale=Vector3.one*(1-OutQuad((t-scaleDuration-holdDuration)/scaleDuration));
                    else {panel.localScale=Vector3.zero;panel.gameObject.SetActive(false);Schedule();yield break;}
                }
                yield return null;
            }
        }
        private static float OutQuad(float t)=>1-(1-t)*(1-t);
        public void Unbind(){generation++;IsScheduled=false;if(panel!=null)panel.gameObject.SetActive(false);player=null;rules=null;}
        private void OnDestroy()=>Unbind();
    }
}
