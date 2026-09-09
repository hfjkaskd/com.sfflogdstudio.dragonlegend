using System;
using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Random=UnityEngine.Random;

public sealed class RecoveredSpinRecoveryIntegrationTests
{
    [UnityTest]
    public IEnumerator ActualCountBarOpensMoreAndDefaultSpinStartsRecoveryThenGmCancelsIt()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        LaunchProfile testProfile=null;string fixturePath=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var view=game.Playfield.SpinRecovery;
            Assert.IsFalse(view.Recovery.IsRunning);Assert.AreEqual(60,view.Label.fontSize);
            Assert.AreEqual("<gradient=\"spin\">SPIN "+game.PlayerProgress.SpinCount+"</gradient>",view.Label.text);
            var sounds=new System.Collections.Generic.List<string>();
            view.SoundRequested+=name=>sounds.Add(name);game.CoreRound.MoreSpins.SoundRequested+=name=>sounds.Add(name);
            game.CoreAudio.Manager.StopSound();
            view.MoreSpinButton.onClick.Invoke();Assert.IsTrue(game.CoreRound.MoreSpins.gameObject.activeSelf);
            game.CoreAudio.Manager.StopSound();view.MoreSpinButton.onClick.Invoke();
            Assert.IsTrue(game.CoreAudio.Manager.SoundSource.isPlaying,"Click while already open must sound without another popup remind masking it.");
            CollectionAssert.AreEqual(new[]{"click","remind","click"},sounds);
            game.PlayerStore.Data.IsMusic=false;game.CoreAudio.Manager.StopSound();view.MoreSpinButton.onClick.Invoke();
            Assert.IsFalse(game.CoreAudio.Manager.SoundSource.isPlaying);game.PlayerStore.Data.IsMusic=true;
            game.CoreRound.MoreSpins.CloseButton.onClick.Invoke();for(int f=0;f<10;f++)yield return null;
            game.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.IsTrue(game.Playfield.IsBusy);int spinningCount=game.PlayerProgress.SpinCount;
            view.MoreSpinButton.onClick.Invoke();Assert.IsTrue(game.CoreRound.MoreSpins.gameObject.activeSelf,"Native plus entry has no busy guard.");
            Assert.AreEqual(spinningCount,game.PlayerProgress.SpinCount);
            game.CoreRound.MoreSpins.CloseButton.onClick.Invoke();
            Assert.AreEqual("<gradient=\"spin\">SPIN "+game.PlayerProgress.SpinCount+"</gradient>",view.Label.text);
            // Both shipped comparison snapshots are organic. Exercise the source default branch
            // through the actual loader using an explicitly synthetic, temporary test snapshot.
            var config=JsonUtility.FromJson<GoldenDragonAutoGenConfig>(File.ReadAllText(Path.Combine(Application.streamingAssetsPath,game.CurrentProfile.snapshotPath)));
            config.Qonrii.Ripg[0]="default";
            string fixtureName="spin-recovery-test-"+Guid.NewGuid().ToString("N")+".json";
            fixturePath=Path.Combine(Application.streamingAssetsPath,fixtureName);File.WriteAllText(fixturePath,JsonUtility.ToJson(config));
            testProfile=UnityEngine.Object.Instantiate(game.CurrentProfile);testProfile.profileId="SyntheticDefaultRecoveryTest";testProfile.snapshotPath=fixtureName;
            var oldCore=game.CoreRound;var oldView=view;int oldSoundCount=sounds.Count;game.Select(testProfile);
            oldView.MoreSpinButton.onClick.Invoke();Assert.AreEqual(oldSoundCount,sounds.Count,"GM detaches discarded plus-button events.");
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==oldCore)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreEqual("default",game.Rules.GetConfigType());view=game.Playfield.SpinRecovery;
            game.PlayerProgress.SetSpinCount(game.Rules.GetMaxSpinCount());
            game.Playfield.SpinButton.Button.onClick.Invoke();Assert.IsTrue(view.Recovery.IsRunning);
            Assert.AreEqual(game.Rules.GetSpinCD(game.PlayerProgress.Level),view.Recovery.RemainingSeconds);
            int count=game.PlayerProgress.SpinCount;
            Time.timeScale=0;int remaining=view.Recovery.RemainingSeconds;for(int f=0;f<30;f++)yield return null;
            Assert.AreEqual(remaining,view.Recovery.RemainingSeconds);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-spin-recovery.png"),capture.EncodeToPNG());
            // Shorten this timer through the production model, then verify actual Update drives it.
            view.Recovery.Begin(1);Time.timeScale=1;for(int f=0;f<25;f++)yield return null;
            Assert.AreEqual(count+1,game.PlayerProgress.SpinCount);Assert.IsFalse(view.Recovery.IsRunning);
            Assert.AreEqual("<gradient=\"spin\">SPIN "+game.PlayerProgress.SpinCount+"</gradient>",view.Label.text);
            game.PlayerProgress.SetSpinCount(count);view.Recovery.Begin(10);var oldRecovery=view.Recovery;var oldPlayer=game.PlayerProgress;
            game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();Assert.IsFalse(oldRecovery.IsRunning);
            oldRecovery.Advance(100,unchecked((int)DateTimeOffset.UtcNow.ToUnixTimeSeconds()));Assert.AreEqual(count,oldPlayer.SpinCount);
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)UnityEngine.Object.Destroy(target);if(capture!=null)UnityEngine.Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
            if(fixturePath!=null&&File.Exists(fixturePath))File.Delete(fixturePath);
            if(testProfile!=null)UnityEngine.Object.Destroy(testProfile);
        }
        if(unload!=null)yield return unload;
    }
}
