using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredFreeExitFlowTests
{
    [UnityTest]
    public IEnumerator CollectionOpensActualEndWindowThenContinueLaunchesSeparateReturnCallbacks()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        Scene scene=default;AsyncOperation unload=null;RecoveredFreeReels reels=null;RecoveredFreeExitFlow flow=null;
        Camera camera=null;RenderTexture target=null;Texture2D capture=null;var previous=RenderTexture.active;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            Assert.IsNotNull(game);yield return null;
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();yield return null;
            var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{QoinOmoinrKgiitr=new List<int>{1},RollOmoinrKgiitr=new List<int>{1}}});
            int saves=0;var player=new RecoveredPlayerProgress(rules,()=>saves++,new PlayerData{GreenCount=50}){GameSlotType=RecoveredSlotType.Free,FreeSpinCount=0,TotalFreeSpinWin=125};
            var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
            var spinEntry=new RecoveredFreeSpinEntry(player,result);
            reels=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
            reels.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
            reels.RewardCollect.Bind(result,player,game.Playfield.DownWin,(RectTransform)game.Playfield.DownWin.transform.parent,300);
            flow=Object.Instantiate(Resources.Load<RecoveredFreeExitFlow>("RecoveredUI/FreeExitFlow"),game.transform,false);
            flow.Bind(reels,player,result,spinEntry,new[]{3},()=>12,game.transform,()=>0);
            var events=new List<string>();int complete=0;double launched=0,cover=0,after=0;
            flow.PauseMusicRequested+=()=>{Assert.AreEqual(RecoveredSlotType.Base,player.GameSlotType);events.Add("pause");};
            flow.SoundRequested+=name=>events.Add(name);
            flow.Completed+=()=>{complete++;launched=Time.timeAsDouble;};
            flow.BaseViewResetRequested+=()=>{cover=Time.timeAsDouble;events.Add("view");};
            flow.BaseReelsInitRequested+=()=>events.Add("reels");
            flow.FreeEndFlagClearRequested+=()=>events.Add("flag");
            flow.ChangeMusicRequested+=name=>{after=Time.timeAsDouble;events.Add(name);};
            reels.RewardCollect.Begin();Assert.IsFalse(flow.Window.IsShown);
            for(int i=0;i<20&&!flow.Window.IsShown;i++)yield return null;
            var window=flow.Window;Assert.IsTrue(window.IsShown);Assert.IsNull(flow.Error);Assert.AreEqual(2,saves);
            Assert.AreEqual("",window.TotalText.text);Assert.IsFalse(window.TipText.gameObject.activeSelf);Assert.IsFalse(window.ContinueButton.gameObject.activeSelf);
            Assert.AreEqual(1,window.Artwork.Selected);Assert.AreEqual(Vector3.one,window.transform.Find("Content").localScale);
            Assert.IsTrue(window.TipText.text.Contains("12"));Assert.AreEqual(0,complete);
            Time.timeScale=0;for(int i=0;i<6;i++)yield return null;
            Assert.AreEqual("",window.TotalText.text);Assert.IsFalse(window.ContinueButton.gameObject.activeSelf);
            // Total is read when the delayed count callback executes, not when Show is called.
            player.TotalFreeSpinWin=321.25f;Time.timeScale=1;
            for(int i=0;i<80&&!window.TipText.gameObject.activeSelf;i++)yield return null;
            Assert.AreEqual(0,window.Artwork.Selected);Assert.IsTrue(window.TipText.gameObject.activeSelf);
            player.TotalFreeSpinWin=999; // The active numeric tween keeps its captured end value.
            for(int i=0;i<30&&window.TotalText.text!=RecoveredCurrency.Format(321.25f,0,2);i++)yield return null;
            Assert.AreEqual(RecoveredCurrency.Format(321.25f,0,2),window.TotalText.text);
            Assert.IsFalse(window.ContinueButton.gameObject.activeSelf);Assert.AreEqual(50,player.GreenCount);Assert.AreEqual(2,saves);
            for(int i=0;i<45&&!window.ContinueButton.gameObject.activeSelf;i++)yield return null;
            Assert.IsTrue(window.ContinueButton.gameObject.activeSelf);for(int i=0;i<16;i++)yield return null;
            Assert.AreEqual(Vector3.one,window.ContinueButton.transform.localScale);Assert.AreEqual(0,complete);
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-end-window.png"),capture.EncodeToPNG());
            var button=window.ContinueButton;var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
            Assert.AreEqual(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.IsFalse(window.gameObject.activeSelf,"BaseWindow hides immediately, without PopupWindow scale exit.");
            Assert.AreEqual(1,complete);Assert.IsTrue(flow.Transition.IsPlaying);Assert.AreEqual(0,cover);
            CollectionAssert.AreEqual(new[]{"pause","fsend","count","click","transform"},events);
            for(int i=0;i<150&&after==0;i++)yield return null;
            Assert.IsNull(flow.Error);Assert.That(cover-launched,Is.InRange(.8,.9));Assert.That(after-launched,Is.InRange(3,3.15));
            CollectionAssert.AreEqual(new[]{"pause","fsend","count","click","transform","view","reels","flag","normalBg"},events);
            Assert.IsFalse(flow.Exit.IsWaiting);Assert.AreEqual(50,player.GreenCount);Assert.AreEqual(2,saves);
            // The nonzero branch generates the next result and starts the real Free reels.
            player.GameSlotType=RecoveredSlotType.Free;player.FreeSpinCount=2;
            flow.Check();for(int i=0;i<10&&result.IsGenerating;i++)yield return null;
            Assert.IsNull(flow.Error);Assert.AreEqual(1,player.FreeSpinCount);Assert.IsFalse(result.IsGenerating);
            Assert.IsTrue(reels.Controller.IsRunning);Assert.IsFalse(window.IsShown);Assert.AreEqual(2,complete);
        } finally {
            if(flow!=null)Object.Destroy(flow.gameObject);if(reels!=null)Object.Destroy(reels.gameObject);
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
