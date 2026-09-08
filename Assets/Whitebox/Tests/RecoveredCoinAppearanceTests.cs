using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredCoinAppearanceTests
{
    [UnityTest]
    public IEnumerator AppearanceUsesOriginalRingsThenResetsBeforeIdle()
    {
        var host=new GameObject("Coin canvas",typeof(Canvas));host.layer=5;
        var cameraHost=new GameObject("Coin camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=5;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;
        var target=new RenderTexture(512,512,24);camera.targetTexture=target;
        var coin=Object.Instantiate(Resources.Load<RecoveredCoinIdle>("RecoveredUI/CoinAppearance"),host.transform,false);
        var previous=RenderTexture.active;Texture2D capture=null;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            coin.PlayAppearance(); // Must survive Start in the first frame.
            yield return null;Assert.IsTrue(coin.IsShowing);
            var player=coin.GetComponent<Animation>();var show=player.GetClip("zcjb_chuxian");
            Assert.AreEqual(.5f,show.length,.0001f);
            show.SampleAnimation(coin.gameObject,1f/6f);
            Image ring=null,add=null;
            foreach(var graphic in coin.GetComponentsInChildren<Image>()) {
                if(graphic.transform.parent.name=="Slot27")ring=graphic;
                if(graphic.transform.parent.name=="Slot29")add=graphic;
                Assert.IsFalse(graphic.raycastTarget);
            }
            Assert.IsNotNull(ring);Assert.IsNotNull(add);Assert.IsTrue(ring.enabled);Assert.IsTrue(add.enabled);
            Assert.AreEqual(1,ring.color.a,.0001f);Assert.AreEqual(1,add.color.a,.0001f);
            Assert.AreEqual(216f/255,add.color.g,.0001f);
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(512,512,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,512,512),0,0);capture.Apply();
            int gold=0;foreach(var pixel in capture.GetPixels32())if(pixel.r>120&&pixel.g>80&&pixel.b<pixel.r)gold++;
            Assert.Greater(gold,1000);
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-coin-appearance.png")),capture.EncodeToPNG());
            for(int i=0;i<25&&coin.IsShowing;i++)yield return null;
            Assert.IsFalse(coin.IsShowing);Assert.IsTrue(player.IsPlaying("zcjb_idle"));
            Assert.IsFalse(ring.enabled);Assert.IsFalse(add.enabled);
            Assert.AreEqual(.88f,add.transform.parent.parent.localScale.x,.0001f);
            coin.PlayAppearance();Assert.IsTrue(coin.IsShowing);
            coin.gameObject.SetActive(false);coin.gameObject.SetActive(true);
            Assert.IsFalse(coin.IsShowing);Assert.IsTrue(player.IsPlaying("zcjb_idle"));
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;
            Object.Destroy(host);Object.Destroy(cameraHost);if(capture!=null)Object.Destroy(capture);target.Release();Object.Destroy(target);
        }
    }
}
