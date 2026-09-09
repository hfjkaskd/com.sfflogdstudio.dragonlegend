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

public sealed class RecoveredCollectTipTests
{
    [UnityTest]
    public IEnumerator NativeTipCountsRecordsAndRevealsAfterHalfSecondWithOriginalArt()
    {
        float timeScale=Time.timeScale,delta=Time.captureDeltaTime;
        var host=new GameObject("Collect tip capture",typeof(RectTransform),typeof(Canvas));
        var cameraHost=new GameObject("Collect tip camera",typeof(Camera));
        var camera=cameraHost.GetComponent<Camera>();var canvas=host.GetComponent<Canvas>();
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
        try {
            Time.timeScale=1;Time.captureDeltaTime=1f/64;
            var tip=Object.Instantiate(Resources.Load<RecoveredCollectTip>("RecoveredUI/CollectTip"),host.transform,false);
            var rect=(RectTransform)tip.transform;
            Assert.AreEqual(new Vector2(1080,275.5964f),rect.sizeDelta);
            Assert.AreEqual(new Vector2(.5f,0),rect.anchorMin);Assert.AreEqual(new Vector2(.5f,0),rect.pivot);
            Assert.AreEqual(Vector2.zero,rect.anchoredPosition);
            Assert.IsFalse(tip.ProgressSlider.interactable);Assert.IsFalse(tip.ProgressSlider.wholeNumbers);
            Assert.IsNotNull(tip.GetComponentInChildren<RecoveredRegionAnimator>(true));
            Assert.IsNotNull(tip.CollectImage.sprite);
            var originalIcon=tip.CollectImage.sprite;
            var c=new QollgqrPoro{Ip=new List<int>{2,7,9,11},Lgtgl=new List<int>{1,1,1,1},
                Ronpom=new List<int>{1,1,1,1},Korrt=new List<int>{100,100,100,100},QollgqrRgkorp=new List<int>{100000}};
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qollgqr=c});
            var data=new PlayerData{PlayerCollectDatas=new List<PlayerCollectData>{
                new PlayerCollectData{id=2,count=200,isRecieve=true},new PlayerCollectData{id=2,count=0},new PlayerCollectData{id=999,count=-5}}};
            int saves=0;var player=new RecoveredPlayerProgress(rules,()=>saves++,data);
            yield return null;
            tip.Initialize(player,rules,false,0);
            Assert.IsFalse(tip.gameObject.activeSelf);
            Assert.AreEqual("Collect <material=\"#003815_3\"><gradient=\"cash\">1</gradient></material> treasures to redeem the rewards.",tip.Tips.text);
            Assert.AreEqual("3/4",tip.ProgressText.text);Assert.AreEqual(.75f,tip.ProgressSlider.value);
            Assert.AreEqual("$1,000",tip.RewardText.text);Assert.AreSame(originalIcon,tip.CollectImage.sprite);
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
            Assert.IsFalse(tip.gameObject.activeSelf);Time.timeScale=1;
            for(int frame=1;frame<=52;frame++) {
                yield return null;
                Assert.AreEqual(frame>=32,tip.gameObject.activeSelf,"frame "+frame);
                if(frame>=32){float t=Mathf.Clamp01((frame/64f-.5f)/.3f);Assert.That(Vector3.Distance(Vector3.one*(2*t-t*t),rect.localScale),Is.LessThan(.00003f));}
            }
            Capture(camera,target);
            data.PlayerCollectDatas.Add(new PlayerCollectData{id=2});data.PlayerCollectDatas.Add(new PlayerCollectData{id=7});
            tip.Initialize(player,rules,false,1);
            Assert.AreEqual("5/4",tip.ProgressText.text);Assert.AreEqual(1,tip.ProgressSlider.value);
            Assert.That(tip.Tips.text,Does.Contain(">-1</gradient>"));Assert.AreEqual("R$1,000",tip.RewardText.text);
            Assert.AreEqual(0,saves);Assert.AreSame(originalIcon,tip.CollectImage.sprite);
        } finally {
            camera.targetTexture=null;Object.Destroy(target);Object.Destroy(host);Object.Destroy(cameraHost);
            Time.timeScale=timeScale;Time.captureDeltaTime=delta;
        }
    }
    [Test]
    public void AProfileHidesWithoutReadingMissingConfigurationOrChangingText()
    {
        var tip=Object.Instantiate(Resources.Load<RecoveredCollectTip>("RecoveredUI/CollectTip"));
        try {
            string text=tip.Tips.text;tip.Initialize(null,null,true,0);
            Assert.IsFalse(tip.gameObject.activeSelf);Assert.AreEqual(text,tip.Tips.text);
        } finally {Object.DestroyImmediate(tip.gameObject);}
    }
    private static void Capture(Camera camera,RenderTexture target)
    {
        var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-collect-tip.png"),image.EncodeToPNG());
        } finally {RenderTexture.active=previous;Object.Destroy(image);}
    }
}
