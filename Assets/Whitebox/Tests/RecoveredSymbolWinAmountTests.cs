using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredSymbolWinAmountTests
{
    [TestCase(0,7)]
    [TestCase(3,0)]
    public void ZeroTotalOrZeroLineReturnsWithoutReadingBonusOrChangingText(float line,float total)
    {
        var value=Object.Instantiate(Resources.Load<RecoveredSymbolWinAmount>("RecoveredUI/SymbolWinAmount"));
        try {
            value.Bind(0);Assert.AreEqual(string.Empty,value.Label.text);value.Label.text="preserved";bool completed=false;
            value.Begin(line,total,()=>{Assert.Fail("Native branch must not create a tween");return 0;},()=>completed=true);
            Assert.IsTrue(completed);Assert.IsFalse(value.IsRunning);Assert.AreEqual("preserved",value.Label.text);
        } finally {Object.DestroyImmediate(value.gameObject);}
    }
    [UnityTest]
    public IEnumerator LiveBonusGetterLineTargetScaledPauseAndHalfSecondContinuation()
    {
        var value=Object.Instantiate(Resources.Load<RecoveredSymbolWinAmount>("RecoveredUI/SymbolWinAmount"));
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            value.Bind(0);Time.timeScale=1;Time.captureDeltaTime=.05f;
            Assert.AreEqual(new Vector2(1080,130),((RectTransform)value.transform).sizeDelta);
            Assert.AreEqual(new Vector2(-.0034179688f,336.26917f),((RectTransform)value.transform).anchoredPosition);
            Assert.AreEqual(Vector3.one*1.5f,value.Label.transform.localScale);Assert.AreEqual(0,value.Label.fontSize);
            Assert.AreEqual("Green",value.Label.font.name);
            float bonus=5,began=Time.time,ended=-1;int reads=0;
            value.Begin(12,17,()=>{reads++;return bonus;},()=>ended=Time.time);bonus=9;
            yield return null;yield return null;
            Assert.AreEqual(1,reads);Assert.AreEqual(9,bonus);Assert.Greater(value.DisplayedAmount,9);Assert.Less(value.DisplayedAmount,12);
            float paused=value.DisplayedAmount;Time.timeScale=0;
            for(int i=0;i<15;i++)yield return null;
            Assert.AreEqual(paused,value.DisplayedAmount);Assert.AreEqual(-1,ended);
            Time.timeScale=1;
            for(int i=0;i<20&&ended<0;i++)yield return null;
            Assert.GreaterOrEqual(ended-began,.5f-.0001f);Assert.AreEqual(12,value.DisplayedAmount,.00001f);
            Assert.AreEqual(RecoveredCurrency.Format(12,0,2),value.Label.text);Assert.IsFalse(value.IsRunning);Assert.IsFalse(value.IsCounting);
            Assert.AreEqual(9,bonus,"Text setter must not mutate bonus or award total");
            bool stale=false;value.Begin(25,25,()=>0,()=>stale=true);yield return null;value.Cancel();
            string stopped=value.Label.text;for(int i=0;i<20;i++)yield return null;
            Assert.IsFalse(stale);Assert.AreEqual(stopped,value.Label.text);value.Bind(0);Assert.AreEqual(string.Empty,value.Label.text);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(value.gameObject);}
    }
}
