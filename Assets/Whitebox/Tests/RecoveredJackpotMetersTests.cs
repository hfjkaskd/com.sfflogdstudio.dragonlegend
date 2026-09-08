using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

public sealed class RecoveredJackpotMetersTests
{
    [Serializable] private class Evidence {public PoseRig[] rigs;}
    [Serializable] private class PoseRig {public string name;public bool win;public Frame[] frames;}
    [Serializable] private class Frame {public float time;public float[] vertices;}
    [Test]
    public void AllThreeIconsMatchIndependentSourceGeometryAcrossBothAnimations()
    {
        var evidence=JsonUtility.FromJson<Evidence>(File.ReadAllText(Path.Combine(Application.dataPath,"../Tools/Evidence/Jackpot/poses.json")));
        var group=Object.Instantiate(Resources.Load<RecoveredJackpotMeters>("RecoveredUI/JackpotMeters"));
        try {
            foreach(var pose in evidence.rigs) {
                var icon=group.At(pose.name=="grand"?0:pose.name=="major"?1:2).Icon;var rig=icon.Rig;
                Assert.AreEqual(22,rig.bones.Length);Assert.AreEqual(31,rig.slots.Length);Assert.AreEqual(50,rig.regions.Length);
                Assert.IsFalse(rig.canvasRenderer.cullTransparentMesh);
                foreach(var frame in pose.frames) {
                    icon.Sample(pose.win,frame.time);int at=0;
                    foreach(var slot in rig.slots) {
                        if(slot.attachment<0)continue;var region=rig.regions[(int)slot.attachment];
                        if(region.sequenceFrames!=null&&region.sequenceFrames.Length>0)region=rig.regions[region.sequenceFrames[slot.sequenceIndex<0?region.setupIndex:slot.sequenceIndex]];
                        foreach(var vertex in region.vertices) {
                            var point=rig.BoneMatrix(slot.bone).MultiplyPoint3x4(vertex)/100;
                            Assert.AreEqual(frame.vertices[at++],point.x,.00003f,pose.name+"/"+pose.win+"/"+frame.time);
                            Assert.AreEqual(frame.vertices[at++],point.y,.00003f);
                        }
                    }
                    Assert.AreEqual(frame.vertices.Length,at);
                }
            }
        }finally{Object.DestroyImmediate(group.gameObject);}
    }
    [Test]
    public void SequenceUsesNativeFrameBoundaryAndReverseModes()
    {
        var frame=new RecoveredRigAnimation.SequenceFrame{delay=.05f,mode=1};
        Assert.AreEqual(0,RecoveredRigAnimation.SequenceIndex(frame,.049999f,20));
        Assert.AreEqual(1,RecoveredRigAnimation.SequenceIndex(frame,.05f,20));
        Assert.AreEqual(19,RecoveredRigAnimation.SequenceIndex(frame,2,20));
        frame.mode=4;Assert.AreEqual(18,RecoveredRigAnimation.SequenceIndex(frame,.05f,20));
        frame.mode=3;Assert.AreEqual(18,RecoveredRigAnimation.SequenceIndex(frame,1,20));
        frame.mode=6;Assert.AreEqual(18,RecoveredRigAnimation.SequenceIndex(frame,.05f,20));
    }
    [UnityTest]
    public IEnumerator ActualSpinPublishesMeterTargetBeforeNumberTweenAndNativeWinReturnsToIdle()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        var cameraHost=new GameObject("Jackpot current camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(100,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;root.GetComponent<Canvas>().worldCamera=camera;
        var previous=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            var field=entry.Playfield;Assert.IsNotNull(field);var meters=field.JackpotMeters;Assert.IsNotNull(meters);
            var progress=entry.PlayerProgress;var rules=entry.Rules;var fans=rules.GetJackPot();
            Assert.AreEqual(new Vector2(-.0018310547f,-111.380005f),((RectTransform)meters.transform).anchoredPosition);
            float before=progress.GrandJackPotReward;int count=progress.JpAddCount;
            Assert.AreEqual(rules.GetJackpotReward(fans[0],field.Bet,count),before);
            string label=meters.At(0).Label.text;
            field.SpinButton.Button.onClick.Invoke();
            float reward=rules.GetJackpotReward(fans[0],field.Bet,progress.JpAddCount);
            Assert.AreEqual(reward,progress.GrandJackPotReward);Assert.AreEqual(label,meters.At(0).Label.text);Assert.IsTrue(meters.At(0).IsAnimating);
            Time.timeScale=0;for(int i=0;i<8;i++)yield return null;
            Assert.IsTrue(meters.At(0).IsAnimating);Assert.AreEqual(label,meters.At(0).Label.text);
            Time.timeScale=1;for(int i=0;i<10&&meters.At(0).IsAnimating;i++)yield return null;
            Assert.IsFalse(meters.At(0).IsAnimating);Assert.AreEqual(RecoveredCurrency.Format(reward,0,2),meters.At(0).Label.text);
            Assert.AreEqual(before,meters.At(0).CurrentReward,"Original tween setter does not update curJackPotWin");
            int completed=0;meters.At(0).PlayAnim(()=>{Assert.IsFalse(meters.At(0).Icon.IsWinning);completed++;progress.SetJpAddCount(progress.JpAddCount+7);});
            meters.At(1).PlayAnim(null);meters.At(2).PlayAnim(null);
            for(int i=0;i<4;i++)yield return null;
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-jackpot-meters.png"),capture.EncodeToPNG());
            int colored=0;var pixels=capture.GetPixels32();for(int y=1450;y<1820;y++)for(int x=0;x<1080;x++){var p=pixels[y*1080+x];if(p.r>70||p.g>70||p.b>70)colored++;}
            Assert.Greater(colored,15000);Assert.AreEqual(0,completed);
            Time.timeScale=1;for(int i=0;i<50&&completed==0;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.AreEqual(rules.GetJackpotReward(fans[0],field.Bet,progress.JpAddCount),progress.GrandJackPotReward);
            meters.At(0).PlayAnim(()=>Assert.Fail("Cancelled win callback"));meters.gameObject.SetActive(false);
            for(int i=0;i<45;i++)yield return null;
        }finally {
            RenderTexture.active=previous;camera.targetTexture=null;if(capture!=null)Object.Destroy(capture);Object.Destroy(target);Object.Destroy(cameraHost);Object.Destroy(root);
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
    }
}
