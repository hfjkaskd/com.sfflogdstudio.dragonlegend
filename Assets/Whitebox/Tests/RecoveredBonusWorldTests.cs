using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.IO;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredBonusWorldTests
{
    [UnityTest]
    public IEnumerator AuthoredWorldPlayerPreservesTurnSpeedPauseAndCancellation()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/BonusWorld/BonusWorld"));
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            var driver=root.GetComponent<RecoveredWorldAnimation>();
            Assert.AreEqual(13,driver.Player.GetClipCount());
            int completed=0;driver.PlaybackSpeed=3;
            driver.Play("zcjb_b_zhao",false,()=>{completed++;driver.Play("idle_zhao",true);driver.PlaybackSpeed=1;});
            Time.timeScale=0;for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(0,completed);Assert.AreEqual(0,driver.Player["zcjb_b_zhao"].time);
            Time.timeScale=1;
            for(int i=0;i<5;i++)yield return null;
            Assert.AreEqual(0,completed,"The .667 second turn at 3x must not finish by .125 seconds.");
            for(int i=0;i<15&&completed==0;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.IsTrue(driver.Player.IsPlaying("idle_zhao"));
            Assert.AreEqual(1,driver.Player["idle_zhao"].speed);
            for(int i=0;i<10;i++)yield return null;Assert.AreEqual(1,completed);
            driver.Play("zcjb_b_bao",false,()=>completed++);root.SetActive(false);
            for(int i=0;i<35;i++)yield return null;Assert.AreEqual(1,completed);
            root.SetActive(true);Assert.IsTrue(driver.Player.IsPlaying("zcjb_idle"));
            driver.Play("glow",false,()=>completed++);driver.Stop();
            for(int i=0;i<35;i++)yield return null;Assert.AreEqual(1,completed);
        }
        finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(root);}
    }
    [System.Serializable] private sealed class Samples {public RigSamples[] rigs;}
    [System.Serializable] private sealed class RigSamples {public string name;public Frame[] frames;}
    [System.Serializable] private sealed class Frame {public float time;public float[] vertices;}
    [Test]
    public void EveryClipMatchesIndependentSourceGeometrySamples()
    {
        var source=JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/bonus-world-samples.json")));
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/BonusWorld/zcjb_idle/zcjb_idle"));
        try
        {
            var rig=root.GetComponent<RecoveredWorldRig>();Assert.AreEqual(13,source.rigs.Length);
            foreach(var clip in source.rigs)
            {
                rig.SelectClipData(Resources.Load<RecoveredWorldRigData>("RecoveredSymbols/BonusWorld/"+clip.name+"/RecoveredCoinEffect"));
                foreach(var frame in clip.frames)
                {
                    rig.Sample(frame.time);var vertices=rig.CurrentMesh.vertices;
                    Assert.AreEqual(frame.vertices.Length/2,vertices.Length,clip.name);
                    for(int i=0;i<vertices.Length;i++)
                    {
                        Assert.AreEqual(frame.vertices[i*2],vertices[i].x,.0001f,clip.name+" x at "+frame.time);
                        Assert.AreEqual(frame.vertices[i*2+1],vertices[i].y,.0001f,clip.name+" y at "+frame.time);
                    }
                }
            }
        }
        finally {Object.DestroyImmediate(root);}
    }
    [UnityTest]
    public IEnumerator RenderAllConvertedClipsAtMidpoint()
    {
        string[] names={"glow","idle_bao","idle_cai","idle_chun","idle_jin","idle_zhao",
            "zcjb_b_bao","zcjb_b_cai","zcjb_b_chun","zcjb_b_jin","zcjb_b_zhao","zcjb_chuxian","zcjb_idle"};
        var roots=new GameObject[names.Length];
        var host=new GameObject("Bonus world verification camera",typeof(Camera));
        var camera=host.GetComponent<Camera>();camera.transform.position=new Vector3(0,0,-10);
        camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1500,1000,24);camera.targetTexture=target;
        var prior=RenderTexture.active;Texture2D capture=null;
        try
        {
            for(int i=0;i<names.Length;i++)
            {
                roots[i]=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/BonusWorld/"+names[i]+"/"+names[i]));
                roots[i].transform.position=new Vector3((i%5-2)*3,(1-i/5)*3,0);
                roots[i].GetComponent<Animation>().Stop();
            }
            yield return null;
            for(int i=0;i<names.Length;i++)
            {
                var rig=roots[i].GetComponent<RecoveredWorldRig>();rig.Sample(rig.Data.duration*.5f);
                Assert.Greater(rig.CurrentMesh.vertexCount,0,names[i]);
            }
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(1500,1000,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1500,1000),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-world-midpoints.png"),capture.EncodeToPNG());
            var pixels=capture.GetPixels32();
            for(int cell=0;cell<names.Length;cell++)
            {
                int cx=150+cell%5*300,cy=800-cell/5*300,visible=0;
                for(int y=cy-100;y<cy+100;y++)for(int x=cx-100;x<cx+100;x++)
                {var pixel=pixels[y*1500+x];if(pixel.r>20||pixel.g>20||pixel.b>20)visible++;}
                Assert.Greater(visible,100,names[cell]+" must render visible pixels.");
            }
        }
        finally
        {
            RenderTexture.active=prior;camera.targetTexture=null;
            foreach(var root in roots)if(root!=null)Object.Destroy(root);
            if(capture!=null)Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);
        }
    }
    [Test]
    public void AllSourceClipsSwitchOnOneMeshWithoutChangingTopology()
    {
        string[] names={"glow","idle_bao","idle_cai","idle_chun","idle_jin","idle_zhao",
            "zcjb_b_bao","zcjb_b_cai","zcjb_b_chun","zcjb_b_jin","zcjb_b_zhao","zcjb_chuxian","zcjb_idle"};
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/BonusWorld/zcjb_idle/zcjb_idle"));
        try
        {
            var rig=root.GetComponent<RecoveredWorldRig>();
            Assert.IsNull(root.GetComponent<CanvasRenderer>());
            Assert.IsNotNull(root.GetComponent<MeshRenderer>());
            var mesh=rig.CurrentMesh;
            foreach(string name in names)
            {
                var data=Resources.Load<RecoveredWorldRigData>("RecoveredSymbols/BonusWorld/"+name+"/RecoveredCoinEffect");
                Assert.IsNotNull(data,name);Assert.AreEqual(29,data.bones.Length);Assert.AreEqual(59,data.attachments.Length);
                rig.SelectClipData(data);
                for(int sample=0;sample<=4;sample++)
                {
                    rig.Sample(data.duration*sample/4);
                    Assert.AreSame(mesh,rig.CurrentMesh,"Clip switches must reuse the instance mesh.");
                    foreach(var vertex in mesh.vertices)
                        Assert.IsTrue(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),name);
                    Assert.AreEqual(mesh.vertexCount,mesh.uv.Length);
                    foreach(int index in mesh.triangles)Assert.That(index,Is.InRange(0,mesh.vertexCount-1));
                }
            }
        }
        finally {Object.DestroyImmediate(root);}
    }
}
