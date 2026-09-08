using System;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    // RollReel.ShowSymbolEffect: claim the row before looking up/spawning its first effect.
    public sealed class RecoveredSymbolWinPresenter : MonoBehaviour
    {
        [SerializeField] private RecoveredWildColumn[] prefabs;
        private ObjectPool<RecoveredWildColumn>[] pools;
        private RecoveredBaseReelController reels;
        private readonly RecoveredWildColumn[] active=new RecoveredWildColumn[15];
        private readonly int[] owners=new int[15];
        private readonly Action[] clearHandlers=new Action[5];
        private int order,baseOrder;
        public int ActiveCount {get;private set;}
        public int NextSortingOrder=>order;
        public RecoveredWildColumn At(int column,int row)=>active[column*3+row];
        public void Bind(RecoveredBaseReelController controller,int canvasOrder)
        {
            Unbind();reels=controller;baseOrder=order=canvasOrder+1;
            if(pools==null) {
                pools=new ObjectPool<RecoveredWildColumn>[prefabs.Length];
                for(int i=0;i<pools.Length;i++) {
                    var prefab=prefabs[i];if(prefab==null)continue;
                    pools[i]=new ObjectPool<RecoveredWildColumn>(()=>Instantiate(prefab,transform,false),null,
                        value=>{value.Suspend();value.gameObject.SetActive(false);},
                        value=>{if(value!=null)Destroy(value.gameObject);},true,3,int.MaxValue);
                }
            }
            for(int i=0;i<5;i++) {int column=i;if(clearHandlers[i]==null)clearHandlers[i]=()=>ClearColumn(column);reels.ReelAt(i).EffectsClearRequested+=clearHandlers[i];}
        }
        public void Present(RecoveredSymbolWinSelection selection,int minimumOrder)
        {
            if(!selection.HasReward)return;
            if(ActiveCount==0)order=Math.Max(baseOrder,minimumOrder);
            for(int i=0;i<selection.Count;i++) {var cell=selection.At(i);PresentCell(cell.Column,cell.Row,cell.SymbolId);}
        }
        public void Present(int column,int row,int symbol)
        {
            if(ActiveCount==0)order=baseOrder;
            PresentCell(column,row,symbol);
        }
        private void PresentCell(int column,int row,int symbol)
        {
            var reel=reels.ReelAt(column);if(!reel.TryHideForEffect(row))return;
            if(symbol<0||symbol>=pools.Length||pools[symbol]==null)return;
            int key=column*3+row;var value=pools[symbol].Get();
            value.gameObject.SetActive(true);value.transform.SetAsLastSibling();
            value.transform.position=reel.SymbolAt(row).Symbol.transform.position;
            value.Resume(order++);active[key]=value;owners[key]=symbol;ActiveCount++;
        }
        private void ClearColumn(int column)
        {
            for(int row=0;row<3;row++) {
                int key=column*3+row;if(active[key]==null)continue;
                pools[owners[key]].Release(active[key]);active[key]=null;ActiveCount--;
            }
        }
        public void Unbind()
        {
            if(reels==null)return;
            for(int i=0;i<5;i++) {reels.ReelAt(i).EffectsClearRequested-=clearHandlers[i];ClearColumn(i);}
            reels=null;
        }
        private void OnDestroy() {Unbind();if(pools!=null)for(int i=0;i<pools.Length;i++)pools[i]?.Clear();}
    }
}
