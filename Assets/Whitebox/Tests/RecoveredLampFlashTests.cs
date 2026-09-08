using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredLampFlashTests
{
    [UnityTest]
    public IEnumerator LampFlashUsesOriginalSixAdditiveRegionsAndHalfSecondClip()
    {
        var root = new GameObject("Flash canvas", typeof(Canvas));
        var flash = Object.Instantiate(Resources.Load<RecoveredLampFlash>("RecoveredUI/LampFlash"), root.transform, false);
        float scale = Time.timeScale, delta = Time.captureDeltaTime;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .05f;
            Assert.AreEqual(new Vector2(50,50), ((RectTransform)flash.transform).sizeDelta);
            var images = flash.GetComponentsInChildren<Image>(); Assert.AreEqual(6, images.Length);
            Image star = null, ring = null;
            foreach (var image in images) {
                Assert.IsFalse(image.raycastTarget); Assert.AreEqual(1, image.material.GetFloat("_DestinationBlend"));
                if(image.transform.parent.name=="Slot7")star=image;
                if(image.transform.parent.name=="Slot2")ring=image;
            }
            Assert.IsNotNull(star); Assert.IsNotNull(ring);
            var clip = flash.GetComponent<Animation>().GetClip("dianliang"); Assert.AreEqual(.5f,clip.length,.00001f);
            clip.SampleAnimation(flash.gameObject,0); Assert.AreEqual(0,star.color.a);
            Assert.AreEqual(.4f,ring.transform.parent.parent.localScale.x,.00001f);
            clip.SampleAnimation(flash.gameObject,1f/15f); Assert.AreEqual(1,star.color.a,.00001f);
            Assert.AreEqual(.6f,ring.transform.parent.parent.localScale.x,.00001f);
            clip.SampleAnimation(flash.gameObject,.5f);
            foreach(var image in images)Assert.AreEqual(0,image.color.a,.00001f);
            Assert.AreEqual(.9f,ring.transform.parent.parent.localScale.x,.00001f);
            int completed=0;flash.Completed+=value=>{Assert.AreSame(flash,value);completed++;};
            flash.Play();Time.timeScale=0;for(int i=0;i<10;i++)yield return null;
            Assert.IsTrue(flash.IsPlaying);Assert.AreEqual(0,completed);Time.timeScale=1;float start=Time.time;
            for(int i=0;i<20&&flash.IsPlaying;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.GreaterOrEqual(Time.time-start,.499f);
            flash.Play();flash.gameObject.SetActive(false);flash.gameObject.SetActive(true);
            for(int i=0;i<15;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.IsFalse(flash.IsPlaying);
        } finally { Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root); }
    }
}
