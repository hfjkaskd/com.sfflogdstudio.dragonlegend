using System;
using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredBankItemTests
{
    [UnityTest]
    public IEnumerator AuthoredBallRevealsThenDispatchesFlightAndWaitsForItsCallback()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        RecoveredBankItem item=null;
        try
        {
            Time.timeScale=0;Time.captureDeltaTime=.05f;
            item=UnityEngine.Object.Instantiate(Resources.Load<RecoveredBankItem>("RecoveredUI/BankItem"));
            Assert.IsNotNull(item.Button);Assert.AreSame(item.gameObject,item.Button.gameObject);
            Assert.AreEqual(0,item.Button.onClick.GetPersistentEventCount());
            int[] idle={5,3,4},fire={2,0,1};
            for(int type=0;type<3;type++)
            {
                item.Initialize(type,0);Assert.AreEqual(idle[type],item.Ball.Selected);
                Assert.IsFalse(item.Reward.gameObject.activeSelf);Assert.IsFalse(item.Ad.gameObject.activeSelf);
                item.ShowAd(true);Assert.IsTrue(item.Ad.gameObject.activeSelf);
                int flights=0,completed=0;Action arrival=null;Transform source=null;string sound=null;
                Action<string> onSound=value=>sound=value;
                Action<float,Action,Transform> onFly=(amount,callback,origin)=>{Assert.AreEqual(17,amount);flights++;arrival=callback;source=origin;};
                item.SoundRequested+=onSound;item.FlyRequested+=onFly;
                item.Play(17,value=>{Assert.AreEqual(17,value);completed++;});
                Assert.AreEqual(fire[type],item.Ball.Selected);Assert.AreEqual("coinReveal",sound);
                for(int i=0;i<4;i++)yield return null;
                Assert.IsFalse(item.Reward.gameObject.activeSelf);Assert.AreEqual(0,flights);
                Time.timeScale=1;
                for(int i=0;i<8;i++)yield return null;
                Assert.IsFalse(item.Reward.gameObject.activeSelf,"No reveal before the native half-second wait.");
                for(int i=0;i<8&&!item.Reward.gameObject.activeSelf;i++)yield return null;
                Assert.IsTrue(item.Reward.gameObject.activeSelf);Assert.AreEqual(RecoveredCurrency.Format(17,0,2),item.Label.text);
                Assert.AreEqual(0,flights);Time.timeScale=0;
                Vector3 frozen=item.Reward.localScale;
                for(int i=0;i<4;i++)yield return null;
                Assert.AreEqual(frozen,item.Reward.localScale);Assert.AreEqual(0,flights);
                Time.timeScale=1;for(int i=0;i<20&&flights==0;i++)yield return null;
                Assert.AreEqual(1,flights);Assert.AreSame(item.Reward,source);Assert.AreEqual(0,completed);
                yield return null;Assert.AreEqual(.6f,item.Reward.localScale.x,.0001f);
                if(type==2){item.Cancel();arrival();Assert.AreEqual(0,completed,"Cancelled GM owner rejects the old flight continuation.");}
                else {arrival();Assert.AreEqual(1,completed);}
                item.SoundRequested-=onSound;item.FlyRequested-=onFly;
                Time.timeScale=0;
            }
            item.Initialize(99,0);Assert.AreEqual(4,item.Ball.Selected,"Native default type maps to green.");
            item.Play(10,null);item.Cancel();Time.timeScale=1;
            for(int i=0;i<25;i++)yield return null;
            Assert.IsFalse(item.Reward.gameObject.activeSelf,"Cancelled pre-reveal wait must not reactivate discarded content.");
        }
        finally {Time.timeScale=scale;Time.captureDeltaTime=delta;if(item!=null)UnityEngine.Object.Destroy(item.gameObject);}
        yield return null;
    }
}
