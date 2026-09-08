using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class RecoveredBalancePanelTests
{
    private static RecoveredGameplayRules Rules() => new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {
        Qonrii = new QonriiPoro { Rgtigk = new List<int>{99}, Lgtgl = new List<int>{1,2,3}, NggpGpin = new List<int>{5,10,20} }
    });

    [TestCase(178900f,0,2,"$1789.00")]
    [TestCase(178900f,1,2,"R$1789,00")]
    [TestCase(178900f,5,2,"R$1789,00")]
    [TestCase(-125f,0,2,"$-1.25")]
    [TestCase(178900f,0,0,"$1,789")]
    [TestCase(178900f,1,0,"R$1,789")]
    public void CurrencyFollowsNativeUnitsAndFormatting(float value,int language,int decimals,string expected)
        => Assert.AreEqual(expected,RecoveredCurrency.Format(value,language,decimals));

    [Test]
    public void CashTweenUsesEventStartAndRebindRemovesOldListeners()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/BalancePanel"));
        try {
            var view=root.GetComponent<RecoveredBalancePanel>(); view.enabled=false;
            var rules=Rules(); var player=new RecoveredPlayerProgress(rules,()=>{},new PlayerData {Level=1,GreenCount=10000});
            view.Bind(player,rules,0); Assert.AreEqual("$100.00",view.BalanceText);
            player.SetGreenCount(20000); Assert.AreEqual("$100.00",view.BalanceText);
            view.AdvanceAnimations(0.25f); Assert.AreEqual("$175.00",view.BalanceText,"Original default OutQuad at halfway is 0.75.");
            player.SetGreenCount(30000); view.AdvanceAnimations(0.25f);
            Assert.AreEqual("$275.00",view.BalanceText,"Restart uses old stored balance 20000, not displayed 17500.");
            view.AdvanceAnimations(0.25f); Assert.AreEqual("$300.00",view.BalanceText);
            var second=new RecoveredPlayerProgress(rules,()=>{},new PlayerData {Level=1,GreenCount=12345});
            view.Bind(second,rules,1); player.SetGreenCount(50000); view.AdvanceAnimations(1);
            Assert.AreEqual("R$123,45",view.BalanceText);
        } finally {Object.DestroyImmediate(root);}
    }

    [Test]
    public void LevelChangesOnlyAfterFullFillAndUsesNewLevelThreshold()
    {
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/BalancePanel"));
        try {
            var view=root.GetComponent<RecoveredBalancePanel>(); view.enabled=false;
            var rules=Rules(); var player=new RecoveredPlayerProgress(rules,()=>{},new PlayerData {Level=1,LevelExpCount=1});
            view.Bind(player,rules,0); Assert.AreEqual("1/5",view.ProgressText); Assert.AreEqual(0.2f,view.FillAmount,0.0001f);
            player.SetExperience(5); Assert.AreEqual("1",view.LevelText); Assert.AreEqual("5/5",view.ProgressText);
            view.AdvanceAnimations(0.25f); Assert.AreEqual(0.6f,view.FillAmount,0.0001f); Assert.AreEqual("1",view.LevelText);
            view.AdvanceAnimations(0.25f); Assert.AreEqual("2",view.LevelText); Assert.AreEqual("0/10",view.ProgressText); Assert.AreEqual(0,view.FillAmount);
            player.SetExperience(3); view.AdvanceAnimations(0.5f);
            Assert.AreEqual("2",view.LevelText); Assert.AreEqual("3/10",view.ProgressText); Assert.AreEqual(0.3f,view.FillAmount,0.0001f);
        } finally {Object.DestroyImmediate(root);}
    }

    [UnityTest]
    public IEnumerator OriginalBalanceAndLevelResourcesImportAndRender()
    {
        var prefab=Resources.Load<GameObject>("RecoveredUI/BalancePanel");
        Assert.IsTrue(prefab != null);
        var cameraHost=new GameObject("BalancePreviewCamera",typeof(Camera));
        var canvasHost=new GameObject("BalancePreviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        canvasHost.layer=5; // Screen-space Canvas draw calls use the root Canvas layer.
        var render=new RenderTexture(1080,1920,24);
        GameObject panel=null; Texture2D capture=null;
        var previous=RenderTexture.active;
        try {
            var camera=cameraHost.GetComponent<Camera>();
            camera.transform.position=new Vector3(100,0,-10);
            camera.orthographic=true; camera.orthographicSize=5;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.cullingMask=1<<5;camera.targetTexture=render;
            var canvas=canvasHost.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=10;
            var scaler=canvasHost.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
            panel=Object.Instantiate(prefab,canvas.transform,false);
            var rules=Rules();
            var player=new RecoveredPlayerProgress(rules,()=>{},new PlayerData {Level=2,GreenCount=178900,LevelExpCount=8});
            panel.GetComponent<RecoveredBalancePanel>().Bind(player,rules,0);
            var texts=panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.AreEqual(3,texts.Length);
            foreach(var text in texts){Assert.IsTrue(text.font != null,text.name);Assert.IsTrue(text.fontSharedMaterial != null,text.name);}
            var images=panel.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(7,images.Length);
            foreach(var image in images) {
                Assert.IsTrue(image.sprite != null,image.name+" requires its original Sprite asset.");
                Assert.IsTrue(image.sprite.texture != null,image.name+" requires its original texture.");
                Assert.Greater(image.sprite.rect.width,0,image.name);
            }
            Canvas.ForceUpdateCanvases();
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=render});
            RenderTexture.active=render;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            int colored=0;
            foreach(var pixel in capture.GetPixels32()) if(pixel.r>8 || pixel.g>8 || pixel.b>8) colored++;
            Assert.Greater(colored,1000,"The current prefab render must contain visible content.");
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-balance-panel.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,capture.EncodeToPNG());
        } finally {
            RenderTexture.active=previous;
            if(capture!=null)Object.Destroy(capture);
            if(panel!=null)Object.Destroy(panel);
            Object.Destroy(canvasHost);Object.Destroy(cameraHost);
            render.Release();Object.Destroy(render);
        }
        yield return null;
    }
}