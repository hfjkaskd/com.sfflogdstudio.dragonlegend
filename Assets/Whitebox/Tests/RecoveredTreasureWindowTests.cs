using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;
using Random=UnityEngine.Random;

public sealed class RecoveredTreasureWindowTests
{
    [UnityTest]
    public IEnumerator NativeWindowShowsEveryCardAndBothClaimsReachActualCashArrival()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;
        Scene scene=default;AsyncOperation unload=null;RecoveredTreasureWindow window=null;GameObject incoming=null;Camera camera=null;RenderTexture texture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);yield return null;
            window=Object.Instantiate(Resources.Load<RecoveredTreasureWindow>("RecoveredUI/TreasureWindow"),entry.transform,false);
            window.Bind(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,entry.transform,false,0);
            incoming=new GameObject("Treasure arrival event argument");
            camera=entry.GetComponent<Canvas>().worldCamera;texture=new RenderTexture(1080,1920,24);camera.targetTexture=texture;
            var infos=entry.Rules.GetCollectInfos();Assert.AreEqual(15,infos.Count);
            foreach(var info in infos){window.Show(info.id,null,null);Assert.IsNotNull(window.CardImage.sprite,"Card ID "+info.id);
                Assert.AreEqual(window.CardImage.sprite.rect.size,window.CardImage.rectTransform.sizeDelta);
                Assert.AreEqual(RecoveredCurrency.Format(info.worth,0,2),window.RewardText.text);}
            var events=new List<string>();window.SoundRequested+=name=>events.Add(name);
            window.MainAnimationRequested+=(a,b)=>{Assert.AreEqual(4,a);Assert.AreEqual(1,b);events.Add("main");};
            window.CollectCardDepartureRequested+=(position,sprite)=>{
                Assert.IsFalse(window.gameObject.activeSelf);Assert.Greater(entry.CashFlight.ActiveCashCount,0);
                Assert.AreEqual(window.EndPosition.position,position);Assert.AreSame(window.CardImage.sprite,sprite);events.Add("card-departure");};
            for(int pass=0;pass<2;pass++) {
                var info=infos[pass==0?0:infos.Count-1];entry.PlayerProgress.SetCollectData(info.id,1);
                int callbacks=0;float paid=-1,balance=entry.PlayerProgress.GreenCount;events.Clear();
                window.Show(info.id,value=>{Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);paid=value;callbacks++;events.Add("caller");},target=>{
                    Assert.AreSame(window.EndPosition,target);Assert.AreEqual(Vector3.zero,target.localPosition);
                    Assert.AreEqual(Vector3.one,window.Content.localScale);Assert.IsFalse(window.Card.gameObject.activeSelf);events.Add("fly-request");});
                CollectionAssert.AreEqual(new[]{"jump","main","fly-request"},events);
                Assert.IsFalse(window.Content.Find("bLACK").gameObject.activeSelf);
                Assert.AreEqual("Only "+RecoveredCurrency.Format(info.worth*entry.Rules.GetCollectClaim(1),0,2),window.PlainText.text);
                incoming.SetActive(true);incoming.transform.localPosition=new Vector3(2,3,4);incoming.transform.localScale=Vector3.one*2;
                window.RefreshCollectCard(incoming);
                Assert.IsFalse(incoming.activeSelf);Assert.AreEqual(Vector3.zero,incoming.transform.localPosition);
                Assert.AreEqual(Vector3.one*.4f,incoming.transform.localScale);Assert.IsTrue(window.Content.Find("bLACK").gameObject.activeSelf);
                Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
                Assert.IsFalse(window.Content.Find("Title").gameObject.activeSelf);Time.timeScale=1;
                for(int i=0;i<25;i++)yield return null;
                Assert.IsTrue(window.Content.Find("Title").gameObject.activeSelf);
                Assert.IsNotNull(window.Content.Find("Title").GetComponent<RecoveredTitleShine>());
                Assert.IsTrue(window.Content.Find("Light").gameObject.activeSelf);
                Assert.IsTrue(window.Content.Find("SkeletonGraphic (ef_caidai)").gameObject.activeSelf);
                for(int i=0;i<35;i++)yield return null;
                Assert.IsTrue(window.CollectTip.gameObject.activeSelf);
                Canvas.ForceUpdateCanvases();
                Assert.AreSame(entry.GetComponent<Canvas>(),window.GetComponent<Canvas>().rootCanvas);
                var screen=camera.WorldToScreenPoint(window.CardImage.transform.position);
                Assert.That(screen.x,Is.InRange(1f,1079f));Assert.That(screen.y,Is.InRange(1f,1919f));
                Assert.Greater(screen.z,camera.nearClipPlane);
                Assert.AreEqual(entry.PlayerProgress.CollectRecords.Count+"/15",window.CollectTip.ProgressText.text);
                Capture(camera,texture,pass==0?"current-treasure-window.png":"current-treasure-window-last-card.png");
                float multiplier=entry.Rules.GetCollectClaim(pass==0?1:0);
                if(pass==0)window.PlainButton.onClick.Invoke();
                else {
                    window.ClaimButton.onClick.Invoke();Assert.IsTrue(entry.Ads.Pending);
                    entry.Ads.Complete(AdOutcome.Failed);Assert.IsFalse(window.Claim.IsClicked);
                    window.ClaimButton.onClick.Invoke();entry.Ads.Complete(AdOutcome.Rewarded);
                }
                float expected=info.worth*multiplier,began=Time.time;
                Assert.AreEqual(expected,window.Claim.Reward);Assert.AreEqual(0,callbacks);
                Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
                Assert.IsTrue(window.gameObject.activeSelf);Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Time.timeScale=1;
                for(int i=0;i<45&&window.gameObject.activeSelf;i++)yield return null;
                Assert.IsFalse(window.gameObject.activeSelf);
                Assert.That(Time.time-began,Is.InRange(expected==info.worth?.299f:.799f,expected==info.worth?.4f:.91f));
                Assert.Contains("card-departure",events);Assert.AreEqual(0,callbacks);
                float deadline=Time.realtimeSinceStartup+5;
                while(callbacks==0&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.AreEqual(1,callbacks);Assert.AreEqual(expected,paid);Assert.AreEqual(balance+expected,entry.PlayerProgress.GreenCount);
                Assert.IsFalse(window.IsRunning);Assert.Less(events.IndexOf("card-departure"),events.IndexOf("caller"));
            }
            window.Bind(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,entry.transform,true,1);
            window.Show(infos[0].id,null,null);incoming.SetActive(true);window.RefreshCollectCard(incoming);
            for(int i=0;i<60;i++)yield return null;
            Assert.IsFalse(window.CollectTip.gameObject.activeSelf);
            Assert.AreEqual(RecoveredCurrency.Format(infos[0].worth,1,2),window.RewardText.text);
        } finally {
            if(camera!=null)camera.targetTexture=null;if(texture!=null)Object.Destroy(texture);if(window!=null)Object.Destroy(window.gameObject);if(incoming!=null)Object.Destroy(incoming);
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Capture(Camera camera,RenderTexture target,string name)
    {
        var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try{Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name),image.EncodeToPNG());}
        finally{RenderTexture.active=previous;Object.Destroy(image);}
    }
}
