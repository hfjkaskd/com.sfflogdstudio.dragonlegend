using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredCashOutEntranceTests
{
    [UnityTest]
    public IEnumerator WaitsForCellsThenStaggersScaleAndSlidesBottomWithScaledTime()
    {
        var window=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutWindow"));float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try
        {
            window.SetActive(true);var entrance=window.GetComponent<RecoveredCashOutEntrance>();var list=window.GetComponentInChildren<RecoveredCashOutList>();var bottom=window.GetComponentInChildren<RecoveredCashOutBottom>();var rect=(RectTransform)bottom.transform;Vector2 initial=rect.anchoredPosition;
            Time.timeScale=0;Time.captureDeltaTime=.025f;entrance.Play(()=>true);for(int i=0;i<3;i++)yield return null;
            Assert.IsFalse(list.IsCreateFinished);Assert.AreEqual(initial,rect.anchoredPosition);
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{Qogt=new List<int>{100,200,300,400}}});list.Scroll.enabled=false;list.Initialize(rules,new RecoveredPlayerProgress(rules,()=>Assert.Fail("Animation must not save"),new PlayerData()),bottom,()=>100,new Vector2(1080,620),1,0);
            for(int i=0;i<3;i++)yield return null;Assert.IsTrue(list.IsCreateFinished);Assert.AreEqual(initial+Vector2.down*450,rect.anchoredPosition);
            var first=list.Scroll.content.GetChild(0);var second=list.Scroll.content.GetChild(1);var third=list.Scroll.content.GetChild(2);
            Assert.AreEqual(Vector3.zero,first.localScale);Assert.AreEqual(Vector3.zero,second.localScale);Assert.AreEqual(Vector3.zero,third.localScale);
            Time.timeScale=1;yield return null;
            Assert.Greater(first.localScale.x,0);Assert.AreEqual(Vector3.zero,third.localScale);Assert.Greater(rect.anchoredPosition.y,initial.y-450);
            for(int i=0;i<4;i++)yield return null;Assert.Greater(first.localScale.x,1,"OutBack overshoots before settling.");Assert.Greater(second.localScale.x,third.localScale.x);
            for(int i=0;i<15;i++)yield return null;
            Assert.That(first.localScale.x,Is.EqualTo(1).Within(.0001f));Assert.That(third.localScale.x,Is.EqualTo(1).Within(.0001f));Assert.That(rect.anchoredPosition.y,Is.EqualTo(initial.y).Within(.0001f));
            Time.timeScale=0;entrance.Play(()=>true);for(int i=0;i<2;i++)yield return null;Assert.AreEqual(Vector3.zero,first.localScale);
            entrance.Cancel();Time.timeScale=1;for(int i=0;i<15;i++)yield return null;Assert.AreEqual(Vector3.zero,first.localScale);Assert.AreEqual(initial.y-450,rect.anchoredPosition.y);
        }
        finally{Object.Destroy(window);Time.timeScale=scale;Time.captureDeltaTime=delta;}
        yield return null;
    }
    [UnityTest]
    public IEnumerator ChecksCurrentModeAfterWaitAndDoesNotAnimateGiftBranch()
    {
        var window=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutWindow"));
        try
        {
            window.SetActive(true);var entrance=window.GetComponent<RecoveredCashOutEntrance>();var list=window.GetComponentInChildren<RecoveredCashOutList>();var bottom=window.GetComponentInChildren<RecoveredCashOutBottom>();Vector2 initial=((RectTransform)bottom.transform).anchoredPosition;
            bool cash=true;entrance.Play(()=>cash);cash=false;
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{Qogt=new List<int>{100}}});list.Scroll.enabled=false;list.Initialize(rules,new RecoveredPlayerProgress(rules,()=>Assert.Fail("No save"),new PlayerData()),bottom,()=>100,new Vector2(1080,620),1,0);
            for(int i=0;i<4;i++)yield return null;Assert.AreEqual(initial,((RectTransform)bottom.transform).anchoredPosition);Assert.AreEqual(Vector3.one,list.ItemAt(0).transform.localScale);
        }
        finally{Object.Destroy(window);}
        yield return null;
    }
}
