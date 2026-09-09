using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredMoreSpinIntegrationTests
{
    [UnityTest]
    public IEnumerator EmptySpinOpensClaimThenResumesAndLimitTipUsesScaledDelay()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);game.PlayerProgress.SetSpinCount(0);var window=game.CoreRound.MoreSpins;
            var openingSounds=new System.Collections.Generic.List<string>();
            game.SpinEntry.ClickSoundRequested+=()=>openingSounds.Add("click");
            game.Playfield.SpinRecovery.SoundRequested+=name=>openingSounds.Add(name);
            window.SoundRequested+=name=>openingSounds.Add(name);
            game.Playfield.SpinButton.Button.onClick.Invoke();Assert.IsTrue(window.gameObject.activeSelf);
            CollectionAssert.AreEqual(new[]{"click","remind"},openingSounds,"Empty Spin must not also invoke the plus-button click path.");
            Assert.IsFalse(game.Playfield.IsBusy);Assert.IsFalse(game.Playfield.Reels.IsRunning);Assert.AreEqual(0,game.PlayerProgress.SpinCount);
            for(int frame=0;frame<10;frame++)yield return null;window.ClaimButton.onClick.Invoke();
            Assert.IsTrue(game.Ads.Pending);Assert.AreEqual("extraspin",game.Ads.Placement);game.Ads.Complete(AdOutcome.Failed);
            Assert.IsFalse(window.IsClicked);Assert.AreEqual(0,game.PlayerProgress.SpinCount);
            window.ClaimButton.onClick.Invoke();game.Ads.Complete(AdOutcome.Rewarded);
            int granted=Mathf.Min(game.Rules.GetAddSpins(),game.Rules.GetMaxSpinCount());Assert.AreEqual(granted,game.PlayerProgress.SpinCount);
            for(int frame=0;frame<10;frame++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);
            game.Playfield.SpinButton.Button.onClick.Invoke();Assert.AreEqual(granted-1,game.PlayerProgress.SpinCount);Assert.IsTrue(game.Playfield.Reels.IsRunning);
            var previous=game.CoreRound;game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==previous)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);Assert.AreNotEqual("default",game.Rules.GetConfigType(),"US snapshot exercises the native non-default cap.");
            game.PlayerProgress.SetSpinCount(0);game.PlayerStore.Data.LimitSpinCount=game.Rules.GetLimitMaxSpinCount();
            window=game.CoreRound.MoreSpins;var tip=game.CoreRound.Tips;
            game.Playfield.SpinButton.Button.onClick.Invoke();for(int frame=0;frame<10;frame++)yield return null;
            window.ClaimButton.onClick.Invoke();Assert.IsFalse(game.Ads.Pending);Assert.IsTrue(tip.gameObject.activeSelf);
            Assert.AreEqual("The ad isn't ready yet, please wait.",tip.Label.text);Assert.IsTrue(window.IsClicked);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-more-spin-limit.png"),capture.EncodeToPNG());
            for(int frame=0;frame<20;frame++)yield return null;Assert.IsTrue(tip.gameObject.activeSelf);
            Time.timeScale=0;for(int frame=0;frame<40;frame++)yield return null;Assert.IsTrue(tip.gameObject.activeSelf);
            Time.timeScale=1;for(int frame=0;frame<25;frame++)yield return null;Assert.IsFalse(tip.gameObject.activeSelf);
            Assert.IsTrue(window.IsClicked);window.CloseButton.onClick.Invoke();Assert.IsTrue(window.gameObject.activeSelf);
            previous=game.CoreRound;game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==previous)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);game.PlayerStore.Data.LimitSpinCount=0;game.PlayerProgress.SetSpinCount(0);
            game.Playfield.SpinButton.Button.onClick.Invoke();for(int frame=0;frame<10;frame++)yield return null;
            game.CoreRound.MoreSpins.ClaimButton.onClick.Invoke();var oldAds=game.Ads;var oldPlayer=game.PlayerProgress;Assert.IsTrue(oldAds.Pending);
            game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();oldAds.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(0,oldPlayer.SpinCount,"GM teardown must reject old ad success.");
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
