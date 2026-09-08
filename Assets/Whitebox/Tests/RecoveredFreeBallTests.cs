using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeBallTests
{
    [TestCase(0,false,"zi")]
    [TestCase(1,true,"lan")]
    [TestCase(2,false,"lv")]
    [TestCase(-1,true,"lv")]
    [TestCase(99,false,"lv")]
    public void InitializerSelectsNativeTypeAndClearsTextWithoutChangingVisibility(int type,bool isInit,string suffix)
    {
        var ball=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"));
        try {
            Assert.IsTrue(ball.Art.Player.IsPlaying("huo_lan"));Assert.AreEqual("$8.99",ball.Reward.text);
            Assert.That(ball.transform.localScale.x,Is.EqualTo(.8f).Within(.00001f));
            Assert.AreSame(Resources.Load<Font>("RecoveredUI/CoinRewardText/Green"),ball.Reward.font);
            Assert.AreEqual(new Vector2(160,30),ball.Reward.rectTransform.sizeDelta);
            Assert.AreEqual(Vector2.zero,ball.Reward.rectTransform.anchoredPosition);
            Assert.AreEqual(40,ball.Reward.resizeTextMaxSize);Assert.IsTrue(ball.Reward.gameObject.activeSelf);
            ball.Initialize(type,isInit);Assert.AreEqual(type,ball.BallType);
            Assert.AreEqual(string.Empty,ball.Reward.text);Assert.IsTrue(ball.Reward.gameObject.activeSelf);
            Assert.IsTrue(ball.Art.Player.IsPlaying("idle_"+suffix));
            ball.Reward.gameObject.SetActive(false);ball.Reward.text="previous reward";
            ball.Initialize(type,!isInit);Assert.AreEqual(string.Empty,ball.Reward.text);
            Assert.IsFalse(ball.Reward.gameObject.activeSelf,"Native Init clears text without forcing visibility.");
        } finally {Object.DestroyImmediate(ball.gameObject);}
    }

    [UnityTest]
    public IEnumerator StartClipsPauseThenReturnToTheirOwnIdleWithoutChangingScale()
    {
        var balls=new RecoveredFreeBall[3];float scale=Time.timeScale,delta=Time.captureDeltaTime;
        string[] suffixes={"zi","lan","lv"};
        try {
            Time.captureDeltaTime=.05f;
            for(int i=0;i<3;i++){balls[i]=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"));balls[i].Initialize(i);balls[i].PlayStart();}
            Time.timeScale=0;for(int i=0;i<5;i++)yield return null;
            for(int i=0;i<3;i++)Assert.IsTrue(balls[i].Art.Player.IsPlaying("start_"+suffixes[i]));
            Time.timeScale=1;for(int i=0;i<18;i++)yield return null;
            for(int i=0;i<3;i++) {
                Assert.IsTrue(balls[i].Art.Player.IsPlaying("idle_"+suffixes[i]));
                Assert.That(balls[i].transform.localScale.x,Is.EqualTo(.8f).Within(.00001f));
                Assert.AreEqual(string.Empty,balls[i].Reward.text);
            }
            balls[0].PlayStart();balls[0].Initialize(1);
            for(int i=0;i<18;i++)yield return null;
            Assert.IsTrue(balls[0].Art.Player.IsPlaying("idle_lan"),"Reinitialization replaces a previous start track.");
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;foreach(var ball in balls)if(ball!=null)Object.Destroy(ball.gameObject);}
    }

    [UnityTest]
    public IEnumerator CurrentThreeTypesRenderIdleAndStartWithOriginalScale()
    {
        var balls=new RecoveredFreeBall[6];var host=new GameObject("Current Free ball camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1200,800,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1200,800,TextureFormat.RGB24,false);
        try {
            for(int i=0;i<6;i++) {
                var ball=balls[i]=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"));
                ball.transform.position=new Vector3(-3+(i%3)*3,i<3?1.5f:-1.5f,0);ball.Initialize(i%3);
                if(i>=3)ball.PlayStart();ball.Art.Stop();ball.Art.Rig.Sample(.173f);
            }
            yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1200,800),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-ball-idle-start.png"),capture.EncodeToPNG());
            var pixels=capture.GetPixels32();
            for(int col=0;col<3;col++)for(int row=0;row<2;row++) {
                int bright=0;for(int y=row*400;y<(row+1)*400;y++)for(int x=col*400;x<(col+1)*400;x++) {
                    var p=pixels[y*1200+x];if(p.r>80||p.g>80||p.b>80)bright++;
                }
                Assert.Greater(bright,1000);
            }
        } finally {RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);foreach(var ball in balls)if(ball!=null)Object.Destroy(ball.gameObject);}
    }
}
