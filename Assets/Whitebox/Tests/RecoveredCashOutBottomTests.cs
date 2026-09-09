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

public sealed class RecoveredCashOutBottomTests
{
    private static RecoveredGameplayRules Rules()=>new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{
        Qogt=new List<int>{100},Rogk1roil=new List<int>{20},Rogk2roil=new List<int>{0},Rogk3roil=new List<int>{2},Rogk4roil=new List<int>{0},Rogk5roil=new List<int>{0},Rogk6roil=new List<int>{0},Rimgg1=new List<int>{7},Rimg7=new List<int>{7}},Qollgqr=new QollgqrPoro{Ip=new List<int>{0},Lgtgl=new List<int>{1},Ronpom=new List<int>{1},Korrt=new List<int>{1}}});
    [UnityTest]
    public IEnumerator ActualButtonUsesLiveBalanceButCachedTaskAndWaitFlags()
    {
        var view=Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"));
        try
        {
            var data=new PlayerData{GreenCount=99};var rules=Rules();int now=106;view.Bind(new RecoveredPlayerProgress(rules,()=>Assert.Fail("Panel must not save"),data),rules,()=>now);
            string tip=null,missing=null;int account=-1,next=-1;view.TipRequested+=text=>tip=text;view.MissingCashRequested+=text=>missing=text;view.AccountRequested+=(type,id)=>{account=type;Assert.AreEqual(0,id);};
            view.TaskContinuationRequested+=(record,step)=>{Assert.AreSame(data.PlayerCashOutDatas[0],record);next=step;};
            view.Initialize(0,3,0);Assert.IsFalse(view.Button.transform.parent.gameObject.activeSelf);StringAssert.StartsWith("Need ",view.DetailText.text);
            data.GreenCount=100;view.Initialize(0,3,0);Assert.IsTrue(view.Button.transform.parent.gameObject.activeSelf);Assert.AreEqual("You Can Cash Out Now!",view.DetailText.text);
            view.Button.onClick.Invoke();Assert.AreEqual(3,account);data.GreenCount=0;view.Button.onClick.Invoke();StringAssert.Contains("more to withdraw",missing);
            var record=new PlayerCashOutData{id=0,step=0,count=19,time=100};data.PlayerCashOutDatas.Add(record);view.Initialize(0,3,0);
            Assert.AreEqual("Spin 19/20 times",view.TaskText.text);Assert.AreEqual("Pending Review 00:00:01",view.TimeText.text);
            record.count=20;now=107;view.Button.onClick.Invoke();Assert.AreEqual("Please complete the task first",tip);Assert.AreEqual(-1,next);
            now=106;view.Initialize(0,3,0);view.Button.onClick.Invoke();Assert.AreEqual("Please wait for Pending Review 00:00:01",tip);
            now=107;view.RefreshTime();Assert.IsFalse(view.WaitComplete);view.Button.onClick.Invoke();Assert.AreEqual("Please wait for Pending Review 00:00:00",tip);
            view.Initialize(0,3,0);view.Button.onClick.Invoke();Assert.AreEqual(2,next);Assert.AreEqual(0,record.step);Assert.AreEqual(20,record.count);
            record.step=6;data.PlayerCollectDatas.Add(new PlayerCollectData{id=0,count=0});view.Initialize(0,3,0);tip=null;view.Button.onClick.Invoke();Assert.AreEqual("Please complete the task first",tip);
            record.step=1000;view.Initialize(0,3,0);foreach(Transform line in view.transform.Find("Layout"))Assert.IsFalse(line.gameObject.activeSelf);
        }
        finally{view.Cancel();Object.Destroy(view.gameObject);}
        yield return null;
    }
    [UnityTest]
    public IEnumerator TimerRunsInactiveUsesScaledTimeAndRequestsRefreshWithoutSettingFlags()
    {
        var view=Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"));float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try
        {
            var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0,count=20,time=100});int now=102;var rules=Rules();view.Bind(new RecoveredPlayerProgress(rules,()=>Assert.Fail("No save"),data),rules,()=>now);
            Time.captureDeltaTime=.1f;Time.timeScale=0;view.Initialize(0,1,0);view.RefreshTime();Assert.AreEqual("00:05:00",view.TimeText.text);
            int refresh=0;view.RefreshRequested+=()=>refresh++;for(int i=0;i<12;i++)yield return null;Assert.AreEqual(5,view.RemainingSeconds);
            view.gameObject.SetActive(false);Time.timeScale=1;view.TimeShow(-1);for(int i=0;i<55;i++)yield return null;
            Assert.AreEqual(0,view.RemainingSeconds);Assert.AreEqual(1,refresh);Assert.IsFalse(view.WaitComplete);Assert.AreEqual("Pending Review 00:00:00",view.TimeText.text);
            view.TimeShow(1);view.TimeShow(0);for(int i=0;i<15;i++)yield return null;Assert.AreEqual(1,refresh);
        }
        finally{view.Cancel();Object.Destroy(view.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
        yield return null;
    }
    [UnityTest]
    public IEnumerator OriginalBottomLayoutRendersWithCurrentRuntimeData()
    {
        GameObject host=null,cameraObject=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try
        {
            cameraObject=new GameObject("Bottom capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.12f);
            target=new RenderTexture(1080,700,24);target.Create();camera.targetTexture=target;
            host=new GameObject("Bottom canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,700);
            var view=Object.Instantiate(Resources.Load<RecoveredCashOutBottom>("RecoveredUI/CashOutBottom"),host.transform,false);var rect=(RectTransform)view.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            Assert.AreEqual(0,view.Button.onClick.GetPersistentEventCount());Assert.IsNotNull(view.Button.targetGraphic);Assert.IsTrue(view.Button.targetGraphic.transform.IsChildOf(view.Button.transform));
            foreach(var image in view.GetComponentsInChildren<Image>(true))Assert.IsNotNull(image.sprite,image.name);
            foreach(var label in view.GetComponentsInChildren<TMPro.TMP_Text>(true)){Assert.IsNotNull(label.font);Assert.IsNotNull(label.fontSharedMaterial);Assert.IsNotNull(label.font.atlasTexture);}
            var data=new PlayerData();data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,step=0,count=20,time=100});var rules=Rules();view.Bind(new RecoveredPlayerProgress(rules,()=>Assert.Fail("No save"),data),rules,()=>102);view.Initialize(0,1,0);
            for(int i=0;i<3;i++)yield return null;Canvas.ForceUpdateCanvases();rect.anchoredPosition=-rect.rect.center;Canvas.ForceUpdateCanvases();
            var eventHost=new GameObject("Bottom event system",typeof(UnityEngine.EventSystems.EventSystem));
            try
            {
                var eventSystem=eventHost.GetComponent<UnityEngine.EventSystems.EventSystem>();var buttonRect=(RectTransform)view.Button.transform;
                var pointer=new UnityEngine.EventSystems.PointerEventData(eventSystem){position=RectTransformUtility.WorldToScreenPoint(camera,buttonRect.TransformPoint(buttonRect.rect.center))};
                var hits=new List<UnityEngine.EventSystems.RaycastResult>();eventSystem.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);Assert.AreSame(view.Button,hits[0].gameObject.GetComponentInParent<Button>());
            }
            finally{Object.Destroy(eventHost);}
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture=new Texture2D(1080,700,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,700),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-bottom.png"),capture.EncodeToPNG());view.Cancel();
        }
        finally{RenderTexture.active=prior;if(host!=null)Object.Destroy(host);if(cameraObject!=null)Object.Destroy(cameraObject);if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);}
        yield return null;
    }
}
