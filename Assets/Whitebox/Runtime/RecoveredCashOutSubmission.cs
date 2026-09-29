using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    // Local mock integration. A pending order is never a payment approval.
    public sealed class RecoveredCashOutSubmission
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress player;
        private readonly ICashFacade cash;
        public RecoveredCashOutSubmission(RecoveredGameplayRules config,RecoveredPlayerProgress progress,ICashFacade facade)
        {rules=config;player=progress;cash=facade;}
        public PlayerCashOutOrder Submit(int tier,int type,string name,string account,int now)
        {
            if(tier<0||tier>=rules.GetCashOutCount())throw new ArgumentOutOfRangeException(nameof(tier));
            PlayerCashOutData record=null;
            foreach(var candidate in player.CashOutRecords)if(candidate.id==tier){record=candidate;break;}
            bool finishing=false;
            if(record!=null&&record.step!=1000)
            {
                var conditions=player.GetCashOutConditions(tier,now);
                finishing=conditions.TaskComplete&&conditions.WaitComplete&&record.isCashout&&
                    rules.GetNextCashOutTaskStep(tier,record.step,true)==1000;
            }
            foreach(var existing in player.CashOutOrders)
                if(existing.index==tier&&(existing.status=="pending"||(existing.status=="task_review"&&!finishing)))return existing;
            if(record!=null&&!finishing)throw new InvalidOperationException("Complete the withdrawal requirements first.");
            float amount=rules.GetCashOutCash(tier);
            if(!(player.GreenCount>=amount))throw new InvalidOperationException("Insufficient balance.");
            player.SaveCashAccount(type,name,account);
            // Persist the stable request id before the facade call; retries reuse it.
            var order=player.PrepareCashOrder(tier);
            var result=cash.Submit(order.orderId,checked((long)Math.Round((double)amount*100,MidpointRounding.AwayFromZero)));
            if(result.Outcome!=CashOutcome.Pending)throw new InvalidOperationException("Unexpected initial mock order outcome.");
            order.reviewVersion=1;
            if(finishing)player.MarkCashOrderPending(tier,type,now);
            else player.BeginCashOutReview(tier,type,now);
            return order;
        }
    }
}
