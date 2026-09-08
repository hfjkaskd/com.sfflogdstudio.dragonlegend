using System;

namespace DragonLegend.Whitebox
{
    // CheckWild3 0x23d1520: every complete Wild column, with an independent .36 s delay.
    public sealed class RecoveredWildSequence
    {
        private readonly float interval;
        private Func<int,int,int> read;
        private Action<int> present,completed;
        private RecoveredReelWait wait;
        private int next;
        public int Count { get; private set; }
        public bool IsRunning { get; private set; }
        public Exception Error { get; private set; }
        public event Action<Exception> Failed;
        public RecoveredWildSequence(float interval) {if(interval<0)throw new ArgumentOutOfRangeException(nameof(interval));this.interval=interval;}
        public void Begin(Func<int,int,int> reader,Action<int> show,Action<int> onComplete)
        {
            if(IsRunning)throw new InvalidOperationException("Wild scan is already running");
            read=reader??throw new ArgumentNullException(nameof(reader));present=show??throw new ArgumentNullException(nameof(show));
            completed=onComplete??throw new ArgumentNullException(nameof(onComplete));next=0;Count=0;Error=null;IsRunning=true;Advance();
        }
        private void Advance()
        {
            wait=null;
            try {
                while(next<5) {
                    int column=next++;int a=read(column,0),b=read(column,1),c=read(column,2);
                    if(a!=7||b!=7||c!=7)continue;
                    Count++;present(column);if(!IsRunning)return;
                    wait=RecoveredReelWait.Delay(interval,Advance,Fail);return;
                }
                IsRunning=false;completed(Count);
            } catch(Exception error) {Fail(error);}
        }
        private void Fail(Exception error) {Error=error;IsRunning=false;Failed?.Invoke(error);}
        public void Cancel() {wait?.Cancel();wait=null;IsRunning=false;read=null;present=null;completed=null;}
    }
}
