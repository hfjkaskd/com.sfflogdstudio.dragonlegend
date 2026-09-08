using UnityEngine;
using UnityEngine.Rendering;

namespace DragonLegend.Whitebox
{
    // Original SkeletonGraphic keeps its track time while inactive in the pool.
    public sealed class RecoveredWildColumn : MonoBehaviour
    {
        [SerializeField] private Animation[] players;
        [SerializeField] private SortingGroup sorting;
        private float[] phases;
        public Animation PlayerAt(int index)=>players[index];
        private void Awake()=>phases=new float[players.Length];
        public void Resume(int order)
        {
            sorting.sortingOrder=order;
            for(int i=0;i<players.Length;i++) {
                string clip=players[i].clip.name;players[i].Play(clip);players[i][clip].time=phases[i];players[i].Sample();
            }
        }
        public void Suspend()
        {
            for(int i=0;i<players.Length;i++)phases[i]=players[i][players[i].clip.name].time;
        }
    }
}
