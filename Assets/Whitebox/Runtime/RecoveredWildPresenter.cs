using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredWildPresenter : MonoBehaviour
    {
        [SerializeField] private RecoveredWildColumn columnPrefab;
        [SerializeField] private RecoveredWildLight lightPrefab;
        [SerializeField] private RecoveredBoardShake shake;
        private RecoveredBaseReelController reels;
        private ObjectPool<RecoveredWildColumn> columns;
        private ObjectPool<RecoveredWildLight> lights;
        private readonly RecoveredWildColumn[] active=new RecoveredWildColumn[5];
        private readonly List<RecoveredWildLight> flashes=new List<RecoveredWildLight>(3);
        private readonly Action[] clearHandlers=new Action[5];
        private int order,baseOrder;
        public RecoveredBoardShake Shake=>shake;
        public int NextSortingOrder=>order;
        public int ActiveCount=>columns==null?0:columns.CountActive;
        public int ActiveLightCount=>flashes.Count;
        public int CreatedCount=>columns==null?0:columns.CountAll;
        public RecoveredWildColumn ColumnAt(int column)=>active[column];
        public RecoveredWildLight LightAt(int index)=>flashes[index];
        public event Action<string> SoundRequested;
        public event Action<int> VibrationRequested;
        public event Action<int> Presented;
        public void Bind(RecoveredBaseReelController controller,int canvasOrder)
        {
            Unbind();reels=controller;baseOrder=order=canvasOrder+1;shake.Initialize();
            if(columns==null)columns=new ObjectPool<RecoveredWildColumn>(()=>Instantiate(columnPrefab,transform,false),null,
                value=>{value.Suspend();value.gameObject.SetActive(false);},value=>{if(value!=null)Destroy(value.gameObject);},true,5,int.MaxValue);
            if(lights==null)lights=new ObjectPool<RecoveredWildLight>(CreateLight,null,
                value=>value.gameObject.SetActive(false),value=>{if(value!=null)Destroy(value.gameObject);},true,3,int.MaxValue);
            for(int i=0;i<5;i++) {int column=i;if(clearHandlers[i]==null)clearHandlers[i]=()=>ClearColumn(column);reels.ReelAt(i).EffectsClearRequested+=clearHandlers[i];}
        }
        private RecoveredWildLight CreateLight() {var light=Instantiate(lightPrefab,transform,false);light.Completed+=ReleaseLight;return light;}
        private void ReleaseLight(RecoveredWildLight light) {flashes.Remove(light);lights.Release(light);}
        public void Present(int column)
        {
            if(ActiveCount==0&&ActiveLightCount==0)order=baseOrder;
            var reel=reels.ReelAt(column);
            reel.TryHideForEffect(0);reel.TryHideForEffect(2);
            if(reel.TryHideForEffect(1)) {
                var value=columns.Get();value.gameObject.SetActive(true);value.transform.SetAsLastSibling();
                value.transform.position=reel.SymbolAt(1).Symbol.transform.position;value.Resume(order++);active[column]=value;
                var light=lights.Get();light.gameObject.SetActive(true);light.transform.SetAsLastSibling();light.transform.position=value.transform.position;
                light.GetComponent<MeshRenderer>().sortingOrder=order++;flashes.Add(light);
                SoundRequested?.Invoke("change");if(reels==null)return;
                VibrationRequested?.Invoke(200);if(reels==null)return;
                light.Play();
            }
            shake.Begin();Presented?.Invoke(column);
        }
        private void ClearColumn(int column)
        {
            if(active[column]!=null) {columns.Release(active[column]);active[column]=null;}
            // Entry lights have their own animation completion and are not reel-owned effects.
        }
        public void Unbind()
        {
            if(shake!=null)shake.Cancel();
            for(int i=flashes.Count-1;i>=0;i--)ReleaseLight(flashes[i]);
            if(reels==null)return;
            for(int i=0;i<5;i++) {reels.ReelAt(i).EffectsClearRequested-=clearHandlers[i];ClearColumn(i);}
            reels=null;
        }
        private void OnDestroy() {Unbind();columns?.Clear();lights?.Clear();}
    }
}
