using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredFreeStartArtTests
{
    [UnityTest]
    public IEnumerator OriginalFreeStartLayersRenderLoopAndPauseOnNativeClock()
    {
        var host=new GameObject("Current Free start artwork",typeof(RectTransform),typeof(Canvas));
        var canvas=host.GetComponent<Canvas>();host.layer=5;
        ((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var cameraHost=new GameObject("Current Free start camera",typeof(Camera));
        var camera=cameraHost.GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);
        camera.orthographic=true;camera.orthographicSize=960;camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=Color.black;camera.cullingMask=32;
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        var rain=Object.Instantiate(Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/ef_xjpl"),host.transform);
        var dragon=Object.Instantiate(Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/ef_starttc"),host.transform);
        ((RectTransform)rain.transform).anchoredPosition=new Vector2(0,-5.5012817f);
        var rect=(RectTransform)dragon.transform;
        Assert.AreEqual(new Vector2(953.99994f,838.45105f),rect.sizeDelta);
        Assert.AreEqual(new Vector2(.47798738f,.3499919f),rect.pivot);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
        var previous=RenderTexture.active;Texture2D capture=null;
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int i=0;i<10;i++)yield return null;
            var startPlayer=dragon.GetComponent<Animation>();var rainPlayer=rain.GetComponent<Animation>();
            Assert.AreEqual(2,startPlayer["starttc"].length,.00001f);
            Assert.AreEqual(5,rainPlayer["idle"].length,.00001f);
            Time.timeScale=0;yield return null;yield return null;
            float startTime=startPlayer["starttc"].time,rainTime=rainPlayer["idle"].time;
            for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(startTime,startPlayer["starttc"].time,.00001f);
            Assert.AreEqual(rainTime,rainPlayer["idle"].time,.00001f);
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            foreach(float time in new[]{.173f,1.27f,3.7f}) {
                dragon.Sample(0,time%2);rain.Sample(0,time%5);
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-start-art-"+Mathf.RoundToInt(time*1000)+".png"),capture.EncodeToPNG());
                int colored=0;foreach(var p in capture.GetPixels32())if(p.r>80||p.g>80||p.b>80)colored++;
                Assert.Greater(colored,100000,"Free start frame "+time);
            }
            Time.timeScale=1;for(int i=0;i<110;i++)yield return null;
            Assert.IsTrue(startPlayer.IsPlaying("starttc"));Assert.IsTrue(rainPlayer.IsPlaying("idle"));
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            if(capture!=null)Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);
        }
    }
}
