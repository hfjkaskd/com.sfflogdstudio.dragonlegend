using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCoreBallBranchesTests
{
    [UnityTest]
    public IEnumerator ActualFreeBallRoutesAllFourGamesAndReturnsTheirRewardsToSettlement()=>Run(false);

    [UnityTest]
    public IEnumerator AllFourBallGamesRetryFailedRewardAdsBeforeReturningToSettlement()=>Run(true);

    private IEnumerator Run(bool advertised)
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null;UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            target=new RenderTexture(1080,1920,24);target.Create();
            prepare=(loaded,mode)=>{if(loaded.name!="GameEntry")return;foreach(var root in loaded.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null){camera=entry.GetComponent<Canvas>().worldCamera;camera.targetTexture=target;}}};
            SceneManager.sceneLoaded+=prepare;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);
            var core=game.CoreRound;var reels=game.Playfield.ModeView.FreeReels;
            var destination=game.Playfield.BallDestination;var board=(RectTransform)game.Playfield.transform.Find("QiPan");
            Assert.AreSame(board,destination.parent);Assert.IsNull(core.transform.Find("LongzhuPos"));
            Assert.AreEqual(new Vector2(-1.6201172f,100),destination.anchoredPosition);Assert.AreEqual(Vector2.one*.5f,destination.anchorMin);
            Canvas.ForceUpdateCanvases();Vector3 beforeTarget=destination.position,beforeBoard=board.position;
            var adapt=game.Playfield.GetComponent<RecoveredScreenAdapt>();adapt.Apply(1080,1920,new Rect(24,60,1032,1740));Canvas.ForceUpdateCanvases();
            Assert.AreNotEqual(beforeTarget,destination.position);Assert.Less(Vector3.Distance(destination.position-beforeTarget,board.position-beforeBoard),.0001f);
            Vector2 boardPosition=board.anchoredPosition;Vector3 targetPosition=destination.position;board.anchoredPosition+=new Vector2(13,-7);Canvas.ForceUpdateCanvases();
            Assert.AreNotEqual(targetPosition,destination.position,"The native target follows board shake.");board.anchoredPosition=boardPosition;adapt.AdaptScreen();
            var slot=core.GetComponentInChildren<RecoveredFreeSlotGame>(true);
            var wheel=core.GetComponentInChildren<RecoveredFreeWheelGame>(true);
            var treasure=core.GetComponentInChildren<RecoveredFreeTreasureGame>(true);
            var lucky=core.GetComponentInChildren<RecoveredFreeLuckyGame>(true);
            int generationSeed=-1;
            for(int seed=0;seed<10000;seed++) {
                Random.InitState(seed);
                if(game.Rules.GetFreeCoinAmount()==0&&game.Rules.GetFreeBallAmount()==1){generationSeed=seed;break;}
            }
            Assert.GreaterOrEqual(generationSeed,0);
            // Test-only duration and RNG selection; all production events and consumers stay bound.
            core.Entry.ChangeMusicRequested+=name=>{game.PlayerProgress.FreeSpinCount=1;Random.InitState(generationSeed);};
            int branch=0,routed=0,finished=0;
            game.BonusFlow.Completed+=()=>{
                if(game.PlayerProgress.GameSlotType!=RecoveredSlotType.Free||!reels.BallScan.IsRunning)return;
                RecoveredFreeBall ball=null;
                for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                    var value=reels.Specials.CurrentStoppedBall(reels.At(col,row));if(value!=null){Assert.IsNull(ball);ball=value;}
                }
                Assert.IsNotNull(ball);int selected=-1;
                for(int seed=0;seed<100000;seed++) {
                    Random.InitState(seed);
                    if((int)game.Rules.GetFreeReward(ball.BallType)==branch){selected=seed;break;}
                }
                Assert.GreaterOrEqual(selected,0,"Requested branch must exist in the current profile weights.");
                Random.InitState(selected);routed++;
            };
            core.CoreRoundCompleted+=()=>finished++;
            Random.InitState(71);game.Playfield.SpinButton.Button.onClick.Invoke();
            for(int frame=0;frame<1800&&finished==0;frame++) {
                Claim(game.Playfield.BigWinPopup.PlainButton);Claim(game.Playfield.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;
            }
            Assert.AreEqual(1,finished);Assert.IsFalse(game.Playfield.IsBusy);
            for(branch=0;branch<4;branch++) {
                game.SpinResult.ForceFreeSpin=true;int before=game.PlayerProgress.SpinCount;
                game.Playfield.SpinButton.Button.onClick.Invoke();game.Playfield.SpinButton.Button.onClick.Invoke();
                game.Playfield.SpinRecovery.MoreSpinButton.onClick.Invoke();
                Assert.AreEqual(before-1,game.PlayerProgress.SpinCount);
                for(int frame=0;frame<1800&&!core.Entry.Window.gameObject.activeSelf;frame++) {
                    Claim(game.Playfield.BigWinPopup.PlainButton);Claim(game.Playfield.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;
                }
                Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);for(int frame=0;frame<20;frame++)yield return null;
                // BigWin AfterHide resumes the main flow before its independent cash flight.
                for(int frame=0;frame<200&&game.CashFlight.ActiveCashCount>0;frame++)yield return null;
                Assert.AreEqual(0,game.CashFlight.ActiveCashCount);
                float balance=game.PlayerProgress.GreenCount;
                core.Entry.Window.PlainButton.onClick.Invoke();
                bool seen=false,captured=false;int claims=0,adStage=0;float expectedAdReward=0;double visibleAt=-1;
                for(int frame=0;frame<1800&&!core.Exit.Window.IsShown;frame++) {
                    seen|=branch==0?slot.IsRunning:branch==1?wheel.IsRunning:branch==2?treasure.IsRunning:lucky.IsRunning;
                    Above(slot.Window.Window,core.MoreSpins.gameObject);Above(wheel.Window.Window,core.MoreSpins.gameObject);
                    Above(treasure.Window.gameObject,core.MoreSpins.gameObject);Above(lucky.Popup.gameObject,core.MoreSpins.gameObject);
                    Above(slot.Window.Popup.gameObject,core.MoreSpins.gameObject);Above(wheel.Window.CashPopup.gameObject,core.MoreSpins.gameObject);Above(wheel.Window.JackpotPopup.gameObject,core.MoreSpins.gameObject);
                    Above(slot.Window.Popup.gameObject,slot.Window.Window);Above(wheel.Window.CashPopup.gameObject,wheel.Window.Window);Above(wheel.Window.JackpotPopup.gameObject,wheel.Window.Window);
                    if(!captured) {
                        bool ready=slot.Window.Popup.PlainButton.gameObject.activeInHierarchy||wheel.Window.CashPopup.PlainButton.gameObject.activeInHierarchy||
                            wheel.Window.JackpotPopup.PlainButton.gameObject.activeInHierarchy||treasure.Window.PlainButton.gameObject.activeInHierarchy||lucky.Popup.PlainButton.gameObject.activeInHierarchy;
                        if(ready&&visibleAt<0)visibleAt=Time.timeAsDouble;
                        if(visibleAt<0||Time.timeAsDouble-visibleAt<.6){yield return null;continue;}
                        Capture(camera,target,"current-core-ball-"+branch+(advertised?"-advertised":"-plain")+".png");captured=true;
                    }
                    if(!advertised) {
                        claims+=ClickVisible(slot.Window.Popup.PlainButton,camera)+ClickVisible(wheel.Window.CashPopup.PlainButton,camera);
                        claims+=ClickVisible(wheel.Window.JackpotPopup.PlainButton,camera)+ClickVisible(treasure.Window.PlainButton,camera)+ClickVisible(lucky.Popup.PlainButton,camera);
                    } else if(adStage==0||adStage==2) {
                        int clicked=ClickVisible(slot.Window.Popup.ClaimButton,camera)+ClickVisible(wheel.Window.CashPopup.ClaimButton,camera)+
                            ClickVisible(wheel.Window.JackpotPopup.ClaimButton,camera)+ClickVisible(treasure.Window.ClaimButton,camera)+ClickVisible(lucky.Popup.ClaimButton,camera);
                        if(clicked>0) {
                            Assert.AreEqual(1,clicked);claims+=clicked;Assert.IsTrue(game.Ads.Pending);
                            Assert.AreEqual(branch==2?"treasure":wheel.Window.JackpotPopup.gameObject.activeInHierarchy?"jackpot":"lucky",game.Ads.Placement);
                            if(adStage==0) {
                                if(branch==2) {
                                    bool found=false;
                                    foreach(var info in game.Rules.GetCollectInfos())if(info.id==treasure.SelectedId){expectedAdReward=(float)info.worth*treasure.Window.Claim.AdvertisedMultiplier;found=true;break;}
                                    Assert.IsTrue(found);
                                } else if(branch==1&&wheel.Window.JackpotPopup.gameObject.activeInHierarchy) {
                                    var claim=wheel.Window.JackpotPopup.Claim;expectedAdReward=claim.OriginalReward*claim.AdvertisedMultiplier;
                                } else {
                                    var claim=branch==0?slot.Window.Popup.Claim:branch==1?wheel.Window.CashPopup.Claim:lucky.Popup.Claim;
                                    expectedAdReward=claim.OriginalReward*claim.AdvertisedMultiplier;
                                }
                            }
                            Assert.AreEqual(balance,game.PlayerProgress.GreenCount);Assert.IsTrue(reels.BallScan.IsRunning);adStage++;
                        }
                    } else if(adStage==1) {
                        if(ClickVisible(game.AdControls.FailureButton,camera)>0) {
                            Assert.IsFalse(game.Ads.Pending);Assert.AreEqual(balance,game.PlayerProgress.GreenCount);
                            Assert.IsTrue(reels.BallScan.IsRunning);Assert.AreEqual(0,reels.CoinScan.Rewards.Count);adStage=2;
                        }
                    } else if(adStage==3) {
                        Assert.AreEqual(balance,game.PlayerProgress.GreenCount);Assert.IsTrue(reels.BallScan.IsRunning);
                        if(ClickVisible(game.AdControls.RewardButton,camera)>0){Assert.IsFalse(game.Ads.Pending);adStage=4;}
                    }
                    yield return null;
                }
                Assert.IsNull(core.Error);Assert.IsNull(reels.Controller.Error);Assert.IsNull(reels.BallScan.Error);Assert.IsNull(reels.RewardCollect.Error);
                Assert.IsNull(slot.Error);Assert.IsNull(wheel.Error);Assert.IsNull(lucky.Error);
                Assert.IsTrue(seen,"Actual requested game did not start: "+branch);
                Assert.Greater(claims,0,"Each branch must complete through a real raycasted claim.");
                if(advertised){Assert.AreEqual(4,adStage);Assert.AreEqual(2,claims);}
                Assert.AreEqual(branch+1,routed);Assert.IsTrue(core.Exit.Window.IsShown,"Free session did not reach its end window: "+branch);
                Assert.AreEqual(1,reels.CoinScan.Rewards.Count);float reward=0;foreach(var value in reels.CoinScan.Rewards.Values)reward+=value;
                if(advertised)Assert.AreEqual(expectedAdReward,reward,"Paid ball reward must match the original offer and advertised multiplier.");
                Assert.AreEqual(reward,reels.RewardCollect.FreeReward);Assert.AreEqual(0,reels.RewardCollect.CoinReward);
                Assert.AreEqual(reward,game.PlayerProgress.TotalFreeSpinWin);Assert.AreEqual(0,game.PlayerProgress.FreeSpinCount);
                Assert.AreEqual(balance+reward,game.PlayerProgress.GreenCount,"Ball payout must be credited exactly once.");
                Assert.IsTrue(game.Playfield.IsBusy);Assert.AreEqual(branch+1,finished);
                for(int frame=0;frame<120&&!core.Exit.Window.ContinueButton.gameObject.activeInHierarchy;frame++)yield return null;
                Assert.IsTrue(core.Exit.Window.ContinueButton.gameObject.activeInHierarchy);core.Exit.Window.ContinueButton.onClick.Invoke();
                Assert.IsTrue(game.Playfield.IsBusy);
                for(int frame=0;frame<150&&finished<=branch+1;frame++){RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
                Assert.AreEqual(branch+2,finished);Assert.AreEqual(RecoveredSlotType.Base,game.PlayerProgress.GameSlotType);
                Assert.IsFalse(game.Playfield.IsBusy);
            }
            int spins=game.PlayerProgress.SpinCount;game.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.IsTrue(game.Playfield.Reels.IsRunning);
        } finally {
            if(prepare!=null)SceneManager.sceneLoaded-=prepare;
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button)
    {if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static void Capture(Camera camera,RenderTexture target,string name)
    {
        Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try {
            RenderTexture.active=target;image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/",name),image.EncodeToPNG());
        } finally {RenderTexture.active=previous;Object.Destroy(image);}
    }
    private static void Above(GameObject shown,GameObject behind)
    {
        if(!shown.activeInHierarchy||!behind.activeInHierarchy)return;
        Assert.Greater(shown.GetComponent<Canvas>().sortingOrder,behind.GetComponent<Canvas>().sortingOrder,shown.name+" must open above "+behind.name);
    }
    private static int ClickVisible(Button button,Camera camera)
    {
        if(!button.gameObject.activeInHierarchy||!button.IsInteractable())return 0;
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        // Wait for the authored entrance/reveal animation to expose the button.
        if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=button.gameObject)return 0;
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);return 1;
    }
}
