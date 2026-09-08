using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredCashFlightCurveTests
{
    [UnityTest]
    public IEnumerator NativeEaseFourSamplesSineAtEveryQuarterOfActualCashFlight()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var parent=new GameObject("Cash curve test parent",typeof(RectTransform));
        var target=new GameObject("Cash curve test target",typeof(RectTransform));target.transform.position=new Vector3(4,3,0);
        var item=Object.Instantiate(Resources.Load<RecoveredCashFlightItem>("RecoveredUI/CashFlight/FlyCoinItem"),parent.transform);
        try {
            Time.timeScale=1;Time.captureDeltaTime=.075f;int arrived=0;item.Arrived+=value=>arrived++;
            item.Scatter(parent.transform,null,Vector2.zero);
            for(int i=0;i<5;i++)yield return null;
            item.Schedule(0,target.transform);for(int i=0;i<5&&!item.IsFlying;i++)yield return null;
            Assert.IsTrue(item.IsFlying);var start=item.StartPoint;var end=item.EndPoint;
            var control=(start+end)*.5f+Vector3.up*(Vector3.Distance(start,end)*.3f);
            target.transform.position+=Vector3.one*10;
            for(int quarter=1;quarter<=4;quarter++) {
                yield return null;
                float time=quarter*.25f;
                // Native Ease enum value 4 resolves to InOutSine, not InQuad (5).
                float eased=(1-Mathf.Cos(Mathf.PI*time))*.5f,inverse=1-eased;
                var expected=start*(inverse*inverse)+control*(2*inverse*eased)+end*(eased*eased);
                Assert.That(Vector3.Distance(expected,item.transform.position),Is.LessThan(.0001f),"Quarter "+quarter);
            }
            Assert.AreEqual(1,arrived);Assert.IsFalse(item.IsFlying);
        } finally {Object.Destroy(parent);Object.Destroy(target);Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
