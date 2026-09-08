using System;
using UnityEngine;
using UnityEngine.UI;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredFreeBall : MonoBehaviour
    {
        [Serializable] private sealed class Clips {public string idle,start;}
        [SerializeField] private RecoveredWorldAnimation art;
        [SerializeField] private Text reward;
        [SerializeField] private Clips[] types;
        public int BallType { get; private set; }
        public RecoveredWorldAnimation Art=>art;
        public Text Reward=>reward;
        private Clips Current=>types[BallType==0?0:BallType==1?1:2];

        // LongzhuItem.Init 0x23ae940: isInit does not change its native behavior.
        public void Initialize(int type,bool isInit=false)
        {
            BallType=type;PlayIdle();reward.text=string.Empty;
        }
        // 0x23aeb64; completion 0x23aeef8 selects idle using the current stored type.
        public void PlayStart()=>art.Play(Current.start,false,PlayIdle);
        private void PlayIdle()=>art.Play(Current.idle,true);
    }
}
