using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    // Coin branch of RollReel.PlayStopAnim / ShowSymbolEffect / Clear.
    public sealed class RecoveredCoinStopPresenter : MonoBehaviour
    {
        [SerializeField] private RecoveredCoinStopEffect effectPrefab;
        [SerializeField] private RecoveredLampFlight flightPrefab;
        [SerializeField] private RecoveredLampFlash flashPrefab;
        private ObjectPool<RecoveredLampFlash> flashPool;
        private readonly List<RecoveredLampFlash> flashes=new List<RecoveredLampFlash>(2);
        public int ActiveFlashCount=>flashes.Count;
        public int CreatedFlashCount=>flashPool==null?0:flashPool.CountAll;
        public RecoveredLampFlash FlashAt(int index)=>flashes[index];
        private RecoveredBonusCollection collection;
        public event Action<GameObject,float> RewardRegistered;
        public event Action<GameObject> RewardPresentationFinished;
        public event Action<Transform> WinFlightRequested;
        private int canvasOrder;
        private Transform newFlightParent;
        private ObjectPool<RecoveredLampFlight> flightPool;
        private readonly List<RecoveredLampFlight> flights=new List<RecoveredLampFlight>(3);
        public int ActiveFlightCount=>flights.Count;
        public int CreatedFlightCount=>flightPool==null?0:flightPool.CountAll;
        public RecoveredLampFlight FlightAt(int index)=>flights[index];
        public event Action ExpSoundRequested;
        public event Action<Transform> LampLitEffectRequested;
        private RecoveredBaseReelController reels;
        private RecoveredScatterPresenter scatters;
        private int language;
        private ObjectPool<RecoveredCoinStopEffect> pool;
        private readonly RecoveredCoinStopEffect[,] active=new RecoveredCoinStopEffect[5,3];
        private readonly RecoveredCoinStopEffect[,] lookup=new RecoveredCoinStopEffect[5,3];
        private readonly Action[] clearHandlers=new Action[5];
        public int CreatedCount => pool==null?0:pool.CountAll;
        public int ActiveCount => pool==null?0:pool.CountActive;
        public event Action<int> VibrationRequested;
        public event Action CoinShowSoundRequested;
        public event Action CoinRevealSoundRequested;
        private RecoveredCoinStopEffect CreateEffect()
        {
            var effect=Instantiate(effectPrefab,transform,false);
            effect.RevealSoundRequested+=RevealSound;
            effect.LampFlightRequested+=StartFlight;
            effect.RewardPresentationFinished+=RewardFinished;
            return effect;
        }
        private void RewardFinished(RecoveredCoinStopEffect effect)
        {
            effect.PlayTransferPulse();
            WinFlightRequested?.Invoke(effect.transform);
            RewardPresentationFinished?.Invoke(effect.gameObject);
        }
        private void RevealSound()=>CoinRevealSoundRequested?.Invoke();
        public void PlayRewardReveal(int column,int row,float reward,int collectionCount)
        {
            // RollReel.PlayBonusAnim only invokes its action for an existing row lookup.
            var effect=lookup[column,row];
            if(effect!=null) { RewardRegistered?.Invoke(effect.gameObject,reward); effect.PlayRewardReveal(reward,language,collection==null?null:collection.GetUnselectedTarget(column,collectionCount)); }
        }
        public RecoveredCoinStopEffect CoinAt(int column,int row)=>lookup[column,row];
        private RecoveredLampFlight CreateFlight()
        {
            var flight=Instantiate(flightPrefab,newFlightParent,false);flight.Arrived+=Arrived;return flight;
        }
        private void StartFlight(RecoveredCoinStopEffect owner,Transform target)
        {
            newFlightParent=owner.transform;
            var flight=flightPool.Get();flight.transform.SetParent(owner.transform,false);
            flight.transform.localPosition=flightPrefab.transform.localPosition;
            flight.transform.localRotation=flightPrefab.transform.localRotation;
            flight.transform.localScale=flightPrefab.transform.localScale;
            flight.gameObject.SetActive(true);flights.Add(flight);flight.Begin(target,canvasOrder);
        }
        private void ReleaseFlight(RecoveredLampFlight flight)
        {
            flights.Remove(flight);flightPool.Release(flight);
        }
        private void Arrived(RecoveredLampFlight flight)
        {
            var target=flight.Destination;
            ExpSoundRequested?.Invoke();
            ReleaseFlight(flight);
            target.GetChild(0).gameObject.SetActive(true);
            var flash=flashPool.Get();flash.transform.SetParent(target,false);
            ((RectTransform)flash.transform).anchoredPosition=Vector2.zero;
            flash.transform.localScale=flashPrefab.transform.localScale;
            flash.gameObject.SetActive(true);flashes.Add(flash);flash.Play();
            LampLitEffectRequested?.Invoke(target);
        }
        private RecoveredLampFlash CreateFlash()
        {
            var flash=Instantiate(flashPrefab,transform,false);flash.Completed+=ReleaseFlash;return flash;
        }
        private void ReleaseFlash(RecoveredLampFlash flash) { flashes.Remove(flash);flashPool.Release(flash); }
        public void Bind(RecoveredBaseReelController controller,int languageType=0,RecoveredBonusCollection bonusCollection=null,int sortingOrder=0,RecoveredScatterPresenter scatterPresenter=null)
        {
            Unbind();reels=controller;language=languageType;collection=bonusCollection;canvasOrder=sortingOrder;scatters=scatterPresenter;
            if(flashPool==null)flashPool=new ObjectPool<RecoveredLampFlash>(CreateFlash,null,
                flash=>{flash.gameObject.SetActive(false);flash.transform.SetParent(transform,false);},
                flash=>{if(flash!=null)Destroy(flash.gameObject);},true,1,int.MaxValue);
            if(flightPool==null)flightPool=new ObjectPool<RecoveredLampFlight>(CreateFlight,null,
                flight=>{flight.gameObject.SetActive(false);flight.transform.SetParent(transform,false);},
                flight=>{if(flight!=null)Destroy(flight.gameObject);},true,1,int.MaxValue);
            if(pool==null)pool=new ObjectPool<RecoveredCoinStopEffect>(
                CreateEffect,
                effect=>{effect.transform.localScale=effectPrefab.transform.localScale;effect.gameObject.SetActive(true);},
                effect=>effect.gameObject.SetActive(false),effect=>{if(effect!=null)Destroy(effect.gameObject);},true,3,int.MaxValue);
            for(int i=0;i<5;i++) {
                int column=i;
                if(clearHandlers[i]==null)clearHandlers[i]=()=>ClearColumn(column);
                reels.ReelAt(i).EffectsClearRequested+=clearHandlers[i];
            }
            reels.StopAnimationRequested+=ShowColumn;
        }
        public void ShowColumn(int column)
        {
            // Native clears BonusAnims, but ShowSymbolEffect's shown-slot set survives until Clear.
            for(int row=0;row<3;row++)lookup[column,row]=null;
            scatters?.BeginColumn(column);
            for(int row=0;row<3;row++) {
                if(reels.ReelAt(column).SymbolId(row)==10){scatters?.ShowCell(column,row);continue;}
                if(reels.ReelAt(column).SymbolId(row)!=9 || active[column,row]!=null)continue;
                if(!reels.ReelAt(column).TryHideForEffect(row))continue;
                var symbol=reels.ReelAt(column).SymbolAt(row).Symbol;
                var effect=pool.Get();effect.transform.position=symbol.transform.position;
                active[column,row]=lookup[column,row]=effect;
                effect.PlayShow();
                VibrationRequested?.Invoke(200);CoinShowSoundRequested?.Invoke();
            }
        }
        private void ClearColumn(int column)
        {
            for(int row=0;row<3;row++) {
                if(active[column,row]!=null)for(int i=flights.Count-1;i>=0;i--)
                    if(flights[i].transform.parent==active[column,row].transform)ReleaseFlight(flights[i]);
                if(active[column,row]!=null)pool.Release(active[column,row]);
                active[column,row]=null;lookup[column,row]=null;
            }
            // Original Clear does not reactivate the source. The following SetImg does.
        }
        public void Unbind()
        {
            for(int i=flashes.Count-1;i>=0;i--)ReleaseFlash(flashes[i]);
            if(reels==null)return;
            reels.StopAnimationRequested-=ShowColumn;
            for(int i=0;i<5;i++){reels.ReelAt(i).EffectsClearRequested-=clearHandlers[i];ClearColumn(i);}
            reels=null;
        }
        private void OnDestroy(){Unbind();pool?.Clear();flightPool?.Clear();flashPool?.Clear();}
    }
}
