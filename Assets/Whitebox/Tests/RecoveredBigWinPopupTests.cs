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

public sealed class RecoveredBigWinPopupTests
{
    static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii=new QonriiPoro{RiikinQloim=new List<int>{2000,500}},Rgpggm=new RgpggmPoro{Qogt=new List<int>{10000,20000,50000}}});
    [UnityTest]
    public IEnumerator ActualPrefabShowsAllTiersDelaysButtonsAndWaitsForCountExitAndFlight()
    {
        var host=new GameObject("Current jackpot popup canvas",typeof(RectTransform),typeof(Canvas));
        var cameraHost=new GameObject("Current jackpot popup camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        host.layer=5;((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var popup=Object.Instantiate(Resources.Load<RecoveredBigWinPopup>("RecoveredUI/BigWinPopup"),host.transform);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            var rules=Rules();var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Popup must not credit"),new PlayerData{GreenCount=5000});
            var ads=new LocalAdFacade();var order=new List<string>();int task=0,calls=0,expectedCalls=0;bool flight=false;
            popup.PauseMusicRequested+=()=>order.Add("pause");popup.Sound1Requested+=s=>order.Add(s);popup.SoundRequested+=s=>order.Add(s);
            popup.CashOutTaskRefreshRequested+=(a,b)=>{Assert.AreEqual(3,a);Assert.AreEqual(1,b);task++;};
            popup.StopSound1Requested+=()=>order.Add("stop");popup.ResumeMusicRequested+=()=>order.Add("resume");
            popup.FlyCoinRequested+=amount=>{Assert.AreEqual(40000,amount);Assert.AreEqual(expectedCalls,calls);flight=true;order.Add("fly");};
            var names=new[]{"big","mega","super"};
            for(int tier=0;tier<3;tier++)
            {
                expectedCalls=tier+1;flight=false;
                popup.Show((RecoveredSlotWinType)(tier+1),20000,progress,rules,ads,false,0,_=>{calls++;order.Add("caller");});
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
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bigwin-window-"+names[tier]+".png"),capture.EncodeToPNG());
                // Check rendered pixels, not just TMP character metadata. Do not mutate
                // a sibling text mesh solely to inspect already-rendered character metadata.
                for(int c=0;c<popup.PlainText.textInfo.characterCount;c++) {
                    var ch=popup.PlainText.textInfo.characterInfo[c];if(char.IsWhiteSpace(ch.character))continue;
                    var lower=camera.WorldToScreenPoint(popup.PlainText.transform.TransformPoint(ch.vertex_BL.position));
                    var upper=camera.WorldToScreenPoint(popup.PlainText.transform.TransformPoint(ch.vertex_TR.position));int white=0;
                    for(int y=(int)lower.y;y<(int)upper.y;y++)for(int x=(int)lower.x;x<(int)upper.x;x++) {
                        var px=capture.GetPixel(x,y);if(px.r>.9f&&px.g>.9f&&px.b>.9f)white++;
                    }
                    Assert.Greater(white,20,"Missing rendered plain-label glyph "+ch.character+" in tier "+tier);
                }
                // TMP 3.0.9 GenerateTextMesh overwrites the parsed spriteCount with
                // m_spriteCount. Check the actual parsed character and mesh instead.
                var character=popup.AdvertisedText.textInfo.characterInfo[0];
                Assert.AreEqual(TMP_TextElementType.Sprite,character.elementType);
                Assert.IsTrue(character.isVisible);Assert.AreEqual(0,character.spriteIndex);
                var mesh=popup.AdvertisedText.textInfo.meshInfo[character.materialReferenceIndex];
                Assert.GreaterOrEqual(mesh.vertexCount,4);
                if(tier<2) {
                    popup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);
                    for(int i=0;i<30&&popup.gameObject.activeSelf;i++)yield return null;
                    Assert.IsFalse(popup.gameObject.activeSelf);Assert.AreEqual(expectedCalls,calls);
                    Assert.IsTrue(flight);Assert.AreEqual(5000,progress.GreenCount);
                }
            }
            Assert.AreEqual(3,task);popup.ClaimButton.onClick.Invoke();Assert.IsTrue(ads.Pending);
            ads.Complete(AdOutcome.Failed);Assert.IsFalse(popup.Claim.IsClicked);Assert.IsFalse(flight);
            popup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);
            Time.timeScale=0;yield return null;yield return null;
            string pausedText=popup.RewardText.text;for(int i=0;i<4;i++)yield return null;
            Assert.AreEqual(pausedText,popup.RewardText.text);Assert.IsTrue(popup.gameObject.activeSelf);Assert.IsFalse(flight);
            Time.timeScale=1;
            for(int i=0;i<30&&popup.gameObject.activeSelf;i++)yield return null;
            Assert.IsFalse(popup.gameObject.activeSelf);Assert.AreEqual("$400.00",popup.RewardText.text);
            Assert.AreEqual(Vector3.zero,popup.Content.localScale);Assert.IsTrue(flight);Assert.AreEqual(3,calls);
            CollectionAssert.AreEqual(new[]{"stop","resume","caller","fly"},order.GetRange(order.Count-4,4));
            Assert.AreEqual(3,calls);Assert.AreEqual(5000,progress.GreenCount);
        }
        finally{Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);}
    }
}
