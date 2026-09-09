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

public sealed class RecoveredCoreRoundFlowTests
{
    [UnityTest]
    public IEnumerator SpinSettlesThenForcedFreeRoundReturnsThroughActualEndWindow()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            target=new RenderTexture(1080,1920,24);target.Create();
            prepare=(loaded,mode)=>{if(loaded.name!="GameEntry")return;foreach(var root in loaded.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null){camera=entry.GetComponent<Canvas>().worldCamera;camera.targetTexture=target;}}};
            SceneManager.sceneLoaded+=prepare;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var field=game.Playfield;var core=game.CoreRound;int completed=0;core.CoreRoundCompleted+=()=>completed++;
            int visibleAnticipations=0;
            field.Reels.AnticipationVisibilityRequested+=(index,visible)=>{
                Assert.AreEqual(visible,field.ModeView.SpeedEffectAt(index).activeInHierarchy);
                if(visible)visibleAnticipations++;
            };
            var boardShake=field.Wilds.Shake;int baseShakes=0,freeShakes=0;float maximumShakeOffset=0;
            System.Action checkShake=()=>{
                Assert.IsTrue(boardShake.IsShaking,"Real stop event must start the authored board shake.");
                Assert.IsTrue(boardShake.gameObject.activeInHierarchy);
                maximumShakeOffset=Mathf.Max(maximumShakeOffset,Vector2.Distance(boardShake.Target.anchoredPosition,boardShake.OriginalPosition));
            };
            field.Reels.ShakeRequested+=()=>{checkShake();baseShakes++;};
            field.ModeView.FreeReels.Controller.ShakeRequested+=()=>{checkShake();freeShakes++;};
            Assert.IsNotNull(game.CoreAudio);
            Assert.AreEqual("normalBg",game.CoreAudio.Manager.MusicSource.clip.name);
            // Clear channels immediately before each observed real event so an older
            // one-shot cannot hide a missing or incorrectly routed audio binding.
            var audio=game.CoreAudio.Manager;
            game.CoreAudio.Unbind();
            game.SpinEntry.ClickSoundRequested+=audio.StopSound;
            game.SpinEntry.SpinSoundRequested+=audio.StopSound;
            field.Reels.ReelStopSoundRequested+=audio.StopSound;
            field.Reels.SpeedupSoundRequested+=audio.StopSound1;
            field.CoinStops.CoinShowSoundRequested+=audio.StopSound;
            field.CoinStops.CoinRevealSoundRequested+=audio.StopSound;
            field.CoinStops.ExpSoundRequested+=audio.StopSound;
            field.WinFlight.CoinBurstSoundRequested+=audio.StopSound;
            field.ModeView.FreeReels.RewardCollect.Flights.CoinBurstSoundRequested+=audio.StopSound;
            game.CoreAudio.Bind(game);
            int clickSounds=0,startSounds=0;
            game.SpinEntry.ClickSoundRequested+=()=>{if(game.PlayerStore.Data.IsMusic){Assert.IsTrue(audio.SoundSource.isPlaying);clickSounds++;}};
            game.SpinEntry.SpinSoundRequested+=()=>{if(game.PlayerStore.Data.IsMusic){Assert.IsTrue(audio.SoundSource.isPlaying);startSounds++;}};
            int reelSounds=0,speedSounds=0,speedStops=0,coinShows=0,coinReveals=0,lampSounds=0,burstSounds=0;
            field.Reels.ReelStopSoundRequested+=()=>{if(game.PlayerStore.Data.IsMusic){Assert.IsTrue(audio.SoundSource.isPlaying);reelSounds++;}};
            field.Reels.SpeedupSoundRequested+=()=>{if(game.PlayerStore.Data.IsMusic){Assert.IsTrue(audio.Sound1Source.isPlaying);speedSounds++;}};
            field.Reels.SpeedupSoundStopRequested+=()=>{Assert.IsFalse(audio.Sound1Source.isPlaying);speedStops++;};
            field.CoinStops.CoinShowSoundRequested+=()=>{Assert.IsTrue(audio.SoundSource.isPlaying);coinShows++;};
            field.CoinStops.CoinRevealSoundRequested+=()=>{Assert.IsTrue(audio.SoundSource.isPlaying);coinReveals++;};
            field.CoinStops.ExpSoundRequested+=()=>{Assert.IsTrue(audio.SoundSource.isPlaying);lampSounds++;};
            field.WinFlight.CoinBurstSoundRequested+=()=>{Assert.IsTrue(audio.SoundSource.isPlaying);burstSounds++;};
            field.ModeView.FreeReels.RewardCollect.Flights.CoinBurstSoundRequested+=()=>{Assert.IsTrue(audio.SoundSource.isPlaying);burstSounds++;};
            // This seed's real Spin path has no Base coins. Exercise a stopped coin
            // through the production presenter/flight bindings before that path.
            // Only the board cell and reward input are fixture-controlled.
            field.Reels.ReelAt(0).ApplyBaseColumn(new[]{9,0,0});
            field.CoinStops.ShowColumn(0);
            field.CoinStops.PlayRewardReveal(0,0,100,1);
            for(int frame=0;frame<100&&(lampSounds==0||burstSounds==0);frame++)yield return null;
            Assert.AreEqual(1,coinShows);Assert.AreEqual(1,coinReveals);
            Assert.AreEqual(1,lampSounds);Assert.AreEqual(1,burstSounds);
            int spins=game.PlayerProgress.SpinCount;
            ClickVisible(field.SpinButton.Button,game);
            string afterAccepted=JsonUtility.ToJson(game.PlayerStore.Data);
            string savedAfterAccepted=PlayerPrefs.GetString(key);
            var generated=new int[15];
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)generated[col*3+row]=game.SpinResult.Board.GetSymbol(col,row);
            for(int click=0;click<20;click++)ClickVisible(field.SpinButton.Button,game);
            Assert.AreEqual(21,clickSounds);Assert.AreEqual(1,startSounds);
            Assert.AreEqual(afterAccepted,JsonUtility.ToJson(game.PlayerStore.Data),"Busy clicks cannot mutate the player record.");
            Assert.AreEqual(savedAfterAccepted,PlayerPrefs.GetString(key),"Busy clicks cannot write another save.");
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)Assert.AreEqual(generated[col*3+row],game.SpinResult.Board.GetSymbol(col,row));
            Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.IsTrue(field.IsBusy);
            for(int frame=0;frame<1800&&completed==0;frame++){Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.IsNull(core.Error);Assert.AreEqual(1,completed);Assert.IsFalse(field.IsBusy);Assert.IsFalse(field.AwaitingRewards);
            int seed=-1;var beforeSearch=Random.state;
            for(int candidate=0;candidate<10000;candidate++){Random.InitState(candidate);game.Rules.GetFreeCoinAmount();if(game.Rules.GetFreeBallAmount()==0){seed=candidate;break;}}
            Random.state=beforeSearch;Assert.GreaterOrEqual(seed,0,"Fixture needs one Free result without a ball branch.");
            // Keep this core-loop fixture short. Other suites cover all four ball routes.
            core.Entry.ChangeMusicRequested+=name=>{game.PlayerProgress.FreeSpinCount=1;Random.InitState(seed);};
            var cashTask=new PlayerCashOutData{id=0,type=99,step=5,count=7,isCashout=true};
            game.PlayerStore.Data.PlayerCashOutDatas.Add(cashTask);
            game.SpinResult.ForceFreeSpin=true;spins=game.PlayerProgress.SpinCount;field.SpinButton.Button.onClick.Invoke();
            field.SpinRecovery.MoreSpinButton.onClick.Invoke();
            Assert.IsTrue(core.MoreSpins.gameObject.activeSelf);
            Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);
            for(int frame=0;frame<1800&&!core.Entry.Window.gameObject.activeSelf;frame++){Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);Assert.IsTrue(field.IsBusy);Assert.Greater(core.Entry.InitialSpinCount,0);
            Assert.Greater(core.Entry.Window.GetComponent<Canvas>().sortingOrder,core.MoreSpins.GetComponent<Canvas>().sortingOrder,"Free intro must open above an existing Popup window.");
            Assert.AreEqual(8,cashTask.count,"One actual Free entry must reach the main cash-task receiver exactly once, before its intro closes.");
            var persisted=JsonUtility.FromJson<PlayerData>(PlayerPrefs.GetString(key));
            Assert.AreEqual(8,persisted.PlayerCashOutDatas[0].count);
            for(int i=0;i<15;i++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-start-popup-depth.png"),capture.EncodeToPNG());
            ClickVisible(core.Entry.Window.PlainButton,game);
            for(int frame=0;frame<1800&&!core.Exit.Window.IsShown;frame++)yield return null;
            Assert.IsNull(core.Error);Assert.IsNull(core.Exit.Error);Assert.IsNull(field.ModeView.FreeReels.Controller.Error);
            Assert.IsNull(field.ModeView.FreeReels.CoinScan.Error);Assert.IsNull(field.ModeView.FreeReels.BallScan.Error);Assert.IsNull(field.ModeView.FreeReels.RewardCollect.Error);
            Assert.IsTrue(core.Exit.Window.IsShown);Assert.AreEqual(0,game.PlayerProgress.FreeSpinCount);Assert.IsTrue(field.IsBusy);Assert.AreEqual(1,completed);
            Assert.Greater(core.Exit.Window.GetComponent<Canvas>().sortingOrder,core.MoreSpins.GetComponent<Canvas>().sortingOrder,"Free total must open above the still-visible Popup window.");
            Assert.AreEqual("freeBg",game.CoreAudio.Manager.RequestedMusic,"Actual Free entry must reach the audio consumer.");
            Assert.Greater(reelSounds,0);Assert.Greater(speedSounds,0);Assert.AreEqual(speedSounds,speedStops);
            Assert.Greater(coinShows,0);Assert.Greater(coinReveals,0);Assert.Greater(lampSounds,0);Assert.Greater(burstSounds,0);
            for(int i=0;i<100&&!core.Exit.Window.ContinueButton.gameObject.activeInHierarchy;i++)yield return null;
            Assert.IsTrue(core.Exit.Window.ContinueButton.gameObject.activeInHierarchy);
            for(int i=0;i<10;i++)yield return null;
            // A dirty persisted setting isolates the final save from earlier reward setters.
            game.PlayerStore.Data.IsMusic=!game.PlayerStore.Data.IsMusic;
            Assert.AreNotEqual(JsonUtility.ToJson(game.PlayerStore.Data),PlayerPrefs.GetString(key));
            ClickVisible(core.Exit.Window.ContinueButton,game);
            Assert.IsTrue(field.IsBusy,"End-window close must not unlock Spin before return transition completes.");
            for(int frame=0;frame<150&&completed==1;frame++){RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.AreEqual(2,completed);Assert.IsFalse(core.Entry.IsFreeSpinEnd);Assert.IsFalse(field.IsBusy);
            Assert.AreEqual(10,baseShakes);Assert.AreEqual(5,freeShakes);Assert.Greater(maximumShakeOffset,.001f);
            Assert.Greater(visibleAnticipations,0);
            for(int i=0;i<5;i++)Assert.IsFalse(field.ModeView.SpeedEffectAt(i).activeSelf);
            Assert.IsFalse(boardShake.IsShaking);Assert.AreEqual(boardShake.OriginalPosition,boardShake.Target.anchoredPosition);
            Assert.AreEqual("normalBg",game.CoreAudio.Manager.RequestedMusic,"Return must update requested music even after the fixture mutes it.");
            Assert.AreEqual(JsonUtility.ToJson(game.PlayerStore.Data),PlayerPrefs.GetString(key),"Return completion must persist the current player record.");
            Assert.AreEqual(RecoveredSlotType.Base,game.PlayerProgress.GameSlotType);Assert.IsTrue(field.ModeView.BaseRoll.activeSelf);Assert.IsFalse(field.ModeView.FreeRoll.activeSelf);
            spins=game.PlayerProgress.SpinCount;field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.IsTrue(field.Reels.IsRunning);
        } finally {
            if(prepare!=null)SceneManager.sceneLoaded-=prepare;
            RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button){if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static void ClickVisible(Button button,GameEntry game)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(game.GetComponent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
        Assert.AreEqual(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
}
