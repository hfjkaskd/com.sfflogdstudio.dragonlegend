using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredBankFloatTests
{
    [UnityTest]
    public IEnumerator AuthoredItemFloatsLinearlyWhileInactiveAndStopRestoresOriginalPosition()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;GameObject item=null;
        try
        {
            Time.timeScale=0;Time.captureDeltaTime=.05f;
            item=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/BankItem"));
            var motion=item.GetComponent<RecoveredBankFloat>();Assert.IsNotNull(motion);
            var original=new Vector3(13,40,7);item.transform.localPosition=original;
            yield return null; // Native Start captures the authored local position.
            item.transform.localPosition=original+Vector3.right;motion.Stop();
            Assert.AreEqual(original+Vector3.right,item.transform.localPosition,"A null latest handle does not restore position.");
            motion.Play(20,1);for(int i=0;i<4;i++)yield return null;
            Assert.AreEqual(40,item.transform.localPosition.y);
            item.SetActive(false);Time.timeScale=1;
            for(int i=0;i<8;i++)yield return null;
            Assert.That(item.transform.localPosition.y,Is.InRange(47f,49f));
            Assert.AreEqual(14,item.transform.localPosition.x);Assert.AreEqual(7,item.transform.localPosition.z);
            Time.timeScale=0;var frozen=item.transform.localPosition;
            for(int i=0;i<4;i++)yield return null;Assert.AreEqual(frozen,item.transform.localPosition);
            Time.timeScale=1;for(int i=0;i<12;i++)yield return null;
            Assert.That(item.transform.localPosition.y,Is.InRange(59f,60.001f));
            for(int i=0;i<10;i++)yield return null;
            Assert.That(item.transform.localPosition.y,Is.InRange(49f,51f),"The second leg returns linearly rather than restarting.");
            motion.Stop();Assert.AreEqual(original,item.transform.localPosition);
            for(int i=0;i<4;i++)yield return null;Assert.AreEqual(original,item.transform.localPosition);
            motion.Play(20,1);yield return null;motion.Play(30,1);yield return null;
            motion.Stop();Assert.AreEqual(original,item.transform.localPosition);
            for(int i=0;i<4;i++)yield return null;
            Assert.Greater(item.transform.localPosition.y,original.y,"Native Stop only kills the latest handle; an earlier overwritten tween remains active.");
        }
        finally {if(item!=null)Object.Destroy(item);Time.timeScale=scale;Time.captureDeltaTime=delta;}
        yield return null;
    }
}
