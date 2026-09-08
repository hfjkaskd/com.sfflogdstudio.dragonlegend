using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeBallFlightTests
{
    [UnityTest]
    public IEnumerator FlightClonesTypeThroughSharedPoolAndRetainsBothObjectsAtArrival()
    {
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var weights=new List<int>();for(int i=0;i<=15;i++)weights.Add(i==15?1000000:0);
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
            QoinOmoinrKgiitr=weights,RollOmoinrKgiitr=new List<int>{1000000},RollRipgKgiitr=new List<int>{1}}});
        var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
        var parent=new GameObject("Flight test parent");parent.transform.position=new Vector3(2,1,0);parent.transform.localScale=Vector3.one*1.5f;
        var source=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"),parent.transform);
        var target=new GameObject("Flight test target");target.transform.position=new Vector3(-2,4,0);
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;RecoveredFreeBall previous=null;
            for(int type=0;type<3;type++) {
                source.Initialize(type);int arrived=0;var start=source.transform.position;var end=target.transform.position;
                var flight=root.Specials.FlyBall(source,target.transform,(copy,original)=>{
                    Assert.AreSame(source,original);Assert.AreNotSame(source,copy);Assert.IsTrue(copy.gameObject.activeInHierarchy);
                    Assert.AreEqual(1,root.Specials.FlightBallCount);arrived++;
                });
                if(previous!=null)Assert.AreSame(previous,flight);
                Assert.AreEqual(type,flight.BallType);Assert.AreEqual(string.Empty,flight.Reward.text);
                Assert.AreSame(parent.transform,flight.transform.parent);Assert.AreEqual(parent.transform.childCount-1,flight.transform.GetSiblingIndex());
                Assert.AreEqual(Vector3.one*.8f,flight.transform.localScale);Assert.AreEqual(start,flight.transform.position);
                Assert.AreEqual(end,flight.FlightEnd);Assert.AreEqual(1,root.Specials.CreatedBalls);
                Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
                Assert.AreEqual(start,flight.transform.position);Assert.AreEqual(0,arrived);
                Time.timeScale=1;for(int i=0;i<3;i++)yield return null;
                var control=(start+end)*.5f+Vector3.up*(Vector3.Distance(start,end)*.3f);
                var midpoint=start*.25f+control*.5f+end*.25f;
                Assert.That(Vector3.Distance(midpoint,flight.transform.position),Is.LessThan(.001f));
                target.transform.position+=Vector3.right; // Native snapshots its destination.
                for(int i=0;i<10&&arrived==0;i++)yield return null;
                Assert.AreEqual(1,arrived);Assert.IsFalse(flight.IsFlying);Assert.That(Vector3.Distance(end,flight.transform.position),Is.LessThan(.001f));
                Assert.AreEqual(start,source.transform.position);Assert.IsTrue(source.gameObject.activeSelf);
                Assert.AreEqual(1,root.Specials.ActiveBalls);root.Specials.ReleaseFlightBall(flight);
                Assert.AreEqual(0,root.Specials.ActiveBalls);Assert.AreEqual(0,root.Specials.FlightBallCount);previous=flight;
            }
        } finally {Object.Destroy(root.gameObject);Object.Destroy(parent);Object.Destroy(target);Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
