using System;
using System.Collections.Generic;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // CheckPlayBonusAnim's Free branch and callback 0x23c1c54.
    public sealed class RecoveredFreeCoinScan : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeReels reels;
        [SerializeField] private float coinInterval;
        private RecoveredBonusCoinSequence sequence;
        private RecoveredFreeSpinResult result;
        private RecoveredPlayerProgress progress;
        private RecoveredBonusCollection collection;
        private Func<int> language;
        private readonly Dictionary<GameObject,float> rewards=new Dictionary<GameObject,float>(15);
        public IReadOnlyDictionary<GameObject,float> Rewards=>rewards;
        public bool IsRunning=>sequence!=null&&sequence.IsRunning;
        public Exception Error=>sequence?.Error;
        public event Action Completed;
        public void Bind(RecoveredGameplayRules rules,RecoveredPlayerProgress player,
            RecoveredFreeSpinResult source,RecoveredBonusCollection targets,Func<int> currentLanguage)
        {
            if(sequence!=null)throw new InvalidOperationException("Free coin scan is already bound.");
            progress=player??throw new ArgumentNullException(nameof(player));
            result=source??throw new ArgumentNullException(nameof(source));
            collection=targets??throw new ArgumentNullException(nameof(targets));
            language=currentLanguage??throw new ArgumentNullException(nameof(currentLanguage));
            sequence=new RecoveredBonusCoinSequence(rules,progress,coinInterval);
            reels.Controller.RoundStarted+=ClearRound;
            reels.Controller.ReelsStopped+=Begin;
        }
        public void ClearRound()=>rewards.Clear();
        public void Begin()
        {
            if(sequence==null)throw new InvalidOperationException("Free coin scan has not been bound.");
            sequence.Begin(result.GetSymbol,Present,()=>Completed?.Invoke());
        }
        private void Present(RecoveredBonusCoin reward)
        {
            var reel=reels.At(reward.Column,reward.Row);
            var target=collection.GetUnselectedTarget(reward.Column,reward.CollectionCount);
            // PlayFreeBonusAnim visits the current result list, not initial/rolling pool entries.
            var coin=reels.Specials.CurrentStoppedCoin(reel);
            if(coin==null)return;
            coin.RewardPresentation.Play(reward.Reward,language(),target);
            rewards.Add(reel.gameObject,reward.Reward);
            progress.TotalFreeSpinWin+=reward.Reward;
        }
        private void OnDestroy()
        {
            sequence?.CancelForProfileChange();
            if(reels!=null&&reels.Controller!=null) {
                reels.Controller.RoundStarted-=ClearRound;
                reels.Controller.ReelsStopped-=Begin;
            }
        }
    }
}
