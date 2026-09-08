using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredSceneTransitionTests
{
    [Serializable] private class Samples {public Rig[] rigs;}
    [Serializable] private class Rig {public Frame[] frames;}
    [Serializable] private class Frame {public float time;public float[] vertices;}
    [UnityTest]
    public IEnumerator WorldGeometryAndIndependentCallbacksMatchSource()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredEffects/SceneTransition"));
        var transition=root.GetComponent<RecoveredSceneTransition>();
        var rig=root.GetComponentInChildren<RecoveredWorldRig>(true);
        var cameraHost=new GameObject("Transition verification camera",typeof(Camera));
        var camera=cameraHost.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=9.6f;
        camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=1;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(540,960,24);camera.targetTexture=target;
        var capture=new Texture2D(540,960,TextureFormat.RGB24,false);
        var previous=RenderTexture.active;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            Assert.IsFalse(rig.gameObject.activeSelf);Assert.AreEqual(0,root.GetComponentsInChildren<CanvasRenderer>(true).Length);
            Assert.AreEqual(new Vector3(0,0,20),rig.transform.localPosition);
            rig.gameObject.SetActive(true);rig.Sample(0);
            Assert.AreEqual(114,rig.Data.bones.Length);Assert.AreEqual(100,rig.Data.attachments.Length);
            var samples=JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/transition-poses.json")));
            foreach(var frame in samples.rigs[0].frames) {
                rig.Sample(frame.time);var vertices=rig.CurrentMesh.vertices;
                Assert.AreEqual(frame.vertices.Length/2,vertices.Length);
                for(int v=0;v<vertices.Length;v++) {
                    Assert.AreEqual(frame.vertices[v*2],vertices[v].x,.0002f,$"time {frame.time} vertex {v} x");
                    Assert.AreEqual(frame.vertices[v*2+1],vertices[v].y,.0002f,$"time {frame.time} vertex {v} y");
                }
            }
            for(int f=0;f<2;f++) {
                rig.Sample(f==0?.8f:1.8f);
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,540,960),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-transition-"+f+".png"),capture.EncodeToPNG());
                int visible=0;foreach(var p in capture.GetPixels32())if(p.r>20||p.g>20||p.b>20)visible++;
                Assert.Greater(visible,1000,"World mesh must render source artwork");
            }
            for(int i=0;i<30;i++)rig.Sample(i*.01f);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)rig.Sample((i%330)*.01f);
            Assert.AreEqual(0,GC.GetAllocatedBytesForCurrentThread()-before,"Reuse pose and mesh buffers");
            int events=0,completions=0;double started=Time.timeAsDouble,eventTime=0,completeTime=0;
            transition.Play(()=>{events++;eventTime=Time.timeAsDouble-started;},()=>{
                completions++;completeTime=Time.timeAsDouble-started;Assert.IsTrue(transition.IsPlaying,"3s callback precedes 3.333s visual end");
            });
            Time.timeScale=0;for(int i=0;i<8;i++)yield return null;
            Assert.AreEqual(0,events);Assert.AreEqual(0,completions);Assert.IsTrue(transition.IsPlaying);
            Time.timeScale=1;
            for(int i=0;i<85&&transition.IsPlaying;i++)yield return null;
            Assert.AreEqual(1,events);Assert.AreEqual(1,completions);
            Assert.That(eventTime,Is.InRange(.799999,.91));Assert.That(completeTime-eventTime,Is.InRange(2.199999,2.31));
            Assert.IsFalse(transition.IsPlaying);Assert.IsFalse(transition.AwaitingCallbacks);Assert.IsFalse(rig.gameObject.activeSelf);
            transition.Play(()=>events++,()=>completions++);yield return null;transition.Cancel();
            for(int i=0;i<70;i++)yield return null;
            Assert.AreEqual(1,events);Assert.AreEqual(1,completions);Assert.IsFalse(rig.gameObject.activeSelf);
            // Cancellation inside the cover callback must not schedule a stale second callback.
            transition.Play(()=>transition.Cancel(),()=>completions++);
            for(int i=0;i<70;i++)yield return null;
            Assert.AreEqual(1,completions);Assert.IsFalse(transition.AwaitingCallbacks);
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;
            camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);
            Object.Destroy(root);Object.Destroy(cameraHost);
        }
    }
}
