using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredJackpotPopupTests
{
    static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii=new QonriiPoro{JpQloim=new List<int>{2000,500}},Rgpggm=new RgpggmPoro{Qogt=new List<int>{10000,20000,50000}}});
    [Test]
    public void AuthoredPopupRetainsOriginalTextButtonAndCashTipLayout()
    {
        var popup=Object.Instantiate(Resources.Load<RecoveredJackpotPopup>("RecoveredUI/JackpotPopup"));
        try
        {
            Assert.AreEqual(new Vector2(0,-202),((RectTransform)popup.RewardText.transform).anchoredPosition);
            Assert.AreEqual(Vector3.one*2,popup.RewardText.transform.localScale);
            Assert.AreEqual(0,popup.RewardText.fontSize);Assert.AreEqual("Green",popup.RewardText.font.name);
            Assert.AreEqual(VerticalWrapMode.Truncate,popup.RewardText.verticalOverflow);
            Assert.AreEqual(new Vector2(507,165),((RectTransform)popup.ClaimButton.transform).sizeDelta);
            Assert.AreEqual(new Vector2(0,57.2f),((RectTransform)popup.ClaimButton.transform).anchoredPosition);
            Assert.AreEqual(64,popup.AdvertisedText.fontSize);Assert.AreEqual(60,popup.PlainText.fontSize);
            Assert.AreEqual(FontStyles.Underline,popup.PlainText.fontStyle);
            Assert.AreSame(popup.ClaimButton.GetComponent<Image>(),popup.ClaimButton.targetGraphic);
            Assert.AreEqual("tc_btn_01",popup.ClaimButton.GetComponent<Image>().sprite.name);
            Assert.AreEqual(0,popup.ClaimButton.onClick.GetPersistentEventCount());
            Assert.AreEqual(0,popup.PlainButton.onClick.GetPersistentEventCount());
            var sprite=popup.AdvertisedText.spriteAsset;
            Assert.AreEqual("tc_btn_bofang",sprite.spriteCharacterTable[0].name);
            Assert.AreEqual(-2029773876,sprite.hashCode);Assert.IsNotNull(sprite.spriteGlyphTable[0].sprite);
            Assert.AreEqual(1.7f,sprite.spriteGlyphTable[0].scale);
            Assert.AreEqual(59.54f,sprite.spriteGlyphTable[0].metrics.horizontalBearingY);
            Assert.AreEqual(new Vector2(1080,275.5964f),((RectTransform)popup.CashOutTip.transform).sizeDelta);
            Assert.IsFalse(popup.CashOutTip.ProgressSlider.interactable);
            foreach(var img in popup.CashOutTip.GetComponentsInChildren<Image>(true))Assert.IsNotNull(img.sprite,img.name);
            foreach(var component in popup.GetComponentsInChildren<Component>(true))Assert.IsNotNull(component,"Missing script");
        }
        finally{Object.DestroyImmediate(popup.gameObject);}
    }
    [Test]
    public void CashTipFindsFirstAbsentIdAndFormatsActualBalanceWithoutWrites()
    {
        var popup=Object.Instantiate(Resources.Load<RecoveredJackpotPopup>("RecoveredUI/JackpotPopup"));
        try
        {
            var rules=Rules();var data=new PlayerData{GreenCount=5000};
            data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,isCashout=false});
            var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Tip must not save"),data);var tip=popup.CashOutTip;
            Assert.IsTrue(tip.Initialize(progress,rules,false,0));Assert.AreEqual(.25f,tip.ProgressSlider.value);
            Assert.AreEqual("$50.00/$200",tip.ProgressText.text);
            StringAssert.Contains("$150.00",tip.Tips.text);StringAssert.Contains("withdrawl",tip.Tips.text);
            data.GreenCount=24000;Assert.IsTrue(tip.Initialize(progress,rules,false,0));
            Assert.AreEqual(1,tip.ProgressSlider.value);Assert.AreEqual("$240.00/$200",tip.ProgressText.text);
            Assert.AreEqual("You Can Cash Out <material=\"#003815_3\"><gradient=\"cash\">$200</gradient></material> Now!",tip.Tips.text);
            Assert.IsFalse(tip.Initialize(progress,rules,true,0));Assert.IsFalse(tip.gameObject.activeSelf);
            data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=1});data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=2});
            Assert.IsFalse(tip.Initialize(progress,rules,false,0));Assert.IsFalse(tip.gameObject.activeSelf);
        }
        finally{Object.DestroyImmediate(popup.gameObject);}
    }
    [UnityTest]
    public IEnumerator ActualPrefabShowsAllTiersDelaysButtonsAndWaitsForCountExitAndFlight()
    {
        var host=new GameObject("Current jackpot popup canvas",typeof(RectTransform),typeof(Canvas));
        var cameraHost=new GameObject("Current jackpot popup camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        host.layer=5;((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var popup=Object.Instantiate(Resources.Load<RecoveredJackpotPopup>("RecoveredUI/JackpotPopup"),host.transform);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            var rules=Rules();var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Popup must not credit"),new PlayerData{GreenCount=5000});
            var ads=new LocalAdFacade();var order=new List<string>();int task=0,calls=0;Action flight=null;
            popup.PauseMusicRequested+=()=>order.Add("pause");popup.Sound1Requested+=s=>order.Add(s);popup.SoundRequested+=s=>order.Add(s);
            popup.CashOutTaskRefreshRequested+=(a,b)=>{Assert.AreEqual(2,a);Assert.AreEqual(1,b);task++;};
            popup.HideWheelRequested+=()=>order.Add("wheel");popup.StopSound1Requested+=()=>order.Add("stop");popup.ResumeMusicRequested+=()=>order.Add("resume");
            popup.FlyCoinRequested+=(amount,completed)=>{Assert.AreEqual(40000,amount);flight=completed;order.Add("fly");};
            var names=new[]{"grand","major","minor"};
            for(int tier=0;tier<3;tier++)
            {
                popup.Show((RecoveredJackpotType)(tier+1),20000,progress,rules,ads,false,0,_=>calls++);
                Assert.AreEqual(Vector3.zero,popup.Content.localScale);
                Assert.IsFalse(popup.PlainButton.gameObject.activeSelf);Assert.IsFalse(popup.CashOutTip.gameObject.activeSelf);
                Assert.AreEqual(tier,popup.Dragon.Selected);Assert.AreEqual("$200.00",popup.RewardText.text);
                Assert.AreEqual("<sprite name=\"tc_btn_bofang\">CLAIMx2",popup.AdvertisedText.text);
                Assert.AreEqual("Only $100.00",popup.PlainText.text);
                for(int i=0;i<32;i++)yield return null;
                Assert.AreEqual(Vector3.one,popup.Content.localScale);Assert.IsFalse(popup.IsTransitioning);
                Assert.IsTrue(popup.PlainButton.gameObject.activeSelf);Assert.IsTrue(popup.CashOutTip.gameObject.activeSelf);
                Assert.AreEqual(Vector3.one,popup.PlainButton.transform.localScale);
                Assert.AreEqual(Vector3.one,popup.CashOutTip.transform.localScale);
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-jackpot-window-"+names[tier]+".png"),capture.EncodeToPNG());
                popup.AdvertisedText.ForceMeshUpdate();
                // TMP 3.0.9 GenerateTextMesh overwrites the parsed spriteCount with
                // m_spriteCount. Check the actual parsed character and mesh instead.
                var character=popup.AdvertisedText.textInfo.characterInfo[0];
                Assert.AreEqual(TMP_TextElementType.Sprite,character.elementType);
                Assert.IsTrue(character.isVisible);Assert.AreEqual(0,character.spriteIndex);
                var mesh=popup.AdvertisedText.textInfo.meshInfo[character.materialReferenceIndex];
                Assert.GreaterOrEqual(mesh.vertexCount,4);
            }
            Assert.AreEqual(3,task);popup.ClaimButton.onClick.Invoke();Assert.IsTrue(ads.Pending);
            ads.Complete(AdOutcome.Failed);Assert.IsFalse(popup.Claim.IsClicked);Assert.IsNull(flight);
            popup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);
            Time.timeScale=0;yield return null;yield return null;
            string pausedText=popup.RewardText.text;for(int i=0;i<4;i++)yield return null;
            Assert.AreEqual(pausedText,popup.RewardText.text);Assert.IsTrue(popup.gameObject.activeSelf);Assert.IsNull(flight);
            Time.timeScale=1;
            for(int i=0;i<30&&popup.gameObject.activeSelf;i++)yield return null;
            Assert.IsFalse(popup.gameObject.activeSelf);Assert.AreEqual("$400.00",popup.RewardText.text);
            Assert.AreEqual(Vector3.zero,popup.Content.localScale);Assert.IsNotNull(flight);Assert.AreEqual(0,calls);
            CollectionAssert.AreEqual(new[]{"wheel","stop","resume","fly"},order.GetRange(order.Count-4,4));
            flight();Assert.AreEqual(1,calls);Assert.AreEqual(5000,progress.GreenCount);
        }
        finally{Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);}
    }
}
