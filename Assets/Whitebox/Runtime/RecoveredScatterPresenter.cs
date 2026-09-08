using System;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredScatterPresenter : MonoBehaviour
    {
        [SerializeField] private RecoveredScatterEffect prefab;
        private RecoveredBaseReelController reels;
        private ObjectPool<RecoveredScatterEffect> pool;
        private readonly RecoveredScatterEffect[,] active=new RecoveredScatterEffect[5,3];
        private readonly RecoveredScatterEffect[,] listed=new RecoveredScatterEffect[5,3];
        private readonly Action[] clearHandlers=new Action[5];
        private int order;
        public event Action<int> VibrationRequested;
        public event Action StopSoundRequested;
        public int ActiveCount=>pool==null?0:pool.CountActive;
        public int CreatedCount=>pool==null?0:pool.CountAll;
        public RecoveredScatterEffect At(int column,int row)=>active[column,row];
        public void Bind(RecoveredBaseReelController controller,int canvasOrder)
        {
            Unbind();reels=controller;order=canvasOrder+1;
            if(pool==null)pool=new ObjectPool<RecoveredScatterEffect>(()=>Instantiate(prefab,transform,false),null,
                value=>value.gameObject.SetActive(false),value=>{if(value!=null)Destroy(value.gameObject);},true,3,int.MaxValue);
            for(int i=0;i<5;i++) {int column=i;if(clearHandlers[i]==null)clearHandlers[i]=()=>ClearColumn(column);reels.ReelAt(i).EffectsClearRequested+=clearHandlers[i];}
        }
        public void BeginColumn(int column){for(int row=0;row<3;row++)listed[column,row]=null;}
        // Called by the shared stop pass in original row order, interleaved with coins.
        public void ShowCell(int column,int row)
        {
            var reel=reels.ReelAt(column);
            if(reel.SymbolId(row)!=10||active[column,row]!=null||!reel.TryHideForEffect(row))return;
            var value=pool.Get();value.transform.localScale=prefab.transform.localScale;
            value.transform.position=reel.SymbolAt(row).Symbol.transform.position;
            value.gameObject.SetActive(true);value.SetOrder(order);value.PlayStart();
            active[column,row]=listed[column,row]=value;VibrationRequested?.Invoke(200);StopSoundRequested?.Invoke();
        }
        public void PlayScatterAnim()
        {for(int column=0;column<5;column++)for(int row=0;row<3;row++)if(listed[column,row]!=null)listed[column,row].PlayIdle(true);}
        public void PlayAllWildStopSound()=>StopSoundRequested?.Invoke();
        private void ClearColumn(int column)
        {
            for(int row=0;row<3;row++){if(active[column,row]!=null)pool.Release(active[column,row]);active[column,row]=listed[column,row]=null;}
        }
        public void Unbind()
        {if(reels==null)return;for(int i=0;i<5;i++){reels.ReelAt(i).EffectsClearRequested-=clearHandlers[i];ClearColumn(i);}reels=null;}
        private void OnDestroy(){Unbind();pool?.Clear();}
    }
}
