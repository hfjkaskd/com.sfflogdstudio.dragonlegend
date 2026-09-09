using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredTreasureCardTests
{
    [UnityTest]
    public IEnumerator OriginalCardFacesFollowQuadraticCloseAndOutBackOpenBeforeCallbacks()
    {
        float timeScale=Time.timeScale,delta=Time.captureDeltaTime;
        var host=new GameObject("Treasure capture",typeof(RectTransform),typeof(Canvas));
        var cameraHost=new GameObject("Treasure camera",typeof(Camera));
        var camera=cameraHost.GetComponent<Camera>();var canvas=host.GetComponent<Canvas>();
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
        try {
            Time.timeScale=1;Time.captureDeltaTime=1f/64;
            var card=Object.Instantiate(Resources.Load<RecoveredTreasureCard>("RecoveredUI/TreasureCard"),host.transform,false);
            Assert.AreEqual(.25f,card.HalfDuration);
            Assert.AreEqual(new Vector2(100,100),((RectTransform)card.transform).sizeDelta);
            Assert.AreEqual("CardBlack",card.Back.name);Assert.AreEqual("CardFont",card.Front.name);
            Assert.AreEqual("tc_sc_bak01",card.Front.GetComponent<Image>().sprite.name);
            Assert.AreEqual("tc_sc_bak02",card.Back.GetComponent<Image>().sprite.name);
            var icon=card.Front.transform.Find("Image").GetComponent<Image>().sprite;
            Assert.IsNotNull(icon);Assert.AreEqual(new Vector2(222,219),icon.rect.size);
            var buttons=card.GetComponentsInChildren<Button>(true);Assert.AreEqual(2,buttons.Length);
            foreach(var button in buttons){Assert.IsNotNull(button.targetGraphic);Assert.AreEqual(0,button.onClick.GetPersistentEventCount());}
            card.transform.localScale=new Vector3(2,3,4);card.Init(999);
            Assert.AreEqual(Vector3.one,card.transform.localScale);Assert.AreEqual(0,card.Reward);
            Assert.IsFalse(card.IsFlipped);Assert.IsTrue(card.Back.activeSelf);Assert.IsFalse(card.Front.activeSelf);
            var order=new List<string>();
            card.SoundRequested+=sound=>order.Add(sound);
            card.OnFlipComplete+=value=>{Assert.AreSame(card,value);order.Add("event");};
            yield return null;
            Capture(camera,target,"current-treasure-card-back.png");
            card.Flip(()=>order.Add("callback"));card.Flip(()=>order.Add("duplicate"));
            Assert.IsTrue(card.IsFlipped);CollectionAssert.AreEqual(new[]{"cardReavel"},order);
            Time.timeScale=0;
            for(int frame=0;frame<3;frame++)yield return null;
            Assert.AreEqual(Vector3.one,card.transform.localScale);Assert.IsTrue(card.Back.activeSelf);
            Time.timeScale=1;
            // Inspect the actual native Unity Update animation at each captured frame.
            for(int frame=1;frame<=32;frame++) {
                yield return null;
                float t=frame/16f;float expected=t<1?1-t*t:OutBack(t-1);
                Assert.That(card.transform.localScale.x,Is.EqualTo(expected).Within(.00002f),"frame "+frame);
                Assert.AreEqual(1,card.transform.localScale.y);Assert.AreEqual(1,card.transform.localScale.z);
                Assert.AreEqual(frame<16,card.Back.activeSelf);Assert.AreEqual(frame>=16,card.Front.activeSelf);
                Assert.AreEqual(frame<32?1:3,order.Count);
                if(frame==26)Assert.Greater(card.transform.localScale.x,1.09f,"Native OutBack overshoots; Bounce would remain below one.");
            }
            CollectionAssert.AreEqual(new[]{"cardReavel","event","callback"},order);
            Capture(camera,target,"current-treasure-card-front.png");
            card.transform.localScale=new Vector3(.37f,2,3);card.SetFlipped(false);
            Assert.AreEqual(new Vector3(.37f,2,3),card.transform.localScale);
            Assert.IsTrue(card.Back.activeSelf);Assert.IsFalse(card.Front.activeSelf);
        } finally {
            camera.targetTexture=null;Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);
            Time.timeScale=timeScale;Time.captureDeltaTime=delta;
        }
    }

    [UnityTest]
    public IEnumerator InitAndDisableDoNotCancelAnAlreadyStartedFlip()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var card=Object.Instantiate(Resources.Load<RecoveredTreasureCard>("RecoveredUI/TreasureCard"));
        try {
            Time.timeScale=1;Time.captureDeltaTime=1f/64;card.Init(0);
            int callbacks=0;card.Flip(()=>callbacks++);
            for(int i=0;i<8;i++)yield return null;
            card.Init(123);card.transform.localScale=new Vector3(1,2,3);card.gameObject.SetActive(false);
            for(int i=0;i<26;i++)yield return null;
            Assert.AreEqual(1,callbacks);Assert.IsFalse(card.IsFlipped,"Init resets flag; sequence callbacks only change faces.");
            Assert.IsFalse(card.Back.activeSelf);Assert.IsTrue(card.Front.activeSelf);
            Assert.AreEqual(new Vector3(1,2,3),card.transform.localScale);
            card.gameObject.SetActive(true);card.Init(1);card.Flip(()=>callbacks++);
            Object.Destroy(card.gameObject);
            for(int i=0;i<34;i++)yield return null;
            Assert.AreEqual(1,callbacks);
        } finally {if(card!=null)Object.Destroy(card.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
    private static float OutBack(float t)
    {
        t-=1;
        return t*t*(2.70158f*t+1.70158f)+1;
    }
    private static void Capture(Camera camera,RenderTexture target,string name)
    {
        var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name),image.EncodeToPNG());
        } finally {RenderTexture.active=previous;Object.Destroy(image);}
    }
}
