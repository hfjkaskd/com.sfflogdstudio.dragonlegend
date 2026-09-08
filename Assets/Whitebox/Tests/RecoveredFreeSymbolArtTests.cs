using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredFreeSymbolArtTests
{
    [Serializable] private class Samples {public Clip[] rigs;}
    [Serializable] private class Clip {public string name;public Frame[] frames;}
    [Serializable] private class Frame {public float time;public float[] vertices;}
    private static Samples Read()=>JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/free-symbol-world-samples.json")));
    private static RecoveredWorldAnimation Create(bool coin)=>Object.Instantiate(Resources.Load<RecoveredWorldAnimation>("RecoveredSymbols/"+(coin?"FreeCoinArt":"FreeBallArt")));

    [Test]
    public void EveryCoinAndBallClipMatchesSourceGeometryWithoutReplacingItsMesh()
    {
        var source=Read();Assert.AreEqual(22,source.rigs.Length);
        var coin=Create(true);var ball=Create(false);
        try {
            var coinMesh=coin.Rig.CurrentMesh;var ballMesh=ball.Rig.CurrentMesh;int frames=0;
            Assert.AreEqual(1,coin.GetComponentsInChildren<MeshRenderer>().Length);
            Assert.AreEqual(1,ball.GetComponentsInChildren<MeshRenderer>().Length);
            Assert.AreEqual(0,coin.GetComponentsInChildren<CanvasRenderer>().Length);
            Assert.AreEqual(0,ball.GetComponentsInChildren<CanvasRenderer>().Length);
            foreach(var clip in source.rigs) {
                bool isCoin=clip.name.StartsWith("ef_jinbi/");var art=isCoin?coin:ball;
                art.Play(clip.name.Substring(clip.name.IndexOf('/')+1),true);art.Player.Stop();
                foreach(var frame in clip.frames) {
                    art.Rig.Sample(frame.time);var mesh=art.Rig.CurrentMesh;Assert.AreSame(isCoin?coinMesh:ballMesh,mesh);
                    var vertices=mesh.vertices;Assert.AreEqual(frame.vertices.Length/2,vertices.Length,clip.name);
                    for(int i=0;i<vertices.Length;i++) {
                        Assert.AreEqual(frame.vertices[i*2],vertices[i].x,.0001f,clip.name+"/"+frame.time);
                        Assert.AreEqual(frame.vertices[i*2+1],vertices[i].y,.0001f,clip.name+"/"+frame.time);
                    }
                    frames++;
                }
            }
            Assert.AreEqual(942,frames);
            Assert.Throws<ArgumentOutOfRangeException>(()=>coin.Play("missing",true));
        } finally {Object.DestroyImmediate(coin.gameObject);Object.DestroyImmediate(ball.gameObject);}
    }

    [UnityTest]
    public IEnumerator NativeOneShotCompletionPausesAndCancelsOnDisable()
    {
        var art=Create(false);float scale=Time.timeScale,delta=Time.captureDeltaTime;int completed=0;
        try {
            Time.captureDeltaTime=.05f;art.Play("start_lan",false,()=>completed++);
            Time.timeScale=0;for(int i=0;i<5;i++)yield return null;Assert.AreEqual(0,completed);
            Time.timeScale=1;for(int i=0;i<18;i++)yield return null;Assert.AreEqual(1,completed);
            Assert.IsTrue(art.Player.IsPlaying("start_lan"));
            art.Play("start_zi",false,()=>completed++);art.gameObject.SetActive(false);
            for(int i=0;i<18;i++)yield return null;Assert.AreEqual(1,completed);
            art.gameObject.SetActive(true);Assert.IsTrue(art.Player.IsPlaying("idle_lan"));
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(art.gameObject);}
    }

    [UnityTest]
    public IEnumerator CurrentNativeSpecialClipsRenderIntoFreshContactSheets()
    {
        var source=Read();var arts=new RecoveredWorldAnimation[source.rigs.Length];
        var host=new GameObject("Current Free special camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=8;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(2400,1600,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(2400,1600,TextureFormat.RGB24,false);
        try {
            for(int i=0;i<arts.Length;i++) {
                var clip=source.rigs[i];arts[i]=Create(clip.name.StartsWith("ef_jinbi/"));
                arts[i].transform.position=new Vector3(-10+(i%6)*4,6-(i/6)*4,0);
                arts[i].Play(clip.name.Substring(clip.name.IndexOf('/')+1),true);arts[i].Player.Stop();
            }
            yield return null;
            for(int frame=0;frame<3;frame++) {
                float time=frame==0?.173f:frame==1?.55f:1.27f;
                foreach(var art in arts)art.Rig.Sample(time);
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
                capture.ReadPixels(new Rect(0,0,2400,1600),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-special-clips-"+frame+".png"),capture.EncodeToPNG());
                int bright=0;foreach(var pixel in capture.GetPixels32())if(pixel.r>80||pixel.g>80||pixel.b>80)bright++;
                Assert.Greater(bright,10000);
            }
        } finally {
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);
            foreach(var art in arts)if(art!=null)Object.Destroy(art.gameObject);
        }
    }
}
