using System;
using DragonLegend.Whitebox.Recovered;

namespace DragonLegend.Whitebox
{
    // UIMainView.InitSpinCount / DeleteTime / <DeleteTime>b__0.
    // The presenter supplies scaled time and UTC on every platform.
    public sealed class RecoveredSpinRecovery : IDisposable
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress player;
        private readonly PlayerData data;
        private float elapsed;
        private bool disposed;
        public bool IsRunning {get;private set;}
        public int RemainingSeconds {get;private set;}
        public event Action<int,int?> DisplayRequested;

        public RecoveredSpinRecovery(RecoveredGameplayRules rules,RecoveredPlayerProgress player,PlayerData data)
        {
            this.rules=rules;this.player=player;this.data=data;
            player.SpinCountChanged+=CountChanged;
        }
        public void Initialize(int utcNow)
        {
            if(disposed)return;
            if(player.SpinCount==rules.GetMaxSpinCount()||rules.GetConfigType()!="default") {ShowCount();return;}
            if(player.SpinCount>=rules.GetMaxSpinCount())return;
            int difference=unchecked(utcNow-data.LastSpinTime);
            int cd=rules.GetSpinCD(player.Level);
            int gained=Divide(difference,cd);
            int remainder=unchecked(difference-Divide(difference,cd)*cd);
            if(gained>=1)
            {
                data.LastSpinTime=unchecked(utcNow-remainder);
                AddAndClamp(gained);
                if(player.SpinCount==rules.GetMaxSpinCount())
                {
                    ShowCount();data.LastSpinTime=utcNow;return;
                }
            }
            Begin(unchecked(rules.GetSpinCD(player.Level)-remainder));
        }
        public void Begin(int seconds)
        {
            if(disposed||seconds<1||rules.GetConfigType()!="default")return;
            RemainingSeconds=seconds;elapsed=0;IsRunning=true;ShowTimer();
        }
        public void Advance(float scaledDelta,int utcNow)
        {
            if(disposed||!IsRunning)return;
            elapsed+=scaledDelta;
            while(IsRunning&&elapsed>=1)
            {
                elapsed-=1;RemainingSeconds--;ShowTimer();
                if(RemainingSeconds!=0)continue;
                IsRunning=false;
                data.LastSpinTime=utcNow;
                AddAndClamp(1);
                if(player.SpinCount<rules.GetMaxSpinCount())Begin(rules.GetSpinCD(player.Level));
                else ShowCount();
                // The callback creates a new sequence; it begins on a subsequent update.
                return;
            }
        }
        private void AddAndClamp(int amount)
        {
            player.SetSpinCount(unchecked(player.SpinCount+amount));
            player.SetSpinCount(Math.Max(0,Math.Min(player.SpinCount,rules.GetMaxSpinCount())));
        }
        private void CountChanged(int ignored)
        {
            if(player.SpinCount!=rules.GetMaxSpinCount())return;
            IsRunning=false;elapsed=0;ShowCount();
        }
        public void ShowCount()=>DisplayRequested?.Invoke(player.SpinCount,null);
        private void ShowTimer()=>DisplayRequested?.Invoke(player.SpinCount,RemainingSeconds);
        // ARM SDIV returns zero for a zero divisor; signed subtraction wraps.
        private static int Divide(int value,int divisor)=>divisor==0?0:unchecked((int)((long)value/divisor));
        public void Dispose()
        {
            if(disposed)return;disposed=true;IsRunning=false;
            player.SpinCountChanged-=CountChanged;DisplayRequested=null;
        }
    }
}
