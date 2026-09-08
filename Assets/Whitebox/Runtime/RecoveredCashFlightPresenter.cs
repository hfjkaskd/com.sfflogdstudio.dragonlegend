using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredCashFlightPresenter:MonoBehaviour
    {
        [SerializeField] private RecoveredCashFlightItem cashPrefab;
        [SerializeField] private RecoveredCashCollectionEffect collectionPrefab;
        [SerializeField] private Transform poolRoot;
        [SerializeField] private int preload,itemCount;
        [SerializeField] private int scatterMin,scatterMax;
        [SerializeField] private float scatterWait,departureInterval;
        private ObjectPool<RecoveredCashFlightItem> cashPool;
        private ObjectPool<RecoveredCashCollectionEffect> collectionPool;
        private RecoveredBalancePanel title;
        private RecoveredRewardBranches rewards;
        private bool isA;
        private readonly List<Batch> batches=new List<Batch>(2);
        private readonly List<RecoveredCashCollectionEffect> effects=new List<RecoveredCashCollectionEffect>(10);
        private sealed class Batch
        {
            public RecoveredCashFlightItem[] items;
            public RecoveredReelWait wait;
            public float amount;
            public Action completed;
            public int arrived;
        }
        public int ActiveCashCount=>cashPool==null?0:cashPool.CountActive;
        public int ActiveEffectCount=>effects.Count;
        public event Action<string> SoundRequested;
        public void Bind(RecoveredRewardBranches branches,RecoveredBalancePanel balance,bool versionA)
        {
            Unbind();rewards=branches;title=balance;isA=versionA;
            cashPool=new ObjectPool<RecoveredCashFlightItem>(CreateCash,null,ReleaseCash,DestroyCash,true,preload);
            collectionPool=new ObjectPool<RecoveredCashCollectionEffect>(CreateEffect,null,ReleaseEffect,DestroyEffect,true,preload);
            var cash=new RecoveredCashFlightItem[preload];var collection=new RecoveredCashCollectionEffect[preload];
            for(int i=0;i<preload;i++){cash[i]=cashPool.Get();collection[i]=collectionPool.Get();}
            for(int i=0;i<preload;i++){cashPool.Release(cash[i]);collectionPool.Release(collection[i]);}
        }
        private RecoveredCashFlightItem CreateCash()
        {
            var item=Instantiate(cashPrefab,poolRoot,false);item.Initialize(isA);item.Arrived+=Arrive;item.gameObject.SetActive(false);return item;
        }
        private RecoveredCashCollectionEffect CreateEffect()
        {
            var item=Instantiate(collectionPrefab,poolRoot,false);item.Completed+=CollectFinished;item.gameObject.SetActive(false);return item;
        }
        private void ReleaseCash(RecoveredCashFlightItem item){item.gameObject.SetActive(false);item.transform.SetParent(poolRoot,false);}
        private void ReleaseEffect(RecoveredCashCollectionEffect item){item.gameObject.SetActive(false);item.transform.SetParent(poolRoot,false);}
        private void DestroyCash(RecoveredCashFlightItem item)=>Destroy(item.gameObject);
        private void DestroyEffect(RecoveredCashCollectionEffect item)=>Destroy(item.gameObject);
        public void Begin(float amount,Action completed,Transform topWindow,bool isMainWindow,Transform source=null)
        {
            if(!isMainWindow)title.MoveToWindow(topWindow);
            var batch=new Batch{amount=amount,completed=completed,items=new RecoveredCashFlightItem[itemCount]};batches.Add(batch);
            for(int i=0;i<itemCount;i++) {
                var item=cashPool.Get();batch.items[i]=item;item.gameObject.SetActive(true);
                item.Scatter(topWindow,source,new Vector2(UnityEngine.Random.Range(scatterMin,scatterMax),UnityEngine.Random.Range(scatterMin,scatterMax)));
            }
            batch.wait=RecoveredReelWait.Delay(scatterWait,()=> {
                batch.wait=null;
                for(int i=0;i<batch.items.Length;i++)batch.items[i].Schedule(i*departureInterval,title.CashTarget);
            },Fail);
        }
        private void Fail(Exception error){Unbind();Debug.LogException(error,this);}
        private void Arrive(RecoveredCashFlightItem item)
        {
            Batch batch=null;
            for(int b=0;b<batches.Count&&batch==null;b++)for(int i=0;i<batches[b].items.Length;i++)
                if(batches[b].items[i]==item){batch=batches[b];batch.items[i]=null;break;}
            if(batch==null)throw new InvalidOperationException("Unowned cash arrival");
            batch.arrived++;cashPool.Release(item);SoundRequested?.Invoke("fly");
            var effect=collectionPool.Get();effects.Add(effect);effect.gameObject.SetActive(true);effect.Begin(title.CashTarget);
            if(batch.arrived<batch.items.Length)return;
            batches.Remove(batch);
            rewards.CompleteFlyCoin(batch.amount,batch.completed,title.ResetPlacement);
        }
        private void CollectFinished(RecoveredCashCollectionEffect effect){effects.Remove(effect);collectionPool.Release(effect);}
        public void Unbind()
        {
            for(int b=0;b<batches.Count;b++) {
                var batch=batches[b];batch.wait?.Cancel();
                for(int i=0;i<batch.items.Length;i++)if(batch.items[i]!=null)cashPool.Release(batch.items[i]);
            }
            batches.Clear();
            for(int i=0;i<effects.Count;i++)collectionPool.Release(effects[i]);effects.Clear();
            cashPool?.Dispose();collectionPool?.Dispose();cashPool=null;collectionPool=null;title=null;rewards=null;
        }
        private void OnDestroy()=>Unbind();
    }
}
