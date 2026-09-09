using System.Collections;
using System.IO;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCashOutItemArtTests
{
    [UnityTest]
    public IEnumerator OriginalCardFontsSpritesAndButtonHierarchyRenderInCurrentProject()
    {
        GameObject host=null,cameraObject=null,card=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try
        {
            cameraObject=new GameObject("Cash item capture",typeof(Camera));var camera=cameraObject.GetComponent<Camera>();camera.orthographic=true;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.08f,.12f);
            target=new RenderTexture(1080,600,24);target.Create();camera.targetTexture=target;
            host=new GameObject("Cash item canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
            var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,600);
            card=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutItem"),host.transform,false);Assert.IsNotNull(card);
            var rect=(RectTransform)card.transform;rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.anchoredPosition=Vector2.zero;
            var button=card.GetComponentInChildren<Button>(true);Assert.IsNotNull(button);Assert.AreEqual(0,button.onClick.GetPersistentEventCount());Assert.AreEqual(1,card.transform.childCount);
            foreach(var image in card.GetComponentsInChildren<Image>(true))if(image.gameObject!=button.gameObject)Assert.IsNotNull(image.sprite,image.name);
            Assert.AreEqual(new Vector4(24,0,24,0),button.transform.Find("Progress").GetComponent<Image>().sprite.border);
            Assert.AreEqual(new Vector4(18,0,18,0),button.transform.Find("Progress/Fill").GetComponent<Image>().sprite.border);
            foreach(var label in card.GetComponentsInChildren<TMP_Text>(true)){Assert.IsNotNull(label.font);Assert.AreEqual("Microsoft YaHei",label.font.faceInfo.familyName);Assert.IsNotNull(label.fontSharedMaterial);Assert.IsNotNull(label.font.atlasTexture);}
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{Qogt=new List<int>{20000},Rogk1roil=new List<int>{20},Rimgg1=new List<int>{3600}}});
            var playerData=new PlayerData{GreenCount=3500};var view=card.GetComponent<RecoveredCashOutItem>();
            view.Bind(new RecoveredPlayerProgress(rules,()=>Assert.Fail("View must not save"),playerData),rules,()=>102);
            view.Initialize(0,-1,1,null,0);
            for(int i=0;i<3;i++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture=new Texture2D(1080,600,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,600),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-item-art.png"),capture.EncodeToPNG());
            playerData.PlayerCashOutDatas.Add(new PlayerCashOutData{id=0,type=3,step=0,count=7,time=100});view.Initialize(0,-1,1,null,0);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture.ReadPixels(new Rect(0,0,1080,600),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-item-task-art.png"),capture.EncodeToPNG());
        }
        finally{RenderTexture.active=prior;if(host!=null)Object.Destroy(host);if(cameraObject!=null)Object.Destroy(cameraObject);if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);}
        yield return null;
    }
}
