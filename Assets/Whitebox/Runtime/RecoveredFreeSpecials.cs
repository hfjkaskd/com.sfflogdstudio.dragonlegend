using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    // PoolManager Create/ClearJinBi, Create/ClearLongzhu and RollReel.CheckFakeCoin.
    public sealed class RecoveredFreeSpecials : MonoBehaviour
    {
        [SerializeField] private RecoveredFreeCoin coinPrefab;
        [SerializeField] private RecoveredFreeBall ballPrefab;
        [SerializeField] private Transform storage;
        [SerializeField] private Vector3 slotCenter;
        [SerializeField] private float coinScale,ballScale;
        private ObjectPool<RecoveredFreeCoin> coins;
        private ObjectPool<RecoveredFreeBall> balls;
        private readonly Dictionary<RecoveredSymbolView,List<RecoveredFreeCoin>> coinSlots=new Dictionary<RecoveredSymbolView,List<RecoveredFreeCoin>>();
        private readonly Dictionary<RecoveredSymbolView,List<RecoveredFreeBall>> ballSlots=new Dictionary<RecoveredSymbolView,List<RecoveredFreeBall>>();
        private RecoveredFreeSpinResult result;
        private Binding[] bindings;
        private int ballIndex;
        private readonly Dictionary<RecoveredReelView,Component> stopped=new Dictionary<RecoveredReelView,Component>();
        public int BallIndex=>ballIndex;
        public int CreatedCoins {get;private set;}
        public int CreatedBalls {get;private set;}
        public int ActiveCoins=>coins==null?0:coins.CountActive;
        public int ActiveBalls=>balls==null?0:balls.CountActive;

        private sealed class Binding
        {
            private readonly RecoveredFreeSpecials owner;
            private readonly RecoveredReelView reel;
            private readonly int column,row;
            public Binding(RecoveredFreeSpecials owner,RecoveredReelView reel,int column,int row)
            {
                this.owner=owner;this.reel=reel;this.column=column;this.row=row;
                reel.EffectsClearRequested+=ClearStopped;reel.CoinsClearRequested+=ClearCoins;reel.BallsClearRequested+=ClearBalls;reel.FakeCoinRequested+=Refresh;
            }
            private void ClearStopped()=>owner.ClearStopped(reel);
            private void ClearCoins()=>owner.ClearCoins(reel);
            private void ClearBalls()=>owner.ClearBalls(reel);
            private void Refresh()=>owner.ShowRolling(reel,column,row);
            public void Release(){reel.EffectsClearRequested-=ClearStopped;reel.CoinsClearRequested-=ClearCoins;reel.BallsClearRequested-=ClearBalls;reel.FakeCoinRequested-=Refresh;}
        }
        public void Bind(RecoveredFreeReels reels,RecoveredFreeSpinResult source)
        {
            result=source??throw new ArgumentNullException(nameof(source));
            if(coins==null) {
                coins=new ObjectPool<RecoveredFreeCoin>(NewCoin,null,ReturnCoin,c=>Destroy(c.gameObject));
                balls=new ObjectPool<RecoveredFreeBall>(NewBall,null,ReturnBall,b=>Destroy(b.gameObject));
            }
            if(bindings!=null)return;
            bindings=new Binding[15];
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)bindings[col*3+row]=new Binding(this,reels.At(col,row),col,row);
        }
        private RecoveredFreeCoin NewCoin(){CreatedCoins++;var coin=Instantiate(coinPrefab,storage,false);coin.gameObject.SetActive(false);return coin;}
        private RecoveredFreeBall NewBall(){CreatedBalls++;var ball=Instantiate(ballPrefab,storage,false);ball.gameObject.SetActive(false);return ball;}
        private void ReturnCoin(RecoveredFreeCoin coin){coin.gameObject.SetActive(false);coin.transform.SetParent(storage,false);}
        private void ReturnBall(RecoveredFreeBall ball){ball.gameObject.SetActive(false);ball.transform.SetParent(storage,false);}
        private void Place(Transform effect,RecoveredSymbolView slot,float scale)
        {
            effect.SetParent(slot.transform,false);effect.localPosition=slotCenter;effect.localScale=Vector3.one*scale;
            effect.gameObject.SetActive(true);slot.Symbol.gameObject.SetActive(false);
        }
        public RecoveredFreeCoin CreateCoin(RecoveredSymbolView slot,bool initial)
        {
            var coin=coins.Get();Place(coin.transform,slot,coinScale);coin.Clipping.Bind(slot.EffectClip);
            if(!initial)coin.PlayShow();
            if(!coinSlots.TryGetValue(slot,out var list)){list=new List<RecoveredFreeCoin>(1);coinSlots.Add(slot,list);}
            list.Add(coin);return coin;
        }
        public RecoveredFreeBall CreateBall(RecoveredSymbolView slot,bool initial)
        {
            if(result.RandomBallInfos(ballIndex))ballIndex=0;
            var ball=balls.Get();Place(ball.transform,slot,ballScale);ball.Clipping.Bind(slot.EffectClip);
            ball.Initialize(result.GetBall(ballIndex),initial);
            if(!ballSlots.TryGetValue(slot,out var list)){list=new List<RecoveredFreeBall>(1);ballSlots.Add(slot,list);}
            list.Add(ball);ballIndex++;return ball;
        }
        public void ShowInitial(int column,int row,RecoveredReelView reel,int id)
        {
            if(id==9)CreateCoin(reel.SymbolAt(0),true);
            else if(id==11)CreateBall(reel.SymbolAt(0),true);
        }
        // ConstantSpeedRoll Free branch: cleanup precedes reading the actual cell.
        public void ApplyStoppedResult(RecoveredReelView reel,int column,int row)
        {
            ClearCoins(reel);ClearBalls(reel);
            int id=result.GetSymbol(column,row);Component effect;
            if(id==9)effect=CreateCoin(reel.SymbolAt(0),false);
            else if(id==11)effect=CreateBall(reel.SymbolAt(0),false);
            else {reel.ApplyFreeStoppedSymbol(id);return;}
            // Native Dictionary.Add rejects a second special landing before Clear.
            stopped.Add(reel,effect);
        }
        public Component StoppedAt(RecoveredReelView reel)=>stopped.TryGetValue(reel,out var effect)?effect:null;
        private void ClearStopped(RecoveredReelView reel)
        {
            if(!stopped.TryGetValue(reel,out var effect))return;
            var slot=reel.SymbolAt(0);
            if(effect is RecoveredFreeCoin coin) {
                if(coinSlots.TryGetValue(slot,out var list)&&list.Remove(coin)&&coin.gameObject.activeInHierarchy)coins.Release(coin);
            } else if(effect is RecoveredFreeBall ball) {
                if(ballSlots.TryGetValue(slot,out var list)&&list.Remove(ball)&&ball.gameObject.activeInHierarchy)balls.Release(ball);
            }
            stopped.Remove(reel);
        }
        public void ShowRolling(RecoveredReelView reel,int column,int row)
        {
            var board=result.GetRandomEffectShow();int id=board[column,row];
            // Native repeats this same cell three times; the two symbol flags are mutually exclusive.
            if(id==9)CreateCoin(reel.SymbolAt(UnityEngine.Random.Range(0,4)),false);
            else if(id==11)CreateBall(reel.SymbolAt(UnityEngine.Random.Range(0,4)),false);
        }
        public RecoveredFreeCoin CoinAt(RecoveredSymbolView slot)=>coinSlots.TryGetValue(slot,out var list)&&list.Count>0?list[0]:null;
        public RecoveredFreeBall BallAt(RecoveredSymbolView slot)=>ballSlots.TryGetValue(slot,out var list)&&list.Count>0?list[0]:null;
        public void ClearCoins(RecoveredReelView reel)
        {
            for(int i=0;i<reel.SlotCount;i++) {
                var slot=reel.SymbolAt(i);
                if(!coinSlots.TryGetValue(slot,out var list)||list.Count==0)continue;
                for(int j=0;j<list.Count;j++) {
                    var coin=list[j];if(coin==null||!coin.gameObject.activeInHierarchy)continue;
                    list.RemoveAt(j);coins.Release(coin);slot.Symbol.gameObject.SetActive(true);break;
                }
            }
        }
        public void ClearBalls(RecoveredReelView reel)
        {
            for(int i=0;i<reel.SlotCount;i++) {
                var slot=reel.SymbolAt(i);
                if(!ballSlots.TryGetValue(slot,out var list)||list.Count==0)continue;
                for(int j=0;j<list.Count;j++) {
                    var ball=list[j];if(ball==null||!ball.gameObject.activeInHierarchy)continue;
                    list.RemoveAt(j);balls.Release(ball);slot.Symbol.gameObject.SetActive(true);break;
                }
            }
        }
        private void OnDestroy()
        {
            if(bindings!=null)foreach(var binding in bindings)binding.Release();
            coins?.Clear();balls?.Clear();
        }
    }
}
