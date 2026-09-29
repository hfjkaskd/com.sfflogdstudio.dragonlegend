using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredDailyTaskWindow:MonoBehaviour
    {
        [SerializeField] private Button close;
        [SerializeField] private TMP_Text timer;
        [SerializeField] private string timerFormat="Task wiil be reset after {0:00}:{1:00}:{2:00}";
        [SerializeField] private RecoveredDailyTaskItem[] items;
        [SerializeField] private RecoveredTaskRewardWindow rewardWindow;
        public GameEntry Game{get;private set;}
        private RecoveredPlayerProgress player;
        public RecoveredDailyTaskItem[] Items=>items;
        public Button CloseButton=>close;
        public event Action<Component> WindowShowRequested;
        private int remaining;
        private float elapsed;
        private bool counting;
        public int RemainingSeconds=>remaining;
        private void Awake()=>close.onClick.AddListener(()=>gameObject.SetActive(false));
        public void Bind(GameEntry game)
        {
            Game=game;GetComponent<Canvas>().worldCamera=game.GetComponent<Canvas>().worldCamera;
            rewardWindow.Bind(game);rewardWindow.WindowShowRequested+=Forward;
            player=game.PlayerProgress;player.TasksChanged+=Refresh;
            var infos=game.Rules.GetTaskInfos();
            if(infos.Count!=items.Length)throw new InvalidOperationException("Daily task prefab and configured task count differ.");
            for(int i=0;i<items.Length;i++)items[i].Bind(this,infos[i]);
        }
        private void Forward(Component window)=>WindowShowRequested?.Invoke(window);
        public void Show(){WindowShowRequested?.Invoke(this);gameObject.SetActive(true);Refresh();BeginTimer(DateTime.Now);}
        private void Refresh(){if(Game!=null)foreach(var item in items)item.Refresh();}
        public void BeginTimer(DateTime now)
        {
            if(Game.Rules.GetConfigType()!="default"){counting=false;timer.gameObject.SetActive(false);return;}
            remaining=(int)(new DateTimeOffset(now.Date.AddDays(1)).ToUnixTimeSeconds()-new DateTimeOffset(now).ToUnixTimeSeconds());
            elapsed=0;counting=true;timer.gameObject.SetActive(true);RefreshTimer();
        }
        private void RefreshTimer()=>timer.text=GameLocalization.Format(timerFormat,Game.CurrentProfile.languageType,remaining/3600,remaining/60%60,remaining%60);
        public void AdvanceTimer(float scaledDelta)
        {
            if(!counting)return;elapsed+=scaledDelta;
            while(counting&&elapsed>=1){elapsed-=1;remaining--;RefreshTimer();if(remaining==0){counting=false;timer.gameObject.SetActive(false);player.ClearDailyTasks();}}
        }
        public void Claim(RecoveredTaskInfo info)
        {
            if(!player.CanClaimTask(info.id)) {
                var record=player.FindTask(info.id);
                if(record==null||!record.isRecieve)Game.CoreRound.Tips.Show("Need to complete the task first before claim the reward.");
                return;
            }
            if(info.jump==1){if(!rewardWindow.gameObject.activeSelf)rewardWindow.Show(info,Refresh);return;}
            if(Game.PlayerProgress.TryClaimTask(info.id,out float amount)) {
                Game.Ads.PlayInterAd("iv_close","task");
                Game.CashFlight.Begin(amount,Refresh,transform,false);
            }
        }
        private void OnDestroy(){if(player!=null)player.TasksChanged-=Refresh;player=null;}
    }
}
