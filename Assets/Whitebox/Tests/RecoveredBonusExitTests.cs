using System;
using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredBonusExitTests
{
    private static RecoveredGameplayRules Rules(int free)=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii=new QonriiPoro{Joqkpor=new List<int>{10000,5000,1000},JpOpp=new List<int>{1}},
        Ronig=new RonigPoro{Ltoo=new List<int>{12},Qoi=new List<int>{0},Jin=new List<int>{0},Roo=new List<int>{0},
            Rgkorp=new List<int>{0},RrggRimgg=new List<int>{free}}});
    [UnityTest]
    public IEnumerator ManualAndFinalCardExitPreserveHideMusicAndSourceTiming()
    {
        var host=new GameObject("Bonus exit canvas",typeof(RectTransform),typeof(Canvas));
        var exit=Object.Instantiate(Resources.Load<RecoveredBonusExit>("RecoveredUI/BonusExit"));
        var transition=Object.Instantiate(Resources.Load<RecoveredSceneTransition>("RecoveredEffects/SceneTransition"));
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=UnityEngine.Random.state;
        try{
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            for(int automatic=0;automatic<2;automatic++){
                var window=Object.Instantiate(Resources.Load<RecoveredBonusWindow>("RecoveredUI/BonusWindow"),host.transform);
                var rules=Rules(automatic==0?0:12);var progress=new RecoveredPlayerProgress(rules,()=>{},new PlayerData());
                Exception failure=null;window.Failed+=e=>failure=e;
                var order=new List<string>();double started=0,musicAt=0,finishedAt=0,endedAt=0;int calls=0;
                Action pause=()=>{order.Add("pause");started=Time.timeAsDouble;};
                Action<string> sound=s=>order.Add(s);
                Action<string> music=s=>{order.Add(s);musicAt=Time.timeAsDouble;Assert.IsFalse(window.gameObject.activeSelf);};
                exit.PauseMusicRequested+=pause;exit.SoundRequested+=sound;exit.ChangeMusicRequested+=music;
                exit.Bind(window,transition,progress,()=>{calls++;finishedAt=Time.timeAsDouble;});
                window.Show(progress,rules,new LocalAdFacade(),null,()=>1,false,0);
                window.ExitRequested+=()=>endedAt=Time.timeAsDouble;
                for(int frame=0;frame<20;frame++)yield return null;
                if(automatic==0){
                    Assert.IsTrue(window.CloseButton.gameObject.activeSelf);
                    window.CloseButton.onClick.Invoke();window.CloseButton.onClick.Invoke();
                    Assert.IsTrue(window.Selection.IsEnd);Assert.IsFalse(exit.IsClosing,"Native WaitUntil resumes on the runner");
                }else{
                    for(int card=0;card<12;card++){
                        window.Card(card).Button.onClick.Invoke();
                        for(int frame=0;frame<60&&window.Selection.IsClicked;frame++)yield return null;
                        Assert.IsNull(failure);Assert.IsFalse(window.Selection.IsClicked);
                    }
                    Assert.IsTrue(window.Selection.IsEnd);Assert.IsFalse(exit.IsClosing);
                    Time.timeScale=0;for(int frame=0;frame<8;frame++)yield return null;
                    Assert.IsFalse(exit.IsClosing);Time.timeScale=1;
                }
                for(int frame=0;frame<100&&!exit.IsClosing;frame++)yield return null;
                Assert.IsTrue(exit.IsClosing);Assert.IsTrue(window.gameObject.activeSelf);
                if(automatic!=0)Assert.That(started-endedAt,Is.InRange(2.0,2.08));
                Time.timeScale=0;for(int frame=0;frame<8;frame++)yield return null;
                Assert.IsTrue(window.gameObject.activeSelf);Assert.AreEqual(0,calls);Time.timeScale=1;
                // Read mode at the second transition callback, not at Bind/close.
                progress.GameSlotType=automatic==0?RecoveredSlotType.Base:RecoveredSlotType.Free;
                for(int frame=0;frame<60&&window.gameObject.activeSelf;frame++)yield return null;
                Assert.IsFalse(window.gameObject.activeSelf);Assert.AreEqual(0,calls);
                Assert.That(Time.timeAsDouble-started,Is.InRange(1.075,1.18));
                for(int frame=0;frame<100&&musicAt==0;frame++)yield return null;
                Assert.Greater(musicAt,0);Assert.AreEqual(0,calls);
                Assert.That(musicAt-started,Is.InRange(3.0,3.1));
                Time.timeScale=0;for(int frame=0;frame<8;frame++)yield return null;Assert.AreEqual(0,calls);
                Time.timeScale=1;for(int frame=0;frame<20&&calls==0;frame++)yield return null;
                Assert.AreEqual(1,calls);Assert.That(finishedAt-musicAt,Is.InRange(.2,.26));
                CollectionAssert.AreEqual(new[]{"pause","transform",automatic==0?"normalBg":"freeBg"},order);
                for(int frame=0;frame<10;frame++)yield return null;Assert.AreEqual(1,calls);Assert.IsNull(failure);
                exit.Unbind();exit.PauseMusicRequested-=pause;exit.SoundRequested-=sound;exit.ChangeMusicRequested-=music;
                Object.Destroy(window.gameObject);yield return null;
            }
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;Object.Destroy(host);Object.Destroy(exit.gameObject);Object.Destroy(transition.gameObject);}
    }
}
