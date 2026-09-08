using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredFreeBallScan : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeReels reels;
        [SerializeField] private float npcDelay;
        private RecoveredFreeSpinResult result;
        private RecoveredPlayerProgress progress;
        private RecoveredNpcPresentation npc;
        private Transform destination,arrivalParent;
        private Func<int> language;
        private Action<int,Vector3,Action<float>> openSmallGame;
        private RecoveredBonusFlow bonus;
        private RecoveredReelWait rowWait,npcWait;
        private int position;
        private bool rowComplete;
        public bool IsRunning {get;private set;}
        public Exception Error {get;private set;}
        public event Action Completed;
        public void Bind(RecoveredFreeSpinResult source,RecoveredPlayerProgress player,RecoveredNpcPresentation actor,
            Transform target,Transform fireParent,Func<int> currentLanguage,Action<int,Vector3,Action<float>> smallGame,RecoveredBonusFlow bonusFlow=null)
        {
            if(result!=null)throw new InvalidOperationException("Free ball scan is already bound.");
            result=source??throw new ArgumentNullException(nameof(source));progress=player??throw new ArgumentNullException(nameof(player));
            npc=actor??throw new ArgumentNullException(nameof(actor));destination=target??throw new ArgumentNullException(nameof(target));
            arrivalParent=fireParent??throw new ArgumentNullException(nameof(fireParent));language=currentLanguage??throw new ArgumentNullException(nameof(currentLanguage));
            openSmallGame=smallGame??throw new ArgumentNullException(nameof(smallGame));bonus=bonusFlow;
            if(bonus!=null)bonus.Completed+=AfterBonus;
        }
        private void AfterBonus(){if(progress.GameSlotType==RecoveredSlotType.Free)Begin();}
        public void Begin()
        {
            if(result==null)throw new InvalidOperationException("Free ball scan is not bound.");
            if(IsRunning)throw new InvalidOperationException("Free ball scan is already running.");
            Error=null;position=0;IsRunning=true;
            if(result.BallAmount<=0){Finish();return;}
            Advance();
        }
        private void Advance()
        {
            rowWait=null;
            try {
                if(position==15){Finish();return;}
                var reel=reels.At(position/3,position%3);position++;rowComplete=false;
                var source=reels.Specials.CurrentStoppedBall(reel);
                if(source==null)rowComplete=true;
                else reels.Specials.FlyBall(source,destination,(copy,original)=>Arrived(reel,copy,original));
                rowWait=RecoveredReelWait.Until(()=>rowComplete,Advance,Fail);
            } catch(Exception error){Fail(error);}
        }
        private void Arrived(RecoveredReelView reel,RecoveredFreeBall copy,RecoveredFreeBall original)
        {
            copy.transform.SetParent(arrivalParent,false);npc.Show(1);
            npcWait=RecoveredReelWait.Delay(npcDelay,()=>{
                npcWait=null;copy.PlayActivation(type=>{
                    var position=copy.transform.position;reels.Specials.ReleaseFlightBall(copy);
                    openSmallGame(type,position,reward=>{
                        rowComplete=true;
                        original.RewardPresentation.Play(reward,language);
                        reels.CoinScan.RecordReward(reel,reward);
                    });
                });
            },Fail);
        }
        private void Finish(){IsRunning=false;Completed?.Invoke();}
        private void Fail(Exception error){Error=error;IsRunning=false;rowWait?.Cancel();npcWait?.Cancel();}
        private void OnDestroy(){rowWait?.Cancel();npcWait?.Cancel();if(bonus!=null)bonus.Completed-=AfterBonus;}
    }
}
