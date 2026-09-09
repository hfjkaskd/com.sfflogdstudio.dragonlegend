using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredGuideTextTests
{
    [UnityTest]
    public IEnumerator NativeCharactersPauseSlowFramesAndBackgroundRevealPreserveCallbackBoundaries()
    {
        var view=Object.Instantiate(Resources.Load<RecoveredGuideText>("RecoveredUI/GuideText"));
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=0;Time.captureDeltaTime=.05f;int completed=0;
            view.SetText(1,()=>completed++);Assert.AreEqual("C",view.Label.text);
            for(int frame=0;frame<5;frame++)yield return null;
            Assert.AreEqual("C",view.Label.text);Assert.AreEqual(0,completed);
            Time.timeScale=1;
            int prior=1;
            for(int frame=0;frame<150&&completed==0;frame++)
            {
                yield return null;
                Assert.LessOrEqual(view.Label.text.Length-prior,1,"A slow frame must not reveal multiple characters.");
                prior=view.Label.text.Length;
                if(prior=="Click SPIN and let the dragon breathe fire into your wins!".Length&&completed==0)break;
            }
            Assert.AreEqual("Click SPIN and let the dragon breathe fire into your wins!",view.Label.text);
            Assert.AreEqual(0,completed,"Last character still owes its scaled wait.");
            for(int frame=0;frame<5&&completed==0;frame++)yield return null;Assert.AreEqual(1,completed);
            view.SetText(2,()=>completed++);Assert.AreEqual("T",view.Label.text);view.RevealAll();
            Assert.AreEqual("Tap here to unleash extra Wilds!",view.Label.text);
            for(int frame=0;frame<80;frame++)yield return null;Assert.AreEqual(1,completed,"Background reveal must not call completion.");
            view.SetText(99);Assert.AreEqual("T",view.Label.text,"Unrecognized step retains prior source text.");
            view.Cancel();string stopped=view.Label.text;for(int frame=0;frame<5;frame++)yield return null;Assert.AreEqual(stopped,view.Label.text);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(view.gameObject);}
    }
}
