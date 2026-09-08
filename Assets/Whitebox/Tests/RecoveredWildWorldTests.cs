using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredWildWorldTests
{
    [Serializable] private class Samples { public Rig[] rigs; }
    [Serializable] private class Rig { public string name; public Frame[] frames; }
    [Serializable] private class Frame { public float time; public float[] vertices; }

    [Test]
    public void TranslationOnlyAndNoScaleReflectionModesPreserveOriginalAxes()
    {
        var parent=Matrix4x4.TRS(new Vector3(10,20,0),Quaternion.Euler(0,0,90),new Vector3(-2,.5f,1));
        var result=RecoveredWorldRig.Compose(parent,1,3,4,0,.4f,.7f);
        Assert.AreEqual(8,result.m03,.00001f);Assert.AreEqual(14,result.m13,.00001f);
        Assert.AreEqual(.4f,result.m00,.00001f);Assert.AreEqual(0,result.m10,.00001f);Assert.AreEqual(.7f,result.m11,.00001f);
        var retained=RecoveredWorldRig.Compose(parent,3,3,4,45,.4f,.7f);
        var removed=RecoveredWorldRig.Compose(parent,4,3,4,45,.4f,.7f);
        Assert.Less(retained.m00*retained.m11-retained.m01*retained.m10,0);
        Assert.Greater(removed.m00*removed.m11-removed.m01*removed.m10,0);
        Assert.AreEqual(retained.GetColumn(0),removed.GetColumn(0));
        Assert.AreEqual(-retained.m01,removed.m01,.00001f);Assert.AreEqual(-retained.m11,removed.m11,.00001f);
        Assert.AreEqual(retained.GetColumn(3),removed.GetColumn(3));
    }

    [UnityTest]
    public IEnumerator NativeWorldMeshesMatchOriginalWeightedDeformsAndRenderCurrentFrames()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/Wild3"));
        var other=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/Wild3"));other.transform.position=new Vector3(100,0,0);
        var rigs=root.GetComponentsInChildren<RecoveredWorldRig>();var others=other.GetComponentsInChildren<RecoveredWorldRig>();
        var cameraHost=new GameObject("Wild verification camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=4;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(900,900,24);camera.targetTexture=target;Texture2D capture=null;
        var previous=RenderTexture.active;float timeScale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;yield return null;
            Assert.AreEqual(2,rigs.Length);Assert.AreEqual(0,root.GetComponentsInChildren<CanvasRenderer>().Length);
            Assert.AreEqual(84,rigs[0].Data.bones.Length);Assert.AreEqual(115,rigs[0].Data.attachments.Length);
            Assert.AreEqual(37,rigs[0].Data.slots.Length);Assert.AreEqual(3,rigs[0].Data.duration,.00001f);
            Assert.AreEqual(20f/30,rigs[1].Data.duration,.00001f);
            Assert.AreSame(rigs[0].Data,others[0].Data);Assert.AreNotSame(rigs[0].CurrentMesh,others[0].CurrentMesh);
            var originalOther=others[0].CurrentMesh.vertices;
            var samples=JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/wild-world-samples.json")));
            for(int r=0;r<rigs.Length;r++) {
                Assert.AreEqual(samples.rigs[r].name,rigs[r].Data.name);
                foreach(var frame in samples.rigs[r].frames) {
                    rigs[r].Sample(frame.time);var vertices=rigs[r].CurrentMesh.vertices;
                    Assert.AreEqual(frame.vertices.Length/2,vertices.Length,"Original active attachment vertices");
                    for(int v=0;v<vertices.Length;v++) {
                        Assert.AreEqual(frame.vertices[v*2],vertices[v].x,.0001f,$"{r} time {frame.time} vertex {v} x");
                        Assert.AreEqual(frame.vertices[v*2+1],vertices[v].y,.0001f,$"{r} time {frame.time} vertex {v} y");
                    }
                }
            }
            CollectionAssert.AreEqual(originalOther,others[0].CurrentMesh.vertices,"Shared definition must not share mutable poses");
            var hand=Array.Find(rigs[0].Data.attachments,a=>a.slot==10&&a.name=="lshou_zuo");
            Assert.AreEqual((664f+.174130231f*148)/985,hand.uv[0].x,.000001f);
            Assert.AreEqual(1-(167f+(1-.607290149f)*162)/691,hand.uv[0].y,.000001f);
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
            capture=new Texture2D(900,900,TextureFormat.RGB24,false);
            var request=new RenderPipeline.StandardRequest{destination=target};
            Color32[] first=null;
            for(int f=0;f<2;f++) {
                rigs[0].Sample(f==0?0:1.5f);rigs[1].Sample(f==0?.173f:.5f);
                RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=target;
                capture.ReadPixels(new Rect(0,0,900,900),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-wild-world-"+f+".png"),capture.EncodeToPNG());
                var pixels=capture.GetPixels32();int visible=0,changed=0;
                for(int i=0;i<pixels.Length;i++) {
                    if(pixels[i].r>20||pixels[i].g>20||pixels[i].b>20)visible++;
                    if(first!=null&&(Math.Abs(pixels[i].r-first[i].r)>10||Math.Abs(pixels[i].g-first[i].g)>10))changed++;
                }
                Assert.Greater(visible,10000,"Actual world renderer must contribute visible pixels");
                if(f==1)Assert.Greater(changed,1000,"Independent original idle poses must change the rendered image");
                first=pixels;
            }
            // Let the native Animation clock reapply its paused pose after explicit QA sampling.
            for(int i=0;i<2;i++)yield return null;var paused=rigs[0].CurrentMesh.vertices;
            for(int i=0;i<8;i++)yield return null;CollectionAssert.AreEqual(paused,rigs[0].CurrentMesh.vertices);
            Time.timeScale=1;for(int i=0;i<8;i++)yield return null;
            Assert.AreNotEqual(paused[4],rigs[0].CurrentMesh.vertices[4],"Native Animation must drive the pose clock");
            for(int i=0;i<30;i++)rigs[0].Sample(i*.01f);
            long allocated=GC.GetAllocatedBytesForCurrentThread(),start=System.Diagnostics.Stopwatch.GetTimestamp();
            for(int i=0;i<1000;i++)rigs[0].Sample((i%300)*.01f);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            double milliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-start)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            TestContext.WriteLine($"1000 complete Wild mesh poses: {milliseconds:F2} ms; managed allocation: {bytes} bytes");
            Assert.AreEqual(0,bytes,"Steady-state pose/deform/mesh updates must reuse their managed buffers");
        } finally {
            Time.timeScale=timeScale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            target.Release();Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            Object.Destroy(root);Object.Destroy(other);Object.Destroy(cameraHost);
        }
    }
}
