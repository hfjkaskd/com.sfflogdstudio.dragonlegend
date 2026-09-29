using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredGmCashOutPanel:MonoBehaviour
    {
        public const string BackupKey="gm.cashout.backup.v1";
        [SerializeField] private GameEntry entry;
        [SerializeField] private RecoveredGmPanel gm;
        [SerializeField] private Button openButton;
        [SerializeField] private GameObject panel;
        [SerializeField] private Button[] actions;
        [SerializeField] private Text status;
        [SerializeField] private Text[] labels;
        [SerializeField] private string fontPath;
        [SerializeField] private string[] taskNames;
        private int tier,step;private bool success;
        private string message="先选择档位和任务，再创建测试状态。首次修改自动备份存档。";
        public Button OpenButton=>openButton;
        public Button ActionButton(int index)=>actions[index];
        public Text Status=>status;
        public bool IsOpen=>panel.activeSelf;
        private static int Now=>unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        private void Awake()
        {
            openButton.onClick.AddListener(Open);
            for(int i=0;i<actions.Length;i++){int index=i;actions[i].onClick.AddListener(()=>Execute(index));}
        }
        public void Open()
        {
            var font=Resources.Load<Font>(fontPath);foreach(var label in labels)label.font=font;
            panel.SetActive(true);Refresh();
        }
        private bool Ready()=>entry.Rules!=null&&entry.PlayerStore!=null&&entry.CoreRound!=null&&entry.CurrentProfile!=null;
        private RecoveredGmCashOutTools Tools()=>new RecoveredGmCashOutTools(entry.Rules,entry.PlayerProgress,entry.PlayerStore.Data,entry.PlayerStore.Save);
        private void Backup()
        {
            if(PlayerPrefs.HasKey(BackupKey))return;
            PlayerPrefs.SetString(BackupKey,JsonUtility.ToJson(entry.PlayerStore.Data));PlayerPrefs.Save();
        }
        private void Execute(int index)
        {
            if(index==0){panel.SetActive(false);return;}
            if(!Ready()){message="游戏正在加载，请稍后再试。";Refresh();return;}
            try {
                if(index>=6&&index<=12&&(entry.Playfield.IsBusy||entry.Ads.Pending||entry.Ads.InterstitialPending))
                    throw new InvalidOperationException("请先结束本局、领取弹窗奖励并等待广告结束，再修改测试状态。");
                var tools=Tools();int now=Now;
                switch(index){
                    case 1:tier=(tier+entry.Rules.GetCashOutCount()-1)%entry.Rules.GetCashOutCount();break;
                    case 2:tier=(tier+1)%entry.Rules.GetCashOutCount();break;
                    case 3:step=(step+6)%7;break;
                    case 4:step=(step+1)%7;break;
                    case 5:success=!success;break;
                    case 6:Backup();tools.Prepare(tier,step,success,now);message="已创建模拟状态，进度设为差 1 次，等待时间重新开始。";break;
                    case 7:Backup();tools.SetNearGoal(tier,now);message="已设置进度差 1 次。";break;
                    case 8:Backup();tools.AddProgress(tier,now,false);message="已通过游戏进度接口补 1 次；同阶段的其他提现记录也会累计。";break;
                    case 9:Backup();tools.AddProgress(tier,now,true);message="进度已补足，未修改等待时间和阶段。";break;
                    case 10:Backup();tools.SetWait(tier,now,true);message="当前任务等待时间已到期。";break;
                    case 11:Backup();tools.SetWait(tier,now,false);message="等待时间已重新开始。";break;
                    case 12:
                        if(!PlayerPrefs.HasKey(BackupKey))throw new InvalidOperationException("没有可恢复的 GM 备份。");
                        PlayerPrefs.SetString(RecoveredPlayerStore.OriginalKey,PlayerPrefs.GetString(BackupKey));PlayerPrefs.DeleteKey(BackupKey);PlayerPrefs.Save();
                        panel.SetActive(false);gm.Close();entry.Select(entry.CurrentProfile);message="已恢复首次 GM 修改前的存档。";return;
                    case 13:
                        if(entry.Playfield.IsBusy)throw new InvalidOperationException("请先结束当前游戏流程。");
                        entry.CoreRound.OpenCashOut();entry.CoreRound.CashOut.List.RefreshData();entry.CoreRound.CashOut.List.FocusTier(tier);
                        panel.SetActive(false);gm.Close();return;
                    case 14:message="已刷新。请用实际游戏行为或提现页按钮核对下方状态。";break;
                }
                if(entry.CoreRound.CashOut!=null&&entry.CoreRound.CashOut.gameObject.activeSelf){entry.CoreRound.CashOut.List.RefreshData();entry.CoreRound.CashOut.List.FocusTier(tier);}
            }catch(Exception error){message=error.Message;}
            Refresh();
        }
        public void Refresh()
        {
            if(!Ready()){status.text="提现任务验证\n等待游戏初始化。";return;}
            tier=Mathf.Clamp(tier,0,entry.Rules.GetCashOutCount()-1);
            var rules=entry.Rules;var record=Tools().Record(tier);var state=entry.PlayerProgress.GetCashOutConditions(tier,Now);
            var text=new StringBuilder();
            text.Append("提现任务验证（本地模拟，不代表真实到账）\n")
                .Append("配置：").Append(entry.CurrentProfile.profileId).Append("\n")
                .Append("档位：").Append(tier+1).Append(" / ").Append(rules.GetCashOutCount()).Append("　金额：").Append(RecoveredCurrency.Format(rules.GetCashOutCash(tier),entry.CurrentProfile.languageType,2)).Append("\n")
                .Append("待创建：").Append(taskNames[step]).Append("　模拟分支：").Append(success?"原版成功分支":"原版失败分支").Append("\n");
            if(record==null)text.Append("当前：尚未申请此档位提现。\n");
            else if(record.step==1000)text.Append("当前：任务流程结束，进入订单状态查询；不等于到账。\n");
            else {
                text.Append("当前任务：").Append(taskNames[Mathf.Clamp(record.step,0,6)]).Append(record.isCashout?"（成功分支）":"（失败分支）").Append("\n")
                    .Append("进度：").Append(state.TaskCount).Append('/').Append(state.TaskGoal).Append(state.TaskComplete?"　已达标":"　未达标").Append("\n")
                    .Append("等待：").Append(Math.Max(0,state.RemainingSeconds)).Append(" 秒").Append(state.WaitComplete?"　已到期":"　未到期").Append("\n");
                int next=rules.GetNextCashOutTaskStep(tier,record.step,record.isCashout);
                text.Append("原按钮预期：").Append(!state.TaskComplete?"提示任务未完成":!state.WaitComplete?"提示继续等待":record.step==6?"仍提示未完成（原版第 7 步行为）":next==1000?"打开账号确认，进入订单流程":"进入「"+taskNames[next]+"」").Append("\n");
            }
            text.Append("\n当前分支配置（任务目标 / 等待秒数）：\n");
            for(int i=0;i<7;i++)text.Append(taskNames[i]).Append("：").Append(i==6?rules.GetCollectInfoCount():success?rules.GetSuccessTaskCount(tier,i):rules.GetFailTaskCount(tier,i)).Append(" / ").Append(rules.GetWaitTime(tier,i)).Append("\n");
            text.Append("\n失败分支最终进入收集任务；原版第 7 步不会直接放行。\n")
                .Append("备份：").Append(PlayerPrefs.HasKey(BackupKey)?"可恢复":"首次修改时自动保存").Append("\n").Append(message);
            status.text=text.ToString();
        }
    }
}
