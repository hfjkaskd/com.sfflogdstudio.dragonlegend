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

public sealed class RecoveredCashOutListTests
{
    private static RecoveredGameplayRules Rules()=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{Qogt=new List<int>{10000,20000,30000,40000,50000,60000},Rogk1roil=new List<int>{20,20,20,20,20,20},Rimgg1=new List<int>{7,7,7,7,7,7}}});
    [UnityTest]
    public IEnumerator PooledCardsDriveBottomAndKeepNativeInitialSelectionDuringPaymentChanges()
    {
        var list=Object.Instantiate(Resources.Load<RecoveredCashOutList>("RecoveredUI/CashOutList"));var bottom=Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"));
        try
        {
            var rules=Rules();var data=new PlayerData{GreenCount=3500};var player=new RecoveredPlayerProgress(rules,()=>Assert.Fail("List must not save"),data);
            list.Scroll.enabled=false;list.Initialize(rules,player,bottom,()=>102,new Vector2(1080,620),1,0);yield return null;
            Assert.AreEqual(3,list.CreatedCount);Assert.AreEqual(0,list.InitialSelection);Assert.AreSame(list.ItemAt(0).transform,list.SelectionFrame.parent);
            Assert.AreEqual(new Vector2(1080,1860),list.Scroll.content.sizeDelta);Assert.AreEqual(new Vector3(540,-155,0),list.ItemAt(0).transform.localPosition);
            list.ItemAt(2).Button.onClick.Invoke();Assert.AreSame(list.ItemAt(2).transform,list.SelectionFrame.parent);StringAssert.EndsWith(RecoveredCurrency.Format(30000,0,0),bottom.DetailText.text);Assert.AreEqual(0,list.InitialSelection);
            list.RefreshPaymentType(4);Assert.AreSame(list.ItemAt(2).transform,list.SelectionFrame.parent);StringAssert.EndsWith(RecoveredCurrency.Format(10000,0,0),bottom.DetailText.text);
            var original=list.ItemAt(0);list.Scroll.content.localPosition=new Vector3(0,621,0);yield return null;
            Assert.IsNull(list.ItemAt(0));Assert.IsNull(list.ItemAt(1));Assert.IsNotNull(list.ItemAt(4));Assert.AreEqual(3,list.CreatedCount);
            list.Scroll.content.localPosition=Vector3.zero;yield return null;Assert.AreEqual(3,list.CreatedCount);Assert.AreSame(original,list.ItemAt(0));
            data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=1000});list.RefreshData();yield return null;
            Assert.AreEqual(1,list.InitialSelection);Assert.AreSame(list.ItemAt(1).transform,list.SelectionFrame.parent);
            for(int i=1;i<6;i++)data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=i,step=1000});list.RefreshData();yield return null;
            Assert.AreEqual(-1,list.InitialSelection);Assert.IsFalse(list.SelectionFrame.gameObject.activeSelf);
        }
        finally{Object.Destroy(list.gameObject);bottom.Cancel();Object.Destroy(bottom.gameObject);}
        yield return null;
    }
    [UnityTest]
    public IEnumerator BottomExpiryRefreshesRealCardAndBothCompletionChecks()
    {
        var list=Object.Instantiate(Resources.Load<RecoveredCashOutList>("RecoveredUI/CashOutList"));var bottom=Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"));float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try
        {
            var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0,count=20,time=100,type=1});var rules=Rules();int now=106;
            Time.timeScale=0;Time.captureDeltaTime=.1f;list.Scroll.enabled=false;list.Initialize(rules,new RecoveredPlayerProgress(rules,()=>Assert.Fail("No save"),data),bottom,()=>now,new Vector2(1080,620),1,0);yield return null;
            Assert.IsFalse(bottom.WaitComplete);Assert.IsTrue(bottom.TaskComplete);now=107;Time.timeScale=1;for(int i=0;i<15;i++)yield return null;
            Assert.IsTrue(bottom.WaitComplete);Assert.AreEqual("Pending Review 00:00:00",bottom.TimeText.text);Assert.AreEqual("Pending Review00:00:00",list.ItemAt(0).TimeText.text);
        }
        finally{Object.Destroy(list.gameObject);bottom.Cancel();Object.Destroy(bottom.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
        yield return null;
    }
    [UnityTest]
    public IEnumerator CurrentListAndBottomRenderTogether()
    {
        GameObject host=null,cameraObject=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try
        {
            cameraObject=new GameObject("Cash list capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.12f);
            target=new RenderTexture(1080,1400,24);target.Create();camera.targetTexture=target;
            host=new GameObject("Cash list canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1400);
            var list=Object.Instantiate(Resources.Load<RecoveredCashOutList>("RecoveredUI/CashOutList"),host.transform,false);var bottom=Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"),host.transform,false);
            var rules=Rules();var data=new PlayerData{GreenCount=3500};list.Initialize(rules,new RecoveredPlayerProgress(rules,()=>Assert.Fail("No save"),data),bottom,()=>102,new Vector2(1080,930),1,0);
            var lr=(RectTransform)list.transform;lr.anchorMin=lr.anchorMax=new Vector2(.5f,1);lr.anchoredPosition=new Vector2(0,-465);
            var br=(RectTransform)bottom.transform;br.anchorMin=br.anchorMax=new Vector2(.5f,0);br.anchoredPosition=Vector2.zero;
            for(int i=0;i<4;i++)yield return null;list.ItemAt(1).Button.onClick.Invoke();Canvas.ForceUpdateCanvases();
            for(int i=0;i<3;i++)
            {
                var icon=list.ItemAt(i).Button.transform.Find("Img").GetComponent<Image>();
                Assert.IsNotNull(icon.sprite,"Payment sprite "+i);
                StringAssert.StartsWith("tx_icon_01",icon.sprite.name);Assert.AreSame(icon.sprite,icon.overrideSprite);Assert.IsNotNull(icon.mainTexture);
            }
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});capture=new Texture2D(1080,1400,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1400),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-list-bottom.png"),capture.EncodeToPNG());
            Assert.AreSame(list.ItemAt(1).transform,list.SelectionFrame.parent);Assert.IsNotNull(list.Scroll.viewport.GetComponent<RectMask2D>());
        }
        finally{RenderTexture.active=prior;if(host!=null)Object.Destroy(host);if(cameraObject!=null)Object.Destroy(cameraObject);if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);}
        yield return null;
    }
}
