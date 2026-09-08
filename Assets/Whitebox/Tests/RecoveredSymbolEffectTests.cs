using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredSymbolEffectTests
{
    [Serializable] private class Samples { public Rig[] rigs; }
    [Serializable] private class Rig { public string name; public Frame[] frames; }
    [Serializable] private class Frame { public float time; public float[] vertices; }
    [Test]
    public void OriginalBinaryVertexSamplesMatchAllWinningClips()
    {
        var samples=JsonUtility.FromJson<Samples>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/symbol-world-samples.json")));
        for(int i=0;i<samples.rigs.Length;i++) {
            var reference=samples.rigs[i];var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/Winning/"+(i==8?"A":reference.name)));
            try {
                var rig=root.GetComponentsInChildren<RecoveredWorldRig>()[i==8?1:0];
                foreach(var frame in reference.frames) {
                    rig.Sample(frame.time);var vertices=rig.CurrentMesh.vertices;Assert.AreEqual(frame.vertices.Length/2,vertices.Length,reference.name);
                    for(int v=0;v<vertices.Length;v++) {
                        Assert.AreEqual(frame.vertices[v*2],vertices[v].x,.0001f,$"{reference.name} t={frame.time} vertex={v} x");
                        Assert.AreEqual(frame.vertices[v*2+1],vertices[v].y,.0001f,$"{reference.name} t={frame.time} vertex={v} y");
                    }
                }
            } finally {Object.DestroyImmediate(root);}
        }
    }
    [Test]
    public void FishRelativeLocalConstraintRetainsNegativeMixAndAnimatedTargetScale()
    {
        var data=Resources.Load<RecoveredWorldRigData>("RecoveredSymbols/Winning/Yu/ef_qizijinli");
        Assert.AreEqual(1,data.relativeLocalConstraints.Length);var c=data.relativeLocalConstraints[0];
        Assert.AreEqual(9,c.bone);Assert.AreEqual(8,c.target);
        Assert.AreEqual(new Vector2(-.5f,-.5f),c.translationMix);Assert.AreEqual(new Vector2(-.5f,-.5f),c.scaleMix);
        float[] pose=new float[data.bones.Length*5];int b=9*5,t=8*5;
        pose[b]=17;pose[b+1]=10;pose[b+2]=20;pose[b+3]=2;pose[b+4]=3;
        pose[t]=39;pose[t+1]=4;pose[t+2]=6;pose[t+3]=1.4f;pose[t+4]=.6f;
        c.Apply(pose);
        Assert.AreEqual(17,pose[b]);Assert.AreEqual(-52.7776985f,pose[b+1],.00001f);
        Assert.AreEqual(19.0805054f,pose[b+2],.00001f);Assert.AreEqual(1.6f,pose[b+3],.00001f);Assert.AreEqual(3.6f,pose[b+4],.00001f);
    }
    [UnityTest]
    public IEnumerator EightOriginalClipsRenderNativeMeshesAndCurrentGallery()
    {
        string[] names={"A","10","J","Q","K","Yu","Gui","Wild1"};
        string[] clips={"a_idle","10_idle","j_idle","q_idle","k_idle","idle","idle","idle"};
        var roots=new GameObject[8];var host=new GameObject("Symbol camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=3.2f;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.025f,.025f,.035f);
        var target=new RenderTexture(1400,700,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1400,700,TextureFormat.RGB24,false);
        try {
            for(int i=0;i<8;i++) {
                roots[i]=Object.Instantiate(Resources.Load<GameObject>("RecoveredSymbols/Winning/"+names[i]));
                roots[i].transform.position=new Vector3((i%4-1.5f)*2.8f,(.5f-i/4)*2.8f,0);
                foreach(var player in roots[i].GetComponentsInChildren<Animation>())player.Stop();
                var rigs=roots[i].GetComponentsInChildren<RecoveredWorldRig>();Assert.AreEqual(2,rigs.Length);
                Assert.AreEqual(clips[i],roots[i].GetComponent<RecoveredWildColumn>().PlayerAt(0).clip.name);
                Assert.AreEqual("animation",roots[i].GetComponent<RecoveredWildColumn>().PlayerAt(1).clip.name);
                Assert.AreEqual(0,roots[i].GetComponentsInChildren<CanvasRenderer>().Length);
                rigs[0].Sample(.2f);rigs[1].Sample(.2f);Assert.Greater(rigs[0].CurrentMesh.vertexCount,0);
                Assert.Greater(rigs[1].CurrentMesh.vertexCount,0);
                if(i==5) {
                    var data=rigs[0].Data;var bone=data.bones[9];var targetBone=data.bones[8];
                    rigs[0].Sample(0);
                    var relative=rigs[0].BoneMatrix(4).inverse*rigs[0].BoneMatrix(9);
                    Assert.AreEqual(bone.x+(targetBone.x+121.555397f)*-.5f,relative.m03,.0001f);
                    Assert.AreEqual(bone.y+(targetBone.y-4.16101074f)*-.5f,relative.m13,.0001f);
                }
            }
            yield return null;
            for(int f=0;f<2;f++) {
                for(int i=0;i<8;i++)foreach(var rig in roots[i].GetComponentsInChildren<RecoveredWorldRig>())rig.Sample(f==0?.2f:.5f);
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
                capture.ReadPixels(new Rect(0,0,1400,700),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-winning-symbols-"+f+".png"),capture.EncodeToPNG());
            }
        } finally {
            RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);
            foreach(var root in roots)if(root!=null)Object.Destroy(root);
        }
    }
    [UnityTest]
    public IEnumerator ReelOwnershipDeduplicatesPreservesWildAndReusesAnimationPhase()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        try {
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.Playfield==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            var field=entry.Playfield;Assert.IsNotNull(field);var presenter=field.SymbolEffects;var reel=field.Reels.ReelAt(0);
            presenter.Present(0,0,5);var first=presenter.At(0,0);Assert.IsNotNull(first);
            Assert.That(Vector3.Distance(reel.SymbolAt(0).Symbol.transform.position,first.transform.position),Is.LessThan(.0001f));
            presenter.Present(0,0,1);Assert.AreSame(first,presenter.At(0,0));Assert.AreEqual(1,presenter.ActiveCount);
            field.Wilds.Present(1);presenter.Present(1,0,7);presenter.Present(1,1,7);presenter.Present(1,2,7);
            Assert.AreEqual(1,presenter.ActiveCount);Assert.IsNull(presenter.At(1,1));
            var player=first.PlayerAt(0);player[player.clip.name].time=.37f;player.Sample();
            reel.Refresh(100000,1,false);Assert.AreEqual(0,presenter.ActiveCount);Assert.IsFalse(first.gameObject.activeSelf);
            presenter.Present(0,0,5);Assert.AreSame(first,presenter.At(0,0));Assert.AreEqual(.37f,player[player.clip.name].time,.0001f);
            presenter.Present(0,1,8);Assert.IsNull(presenter.At(0,1));Assert.IsFalse(reel.SymbolAt(1).Symbol.gameObject.activeSelf);
            presenter.Unbind();Assert.AreEqual(0,presenter.ActiveCount);
        } finally {Object.Destroy(root);}
    }
}
