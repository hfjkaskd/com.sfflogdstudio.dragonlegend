using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredWildLightTests
{
    [Serializable] private class Samples { public Frame[] frames; }
    [Serializable] private class Frame { public float time,area,momentX,momentY,unclippedArea; }
    [Test]
    public void ConvexClippingPreservesAffineUvForBothWindingsAndRejectsEmptyCoverage()
    {
        var clip=new RecoveredConvexClipper();
        var boundary=new[]{new Vector3(0,1),new Vector3(1,0),new Vector3(0,-1),new Vector3(-1,0)};
        var a=new RecoveredConvexClipper.Vertex(new Vector3(-2,-2),new Vector2(0,0));
        var b=new RecoveredConvexClipper.Vertex(new Vector3(2,-2),new Vector2(1,0));
        var c=new RecoveredConvexClipper.Vertex(new Vector3(0,3),new Vector2(.5f,1));
        for(int winding=0;winding<2;winding++) {
            clip.SetBoundary(boundary,4);clip.Clip(a,b,c);Assert.AreEqual(4,clip.Count);
            float area=0;
            for(int i=0;i<clip.Count;i++) {
                var v=clip.At(i);var next=clip.At((i+1)%clip.Count);
                Assert.AreEqual(1,Mathf.Abs(v.position.x)+Mathf.Abs(v.position.y),.00001f);
                Assert.AreEqual((v.position.x+2)/4,v.uv.x,.00001f);Assert.AreEqual((v.position.y+2)/5,v.uv.y,.00001f);
                area+=v.position.x*next.position.y-next.position.x*v.position.y;
            }
            Assert.AreEqual(2,Mathf.Abs(area)/2,.00001f);Array.Reverse(boundary);
        }
        clip.Clip(new RecoveredConvexClipper.Vertex(new Vector3(3,3),Vector2.zero),new RecoveredConvexClipper.Vertex(new Vector3(4,3),Vector2.right),new RecoveredConvexClipper.Vertex(new Vector3(3,4),Vector2.up));
        Assert.AreEqual(0,clip.Count);
        clip.SetBoundary(new Vector3[4],4);clip.Clip(a,b,c);Assert.AreEqual(0,clip.Count);
    }
    private static double[] Coverage(Mesh mesh)
    {
        var points=mesh.vertices;var indices=mesh.triangles;double area=0,mx=0,my=0;
        for(int i=0;i<indices.Length;i+=3) {
            var a=points[indices[i]];var b=points[indices[i+1]];var c=points[indices[i+2]];
            double weight=Math.Abs(((double)b.x-a.x)*(c.y-a.y)-((double)b.y-a.y)*(c.x-a.x))/2;
            area+=weight;mx+=weight*(a.x+b.x+c.x)/3;my+=weight*(a.y+b.y+c.y)/3;
        }
        return new[]{area,mx,my};
    }
    [UnityTest]
    public IEnumerator OriginalClippedLightRendersOverWildAndReturnsAfterItsOwnAnimation()
    {
        var wild=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/Wild3"));
        var light=Object.Instantiate(Resources.Load<RecoveredWildLight>("RecoveredSymbols/Wild3Light"));
        var rig=light.GetComponent<RecoveredWorldRig>();light.GetComponent<MeshRenderer>().sortingOrder=2;
        var cameraHost=new GameObject("Wild light QA camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1000,1000,24);camera.targetTexture=target;var previous=RenderTexture.active;Texture2D capture=null;
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var clip=Array.Find(rig.Data.attachments,a=>a.clipping);
        try {
            Assert.AreEqual(38,rig.Data.bones.Length);Assert.AreEqual(26,rig.Data.slots.Length);Assert.AreEqual(153,rig.Data.attachments.Length);
            Assert.AreEqual(1,clip.slot);Assert.AreEqual(24,clip.endSlot);Assert.AreEqual(4,clip.positions.Length);
            Assert.AreEqual(1,clip.deform.Length);Assert.AreEqual(8,clip.deform[0].values.Length);
            Assert.AreEqual(.8f,rig.Data.duration,.00001f);Assert.IsFalse(light.GetComponent<Animation>().playAutomatically);
            Assert.AreEqual(WrapMode.Once,light.GetComponent<Animation>().clip.wrapMode);
            Assert.AreEqual("wild3",light.GetComponent<Animation>().clip.name);
            var samples=JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/wild-light-samples.json")));
            foreach(var frame in samples.frames) {
                rig.Sample(frame.time);var actual=Coverage(rig.CurrentMesh);
                Assert.AreEqual(frame.area,actual[0],.002,$"Clipped area at {frame.time}");
                Assert.AreEqual(frame.momentX,actual[1],.003,$"Clipped x moment at {frame.time}");
                Assert.AreEqual(frame.momentY,actual[2],.003,$"Clipped y moment at {frame.time}");
                clip.clipping=false;rig.Sample(frame.time);actual=Coverage(rig.CurrentMesh);clip.clipping=true;
                Assert.AreEqual(frame.unclippedArea,actual[0],.003,"Counterfactual unclipped geometry");
                Assert.Greater(frame.unclippedArea-frame.area,10,"The original mask materially changes geometry");
            }
            Time.timeScale=0;Time.captureDeltaTime=.05f;yield return null;
            capture=new Texture2D(1000,1000,TextureFormat.RGB24,false);Color32[] clipped=null;
            var request=new RenderPipeline.StandardRequest{destination=target};
            for(int mode=0;mode<2;mode++) {
                clip.clipping=mode==0;rig.Sample(.173f);
                RenderPipeline.SubmitRenderRequest(camera,request);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1000,1000),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-wild-light-"+(mode==0?"clipped":"unclipped-control")+".png"),capture.EncodeToPNG());
                var pixels=capture.GetPixels32();int changed=0;
                if(mode==0)clipped=pixels;
                else {
                    for(int i=0;i<pixels.Length;i++)if(Math.Abs(pixels[i].r-clipped[i].r)>10||Math.Abs(pixels[i].g-clipped[i].g)>10)changed++;
                    Assert.Greater(changed,500,"Actual clipping must affect the rendered image");
                }
            }
            clip.clipping=true;int completed=0;var mesh=rig.CurrentMesh;
            light.Completed+=value=>{completed++;value.gameObject.SetActive(false);};
            light.Play();for(int i=0;i<10;i++)yield return null;Assert.AreEqual(0,completed);Assert.IsTrue(light.IsPlaying);
            Time.timeScale=1;float start=Time.time;
            for(int i=0;i<25&&light.IsPlaying;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.GreaterOrEqual(Time.time-start,.8f-.0001f);Assert.IsFalse(light.gameObject.activeSelf);
            light.gameObject.SetActive(true);light.Play();Assert.AreSame(mesh,rig.CurrentMesh);
            yield return null;light.gameObject.SetActive(false);light.gameObject.SetActive(true);
            for(int i=0;i<25;i++)yield return null;Assert.AreEqual(1,completed,"Disable cancels completion instead of leaking a pool callback");
            for(int i=0;i<30;i++)rig.Sample(i*.01f);
            long allocated=GC.GetAllocatedBytesForCurrentThread(),begin=System.Diagnostics.Stopwatch.GetTimestamp();
            for(int i=0;i<1000;i++)rig.Sample((i%80)*.01f);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-allocated;
            double milliseconds=(System.Diagnostics.Stopwatch.GetTimestamp()-begin)*1000.0/System.Diagnostics.Stopwatch.Frequency;
            TestContext.WriteLine($"1000 clipped Wild light poses: {milliseconds:F2} ms; managed allocation: {bytes} bytes");
            Assert.AreEqual(0,bytes);
        } finally {
            if(clip!=null)clip.clipping=true;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;
            camera.targetTexture=null;target.Release();Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            Object.Destroy(wild);Object.Destroy(light.gameObject);Object.Destroy(cameraHost);
        }
    }
}
