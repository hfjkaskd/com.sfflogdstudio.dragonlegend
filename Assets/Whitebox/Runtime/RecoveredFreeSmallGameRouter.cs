using System;
using UnityEngine;

namespace DragonLegend.Whitebox
{
    // UIMainView.CheckSmallGame 0x23cfe28: select once before the branch entry.
    public sealed class RecoveredFreeSmallGameRouter
    {
        private readonly RecoveredGameplayRules rules;
        private readonly RecoveredPlayerProgress progress;
        private readonly Action<Vector3,Action<float>> slot,wheel,treasure,lucky;
        public RecoveredFreeSmallGameRouter(RecoveredGameplayRules config,RecoveredPlayerProgress player,
            Action<Vector3,Action<float>> slotEntry,Action<Vector3,Action<float>> wheelEntry,
            Action<Vector3,Action<float>> treasureEntry,Action<Vector3,Action<float>> luckyEntry)
        {
            rules=config??throw new ArgumentNullException(nameof(config));
            progress=player??throw new ArgumentNullException(nameof(player));
            slot=slotEntry??throw new ArgumentNullException(nameof(slotEntry));
            wheel=wheelEntry??throw new ArgumentNullException(nameof(wheelEntry));
            treasure=treasureEntry??throw new ArgumentNullException(nameof(treasureEntry));
            lucky=luckyEntry??throw new ArgumentNullException(nameof(luckyEntry));
        }
        public void Open(int ballType,Vector3 position,Action<float> completed)
        {
            int column=ballType==0?0:ballType==1?1:2;
            switch(rules.GetFreeReward(column)) {
                case RecoveredFreeSpinReward.Slot:
                    progress.SetTaskData(4,1);slot(position,completed);break;
                case RecoveredFreeSpinReward.Wheel:wheel(position,completed);break;
                case RecoveredFreeSpinReward.Treasure:treasure(position,completed);break;
                case RecoveredFreeSpinReward.Lucky:lucky(position,completed);break;
            }
        }
    }
}
