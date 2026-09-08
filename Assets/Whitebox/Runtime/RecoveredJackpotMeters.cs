using System;
using UnityEngine;
namespace DragonLegend.Whitebox
{
    public sealed class RecoveredJackpotMeters:MonoBehaviour
    {
        [SerializeField] private RecoveredJackpotMeter[] meters;
        public RecoveredJackpotMeter At(int index)=>meters[index];
        public void Initialize(RecoveredPlayerProgress progress,RecoveredGameplayRules rules,Func<int> bet,int language)
        {
            var fans=rules.GetJackPot();
            // InitJackPot enumerates configured entries: all indices beyond one reuse Mini.
            for(int i=0;i<fans.Count;i++)meters[i==0?0:i==1?1:2].Initialize(i,fans[i],progress,rules,bet,language);
        }
        public void PlayRewardAnim(){for(int i=0;i<meters.Length;i++)meters[i].PlayRewardAnim();}
        public void Cancel(){for(int i=0;i<meters.Length;i++)meters[i].Cancel();}
    }
}
