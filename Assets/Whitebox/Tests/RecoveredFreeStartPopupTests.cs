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

public sealed class RecoveredFreeStartPopupTests
{
    [UnityTest]
    public IEnumerator SourceCountFontButtonsAdBranchesAndAfterHideCompletion()
    {
        var host=new GameObject("Current Free start canvas",typeof(RectTransform),typeof(Canvas));host.layer=5;
        var cameraHost=new GameObject("Current Free start popup camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;canvas.worldCamera=camera;
        ((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var popup=Object.Instantiate(Resources.Load<RecoveredFreeStartPopup>("RecoveredUI/FreeStartPopup"),host.transform);
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;yield return null;
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{GjrroRrggGping=new List<int>{4,99}}});
            var progress=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Free start must not save"),new PlayerData{GreenCount=5000});
            progress.FreeSpinCount=8;var ads=new LocalAdFacade();var sounds=new List<string>();
            int completed=0,fingers=0,hidden=0;popup.SoundRequested+=sounds.Add;
            popup.FingerRequested+=parent=>{Assert.AreSame(popup.AdvertisedText.transform,parent);fingers++;};
            popup.FingerHideRequested+=()=>hidden++;
            Action done=()=>{Assert.IsFalse(popup.gameObject.activeSelf);completed++;};
            popup.Show(8,progress,rules,ads,done);
            var finger=popup.Finger;Assert.IsNotNull(finger);Assert.AreSame(popup.AdvertisedText.transform,finger.parent);
            Assert.IsTrue(finger.gameObject.activeInHierarchy);Assert.AreEqual(Vector2.zero,finger.anchoredPosition);Assert.AreEqual(Vector3.one,finger.localScale);
            for(int i=0;i<8;i++)yield return null;
            Assert.IsFalse(popup.IsTransitioning);Assert.AreEqual(Vector3.one,popup.Content.localScale);
            Assert.AreEqual("8",popup.CountText.text);Assert.AreEqual("<sprite name=\"tc_btn_bofang\">FREE+4",popup.AdvertisedText.text);
            Assert.AreEqual("START",popup.PlainText.text);Assert.AreSame(popup.PlainText,popup.PlainButton.targetGraphic);
            Assert.AreSame(popup.PlainText.gameObject,popup.PlainButton.gameObject);
            Assert.AreEqual("FreeCountFont",popup.CountText.font.name);Assert.AreEqual(0,popup.CountText.fontSize);
            CharacterInfo zero;Assert.IsTrue(popup.CountText.font.GetCharacterInfo('0',out zero,0));
            Assert.AreEqual(193,zero.advance);Assert.AreEqual(126,zero.maxY);Assert.AreEqual(-126,zero.minY);
            Assert.AreEqual(new Vector3(1.3f,1.3f,1.3f),popup.CountText.transform.localScale);
            Assert.AreEqual(new Vector2(0,107),popup.CountText.rectTransform.anchoredPosition);
            var description=popup.Content.Find("Text (TMP)").GetComponent<TMPro.TMP_Text>();
            // Original UIFreeSpinStart TMP 114918264173551851 and Rect 224946936116752871.
            Assert.AreEqual("Watch an ad to get more free spins.",description.text);
            Assert.AreEqual(58,description.fontSize);Assert.IsFalse(description.enableAutoSizing);
            Assert.AreEqual(TMPro.FontStyles.Bold,description.fontStyle);
            Assert.IsFalse(description.enableWordWrapping);Assert.AreEqual(TMPro.TextOverflowModes.Overflow,description.overflowMode);
            Assert.AreEqual(TMPro.HorizontalAlignmentOptions.Center,description.horizontalAlignment);
            Assert.AreEqual(TMPro.VerticalAlignmentOptions.Middle,description.verticalAlignment);
            Assert.AreEqual(Vector4.zero,description.margin);
            Assert.AreEqual(new Vector2(200,50),description.rectTransform.sizeDelta);
            Assert.AreEqual(new Vector2(0,-394),description.rectTransform.anchoredPosition);
            Assert.AreEqual(Vector3.one,description.transform.localScale);
            foreach(var c in popup.GetComponentsInChildren<Component>(true))Assert.IsNotNull(c);
            foreach(var button in popup.GetComponentsInChildren<Button>(true)){
                Assert.AreEqual(0,button.onClick.GetPersistentEventCount());Assert.IsNotNull(button.targetGraphic);Assert.Greater(button.targetGraphic.color.a,0);
            }
            Time.timeScale=0;yield return null;Render(popup,camera,target,capture,"initial");
            popup.ClaimButton.onClick.Invoke();Assert.IsTrue(ads.Pending);Assert.IsFalse(popup.IsClicked);
            Assert.IsFalse(finger.gameObject.activeSelf);
            Assert.AreEqual("freespin",ads.Placement);Assert.AreEqual("freespin",ads.Scene);
            ads.Complete(AdOutcome.Failed);Assert.IsTrue(popup.gameObject.activeSelf);Assert.AreEqual(8,progress.FreeSpinCount);
            Assert.IsFalse(finger.gameObject.activeSelf,"Native ad failure resets the flag, but does not restore the hand.");
            popup.PlainButton.onClick.Invoke();for(int i=0;i<4;i++)yield return null;
            Assert.IsTrue(popup.gameObject.activeSelf);Assert.AreEqual(0,completed);
            Time.timeScale=1;for(int i=0;i<10&&popup.gameObject.activeSelf;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.AreEqual(0,ads.InterstitialCount);Assert.AreEqual(8,progress.FreeSpinCount);
            CollectionAssert.AreEqual(new[]{"fsstart","click","click"},sounds);

            sounds.Clear();popup.Show(8,progress,rules,ads,done);for(int i=0;i<8;i++)yield return null;
            Assert.AreSame(finger,popup.Finger);Assert.IsTrue(finger.gameObject.activeInHierarchy);
            Time.timeScale=0;yield return null;
            popup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(12,progress.FreeSpinCount);Assert.AreEqual("8",popup.CountText.text);
            // A second completed ad uses the immutable initial count; it must not add another four.
            popup.ClaimButton.onClick.Invoke();ads.Complete(AdOutcome.Rewarded);Assert.AreEqual(12,progress.FreeSpinCount);
            for(int i=0;i<4;i++)yield return null;Assert.AreEqual("8",popup.CountText.text);Assert.IsTrue(popup.gameObject.activeSelf);
            Time.timeScale=1;float started=Time.time;
            while(Time.time-started<.35f)yield return null;
            Assert.AreEqual("12",popup.CountText.text);Assert.IsFalse(popup.IsTransitioning);
            Assert.AreEqual(1,completed);Assert.AreEqual(8,popup.InitialCount);
            Time.timeScale=0;yield return null;Render(popup,camera,target,capture,"extra");
            for(int i=0;i<5;i++)yield return null;Assert.IsTrue(popup.gameObject.activeSelf);
            Time.timeScale=1;
            for(int i=0;i<20&&popup.gameObject.activeSelf;i++)yield return null;
            Assert.AreEqual(2,completed);Assert.AreEqual(12,progress.FreeSpinCount);Assert.AreEqual(5000,progress.GreenCount);
            Assert.That(Time.time-started,Is.InRange(.75f,.95f),".5-second ad wait plus .3-second exit; count runs concurrently");
            CollectionAssert.AreEqual(new[]{"fsstart","click","click"},sounds);
            Assert.AreEqual(2,fingers);Assert.AreEqual(4,hidden);Assert.AreEqual(0,ads.InterstitialCount);
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            Object.Destroy(capture);Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);
        }
    }
    private static void Render(RecoveredFreeStartPopup popup,Camera camera,RenderTexture target,Texture2D capture,string name)
    {
        Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
        File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-start-popup-"+name+".png"),capture.EncodeToPNG());
        foreach(var button in new[]{popup.ClaimButton,popup.PlainButton}){
            var point=camera.WorldToViewportPoint(((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
            Assert.That(point.x,Is.InRange(0f,1f));Assert.That(point.y,Is.InRange(0f,1f));
        }
    }
}
