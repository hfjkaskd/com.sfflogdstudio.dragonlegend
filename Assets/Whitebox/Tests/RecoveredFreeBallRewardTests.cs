using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeBallRewardTests
{
    [UnityTest]
    public IEnumerator ActivationWaitsPointSevenRatherThanTwoSecondClipAndReadsCurrentType()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var ball=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"));
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            string[] names={"huo_zi","huo_lan","huo_lv"};
            for(int type=0;type<3;type++) {
                ball.Initialize(type);int calls=0,received=-1;double began=Time.timeAsDouble,finished=0;
                ball.PlayActivation(value=>{received=value;calls++;finished=Time.timeAsDouble;});
                Assert.IsTrue(ball.Art.Player.IsPlaying(names[type]));
                Time.timeScale=0;for(int i=0;i<3;i++)yield return null;Assert.AreEqual(0,calls);
                Time.timeScale=1;for(int i=0;i<20&&calls==0;i++)yield return null;
                Assert.IsNull(ball.Error);Assert.AreEqual(1,calls);Assert.AreEqual(type,received);
                Assert.That(finished-began,Is.InRange(.699, .81));Assert.IsTrue(ball.Art.Player.IsPlaying(names[type]));
            }
            int changed=-1;ball.Initialize(0);ball.PlayActivation(value=>changed=value);ball.Initialize(2);
            for(int i=0;i<20&&changed<0;i++)yield return null;Assert.AreEqual(2,changed);
        } finally {Object.Destroy(ball.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
    [UnityTest]
    public IEnumerator RewardWaitsCountsFromZeroThenPulsesWithoutChangingVisibility()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var ball=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"));
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;ball.Initialize(1);var reward=ball.RewardPresentation;
            ball.Reward.gameObject.SetActive(false);ball.Reward.text="old";int language=0,scheduled=0;
            double began=Time.timeAsDouble,started=0;
            reward.Play(1000,()=>language,()=>{scheduled++;started=Time.timeAsDouble;Assert.AreEqual("old",ball.Reward.text);});
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;Assert.AreEqual(0,scheduled);Assert.AreEqual("old",ball.Reward.text);
            Time.timeScale=1;for(int i=0;i<6&&scheduled==0;i++)yield return null;
            Assert.AreEqual(1,scheduled);Assert.That(started-began,Is.InRange(.099,.21));
            Assert.IsTrue(reward.IsAnimating);Assert.IsFalse(ball.Reward.gameObject.activeSelf);
            language=1;float peak=ball.Reward.transform.localScale.x;
            for(int i=0;i<25&&reward.IsAnimating;i++){yield return null;peak=Mathf.Max(peak,ball.Reward.transform.localScale.x);}
            Assert.IsNull(reward.Error);Assert.IsFalse(reward.IsAnimating);
            Assert.AreEqual(RecoveredCurrency.Format(1000,language,2),ball.Reward.text);
            Assert.That(peak,Is.EqualTo(1.2f).Within(.0001f));Assert.AreEqual(Vector3.one,ball.Reward.transform.localScale);
            Assert.IsFalse(ball.Reward.gameObject.activeSelf);
            // Two calls before either wait ends both create numeric tweens in the source.
            int pending=0;reward.Play(2000,()=>0,()=>pending++);reward.Play(3000,()=>0,()=>pending++);
            for(int i=0;i<6&&pending<2;i++)yield return null;Assert.AreEqual(2,pending);
            reward.Play(4000,()=>0);
            for(int i=0;i<30&&reward.IsAnimating;i++)yield return null;
            Assert.AreEqual(RecoveredCurrency.Format(4000,0,2),ball.Reward.text);Assert.IsFalse(reward.IsAnimating);
        } finally {Object.Destroy(ball.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
