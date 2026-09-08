using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredDownWinTextTests
{
    [UnityTest]
    public IEnumerator DelayedLookupPreservesNativeAccumulatorAndCancellation()
    {
        var canvas=new GameObject("Canvas",typeof(Canvas));
        var win=Object.Instantiate(Resources.Load<RecoveredDownWinText>("RecoveredUI/DownWinText"),canvas.transform,false);
        var coin=new GameObject("Coin");var absent=new GameObject("Absent");
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            Assert.AreEqual(70,win.Label.fontSize);Assert.IsFalse(win.Label.enableAutoSizing);
            Assert.AreEqual(new Vector2(-7,58),win.Label.rectTransform.anchoredPosition);
            Assert.AreEqual(new Vector2(225.87f,59.02f),win.Label.rectTransform.sizeDelta);
            Assert.AreEqual("QuorumStd-Black_zitidi.com SDF",win.Label.font.name);
            Assert.IsTrue(win.Label.enableVertexGradient);Assert.AreEqual(.54509807f,win.Label.colorGradient.topLeft.r,.00001f);
            win.Bind(0);Assert.AreEqual("GOOD LUCK",win.Label.text);win.Started();Assert.AreEqual("GOOD LUCK",win.Label.text);win.BeginScan();
            win.Register(coin,123);Assert.Throws<System.ArgumentException>(()=>win.Register(coin,1));
            int changes=0;win.Changed+=value=>changes++;
            win.PresentationFinished(coin);Time.timeScale=0;
            for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(0,changes);Time.timeScale=1;float start=Time.time;
            for(int i=0;i<20&&changes==0;i++)yield return null;
            Assert.GreaterOrEqual(Time.time-start,.299f);Assert.AreEqual(123,win.Total);Assert.AreEqual("$1.23",win.Label.text);
            win.PresentationFinished(coin); // No deduplication: original dictionary is not consumed.
            for(int i=0;i<20&&changes<2;i++)yield return null;
            Assert.AreEqual(246,win.Total);
            win.PresentationFinished(coin);win.BeginScan(); // Delayed read observes the cleared map.
            Assert.AreEqual(246,win.Total);Assert.AreEqual("$2.46",win.Label.text);
            for(int i=0;i<20&&changes<3;i++)yield return null;
            Assert.AreEqual(0,win.Total);Assert.AreEqual("$0.00",win.Label.text);
            win.Bind(1);win.BeginScan();win.Register(coin,245);win.PresentationFinished(coin);
            for(int i=0;i<20&&changes<4;i++)yield return null;
            Assert.AreEqual("R$2,45",win.Label.text);
            win.PresentationFinished(absent);win.gameObject.SetActive(false);
            for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(4,changes);Assert.IsNull(win.Error);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(canvas);Object.Destroy(coin);Object.Destroy(absent);}
    }
}
