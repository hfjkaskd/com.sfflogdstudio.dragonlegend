using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace DragonLegend.Whitebox
{
    // PlayAnim b__3/b__4: spawn under the coin, fly, return, light target, flash.
    public sealed class RecoveredFreeLampFlights : MonoBehaviour
    {
        [SerializeField] private RecoveredLampFlight flightPrefab;
        [SerializeField] private RecoveredLampFlash flashPrefab;
        [SerializeField] private Transform storage;
        [SerializeField] private int canvasOrder;
        [SerializeField] private string arrivalSound;
        private ObjectPool<RecoveredLampFlight> flightPool;
        private ObjectPool<RecoveredLampFlash> flashPool;
        private readonly List<RecoveredLampFlight> flights=new List<RecoveredLampFlight>(3);
        private readonly List<RecoveredLampFlash> flashes=new List<RecoveredLampFlash>(2);
        public event Action<string> SoundRequested;
        public int ActiveFlights=>flights.Count;
        public int ActiveFlashes=>flashes.Count;
        public int CreatedFlights=>flightPool==null?0:flightPool.CountAll;
        public RecoveredLampFlight FlightAt(int index)=>flights[index];
        public void Launch(RecoveredFreeCoinReward owner,Transform target)
        {
            if(flightPool==null) {
                flightPool=new ObjectPool<RecoveredLampFlight>(NewFlight,null,ReturnFlight,f=>Destroy(f.gameObject));
                flashPool=new ObjectPool<RecoveredLampFlash>(NewFlash,null,ReturnFlash,f=>Destroy(f.gameObject));
            }
            var flight=flightPool.Get();flight.transform.SetParent(owner.transform,false);
            flight.transform.localPosition=flightPrefab.transform.localPosition;flight.transform.localRotation=flightPrefab.transform.localRotation;
            flight.transform.localScale=flightPrefab.transform.localScale;flight.gameObject.SetActive(true);
            flights.Add(flight);flight.Begin(target,canvasOrder);
        }
        private RecoveredLampFlight NewFlight(){var value=Instantiate(flightPrefab,storage,false);value.Arrived+=Arrived;return value;}
        private RecoveredLampFlash NewFlash(){var value=Instantiate(flashPrefab,storage,false);value.Completed+=ReleaseFlash;return value;}
        private void ReturnFlight(RecoveredLampFlight value){value.gameObject.SetActive(false);value.transform.SetParent(storage,false);}
        private void ReturnFlash(RecoveredLampFlash value){value.gameObject.SetActive(false);value.transform.SetParent(storage,false);}
        private void Arrived(RecoveredLampFlight flight)
        {
            var target=flight.Destination;SoundRequested?.Invoke(arrivalSound);
            flights.Remove(flight);flightPool.Release(flight);
            target.GetChild(0).gameObject.SetActive(true);
            var flash=flashPool.Get();flash.transform.SetParent(target,false);
            ((RectTransform)flash.transform).anchoredPosition=Vector2.zero;flash.transform.localScale=flashPrefab.transform.localScale;
            flash.gameObject.SetActive(true);flashes.Add(flash);flash.Play();
        }
        private void ReleaseFlash(RecoveredLampFlash flash){flashes.Remove(flash);flashPool.Release(flash);}
        private void OnDestroy()
        {
            foreach(var flight in flights)if(flight!=null)Destroy(flight.gameObject);
            foreach(var flash in flashes)if(flash!=null)Destroy(flash.gameObject);
            flightPool?.Clear();flashPool?.Clear();
        }
    }
}
