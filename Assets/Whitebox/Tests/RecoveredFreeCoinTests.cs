using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredFreeCoinTests
{
    [UnityTest]
    public IEnumerator InitialPrefabAndRollingAppearanceKeepDistinctNativeStates()
    {
        var coin=Object.Instantiate(Resources.Load<RecoveredFreeCoin>("RecoveredSymbols/FreeCoin"));
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.captureDeltaTime=.05f;Time.timeScale=1;
            Assert.That(coin.transform.localScale.x,Is.EqualTo(.7f).Within(.00001f));
            Assert.IsTrue(coin.Art.Player.IsPlaying("idle_chun"));
            Assert.IsTrue(coin.Glow.gameObject.activeSelf);Assert.IsTrue(coin.Glow.Player.IsPlaying("__setup"));
            Assert.IsTrue(coin.Reward.gameObject.activeSelf);Assert.AreEqual("96.3",coin.Reward.text);
            Assert.AreSame(Resources.Load<Font>("RecoveredUI/CoinRewardText/Green"),coin.Reward.font);
            Assert.AreEqual(new Vector2(0,4.6f),coin.Reward.rectTransform.anchoredPosition);
            Assert.AreEqual(0,coin.Reward.fontSize);Assert.IsFalse(coin.IsScaling);
            yield return null;
            coin.PlayShow();
            Assert.IsFalse(coin.Glow.gameObject.activeSelf);Assert.IsFalse(coin.Reward.gameObject.activeSelf);
            Assert.IsTrue(coin.Art.Player.IsPlaying("zcjb_chuxian"));Assert.IsTrue(coin.IsScaling);
            Time.timeScale=0;for(int i=0;i<4;i++)yield return null;
            Assert.That(coin.transform.localScale.x,Is.EqualTo(.7f).Within(.00001f));
            Time.timeScale=1;float peak=0;
            for(int i=0;i<14;i++){yield return null;peak=Mathf.Max(peak,coin.transform.localScale.x);}
            Assert.That(peak,Is.EqualTo(1).Within(.00001f));
            Assert.IsFalse(coin.IsScaling);Assert.That(coin.transform.localScale.x,Is.EqualTo(.7f).Within(.00001f));
            Assert.IsTrue(coin.Art.Player.IsPlaying("zcjb_idle"));
            Assert.AreEqual("96.3",coin.Reward.text,"PlayShow hides the label without clearing its contents.");
        } finally {Time.timeScale=scale;Time.captureDeltaTime=delta;Object.Destroy(coin.gameObject);}
    }

    [UnityTest]
    public IEnumerator FreshCoinInitialAndAppearanceStatesRender()
    {
        var coins=new RecoveredFreeCoin[2];var host=new GameObject("Free coin camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=1.8f;camera.transform.position=new Vector3(0,0,-10);camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var target=new RenderTexture(1200,600,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1200,600,TextureFormat.RGB24,false);float scale=Time.timeScale;
        try {
            for(int i=0;i<2;i++){coins[i]=Object.Instantiate(Resources.Load<RecoveredFreeCoin>("RecoveredSymbols/FreeCoin"));coins[i].transform.position=new Vector3(i==0?-1.8f:1.8f,0,0);}
            yield return null;Time.timeScale=0;
            coins[1].PlayShow();
            for(int i=0;i<2;i++) {
                coins[i].Art.Stop();coins[i].Glow.Stop();coins[i].Art.Rig.Sample(.173f);coins[i].Glow.Rig.Sample(0);
            }
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1200,600),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-coin-initial-appearance.png"),capture.EncodeToPNG());
            int bright=0;foreach(var pixel in capture.GetPixels32())if(pixel.r>80||pixel.g>80||pixel.b>80)bright++;
            Assert.Greater(bright,10000);
        } finally {
            Time.timeScale=scale;RenderTexture.active=previous;camera.targetTexture=null;target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);
            foreach(var coin in coins)if(coin!=null)Object.Destroy(coin.gameObject);
        }
    }
}
