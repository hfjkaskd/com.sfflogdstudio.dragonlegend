using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    // Explicit GM fixture edits. Completion is always evaluated by the production rules.
    public sealed class RecoveredGmCashOutTools
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress player;
        private readonly PlayerData data;
        private readonly Action save;
        public RecoveredGmCashOutTools(RecoveredGameplayRules config,RecoveredPlayerProgress progress,PlayerData record,Action persist)
        {rules=config;player=progress;data=record;save=persist;}
        public PlayerCashOutData Record(int tier)
        {foreach(var record in data.PlayerCashOutDatas)if(record.id==tier)return record;return null;}
        public void Prepare(int tier,int step,bool success,int now)
        {
            if(tier<0||tier>=rules.GetCashOutCount()||step<0||step>6)throw new ArgumentOutOfRangeException();
            var record=Record(tier);
            if(record==null){record=new PlayerCashOutData{id=tier,type=1};data.PlayerCashOutDatas.Add(record);}
            record.step=step;record.isCashout=success;record.count=0;record.time=now;
            data.PlayerCashOutOrders.RemoveAll(order=>order.index==tier);
            player.SetGreenCount(Math.Max(player.GreenCount,rules.GetCashOutCash(tier)));
            SetNearGoal(tier,now);save();
        }
        public void SetNearGoal(int tier,int now)
        {
            var record=Require(tier);var state=player.GetCashOutConditions(tier,now);
            int count=Math.Max(0,state.TaskGoal-1);
            if(record.step==6){data.PlayerCollectDatas.Clear();AddCollections(count);}
            else record.count=count;
            save();
        }
        public void AddProgress(int tier,int now,bool finish)
        {
            var record=Require(tier);var state=player.GetCashOutConditions(tier,now);
            int amount=finish?Math.Max(0,state.TaskGoal-state.TaskCount):1;
            if(record.step==6)AddCollections(Math.Min(rules.GetCollectInfoCount(),state.TaskCount+amount));
            else if(amount>0)player.RefreshCashOutTask(record.step,amount);
            save();
        }
        private void AddCollections(int count)
        {
            foreach(var info in rules.GetCollectInfos()){
                if(data.PlayerCollectDatas.Count>=count)break;
                bool found=false;foreach(var existing in data.PlayerCollectDatas)if(existing.id==info.id){found=true;break;}
                if(!found)player.SetCollectData(info.id,1);
            }
        }
        public void SetWait(int tier,int now,bool expire)
        {var record=Require(tier);record.time=expire?unchecked(now-rules.GetWaitTime(tier,record.step)):now;save();}
        private PlayerCashOutData Require(int tier)
        {var record=Record(tier);if(record==null||record.step==1000)throw new InvalidOperationException("请先创建任务测试状态。");return record;}
    }
}
