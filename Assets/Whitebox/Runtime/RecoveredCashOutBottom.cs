using System;
using DragonLegend.Whitebox.Recovered;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashOutBottom : MonoBehaviour
    {
        [SerializeField] private RectTransform line1,line2,line3,line4;
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text taskText,timeText,detailText;
        [SerializeField] private GameObject taskCheck,waitCheck;
        [SerializeField] private string[] taskFormats;
        [SerializeField] private string needPrefix,needMiddle,readyText,timeFormat,refreshTimeFormat,expiredText;
        [SerializeField] private string incompleteText,waitPrefix,missingFormat;
        private RecoveredPlayerProgress player;private RecoveredGameplayRules rules;private Func<int> clock;
        private int id,type,language,remaining;private bool taskComplete,waitComplete;private Countdown countdown;
        public Button Button=>button;
        public TMP_Text TaskText=>taskText;
        public TMP_Text TimeText=>timeText;
        public TMP_Text DetailText=>detailText;
        public bool TaskComplete=>taskComplete;
        public bool WaitComplete=>waitComplete;
        public int RemainingSeconds=>remaining;
        public event Action<string> SoundRequested,TipRequested,MissingCashRequested;
        public event Action<int,int> AccountRequested;
        public event Action<PlayerCashOutData,int> TaskContinuationRequested;
        public event Action RefreshRequested;
        private void Awake()=>button.onClick.AddListener(Click);
        public void Bind(RecoveredPlayerProgress model,RecoveredGameplayRules config,Func<int> utcClock)
        {player=model;rules=config;clock=utcClock;}
        private PlayerCashOutData Record(){foreach(var record in player.CashOutRecords)if(record.id==id)return record;return null;}
        public void Initialize(int index,int paymentType,int currencyLanguage)
        {
            id=index;type=paymentType;language=currencyLanguage;
            var state=player.GetCashOutConditions(id,clock());taskComplete=state.TaskComplete;waitComplete=state.WaitComplete;remaining=state.RemainingSeconds;
            line1.gameObject.SetActive(false);line2.gameObject.SetActive(false);line3.gameObject.SetActive(false);line4.gameObject.SetActive(false);
            if(!state.HasRecord)
            {
                detailText.text=state.ShowActionRow?readyText:needPrefix+RecoveredCurrency.Format(state.MissingCash,language,2)+needMiddle+RecoveredCurrency.Format(rules.GetCashOutCash(id),language,0);
                line4.gameObject.SetActive(true);line3.gameObject.SetActive(state.ShowActionRow);return;
            }
            if(state.RequiresOrderStatus)return; // Existing SDK handling remains outside this local panel.
            int step=Record().step;
            taskText.text=string.Format(taskFormats[step>=0&&step<6?step:6],state.TaskCount,state.TaskGoal);
            taskCheck.SetActive(taskComplete);waitCheck.SetActive(waitComplete);
            line1.gameObject.SetActive(true);line2.gameObject.SetActive(true);line3.gameObject.SetActive(true);
            if(remaining<=0)timeText.text=expiredText;else{FormatTime(remaining);TimeShow(remaining);}
        }
        private void Click()
        {
            SoundRequested?.Invoke("click");var record=Record();
            if(record==null)
            {
                float target=rules.GetCashOutCash(id);
                if(player.GreenCount>=target)AccountRequested?.Invoke(type,id);
                else MissingCashRequested?.Invoke(string.Format(missingFormat,RecoveredCurrency.Format(target-player.GreenCount,language,2)));
                return;
            }
            if(!taskComplete){TipRequested?.Invoke(incompleteText);return;}
            if(!waitComplete){TipRequested?.Invoke(waitPrefix+string.Format(timeFormat,remaining/3600,remaining%3600/60,remaining%60));return;}
            if(record.step==6){TipRequested?.Invoke(incompleteText);return;}
            TaskContinuationRequested?.Invoke(record,rules.GetNextCashOutTaskStep(id,record.step,record.isCashout));
        }
        public void RefreshTime()
        {
            var record=Record();if(record==null||record.step==1000)return;
            remaining=unchecked(rules.GetWaitTime(id,record.step)+record.time-clock());
            if(remaining<=0){timeText.text=expiredText;return;}
            // RefreshGoldTime's format uses minutes, seconds, hours, unlike InitUI.
            timeText.text=string.Format(refreshTimeFormat,remaining%3600/60,remaining%60,remaining/3600);TimeShow(remaining);
        }
        private void FormatTime(int value)=>timeText.text=string.Format(timeFormat,value/3600,value%3600/60,value%60);
        public void TimeShow(int seconds)
        {if(seconds<0)return;Cancel();if(seconds==0)return;countdown=new Countdown(this,seconds);RecoveredReelStopLoop.Requeue(countdown);}
        public void Cancel(){countdown?.Cancel();countdown=null;}
        private void OnDestroy()=>Cancel();
        private sealed class Countdown:IRecoveredReelUpdateItem
        {
            private readonly RecoveredCashOutBottom owner;private int count;private float elapsed;private bool cancelled;
            public Countdown(RecoveredCashOutBottom view,int seconds){owner=view;count=seconds;}
            public void Cancel()=>cancelled=true;
            public bool Step(int frame,float delta)
            {
                if(cancelled||owner==null)return false;elapsed+=delta;
                while(elapsed>=1&&!cancelled&&count>0){elapsed-=1;count--;owner.remaining=count;owner.FormatTime(count);if(count==0)owner.RefreshRequested?.Invoke();}
                return !cancelled&&count>0;
            }
        }
    }
}
