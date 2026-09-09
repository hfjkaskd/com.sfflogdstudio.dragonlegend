using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
public sealed class RecoveredJackpotPopupArtTests
{
    [Serializable] private class Evidence {public Rig[] rigs;}
    [Serializable] private class Rig {public string name;public int animation;public Frame[] frames;}
    [Serializable] private class Frame {public float time;public float[] vertices;}
    [TestCase("JackpotPopup/poses.json")]
    [TestCase("bigwin-poses.json")]
    [TestCase("npc-geometry.json")]
    [TestCase("bonus-card-poses.json")]
    [TestCase("free-start-poses.json")]
    [TestCase("free-end-poses.json")]
    [TestCase("main-fireworks-poses.json")]
    [TestCase("reel-anticipation-poses.json")]
    [TestCase("spin-finger-poses.json")]
    [TestCase("lucky-spin-art-poses.json")]
    [TestCase("wheel-art-poses.json")]
    [TestCase("treasure-icon-poses.json")]
    [TestCase("cashout-entry-poses.json")]
    [TestCase("top-withdraw-poses.json")]
    public void PopupWeightedAndSequenceGeometryMatchesAllSourceClips(string evidenceFile)
    {
        var evidence=JsonUtility.FromJson<Evidence>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/"+evidenceFile)));
        foreach(var source in evidence.rigs) {
            var animator=Object.Instantiate(Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/"+source.name));
            try {
                var rig=animator.Rig;
                foreach(var frame in source.frames) {
                    animator.Sample(source.animation,frame.time);int at=0;
                    foreach(var slot in rig.slots) {
                        if(slot.attachment<0)continue;var region=rig.regions[(int)slot.attachment];
                        if(region.sequenceFrames!=null&&region.sequenceFrames.Length>0)region=rig.regions[region.sequenceFrames[slot.sequenceIndex<0?region.setupIndex:slot.sequenceIndex]];
                        if(region.counts!=null&&region.counts.Length>0) {
                            int influence=0;
                            for(int v=0;v<region.counts.Length;v++) {
                                var point=Vector3.zero;for(int w=0;w<region.counts[v];w++,influence++)point+=rig.BoneMatrix(region.boneIndices[influence]).MultiplyPoint3x4(region.CurrentVertices[influence])*region.weights[influence];
                                Check(point,frame,ref at,source.name);
                            }
                        }else foreach(var vertex in region.CurrentVertices)Check(rig.BoneMatrix(slot.bone).MultiplyPoint3x4(vertex),frame,ref at,source.name);
                    }
                    Assert.AreEqual(frame.vertices.Length,at);
                }
                animator.Sample(source.animation,.173f);
                long before=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();long timerBytes=GC.GetAllocatedBytesForCurrentThread()-before;
                for(int i=0;i<1000;i++)animator.Sample(source.animation,(i%200)*.01f);
                clock.Stop();long allocated=GC.GetAllocatedBytesForCurrentThread()-before-timerBytes;
                Assert.AreEqual(0,allocated);UnityEngine.Debug.Log("Popup pose "+source.name+"/"+source.animation+": "+clock.Elapsed.TotalMilliseconds+" ms/1000, "+allocated+" B");
            }finally{Object.DestroyImmediate(animator.gameObject);}
        }
    }
    private static void Check(Vector3 point,Frame frame,ref int at,string name)
    {
        Assert.AreEqual(frame.vertices[at++],point.x/100,.00008f,name+"/"+frame.time);
        Assert.AreEqual(frame.vertices[at++],point.y/100,.00008f,name+"/"+frame.time);
    }
    [UnityTest]
    public IEnumerator PopupCurrentCanvasRendersEveryTierAndNativeClockPauses()
    {
        var host=new GameObject("Popup art current canvas",typeof(RectTransform),typeof(Canvas));var canvas=host.GetComponent<Canvas>();
        var cameraHost=new GameObject("Popup art current camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;host.layer=5;((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var coins=Object.Instantiate(Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/ef_slpenqian"),host.transform);
        var dragon=Object.Instantiate(Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/ef_jackpottc"),host.transform);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;var previous=RenderTexture.active;
        Texture2D capture=null;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;for(int i=0;i<5;i++)yield return null;
            var clip=dragon.GetComponent<Animation>();
            Time.timeScale=0;yield return null;yield return null;float before=clip["grand"].time;
            for(int i=0;i<5;i++)yield return null;Assert.AreEqual(before,clip["grand"].time,.0001f);
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);var names=new[]{"grand","major","minor"};
            for(int tier=0;tier<3;tier++) {
                dragon.Play(tier);dragon.Sample(tier,.173f);coins.Sample(0,.55f);
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-jackpot-popup-"+names[tier]+".png"),capture.EncodeToPNG());
                int colored=0;foreach(var p in capture.GetPixels32())if(p.r>80||p.g>80||p.b>80)colored++;
                Assert.Greater(colored,100000,names[tier]);
            }
            Time.timeScale=1;for(int i=0;i<70;i++)yield return null;
            Assert.IsTrue(clip.IsPlaying("minor"));Assert.AreEqual(2,dragon.Selected);
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;if(capture!=null)Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);}
    }
}
