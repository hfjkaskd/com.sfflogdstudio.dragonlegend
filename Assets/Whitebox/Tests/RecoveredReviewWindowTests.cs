using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredReviewWindowTests
{
    [UnityTest]
    public IEnumerator StarsResetLowRatingsHideAndFiveStarsNavigateThenDestroy()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;RecoveredReviewWindow window=null;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            window=Object.Instantiate(Resources.Load<RecoveredReviewWindow>("RecoveredUI/ReviewWindow"));
            int calls=0,navigations=0;string uri=null;window.Bind(null,"test.package",value=>{uri=value;navigations++;});
            for(int count=0;count<=5;count++)
            {
                window.Show(()=>calls++);Assert.AreEqual(0,window.StarCount);
                for(int i=0;i<5;i++)Assert.IsFalse(window.Highlight(i).activeSelf);
                for(int i=0;i<8;i++)yield return null;
                if(count>0)window.Star(count-1).onClick.Invoke();
                Assert.AreEqual(count,window.StarCount);for(int i=0;i<5;i++)Assert.AreEqual(i<count,window.Highlight(i).activeSelf);
                Time.timeScale=0;window.ClaimButton.onClick.Invoke();
                Assert.AreEqual(count==5?1:0,navigations);Assert.AreEqual(count,calls);
                for(int i=0;i<4;i++)yield return null;Assert.AreEqual(count,calls);
                Time.timeScale=1;for(int i=0;i<10;i++)yield return null;
                Assert.AreEqual(count+1,calls);
                if(count<5){Assert.IsNotNull(window);Assert.IsFalse(window.gameObject.activeSelf);}
            }
            Assert.AreEqual("market://details?id=test.package",uri);Assert.IsTrue(window==null,"Five-star Claim uses Close(true), destroying the cached window.");
            window=Object.Instantiate(Resources.Load<RecoveredReviewWindow>("RecoveredUI/ReviewWindow"));window.Bind(null,"test.package",value=>navigations++);
            window.Show(()=>calls++);window.Star(4).onClick.Invoke();window.Star(1).onClick.Invoke();Assert.AreEqual(2,window.StarCount);Assert.IsFalse(window.Highlight(2).activeSelf);
            window.CloseButton.onClick.Invoke();for(int i=0;i<10;i++)yield return null;Assert.AreEqual(7,calls);Assert.AreEqual(1,navigations);
            window.Show(()=>calls++);window.CloseButton.onClick.Invoke();window.Cancel();for(int i=0;i<10;i++)yield return null;Assert.AreEqual(7,calls);
        }
        finally{if(window!=null)Object.Destroy(window.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
        yield return null;
    }
}
