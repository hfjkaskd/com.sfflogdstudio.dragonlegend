using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashOutItem : MonoBehaviour
    {
        [SerializeField] private Image background,icon;
        [SerializeField] private TMP_Text amountText,progressText,taskText,timeText,taskTip,detailText;
        [SerializeField] private RectTransform progress,fill,task;
        [SerializeField] private Button button;
        [SerializeField] private float progressWidth;
        [SerializeField] private string[] backgroundPaths,iconPaths,taskFormats;
        [SerializeField] private string progressing,timeFormat,expiredInitial,expiredRefresh;
        private RecoveredPlayerProgress player;
        private RecoveredGameplayRules rules;
        private Func<int> utcNow;
        private RectTransform frame;
        private int id=-1;
        private Countdown countdown;
        public Button Button=>button;
        public TMP_Text AmountText=>amountText;
        public TMP_Text ProgressText=>progressText;
        public TMP_Text TaskText=>taskText;
        public TMP_Text TimeText=>timeText;
        public RectTransform Fill=>fill;
        public event Action<string> SoundRequested;
        public event Action<int,RectTransform> SelectionRequested;
        public event Action CountdownCompleted;
        private void Awake()=>button.onClick.AddListener(()=>{SoundRequested?.Invoke("click");SelectionRequested?.Invoke(id,frame);});
        public void Bind(RecoveredPlayerProgress progressModel,RecoveredGameplayRules config,Func<int> clock)
        {player=progressModel;rules=config;utcNow=clock;}
        private PlayerCashOutData Record()
        {foreach(var record in player.CashOutRecords)if(record.id==id)return record;return null;}
        public void Initialize(int index,int selected,int paymentType,RectTransform selectionFrame,int language)
        {
            id=index;frame=selectionFrame;float target=rules.GetCashOutCash(id);
            amountText.text=RecoveredCurrency.Format(target,language,0);RefreshSelection(selected,frame);
            var record=Record();SetStyle(record==null?paymentType:record.type);
            if(record==null)
            {
                detailText.gameObject.SetActive(false);progress.gameObject.SetActive(true);task.gameObject.SetActive(false);
                progressText.text=RecoveredCurrency.Format(player.GreenCount,language,2)+"/"+RecoveredCurrency.Format(target,language,0);
                fill.sizeDelta=new Vector2(Mathf.Clamp01(player.GreenCount/target)*progressWidth,fill.rect.height);return;
            }
            progress.gameObject.SetActive(false);task.gameObject.SetActive(true);taskTip.text=progressing;
            if(record.step==1000)
            {
                taskText.gameObject.SetActive(false);timeText.gameObject.SetActive(false);
                // Native continues into SDK order lookup here. No order query or approval is synthesized.
                return;
            }
            detailText.gameObject.SetActive(false);
            int goal=record.isCashout?rules.GetSuccessTaskCount(id,record.step):rules.GetFailTaskCount(id,record.step);
            int count=record.count;
            if(record.step==6){count=player.CollectRecords.Count;goal=rules.GetCollectInfoCount();}
            taskText.text=string.Format(taskFormats[record.step>=0&&record.step<6?record.step:6],count,goal);
            RefreshTime(true);
        }
        private void SetStyle(int type)
        {
            background.sprite=Load(backgroundPaths,type);background.SetNativeSize();icon.sprite=Load(iconPaths,type);icon.SetNativeSize();
        }
        // CashOutItem.RefreshCashOutItemBg 23a526c leaves recorded payment choices alone.
        public void RefreshPaymentType(int type){if(Record()==null)SetStyle(type);}
        private static Sprite Load(string[] paths,int type)=>type>=0&&type<paths.Length&&!string.IsNullOrEmpty(paths[type])?Resources.Load<Sprite>(paths[type]):null;
        public void RefreshSelection(int selected,RectTransform sharedFrame)
        {if(selected!=id)return;sharedFrame.SetParent(transform,false);sharedFrame.gameObject.SetActive(true);sharedFrame.anchoredPosition=Vector2.zero;}
        public void RefreshTime(bool initial=false)
        {
            var record=Record();if(record==null||record.step==1000)return;
            int elapsed=unchecked(utcNow()-record.time);int remaining=unchecked(rules.GetWaitTime(id,record.step)-elapsed);
            if(remaining<=0){timeText.text=initial?expiredInitial:expiredRefresh;return;}
            FormatTime(elapsed);TimeShow(remaining);
        }
        private void FormatTime(int seconds)=>timeText.text=string.Format(timeFormat,seconds/3600,seconds%3600/60,seconds%60);
        public void TimeShow(int seconds)
        {
            if(seconds<0)return;countdown?.Cancel();countdown=null;if(seconds==0)return;
            countdown=new Countdown(this,seconds);RecoveredReelStopLoop.Requeue(countdown);
        }
        public void Cancel(){countdown?.Cancel();countdown=null;}
        private void OnDestroy()=>Cancel();
        private sealed class Countdown:IRecoveredReelUpdateItem
        {
            private readonly RecoveredCashOutItem owner;private int remaining;private float elapsed;private bool cancelled;
            public Countdown(RecoveredCashOutItem view,int seconds){owner=view;remaining=seconds;}
            public void Cancel()=>cancelled=true;
            public bool Step(int frame,float delta)
            {
                if(cancelled||owner==null)return false;elapsed+=delta;
                while(elapsed>=1&&!cancelled&&remaining>0){elapsed-=1;remaining--;owner.FormatTime(remaining);if(remaining==0)owner.CountdownCompleted?.Invoke();}
                return !cancelled&&remaining>0;
            }
        }
    }
}
