namespace DragonLegend.Whitebox
{
    // Snapshot for the native bottom panel. Showing its action row does not authorize a payout.
    public readonly struct RecoveredCashOutConditions
    {
        public readonly bool HasRecord,RequiresOrderStatus,ShowActionRow,TaskComplete,WaitComplete;
        public readonly int TaskCount,TaskGoal,RemainingSeconds;
        public readonly float MissingCash;
        public RecoveredCashOutConditions(bool hasRecord,bool requiresOrderStatus,bool showActionRow,
            bool taskComplete,bool waitComplete,int taskCount,int taskGoal,int remainingSeconds,float missingCash)
        {
            HasRecord=hasRecord;RequiresOrderStatus=requiresOrderStatus;ShowActionRow=showActionRow;
            TaskComplete=taskComplete;WaitComplete=waitComplete;TaskCount=taskCount;TaskGoal=taskGoal;
            RemainingSeconds=remainingSeconds;MissingCash=missingCash;
        }
    }
}
