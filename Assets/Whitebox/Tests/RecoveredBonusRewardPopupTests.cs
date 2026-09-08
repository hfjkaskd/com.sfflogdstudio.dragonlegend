using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredBonusRewardPopupTests
{
    [UnityTest]
    public IEnumerator AuthoredWindowCountsExitsAndDefersClaimUntilCashArrival()
    {
        var host=new GameObject("Current Bonus reward canvas",typeof(RectTransform),typeof(Canvas));host.layer=5;
        var cameraHost=new GameObject("Current Bonus reward camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;camera.cullingMask=33;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        ((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var popup=Object.Instantiate(Resources.Load<RecoveredBonusRewardPopup>("RecoveredUI/BonusRewardPopup"),host.transform);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;yield return null;
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {Rgpggm=new RgpggmPoro{Qogt=new List<int>{10000}}});
            var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Popup must not credit"),new PlayerData{GreenCount=5000});
            progress.IsFirstFreeReward=true;
            var ads=new LocalAdFacade();int calls=0;Action flight=null;var order=new List<string>();
            popup.SoundRequested+=sound=>order.Add(sound);popup.HideWheelRequested+=()=>order.Add("wheel");
            popup.FlyCoinRequested+=(amount,callback)=>{Assert.AreEqual(1926,amount);flight=callback;order.Add("flight");};
            popup.Show(963,progress,rules,ads,false,0,amount=>{Assert.AreEqual(1926,amount);calls++;});
            for(int i=0;i<30;i++)yield return null;
            Assert.IsFalse(popup.IsTransitioning);Assert.AreEqual(Vector3.one,popup.Content.localScale);
            Assert.AreEqual("$9.63",popup.RewardText.text);Assert.AreEqual("Only $4.82",popup.PlainText.text);
            Assert.AreSame(popup.PlainText.gameObject,popup.PlainButton.gameObject);
            Assert.AreSame(popup.PlainText,popup.PlainButton.targetGraphic);
            Assert.IsTrue(popup.PlainButton.gameObject.activeInHierarchy);
            Assert.AreEqual(0,popup.ClaimButton.onClick.GetPersistentEventCount());
            Assert.AreEqual(0,popup.PlainButton.onClick.GetPersistentEventCount());
            foreach(var component in popup.GetComponentsInChildren<Component>(true))Assert.IsNotNull(component);
            Assert.IsNotNull(popup.Content.Find("Title").GetComponent<Image>().sprite);
            var shine=popup.Content.Find("Title").GetComponent<RecoveredTitleShine>();Assert.IsNotNull(shine);
            Time.timeScale=0;yield return null;
            float factor=shine.EffectFactor;
            for(int i=0;i<4;i++)yield return null;Assert.AreEqual(factor,shine.EffectFactor);
            shine.Playing=false;
            long baseLight=0,shinyLight=0;
            for(int phase=0;phase<2;phase++){
                shine.EffectFactor=phase*.5f;
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                long light=0;foreach(var pixel in capture.GetPixels32())light+=pixel.r+pixel.g+pixel.b;
                if(phase==0)baseLight=light;else shinyLight=light;
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-reward-shine-"+phase+".png"),capture.EncodeToPNG());
            }
            Assert.Greater(shinyLight-baseLight,10000,"The native title shader must visibly brighten its sweep band");
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-reward-window.png"),capture.EncodeToPNG());
            Time.timeScale=1;
            popup.ClaimButton.onClick.Invoke();Assert.IsTrue(ads.Pending);
            ads.Complete(AdOutcome.Failed);Assert.IsFalse(popup.Claim.IsClicked);
            popup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);
            Time.timeScale=0;for(int i=0;i<6;i++)yield return null;
            Assert.AreEqual("$9.63",popup.RewardText.text);Assert.IsNull(flight);
            Time.timeScale=1;for(int i=0;i<8;i++)yield return null;
            Assert.IsTrue(popup.gameObject.activeSelf);Assert.IsNull(flight);
            for(int i=0;i<20&&popup.gameObject.activeSelf;i++)yield return null;
            Assert.IsFalse(popup.gameObject.activeSelf);Assert.AreEqual("$19.26",popup.RewardText.text);
            Assert.IsNotNull(flight);Assert.AreEqual(0,calls);flight();Assert.AreEqual(1,calls);
            CollectionAssert.AreEqual(new[]{"jump","click","click","count","wheel","flight"},order);
        }finally{Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);}
    }
}
