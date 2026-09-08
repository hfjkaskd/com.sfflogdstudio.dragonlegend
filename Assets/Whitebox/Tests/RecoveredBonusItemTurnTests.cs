using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredBonusItemTurnTests
{
    [UnityTest]
    public IEnumerator AuthoredItemSeparatesRewardDelayTurnReadyAndTextAnimation()
    {
        var host=new GameObject("Bonus item current canvas",typeof(RectTransform),typeof(Canvas));
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        ((RectTransform)host.transform).sizeDelta=new Vector2(720,480);
        var cameraRoot=new GameObject("Bonus item current camera",typeof(Camera));var camera=cameraRoot.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=240;camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;canvas.worldCamera=camera;
        var target=new RenderTexture(720,480,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(720,480,TextureFormat.RGB24,false);
        var left=Object.Instantiate(Resources.Load<RecoveredBonusItemTurn>("RecoveredUI/BonusItem"),host.transform);
        var right=Object.Instantiate(Resources.Load<RecoveredBonusItemTurn>("RecoveredUI/BonusItem"),host.transform);
        ((RectTransform)left.transform).anchoredPosition=new Vector2(-160,0);((RectTransform)right.transform).anchoredPosition=new Vector2(160,0);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;
        try {
            Time.captureDeltaTime=.025f;Time.timeScale=0;yield return null;
            left.Initialize();right.Initialize();
            Assert.AreEqual(12,left.Body.Selected);Assert.IsFalse(left.Glow.gameObject.activeSelf);
            Assert.IsFalse(left.RewardText.gameObject.activeSelf);Assert.IsFalse(left.Ad.gameObject.activeSelf);
            Assert.AreEqual(0,left.Button.onClick.GetPersistentEventCount());
            Assert.AreSame(left.Body.Rig,left.Button.targetGraphic);Assert.AreSame(left.Button.gameObject,left.Button.targetGraphic.gameObject);
            Assert.AreEqual(new Vector4(-23.5f,-35,-23.5f,-35),left.Body.Rig.raycastPadding);
            int selected=0;left.Selected+=()=>selected++;left.Button.onClick.Invoke();Assert.AreEqual(1,selected);
            right.ShowAd(true);Assert.IsTrue(right.Ad.gameObject.activeSelf);right.ShowAd(false);
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {Ronig=new RonigPoro {
                RgkorpKgiitr=new List<int>{1},RgokrpMin=new List<int>{963},RgkorpMoj=new List<int>{963},Jimp=new List<int>{-1} }});
            int characterReady=0,cashReady=0;bool cashScaleStillRunning=false;System.Exception error=null;
            left.Failed+=e=>error=e;right.Failed+=e=>error=e;
            left.Begin(RecoveredBonusType.Zhao,rules,0,()=>{characterReady++;Assert.AreEqual(5,left.Body.Selected);Assert.IsTrue(left.Glow.gameObject.activeSelf);});
            right.Begin(RecoveredBonusType.Reward,rules,0,()=>{cashReady++;Assert.IsTrue(right.RewardText.gameObject.activeSelf);cashScaleStillRunning=right.RewardText.transform.localScale.x<1.2f;});
            var before=Random.state;
            for(int i=0;i<8;i++)yield return null;
            Assert.AreEqual(before,Random.state);Assert.AreEqual(0,characterReady);Assert.AreEqual(0,cashReady);
            Assert.IsFalse(right.RewardText.gameObject.activeSelf);
            Time.timeScale=1;
            for(int i=0;i<6;i++)yield return null;Assert.IsFalse(right.RewardText.gameObject.activeSelf);
            for(int i=0;i<9;i++)yield return null;
            Assert.IsNull(error);Assert.AreEqual(963,right.Reward);Assert.IsTrue(right.RewardJump);
            Assert.AreEqual(1,characterReady);Assert.AreEqual(1,cashReady);Assert.IsTrue(cashScaleStillRunning);
            Assert.AreEqual(1,left.Body.PlaybackSpeed);Assert.IsFalse(left.RewardText.gameObject.activeSelf);
            for(int i=0;i<16;i++)yield return null;Assert.AreEqual(Vector3.one,right.RewardText.transform.localScale);
            Time.timeScale=0;yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,720,480),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-item-reward.png"),capture.EncodeToPNG());
            // Reinitialization/cancellation must not trigger delayed reward draws or callbacks.
            right.Initialize();right.Begin(RecoveredBonusType.Reward,rules,0,()=>cashReady++);right.Cancel();
            var cancelled=Random.state;Time.timeScale=1;
            for(int i=0;i<45;i++)yield return null;
            Assert.AreEqual(cancelled,Random.state);Assert.AreEqual(1,cashReady);Assert.IsFalse(right.RewardText.gameObject.activeSelf);
        }finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);Object.Destroy(cameraRoot);
        }
    }
}
