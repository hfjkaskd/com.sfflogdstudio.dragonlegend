using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

public sealed class RecoveredLampFlightTests
{
    [UnityTest]
    public IEnumerator NativeFlightUsesInOutSineQuadraticArcAndCapturedDestination()
    {
        var flight = Object.Instantiate(Resources.Load<RecoveredLampFlight>("RecoveredSymbols/LampFlight"));
        var destination = new GameObject("Destination");
        float scale = Time.timeScale, delta = Time.captureDeltaTime;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .075f;
            Assert.AreEqual(1, flight.GetComponentsInChildren<ParticleSystem>().Length);
            var trails = flight.GetComponentsInChildren<TrailRenderer>(); Assert.AreEqual(2, trails.Length);
            foreach (var trail in trails) { Assert.AreEqual(.2f, trail.widthMultiplier); Assert.AreEqual(.1f, trail.minVertexDistance); }
            Assert.IsTrue(flight.GetComponent<SortingGroup>().sortAtRoot);
            yield return null;
            flight.transform.position = Vector3.zero; destination.transform.position = new Vector3(0, 4, 0);
            int arrived = 0; flight.Arrived += f => { Assert.AreEqual(new Vector3(0, 4, 0), f.transform.position); arrived++; };
            flight.Begin(destination.transform, 9);
            Assert.AreEqual(new Vector3(0, 3.2f, 0), flight.ControlPoint);
            foreach (var renderer in flight.GetComponentsInChildren<Renderer>()) {
                Assert.AreEqual(10, renderer.sortingOrder);
                foreach(var material in renderer.sharedMaterials) { Assert.IsNotNull(material); Assert.IsTrue(material.shader.isSupported); }
            }
            destination.transform.position = new Vector3(8, 9, 0);
            Time.timeScale = 0; for (int i = 0; i < 5; i++) yield return null;
            Assert.AreEqual(Vector3.zero, flight.transform.position); Assert.AreEqual(0, arrived); Time.timeScale = 1;
            float[] expected = { .885786438f, 2.6f, 3.71421356f, 4 };
            for (int i = 0; i < expected.Length; i++) {
                yield return null; Assert.AreEqual(expected[i], flight.transform.position.y, .0001f);
                Assert.AreEqual(i == 3 ? 1 : 0, arrived);
            }
            Assert.IsFalse(flight.IsFlying);
            flight.Begin(destination.transform, 0); flight.gameObject.SetActive(false); flight.gameObject.SetActive(true);
            for (int i = 0; i < 6; i++) yield return null;
            Assert.AreEqual(1, arrived); Assert.IsFalse(flight.IsFlying);
        } finally { Time.timeScale = scale; Time.captureDeltaTime = delta; Object.Destroy(flight.gameObject); Object.Destroy(destination); }
    }
    [UnityTest]
    public IEnumerator BottomWinFlightUsesInQuadAndKeepsOriginalParticleVariant()
    {
        var flight=Object.Instantiate(Resources.Load<RecoveredLampFlight>("RecoveredSymbols/DownWinFlight"));
        var destination=new GameObject("Bottom destination");
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Assert.IsFalse(flight.gameObject.activeSelf,"Position the pooled particle before enabling emission.");
            Assert.AreEqual(1,flight.GetComponentsInChildren<ParticleSystem>(true).Length);
            Assert.AreEqual(2,flight.GetComponentsInChildren<TrailRenderer>(true).Length);
            Time.timeScale=1;Time.captureDeltaTime=.075f;
            flight.transform.position=Vector3.zero;destination.transform.position=new Vector3(0,-4,0);
            flight.gameObject.SetActive(true);yield return null;
            int arrivals=0;flight.Arrived+=value=>arrivals++;flight.Begin(destination.transform,3);
            Assert.AreEqual(-.8f,flight.ControlPoint.y,.00001f);
            destination.transform.position=new Vector3(10,-20,3);
            float[] expected={-.109375f,-.55f,-1.659375f,-4};
            for(int i=0;i<4;i++) {yield return null;Assert.AreEqual(expected[i],flight.transform.position.y,.0001f);}
            Assert.AreEqual(1,arrivals);Assert.AreEqual(4,flight.GetComponent<SortingGroup>().sortingOrder);
            flight.Begin(destination.transform,0);Time.timeScale=0;
            for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(new Vector3(0,-4,0),flight.transform.position);
            flight.gameObject.SetActive(false);Time.timeScale=1;
            for(int i=0;i<5;i++)yield return null;Assert.AreEqual(1,arrivals);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(flight.gameObject);Object.Destroy(destination);}
    }

}
