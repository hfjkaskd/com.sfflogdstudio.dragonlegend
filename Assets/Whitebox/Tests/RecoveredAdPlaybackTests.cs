using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredAdPlaybackTests
{
    [UnityTest]
    public IEnumerator RequestShowsToastAndCompletesExactlyOnceWhileGamePaused()
    {
        var host=new GameObject("Ad test canvas",typeof(RectTransform),typeof(Canvas));
        var template=Resources.Load<GameObject>("Whitebox/GameEntry").GetComponentInChildren<RecoveredAdSimulationControls>(true);
        var view=Object.Instantiate(template,host.transform,false);float scale=Time.timeScale;
        try
        {
            var ads=new LocalAdFacade();view.Bind(ads);Time.timeScale=0;int rewarded=0,failed=0;
            ads.PlayRewardAd(()=>rewarded++,()=>failed++,"freespin","freespin");
            var tip=view.GetComponentInChildren<RecoveredTipsWindow>(true);
            Assert.IsNotNull(tip);Assert.AreEqual("Simulated ad started",tip.Label.text);Assert.IsTrue(ads.Pending);
            yield return new WaitForSecondsRealtime(2.2f);
            Assert.IsFalse(ads.Pending);Assert.AreEqual(1,rewarded);Assert.AreEqual(0,failed);
            Assert.AreEqual("Simulated ad completed",tip.Label.text);
            view.RewardButton.onClick.Invoke();Assert.AreEqual(1,rewarded);
            ads.PlayRewardAd(()=>rewarded++,()=>failed++,"treasure","treasure");
            view.FailureButton.onClick.Invoke();Assert.AreEqual(1,failed);Assert.AreEqual("Ad failed - please try again",tip.Label.text);
            yield return new WaitForSecondsRealtime(2.2f);Assert.AreEqual(1,rewarded);
            ads.PlayRewardAd(()=>rewarded++,()=>failed++,"freespin","freespin");
            view.Bind(null);yield return new WaitForSecondsRealtime(2.2f);Assert.AreEqual(1,rewarded);Assert.IsFalse(tip.gameObject.activeSelf);
        }
        finally{Time.timeScale=scale;Object.Destroy(host);}
    }
    [Test]
    public void CompletionCanStartAnotherRequestWithoutLosingIt()
    {
        var ads=new LocalAdFacade();int started=0,completed=0;
        ads.RewardAdStarted+=()=>started++;ads.RewardAdCompleted+=outcome=>completed++;
        ads.PlayRewardAd(()=>ads.PlayRewardAd(()=>{},()=>{},"next","next"),()=>{},"first","first");
        ads.Complete(AdOutcome.Rewarded);Assert.IsTrue(ads.Pending);Assert.AreEqual("next",ads.Placement);
        Assert.AreEqual(2,started);Assert.AreEqual(1,completed);ads.Complete(AdOutcome.Failed);Assert.IsFalse(ads.Pending);
    }
}
