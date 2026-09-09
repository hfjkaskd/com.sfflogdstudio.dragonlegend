using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public sealed class RecoveredGiftListTests
{
    [UnityTest]
    public IEnumerator GiftTabLazilyCreatesSourceCardAndReusesItWithRawCollectionCount()
    {
        var window=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/CashOutWindow"));
        var cameraHost=new GameObject("Gift capture camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        var target=new RenderTexture(1080,1920,24);var prior=RenderTexture.active;Texture2D image=null;
        try {
            var config=new GoldenDragonAutoGenConfig{Rgpggm=new RgpggmPoro{Qogt=new List<int>{100}},Qonrii=new QonriiPoro{Ripg=new List<string>{"default"}},Qollgqr=new QollgqrPoro{
                Ip=new List<int>{1,2},Lgtgl=new List<int>{1,1},Ronpom=new List<int>{1,1},Korrt=new List<int>{1,1},QollgqrRgkorp=new List<int>{100}}};
            var rules=new RecoveredGameplayRules(config);var data=new PlayerData();var player=new RecoveredPlayerProgress(rules,()=>Assert.Fail("Gift display must not save"),data);
            var mode=window.GetComponent<RecoveredCashOutModeView>();var list=window.GetComponentInChildren<RecoveredGiftList>(true);
            var cashList=window.GetComponentInChildren<RecoveredCashOutList>(true);cashList.Initialize(rules,player,window.GetComponentInChildren<RecoveredCashOutBottom>(true),()=>100,new Vector2(1080,620),1,0);
            var canvas=window.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=1;camera.targetTexture=target;
            mode.Bind(rules,player);window.SetActive(true);mode.ResetForShow();yield return null;
            Assert.IsNull(list.Item);mode.GiftButton.onClick.Invoke();yield return null;yield return null;
            var item=list.Item;Assert.IsNotNull(item);Assert.AreEqual("Amazon",item.NameText.text);Assert.AreEqual("Amazon",item.AlternateNameText.text);
            Assert.AreEqual("0/2",item.ProgressText.text);Assert.AreEqual(0,item.Fill.sizeDelta.x);Assert.AreEqual(rules.GetCollectReward(0),item.RewardText.text);
            Assert.AreSame(item.transform,list.SelectionFrame.parent);Assert.AreEqual(Vector3.zero,list.SelectionFrame.localPosition);
            Assert.AreSame(cashList.SelectionFrame,list.SelectionFrame);
            Assert.AreEqual(new Vector3(540,-155,0),item.transform.localPosition);
            mode.CashButton.onClick.Invoke();yield return null;Assert.AreSame(cashList.ItemAt(0).transform,list.SelectionFrame.parent);
            data.PlayerCollectDatas.Add(new PlayerCollectData{id=1,count=0,isRecieve=true});data.PlayerCollectDatas.Add(new PlayerCollectData{id=1,count=99});data.PlayerCollectDatas.Add(new PlayerCollectData{id=2,count=-1});
            mode.GiftButton.onClick.Invoke();yield return null;
            Assert.AreSame(item,list.Item);Assert.AreEqual("3/2",item.ProgressText.text);Assert.AreEqual(1072.5f,item.Fill.sizeDelta.x);
            data.PlayerCollectDatas.RemoveAt(2);data.PlayerCollectDatas.RemoveAt(1);mode.Refresh();yield return null;
            Assert.AreSame(item,list.Item);Assert.AreEqual("1/2",item.ProgressText.text);Assert.AreEqual(357.5f,item.Fill.sizeDelta.x);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;image=new Texture2D(1080,1920,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1080,1920),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-gift-list.png"),image.EncodeToPNG());
        }finally{RenderTexture.active=prior;camera.targetTexture=null;Object.Destroy(window);Object.Destroy(cameraHost);if(image!=null)Object.Destroy(image);target.Release();Object.Destroy(target);}
        yield return null;
    }
}
