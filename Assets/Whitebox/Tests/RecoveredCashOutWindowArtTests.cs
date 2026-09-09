using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCashOutWindowArtTests
{
    [UnityTest]
    public IEnumerator OriginalWindowHierarchyContainsWorkingListBottomAndRaycastablePaymentButtons()
    {
        GameObject host=null,cameraObject=null,eventHost=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try
        {
            host=new GameObject("Window layout host",typeof(RectTransform));((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
            cameraObject=new GameObject("Cash window capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.orthographicSize=960;camera.transform.position=new Vector3(0,0,-100);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.12f);
            target=new RenderTexture(1080,1920,24);target.Create();camera.targetTexture=target;
            var window=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutWindow"),host.transform,false);Assert.IsFalse(window.activeSelf);window.SetActive(true);
            var canvas=window.GetComponent<Canvas>();Assert.AreEqual(RenderMode.WorldSpace,canvas.renderMode);canvas.worldCamera=camera;
            var content=window.transform.Find("Content");Assert.IsNotNull(content.Find("Top/backBtn").GetComponent<Button>());
            var input=content.Find("Top/InputField (TMP)").GetComponent<TMP_InputField>();Assert.IsFalse(input.interactable);Assert.IsFalse(input.readOnly);Assert.AreEqual(0,input.characterLimit);Assert.IsNotNull(input.textComponent);Assert.IsNotNull(input.placeholder);
            foreach(var label in window.GetComponentsInChildren<TMP_Text>(true)){Assert.IsNotNull(label.font,label.name);Assert.IsNotNull(label.fontSharedMaterial,label.name);Assert.IsNotNull(label.font.atlasTexture,label.name);}
            foreach(var button in window.GetComponentsInChildren<Button>(true))Assert.AreEqual(0,button.onClick.GetPersistentEventCount(),button.name);
            // Preview the native cash branch; production tab/account routing remains separate work.
            content.Find("Node/GiftRect").gameObject.SetActive(false);content.Find("Top/CashBtn").GetChild(2).gameObject.SetActive(true);content.Find("Top/GiftBtn").GetChild(2).gameObject.SetActive(false);
            var list=window.GetComponentInChildren<RecoveredCashOutList>();var bottom=window.GetComponentInChildren<RecoveredCashOutBottom>();Assert.IsNotNull(list);Assert.IsNotNull(bottom);
            Canvas.ForceUpdateCanvases();var size=((RectTransform)content.Find("Node/Rect")).rect.size;
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Qonrii=new QonriiPoro{Ripg=new List<string>{"default"}},Rgpggm=new RgpggmPoro{Qogt=new List<int>{20000,30000,50000,100000}}});var data=new PlayerData{GreenCount=3500};var player=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Preview must not save"),data);list.Initialize(rules,player,bottom,()=>102,size,1,0);
            var header=window.GetComponent<RecoveredCashOutPaymentHeader>();header.Bind(rules,player);header.RefreshMode(false);
            for(int i=0;i<4;i++)yield return null;Canvas.ForceUpdateCanvases();
            eventHost=new GameObject("Cash window events",typeof(EventSystem));var events=eventHost.GetComponent<EventSystem>();
            foreach(var name in new[]{"PaypalBtn","CashAppBtn","CoinBaseBtn","ZelleBtn"})
            {
                var button=content.Find("Node/Layout/"+name).GetComponent<Button>();var rect=(RectTransform)button.transform;var pointer=new PointerEventData(events){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
                var hits=new List<RaycastResult>();events.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits,name);Assert.AreSame(button,hits[0].gameObject.GetComponentInParent<Button>(),name);
                Assert.IsNotNull(button.GetComponent<Image>().sprite,name);
            }
            list.ItemAt(1).Button.onClick.Invoke();Assert.AreSame(list.ItemAt(1).transform,list.SelectionFrame.parent);StringAssert.EndsWith(RecoveredCurrency.Format(30000,0,0),bottom.DetailText.text);
            content.Find("Node/Layout/CoinBaseBtn").GetComponent<Button>().onClick.Invoke();Assert.AreEqual("Please enter your CoinBase account here.",input.text);StringAssert.EndsWith(RecoveredCurrency.Format(20000,0,0),bottom.DetailText.text);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-window.png"),capture.EncodeToPNG());
        }
        finally{RenderTexture.active=prior;if(host!=null)Object.Destroy(host);if(cameraObject!=null)Object.Destroy(cameraObject);if(eventHost!=null)Object.Destroy(eventHost);if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);}
        yield return null;
    }
}
