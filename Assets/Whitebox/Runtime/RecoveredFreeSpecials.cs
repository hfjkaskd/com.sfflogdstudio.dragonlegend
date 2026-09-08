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
        [SerializeField] private RecoveredFreeLampFlights lampFlights;
        public RecoveredFreeLampFlights LampFlights=>lampFlights;
        [SerializeField] private Vector3 slotCenter;
        [SerializeField] private float coinScale,ballScale;
        [SerializeField] private string coinStopSound,ballStopSound;
        [SerializeField] private int stopVibrationMilliseconds;
        public event Action<string> SoundRequested;
        public event Action<int> VibrationRequested;
        private readonly Dictionary<RecoveredReelView,Binding> byReel=new Dictionary<RecoveredReelView,Binding>();
        private ObjectPool<RecoveredFreeCoin> coins;
        private ObjectPool<RecoveredFreeBall> balls;
        private readonly Dictionary<RecoveredSymbolView,List<RecoveredFreeCoin>> coinSlots=new Dictionary<RecoveredSymbolView,List<RecoveredFreeCoin>>();
        private readonly Dictionary<RecoveredSymbolView,List<RecoveredFreeBall>> ballSlots=new Dictionary<RecoveredSymbolView,List<RecoveredFreeBall>>();
        private RecoveredFreeSpinResult result;
        private Binding[] bindings;
        private int ballIndex;
        private readonly List<RecoveredFreeBall> flightBalls=new List<RecoveredFreeBall>(2);
        public int FlightBallCount=>flightBalls.Count;
        public RecoveredFreeBall FlyBall(RecoveredFreeBall source,Transform target,Action<RecoveredFreeBall,RecoveredFreeBall> completed)
        {
            var ball=balls.Get();ball.transform.SetParent(source.transform.parent,false);
            ball.transform.localScale=Vector3.one*ballScale;ball.transform.position=source.transform.position;
            ball.gameObject.SetActive(true);ball.Initialize(source.BallType);ball.transform.SetAsLastSibling();ball.Clipping.Bind(null);
            flightBalls.Add(ball);ball.BeginFlight(source,target,completed);return ball;
        }
        public void ReleaseFlightBall(RecoveredFreeBall ball)
        {
            if(!flightBalls.Remove(ball))throw new InvalidOperationException("Ball is not a borrowed flight object.");
            balls.Release(ball);
        }
        private readonly Dictionary<RecoveredReelView,Component> stopped=new Dictionary<RecoveredReelView,Component>();
        public int BallIndex=>ballIndex;
        public RecoveredFreeCoin CurrentStoppedCoin(RecoveredReelView reel)=>byReel[reel].stoppedCoin;
        public int CreatedCoins {get;private set;}
        public int CreatedBalls {get;private set;}
        public int ActiveCoins=>coins==null?0:coins.CountActive;
        public int ActiveBalls=>balls==null?0:balls.CountActive;

        private sealed class Binding
        {
            private readonly RecoveredFreeSpecials owner;
            private readonly RecoveredReelView reel;
            private readonly int column,row;
            public RecoveredFreeCoin stoppedCoin;
            public RecoveredFreeBall stoppedBall;
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
                lampFlights.SoundRequested+=ForwardSound;
                coins=new ObjectPool<RecoveredFreeCoin>(NewCoin,null,ReturnCoin,c=>Destroy(c.gameObject));
                balls=new ObjectPool<RecoveredFreeBall>(NewBall,null,ReturnBall,b=>Destroy(b.gameObject));
            }
            if(bindings!=null)return;
            bindings=new Binding[15];
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                var reel=reels.At(col,row);var binding=new Binding(this,reel,col,row);
                bindings[col*3+row]=binding;byReel.Add(reel,binding);
            }
        }
        private RecoveredFreeCoin NewCoin(){CreatedCoins++;var coin=Instantiate(coinPrefab,storage,false);coin.gameObject.SetActive(false);coin.RewardPresentation.LampFlightRequested+=lampFlights.Launch;coin.RewardPresentation.SoundRequested+=ForwardSound;return coin;}
        private void ForwardSound(string value)=>SoundRequested?.Invoke(value);
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
            var binding=byReel[reel];binding.stoppedCoin=null;binding.stoppedBall=null;
            int id=result.GetSymbol(column,row);Component effect;
            if(id==9)effect=binding.stoppedCoin=CreateCoin(reel.SymbolAt(0),false);
            else if(id==11)effect=binding.stoppedBall=CreateBall(reel.SymbolAt(0),false);
            else {reel.ApplyFreeStoppedSymbol(id);return;}
            // Native Dictionary.Add rejects a second special landing before Clear.
            stopped.Add(reel,effect);
        }
        public Component StoppedAt(RecoveredReelView reel)=>stopped.TryGetValue(reel,out var effect)?effect:null;
        public void PlayStopAnimation(RecoveredReelView reel)
        {
            var binding=byReel[reel];
            if(binding.stoppedCoin!=null) {
                binding.stoppedCoin.PlayShow();SoundRequested?.Invoke(coinStopSound);VibrationRequested?.Invoke(stopVibrationMilliseconds);
            }
            if(binding.stoppedBall!=null) {
                binding.stoppedBall.PlayStart();SoundRequested?.Invoke(ballStopSound);VibrationRequested?.Invoke(stopVibrationMilliseconds);
            }
        }
        public void ShowStoppedEffect(RecoveredReelView reel,Transform resultLayer)
        {
            if(!stopped.TryGetValue(reel,out var effect))return;
            effect.transform.SetParent(resultLayer,false);
            effect.transform.position=reel.SymbolAt(0).Symbol.transform.position;
            if(effect is RecoveredFreeCoin coin)coin.Clipping.Bind(null);
            else if(effect is RecoveredFreeBall ball)ball.Clipping.Bind(null);
        }
        public void ResetStoppedPresentation(RecoveredReelView reel)
        {
            if(!stopped.TryGetValue(reel,out var effect))return;
            var slot=reel.SymbolAt(0);effect.transform.SetParent(slot.transform,false);effect.transform.localPosition=slotCenter;
            if(effect is RecoveredFreeCoin coin)coin.Clipping.Bind(slot.EffectClip);
            else if(effect is RecoveredFreeBall ball)ball.Clipping.Bind(slot.EffectClip);
        }
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
                    var coin=list[j];if(coin==null||!coin.gameObject.activeInHierarchy||coin.transform.parent!=slot.transform)continue;
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
                    var ball=list[j];if(ball==null||!ball.gameObject.activeInHierarchy||ball.transform.parent!=slot.transform)continue;
                    list.RemoveAt(j);balls.Release(ball);slot.Symbol.gameObject.SetActive(true);break;
                }
            }
        }
        private void OnDestroy()
        {
            foreach(var ball in flightBalls)if(ball!=null)Destroy(ball.gameObject);
            if(bindings!=null)foreach(var binding in bindings)binding.Release();
            if(lampFlights!=null)lampFlights.SoundRequested-=ForwardSound;
            coins?.Clear();balls?.Clear();
        }
    }
}
