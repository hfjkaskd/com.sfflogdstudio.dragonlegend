using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredScatterTests
{
    [Serializable] private class Samples {public Clip[] rigs;}
    [Serializable] private class Clip {public string name;public Frame[] frames;}
    [Serializable] private class Frame {public float time;public float[] vertices;}
    [Test]
    public void AllOriginalScatterClipsShareOneMeshAndMatchBinaryGeometry()
    {
        var source=JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/scatter-world-samples.json")));
        var effect=Object.Instantiate(Resources.Load<RecoveredScatterEffect>("RecoveredSymbols/Scatter"));
        try {
            var mesh=effect.Rig.CurrentMesh;Assert.AreEqual(1,effect.GetComponentsInChildren<MeshRenderer>().Length);
            Assert.AreEqual(0,effect.GetComponentsInChildren<CanvasRenderer>().Length);
            foreach(var clip in source.rigs) {
                if(clip.name=="start")effect.PlayStart();else if(clip.name=="idle")effect.PlayIdle(true);else effect.ShowPng();
                effect.Player.Stop();
                foreach(var frame in clip.frames) {
                    effect.Rig.Sample(frame.time);Assert.AreSame(mesh,effect.Rig.CurrentMesh);var vertices=mesh.vertices;
                    Assert.AreEqual(frame.vertices.Length/2,vertices.Length,clip.name);
                    for(int i=0;i<vertices.Length;i++) {
                        Assert.AreEqual(frame.vertices[i*2],vertices[i].x,.0001f,clip.name+"/"+frame.time);
                        Assert.AreEqual(frame.vertices[i*2+1],vertices[i].y,.0001f,clip.name+"/"+frame.time);
                    }
                }
            }
            effect.PlayIdle(true);effect.Rig.Sample(.173f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)effect.Rig.Sample((i%100)*.01f);
            Assert.AreEqual(0,GC.GetAllocatedBytesForCurrentThread()-before);
        } finally {Object.DestroyImmediate(effect.gameObject);}
    }
    [UnityTest]
    public IEnumerator ActualStopPassInterleavesCoinsAndScatterAndRecyclesOnClear()
    {
        var random=UnityEngine.Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.Playfield==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            var field=entry.Playfield;Assert.IsNotNull(field);var scatters=field.Scatters;Assert.IsNotNull(scatters);
            var reel=field.Reels.ReelAt(0);reel.ApplyBaseColumn(new[]{10,9,10});
            var order=new List<string>();scatters.StopSoundRequested+=()=>order.Add("scatter");field.CoinStops.CoinShowSoundRequested+=()=>order.Add("coin");
            int vibrations=0;scatters.VibrationRequested+=ms=>{Assert.AreEqual(200,ms);vibrations++;};
            field.CoinStops.ShowColumn(0);
            CollectionAssert.AreEqual(new[]{"scatter","coin","scatter"},order);Assert.AreEqual(2,vibrations);
            Assert.AreEqual(2,scatters.ActiveCount);Assert.IsNull(scatters.At(0,1));
            var first=scatters.At(0,0);var second=scatters.At(0,2);var mesh=first.Rig.CurrentMesh;
            Assert.That(Vector3.Distance(reel.SymbolAt(0).Symbol.transform.position,first.transform.position),Is.LessThan(.0001f));
            Assert.IsFalse(reel.SymbolAt(0).Symbol.gameObject.activeSelf);Assert.IsTrue(first.Player.IsPlaying("start"));
            int completed=0;first.PlayStart(()=>completed++);
            Time.timeScale=0;for(int i=0;i<5;i++)yield return null;Assert.AreEqual(0,completed);
            Time.timeScale=1;for(int i=0;i<18;i++)yield return null;Assert.AreEqual(1,completed);
            Assert.IsTrue(first.Player.IsPlaying("start")); // Original holds its final pose, no automatic idle.
            scatters.PlayScatterAnim();Assert.IsTrue(first.Player.IsPlaying("idle"));Assert.IsTrue(second.Player.IsPlaying("idle"));
            field.CoinStops.ShowColumn(0);Assert.AreEqual(2,scatters.CreatedCount);Assert.AreEqual(3,order.Count);
            first.PlayStart();scatters.PlayScatterAnim();Assert.IsTrue(first.Player.IsPlaying("start")); // repeat stop cleared only the native list
            reel.Refresh(100000,1,false);Assert.AreEqual(0,scatters.ActiveCount);Assert.IsFalse(first.gameObject.activeSelf);
            reel.SetOffsetPixels(0);reel.ApplyBaseColumn(new[]{10,10,0});field.CoinStops.ShowColumn(0);
            Assert.AreEqual(2,scatters.CreatedCount);Assert.AreEqual(2,scatters.ActiveCount);
            Assert.IsTrue(scatters.At(0,0)==first||scatters.At(0,1)==first);Assert.AreSame(mesh,first.Rig.CurrentMesh);
            var wild=field.Reels.ReelAt(1);wild.ApplyBaseColumn(new[]{7,7,7});order.Clear();int priorVibrations=vibrations;
            field.CoinStops.ShowColumn(1);CollectionAssert.AreEqual(new[]{"scatter","scatter","scatter"},order);
            Assert.AreEqual(priorVibrations,vibrations);Assert.IsNull(scatters.At(1,0));
            scatters.Unbind();Assert.AreEqual(0,scatters.ActiveCount);
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;Object.Destroy(root);}
    }
    [UnityTest]
    public IEnumerator FreshNativeScatterFramesRender()
    {
        var cameraHost=new GameObject("Current Scatter camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=1.2f;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1200,400,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1200,400,TextureFormat.RGB24,false);var effects=new RecoveredScatterEffect[3];
        try {
            for(int i=0;i<3;i++){effects[i]=Object.Instantiate(Resources.Load<RecoveredScatterEffect>("RecoveredSymbols/Scatter"));effects[i].transform.position=new Vector3((i-1)*2.3f,0,0);}
            yield return null;
            effects[0].PlayStart();effects[1].PlayIdle(true);effects[2].ShowPng();
            for(int frame=0;frame<2;frame++) {
                foreach(var effect in effects){effect.Player.Stop();effect.Rig.Sample(frame==0?.173f:.55f);}
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
                capture.ReadPixels(new Rect(0,0,1200,400),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-scatter-clips-"+frame+".png"),capture.EncodeToPNG());
                int bright=0;foreach(var p in capture.GetPixels32())if(p.r>80||p.g>80||p.b>80)bright++;Assert.Greater(bright,30000);
            }
        } finally {RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(capture);Object.Destroy(target);Object.Destroy(cameraHost);foreach(var effect in effects)if(effect!=null)Object.Destroy(effect.gameObject);}
    }
}
