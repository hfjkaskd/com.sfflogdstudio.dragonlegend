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
        private ObjectPool<RecoveredLampFlight> pool;
        private readonly List<RecoveredLampFlight> active=new List<RecoveredLampFlight>(2);
        private int canvasOrder;
        public int ActiveCount=>active.Count;
        public int CreatedCount=>pool==null?0:pool.CountAll;
        public RecoveredLampFlight FlightAt(int index)=>active[index];
        public event Action ArrivalEffectRequested;
        public event Action CoinBurstSoundRequested;
        public event Action<int> VibrationRequested;
        public void Bind(int order)
        {
            Cancel();canvasOrder=order;
            if(pool==null)pool=new ObjectPool<RecoveredLampFlight>(Create,null,
                value=>value.gameObject.SetActive(false),value=>{if(value!=null)Destroy(value.gameObject);},true,1,int.MaxValue);
        }
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
            ArrivalEffectRequested?.Invoke();
            CoinBurstSoundRequested?.Invoke();VibrationRequested?.Invoke(200);
        }
        public void Cancel() {for(int i=active.Count-1;i>=0;i--)Release(active[i]);}
        private void OnDisable()=>Cancel();
        private void OnDestroy(){Cancel();pool?.Clear();}
    }
}
