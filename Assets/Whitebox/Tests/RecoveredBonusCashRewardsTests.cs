using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredBonusCashRewardsTests
{
    private static RecoveredGameplayRules Rules(int jump)=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Ronig=new RonigPoro {RgkorpKgiitr=new List<int>{1},RgokrpMin=new List<int>{963},RgkorpMoj=new List<int>{963},Jimp=new List<int>{jump}}});

    [UnityTest]
    public IEnumerator CashReleasesBeforeFlightWhileJumpWaitsForPopupAndCancellationSuppressesCallbacks()
    {
        var host=new GameObject("Bonus cash test",typeof(RectTransform),typeof(Canvas));
        var sequence=Object.Instantiate(Resources.Load<RecoveredBonusCashRewards>("RecoveredUI/BonusCashRewards"),host.transform);
        var item=Object.Instantiate(Resources.Load<RecoveredBonusItemTurn>("RecoveredUI/BonusItem"),host.transform);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=UnityEngine.Random.state;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;yield return null;item.Initialize();
            Exception failure=null;sequence.Failed+=e=>failure=e;item.Failed+=e=>failure=e;
            var order=new List<string>();int popups=0;Action<float> claim=null;
            sequence.SoundRequested+=sound=>order.Add(sound);
            sequence.FlyCoinRequested+=(amount,callback,source)=>{
                Assert.AreEqual("release",order[order.Count-1]);Assert.AreEqual(963,amount);
                Assert.IsNull(callback);Assert.AreSame(item.transform,source);order.Add("flight");
            };
            sequence.RewardPopupRequested+=(amount,callback)=>{Assert.AreEqual(963,amount);popups++;claim=callback;};
            sequence.Begin(item,Rules(0),0,()=>order.Add("release"));
            for(int i=0;i<30&&sequence.PendingCount>0;i++)yield return null;
            Assert.IsNull(failure);Assert.AreEqual(0,sequence.PendingCount);Assert.AreEqual(0,popups);
            CollectionAssert.AreEqual(new[]{"coinReveal","release","flight"},order);
            item.Initialize();int released=0;sequence.Begin(item,Rules(-1),0,()=>released++);
            for(int i=0;i<30&&item.IsTurning;i++)yield return null;
            Assert.IsFalse(item.IsTurning);Assert.AreEqual(0,popups);Assert.AreEqual(0,released);
            double turnedAt=Time.timeAsDouble;
            Time.timeScale=0;for(int i=0;i<10;i++)yield return null;Assert.AreEqual(0,popups);
            Time.timeScale=1;for(int i=0;i<12&&popups==0;i++)yield return null;
            Assert.AreEqual(1,popups);Assert.That(Time.timeAsDouble-turnedAt,Is.InRange(.17,.26));
            Assert.AreEqual(0,released);Assert.AreEqual(1,sequence.PendingCount);
            claim(-123);claim(999);Assert.AreEqual(1,released);Assert.AreEqual(0,sequence.PendingCount);
            item.Initialize();sequence.Begin(item,Rules(1),0,()=>released++);
            for(int i=0;i<30&&item.IsTurning;i++)yield return null;
            sequence.Cancel();for(int i=0;i<20;i++)yield return null;
            Assert.AreEqual(1,popups);Assert.AreEqual(1,released);Assert.AreEqual(0,sequence.PendingCount);
            item.Initialize();Time.timeScale=0;sequence.Begin(item,Rules(1),0,()=>released++);
            var before=UnityEngine.Random.state;sequence.Cancel();Time.timeScale=1;
            for(int i=0;i<20;i++)yield return null;
            Assert.AreEqual(before,UnityEngine.Random.state);Assert.AreEqual(1,popups);Assert.IsNull(failure);
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;Object.Destroy(host);}
    }
}
