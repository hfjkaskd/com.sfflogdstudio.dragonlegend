using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeEndArtTests
{
    [UnityTest]
    public IEnumerator CurrentEndArtworkPreservesShearAndResetsItWhenChangingClips()
    {
        var host=new GameObject("Current Free end artwork",typeof(RectTransform),typeof(Canvas));host.layer=5;
        var canvas=host.GetComponent<Canvas>();((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var cameraHost=new GameObject("Current Free end camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        var art=Object.Instantiate(Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/ef_overtc"),host.transform);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
        var previous=RenderTexture.active;Texture2D capture=null;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            var rect=(RectTransform)art.transform;Assert.AreEqual(new Vector2(1090.0714f,1273.1414f),rect.sizeDelta);
            Assert.AreEqual(new Vector2(.50049144f,.49966273f),rect.pivot);
            var player=art.GetComponent<Animation>();Assert.AreEqual(1,player["start"].length,.00001f);Assert.AreEqual(2,player["idle"].length,.00001f);
            int complete=0;art.PlayOnce(1,()=>complete++);
            for(int i=0;i<5;i++)yield return null;
            Time.timeScale=0;yield return null;yield return null;float paused=player["start"].time;
            for(int i=0;i<5;i++)yield return null;Assert.AreEqual(paused,player["start"].time,.00001f);Assert.AreEqual(0,complete);
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            foreach(int clip in new[]{1,0}) {
                art.Play(clip);art.Sample(clip,.55f);Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-end-art-"+(clip==1?"start":"idle")+".png"),capture.EncodeToPNG());
                int colored=0;foreach(var pixel in capture.GetPixels32())if(pixel.r>80||pixel.g>80||pixel.b>80)colored++;
                Assert.Greater(colored,100000);
            }
            var shear=System.Array.Find(art.Rig.bones,b=>b.name=="suiziy4");Assert.IsNotNull(shear);
            // Only start contains this shear timeline; sampling idle restores setup values.
            art.Sample(1,.55f);Assert.Greater(Mathf.Abs(shear.shearX)+Mathf.Abs(shear.shearY),.001f);
            art.Sample(0,.55f);Assert.AreEqual(0,shear.shearX);Assert.AreEqual(0,shear.shearY);
            Time.timeScale=1;art.PlayOnce(1,()=>complete++);
            for(int i=0;i<30;i++)yield return null;
            Assert.AreEqual(1,complete);art.Play(0);
            for(int i=0;i<45;i++)yield return null;Assert.IsTrue(player.IsPlaying("idle"));
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            if(capture!=null)Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);
        }
    }
}
