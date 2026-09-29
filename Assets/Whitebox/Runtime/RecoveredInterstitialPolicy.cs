using System;

namespace DragonLegend.Whitebox
{
    // SdkAdManager.PlayInterAd 2379904; successful SDK callback 237a198.
    public sealed class RecoveredInterstitialPolicy
    {
        private readonly RecoveredGameplayRules rules;
        private readonly Func<bool> isA;
        private readonly Func<int> level;
        private readonly Func<long> clock;
        private readonly Action onShown;
        public long LastPlayTime { get; private set; }
        public RecoveredInterstitialPolicy(RecoveredGameplayRules config,Func<bool> version,Func<int> playerLevel,Func<long> seconds=null,Action onShown=null)
        {rules=config;isA=version;level=playerLevel;clock=seconds??LocalSeconds;this.onShown=onShown;}
        // Match the native local DateTime epoch arithmetic (not UTC Unix conversion).
        private static long LocalSeconds()=>(long)(DateTime.Now-new DateTime(1970,1,1)).TotalSeconds;
        public bool CanRequest()
        {
            if(isA())return false;
            if(!rules.CheckInsertAd(level()))return false;
            if(LastPlayTime==0)LastPlayTime=clock()-rules.InsertCD();
            int cooldown=rules.InsertCD();
            return unchecked(LastPlayTime+cooldown)<=clock();
        }
        public void OnShown(){LastPlayTime=clock();onShown?.Invoke();}
    }
}
