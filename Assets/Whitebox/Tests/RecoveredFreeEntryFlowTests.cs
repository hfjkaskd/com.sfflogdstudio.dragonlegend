using System;
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

public sealed class RecoveredFreeEntryFlowTests
{
    [UnityTest]
    public IEnumerator EntryWaitsForWindowThenLaunchesTransitionBeforeCompletingAndAutoDebitsLater()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=UnityEngine.Random.state;
        Scene scene=default;Camera camera=null;RenderTexture target=null;Texture2D capture=null;var previous=RenderTexture.active;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;UnityEngine.Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var item=root.GetComponentInChildren<GameEntry>();if(item!=null)game=item;}
            Assert.IsNotNull(game);for(int i=0;i<200&&game.Playfield==null;i++)yield return null;
            var field=game.Playfield;Assert.IsNotNull(field);
            var flow=Object.Instantiate(Resources.Load<RecoveredFreeEntryFlow>("RecoveredUI/FreeEntryFlow"),game.transform);
            var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");var ids=new List<int>();
            for(int i=0;i<catalog.ModeCount(RecoveredSlotType.Free);i++)ids.Add(catalog.ModeId(RecoveredSlotType.Free,i));
            flow.Bind(field,game.PlayerProgress,game.Rules,game.RewardBranches,game.FreeSpinResult,game.FreeSpinEntry,ids,game.Ads);
            int complete=0,present=0;double launched=0,cover=0,auto=0;Exception failure=null;var order=new List<string>();
            flow.Failed+=e=>failure=e;flow.Completed+=()=>{complete++;launched=Time.timeAsDouble;};
            flow.CashOutTaskRefreshRequested+=(a,b)=>{Assert.AreEqual(5,a);Assert.AreEqual(1,b);order.Add("task");};
            flow.InitShowRequested+=()=>{cover=Time.timeAsDouble;order.Add("view");};
            flow.InitFreeReelsRequested+=()=>order.Add("reels");
            flow.InitFreeSpinTimesRequested+=count=>{Assert.AreEqual(game.PlayerProgress.FreeSpinCount,count);order.Add("times");};
            flow.ChangeMusicRequested+=name=>{Assert.AreEqual("freeBg",name);auto=Time.timeAsDouble;order.Add("music");};
            game.FreeSpinEntry.PresentationRequested+=()=>{present++;order.Add("spin");};
            flow.Begin(0);Assert.AreEqual(1,complete);Assert.IsFalse(flow.IsRunning);Assert.IsFalse(flow.Transition.IsPlaying);
            complete=0;launched=0;field.Reels.ReelAt(0).ApplyBaseColumn(new[]{10,0,0});field.CoinStops.ShowColumn(0);
            double began=Time.timeAsDouble;flow.Begin(3);int count=game.Rules.GetFreeSpins(3);
            Assert.Greater(count,0);Assert.AreEqual(count,game.PlayerProgress.FreeSpinCount);Assert.AreEqual(RecoveredSlotType.Free,game.PlayerProgress.GameSlotType);
            Assert.IsTrue(flow.IsFreeSpinEnd);Assert.AreEqual(count,flow.InitialSpinCount);
            Assert.AreEqual(2,field.Npc.State);Assert.IsTrue(field.Scatters.At(0,0).Player.IsPlaying("idle"));
            Time.timeScale=0;for(int i=0;i<5;i++)yield return null;Assert.IsFalse(flow.Window.gameObject.activeSelf);
            Time.timeScale=1;for(int i=0;i<100&&!flow.Window.gameObject.activeSelf;i++)yield return null;
            Assert.IsTrue(flow.Window.gameObject.activeSelf);Assert.That(Time.timeAsDouble-began,Is.InRange(1.8,1.9));Assert.AreEqual(0,complete);
            for(int i=0;i<16;i++)yield return null;
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            camera.targetTexture=target;yield return null;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-entry-window.png"),capture.EncodeToPNG());
            flow.Window.PlainButton.onClick.Invoke();for(int i=0;i<30&&complete==0;i++)yield return null;
            Assert.IsNull(failure);Assert.AreEqual(1,complete);Assert.IsTrue(flow.Transition.IsPlaying);Assert.AreEqual(0,cover);Assert.AreEqual(0,present);
            Assert.IsFalse(game.FreeSpinResult.IsGenerating);Assert.AreEqual(count,game.PlayerProgress.FreeSpinCount);
            for(int i=0;i<160&&present==0;i++)yield return null;
            Assert.IsNull(failure);Assert.AreEqual(1,present);Assert.AreEqual(count-1,game.PlayerProgress.FreeSpinCount);
            Assert.That(cover-launched,Is.InRange(.8,.9));Assert.That(auto-launched,Is.InRange(3,3.15));
            CollectionAssert.AreEqual(new[]{"task","view","reels","times","music","spin"},order);
            Assert.AreEqual(1,complete);Assert.IsFalse(flow.IsRunning);
            flow.Unbind();Object.Destroy(flow.gameObject);
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
