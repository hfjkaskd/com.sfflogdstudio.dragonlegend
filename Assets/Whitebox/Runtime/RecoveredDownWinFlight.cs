using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    public sealed class RecoveredDownWinFlight : MonoBehaviour
    {
        [SerializeField] private RecoveredLampFlight flightPrefab;
        [SerializeField] private Transform destination;
        [SerializeField] private RecoveredWinBurst burstPrefab;
        [SerializeField] private RectTransform burstParent;
        private ObjectPool<RecoveredWinBurst> bursts;
        private readonly List<RecoveredWinBurst> activeBursts=new List<RecoveredWinBurst>(3);
        public int ActiveBurstCount=>activeBursts.Count;
        public RecoveredWinBurst BurstAt(int index)=>activeBursts[index];
        private ObjectPool<RecoveredLampFlight> pool;
        private readonly List<RecoveredLampFlight> active=new List<RecoveredLampFlight>(2);
        private int canvasOrder;
        public int ActiveCount=>active.Count;
        public int CreatedCount=>pool==null?0:pool.CountAll;
        public RecoveredLampFlight FlightAt(int index)=>active[index];
        public event Action ArrivalEffectRequested;
        public event Action CoinBurstSoundRequested;
        public event Action<int> VibrationRequested;
        public void Bind(int order, Transform target, RectTransform bottom)
        {
            destination=target;burstParent=bottom;Bind(order);
        }
        public void Bind(int order)
        {
            Cancel();canvasOrder=order;
            if(bursts==null)bursts=new ObjectPool<RecoveredWinBurst>(CreateBurst,null,
                value=>value.gameObject.SetActive(false),value=>{if(value!=null)Destroy(value.gameObject);},true,3,int.MaxValue);
            if(pool==null)pool=new ObjectPool<RecoveredLampFlight>(Create,null,
                value=>value.gameObject.SetActive(false),value=>{if(value!=null)Destroy(value.gameObject);},true,1,int.MaxValue);
        }
        private RecoveredWinBurst CreateBurst(){var burst=Instantiate(burstPrefab,burstParent,false);burst.Completed+=ReleaseBurst;return burst;}
        private void ReleaseBurst(RecoveredWinBurst burst){activeBursts.Remove(burst);bursts.Release(burst);}
        private RecoveredLampFlight Create()
        {
            var flight=Instantiate(flightPrefab,transform,false);flight.Arrived+=Arrived;return flight;
        }
        public void Play(Transform origin)
        {
            var flight=pool.Get();
            flight.transform.position=origin.position;
            flight.transform.localScale=flightPrefab.transform.localScale;
            flight.gameObject.SetActive(true);flight.transform.SetAsLastSibling();
            active.Add(flight);flight.Begin(destination,canvasOrder);
        }
        private void Release(RecoveredLampFlight flight) {active.Remove(flight);pool.Release(flight);}
        private void Arrived(RecoveredLampFlight flight)
        {
            Release(flight);
            PlayBurst();
        }
        public void PlayBurst()
        {
            var burst=bursts.Get();burst.gameObject.SetActive(true);activeBursts.Add(burst);
            ArrivalEffectRequested?.Invoke();
            CoinBurstSoundRequested?.Invoke();VibrationRequested?.Invoke(200);
            ((RectTransform)burst.transform).anchoredPosition=Vector2.zero;burst.transform.SetAsLastSibling();burst.Play();
        }
        public void Cancel() {for(int i=active.Count-1;i>=0;i--)Release(active[i]);for(int i=activeBursts.Count-1;i>=0;i--)ReleaseBurst(activeBursts[i]);}
        private void OnDisable()=>Cancel();
        private void OnDestroy(){Cancel();pool?.Clear();bursts?.Clear();}
    }
}
