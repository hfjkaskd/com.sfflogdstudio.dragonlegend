using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RecoveredBigWinFreeChainTests
{
    [UnityTest]
    public IEnumerator OnePaidSpinRetriesBigWinThenFreeIntroAndReturnsToBase()=>Run(false);

    [UnityTest]
    public IEnumerator OnePaidSpinSettlesBigWinAndBonusBeforeItsFreeIntro()=>Run(true);

    [UnityTest]
    public IEnumerator OnePaidSpinCompletesEveryAwardedFreeRoundAfterBigWinAndBonus()=>Run(true,true);

    [UnityTest]
    public IEnumerator ActualBaseCoinCompletesCollectionThenBonusAndAllFreeRounds()=>Run(true,true,true);

    private IEnumerator Run(bool includeBonus,bool fullFree=false,bool collectBonus=false)
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key);var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        Scene scene=default;AsyncOperation unload=null;Camera camera=null;RenderTexture target=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)game=value;}
            Assert.IsNotNull(game);float deadline=Time.realtimeSinceStartup+10;
            while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;var field=game.Playfield;var player=game.PlayerProgress;
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);
            target.Create();camera.targetTexture=target;yield return null;Canvas.ForceUpdateCanvases();
            core.FirstSpinGuide.Hide();game.PlayerStore.Data.GuideStep=3;
            if(collectBonus) {
                for(int col=0;col<5;col++)game.PlayerStore.Data.BonusArea[col]=col==4?1:2;
                field.BonusCollection.Initialize(player.BonusArea);
                Assert.IsFalse(player.IsBonusGame);
                Assert.IsFalse(field.BonusCollection.GetUnselectedTarget(4,2).GetChild(0).gameObject.activeSelf);
            }
            int mixedSeed=-1;
            for(int seed=0;seed<(collectBonus?200000:10000);seed++) {
                Random.InitState(seed);var settlement=new RecoveredSlotSettlement(game.Rules);
                var probe=new RecoveredSpinResult(game.Rules,settlement){ForceFreeSpin=true};
                probe.Begin(false,field.Bet,Mathf.Max(0,player.MoreWild-1),game.PlayerStore.Data.BonusArea);
                int steps=0;while(probe.IsGenerating&&steps++<10000)probe.Step();
                bool completesCollection=!collectBonus;
                if(collectBonus)for(int row=0;row<3;row++)if(probe.Board.GetSymbol(4,row)==9)completesCollection=true;
                if(!probe.IsGenerating&&completesCollection&&probe.ScatterCount>=3&&game.Rules.GetBigWin(settlement.GetWinTotalLine(),field.Bet)!=RecoveredSlotWinType.None){mixedSeed=seed;break;}
            }
            Assert.GreaterOrEqual(mixedSeed,0,"Find a real BigWin plus Scatter result without replacing cells.");
            int freeSeed=-1;
            for(int seed=0;seed<10000;seed++){Random.InitState(seed);game.Rules.GetFreeCoinAmount();if(game.Rules.GetFreeBallAmount()==0){freeSeed=seed;break;}}
            Assert.GreaterOrEqual(freeSeed,0);
            // Bound the later Free loop to one actual round without a minigame. The
            // intro's native initial/extra count is checked before this fixture override.
            if(!fullFree)core.Entry.ChangeMusicRequested+=name=>{player.FreeSpinCount=1;Random.InitState(freeSeed);};
            var reels=field.ModeView.FreeReels;
            var slot=core.GetComponentInChildren<RecoveredFreeSlotGame>(true);
            var wheel=core.GetComponentInChildren<RecoveredFreeWheelGame>(true);
            var treasure=core.GetComponentInChildren<RecoveredFreeTreasureGame>(true);
            var lucky=core.GetComponentInChildren<RecoveredFreeLuckyGame>(true);
            int awardedFree=0,rounds=0,stops=0,scans=0,ballCount=0,coinCount=0;
            float freeLedger=0,coinLedger=0;
            reels.Controller.RoundStarted+=()=>{
                rounds++;Assert.LessOrEqual(rounds,awardedFree);
                Assert.AreEqual(awardedFree-rounds,player.FreeSpinCount);
                Assert.IsTrue(field.IsBusy);Assert.IsFalse(core.Exit.Window.IsShown);
            };
            reels.Controller.ReelsStopped+=()=>stops++;
            reels.BallScan.Completed+=()=>{
                scans++;
                for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                    int id=game.FreeSpinResult.GetSymbol(col,row);if(id!=9&&id!=11)continue;
                    Assert.IsTrue(reels.CoinScan.Rewards.TryGetValue(reels.At(col,row).gameObject,out float reward));
                    freeLedger+=reward;
                    if(id==9){coinCount++;coinLedger+=reward;}else ballCount++;
                }
            };
            int completed=0,baseFlights=0,credits=0;core.CoreRoundCompleted+=()=>completed++;
            player.GreenCountChanged+=(before,after)=>credits++;
            float initialCash=player.GreenCount;int initialSpins=player.SpinCount;
            field.FlyCoinRequested+=(amount,callback)=>{baseFlights++;Assert.IsFalse(core.Entry.Window.gameObject.activeSelf);};
            Random.InitState(mixedSeed);game.SpinResult.ForceFreeSpin=true;
            if(!collectBonus)player.IsBonusGame=includeBonus;
            field.SpinButton.Button.onClick.Invoke();
            if(collectBonus){Assert.IsFalse(player.IsBonusGame);Assert.AreEqual(1,player.BonusArea[4]);}
            Assert.GreaterOrEqual(game.SpinResult.ScatterCount,3);
            Assert.AreNotEqual(RecoveredSlotWinType.None,game.Rules.GetBigWin(game.Settlement.GetWinTotalLine(),field.Bet));
            for(int frame=0;frame<1000&&!field.BigWinPopup.gameObject.activeSelf;frame++)yield return null;
            Assert.IsNull(field.Error);Assert.IsTrue(field.BigWinPopup.gameObject.activeSelf);
            if(collectBonus) {
                Assert.GreaterOrEqual(player.BonusArea[4],2,"Actual Base coin scan must fill the missing collection slot.");
                Assert.IsTrue(field.BonusCollection.GetUnselectedTarget(4,2).GetChild(0).gameObject.activeSelf,"The real coin flight must light the collection target.");
                Assert.IsFalse(player.IsBonusGame,"Readiness is evaluated later, at CheckBonusGame.");
            }
            Assert.IsFalse(core.Entry.IsRunning);Assert.AreEqual(initialCash,player.GreenCount);
            for(int frame=0;frame<30;frame++)yield return null;
            field.BigWinPopup.ClaimButton.onClick.Invoke();Assert.IsTrue(game.Ads.Pending);
            game.AdControls.FailureButton.onClick.Invoke();
            Assert.IsFalse(game.Ads.Pending);Assert.IsFalse(field.BigWinPopup.Claim.IsClicked);
            Assert.AreEqual(0,baseFlights);Assert.AreEqual(initialCash,player.GreenCount);Assert.IsFalse(core.Entry.IsRunning);
            float baseAward=field.SymbolWin.TotalWin*game.Rules.GetBigWinClaim(0);
            field.BigWinPopup.ClaimButton.onClick.Invoke();game.AdControls.RewardButton.onClick.Invoke();
            for(int frame=0;frame<1000&&!(includeBonus?game.BonusFlow.Window.gameObject.activeSelf:core.Entry.Window.gameObject.activeSelf);frame++)yield return null;
            Assert.IsNull(core.Error);
            Assert.IsTrue(includeBonus?game.BonusFlow.Window.gameObject.activeSelf:core.Entry.Window.gameObject.activeSelf);
            Assert.IsFalse(field.BigWinPopup.gameObject.activeSelf);Assert.AreEqual(1,baseFlights);
            Assert.AreEqual(0,completed);Assert.IsTrue(field.IsBusy);
            // Native popup continuation does not await cash arrival. Departure
            // stagger uses unscaled time while Free entry uses scaled time.
            float cashDeadline=Time.realtimeSinceStartup+5;
            while(game.CashFlight.ActiveCashCount>0&&Time.realtimeSinceStartup<cashDeadline)yield return null;
            Assert.AreEqual(0,game.CashFlight.ActiveCashCount);Assert.AreEqual(1,credits);
            Assert.AreEqual(initialCash+baseAward,player.GreenCount);
            if(includeBonus) {
                var bonus=game.BonusFlow;var window=bonus.Window;
                Assert.IsFalse(core.Entry.IsRunning);Assert.AreEqual(RecoveredSlotType.Base,player.GameSlotType);
                Assert.IsFalse(player.IsBonusGame);CollectionAssert.AreEqual(new[]{0,0,0,0,0},player.BonusArea);
                int scatter=game.SpinResult.ScatterCount;
                for(int frame=0;frame<100&&bonus.Transition.IsPlaying;frame++)yield return null;
                for(int card=0;card<game.Rules.GetBonusFreeTimes();card++) {
                    window.Card(card).Button.onClick.Invoke();float cardDeadline=Time.realtimeSinceStartup+8;
                    while(window.Selection.IsClicked&&Time.realtimeSinceStartup<cardDeadline) {
                        if(window.RewardPopup.gameObject.activeInHierarchy)window.RewardPopup.PlainButton.onClick.Invoke();
                        if(window.JackpotPopup.gameObject.activeInHierarchy)window.JackpotPopup.PlainButton.onClick.Invoke();
                        yield return null;
                    }
                    Assert.IsFalse(window.Selection.IsClicked);Assert.IsFalse(core.Entry.IsRunning);
                    Assert.AreEqual(scatter,game.SpinResult.ScatterCount,"Bonus rewards must preserve this Spin's Scatter result.");
                }
                float rewardDeadline=Time.realtimeSinceStartup+5;
                while(game.CashFlight.ActiveCashCount>0&&Time.realtimeSinceStartup<rewardDeadline)yield return null;
                Assert.AreEqual(0,game.CashFlight.ActiveCashCount);float cashAfterBonus=player.GreenCount;
                Assert.IsTrue(window.CloseButton.gameObject.activeInHierarchy);window.CloseButton.onClick.Invoke();
                for(int frame=0;frame<1000&&!core.Entry.Window.gameObject.activeSelf;frame++)yield return null;
                Assert.IsFalse(bonus.IsRunning);Assert.IsFalse(window.gameObject.activeSelf);
                Assert.AreEqual(scatter,game.SpinResult.ScatterCount);Assert.AreEqual(cashAfterBonus,player.GreenCount);
            }
            Assert.IsTrue(core.Entry.Window.gameObject.activeSelf,"The same Spin must reach its Free intro.");
            field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(initialSpins-1,player.SpinCount,"Free intro must retain the paid Spin lock.");
            int initialFree=game.Rules.GetFreeSpins(game.SpinResult.ScatterCount);
            Assert.AreEqual(initialFree,core.Entry.InitialSpinCount);Assert.AreEqual(initialFree,player.FreeSpinCount);
            for(int frame=0;frame<20;frame++)yield return null;
            yield return ClickVisible(core.Entry.Window.ClaimButton,game);Assert.IsTrue(game.Ads.Pending);
            Assert.AreEqual("freespin",game.Ads.Placement);
            yield return ClickVisible(game.AdControls.FailureButton,game);
            Assert.IsFalse(game.Ads.Pending);Assert.IsFalse(core.Entry.Window.IsClicked);
            Assert.AreEqual(initialFree,player.FreeSpinCount);Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);
            yield return ClickVisible(core.Entry.Window.ClaimButton,game);
            Assert.IsTrue(game.Ads.Pending);Assert.AreEqual(initialFree,player.FreeSpinCount);
            yield return ClickVisible(game.AdControls.RewardButton,game);
            Assert.AreEqual(initialFree+game.Rules.GetExtraFreeSpins(),player.FreeSpinCount);
            awardedFree=fullFree?player.FreeSpinCount:1;
            float freeDeadline=Time.realtimeSinceStartup+120;
            while(!core.Exit.Window.IsShown&&Time.realtimeSinceStartup<freeDeadline) {
                if(fullFree) {
                    int active=(slot.IsRunning?1:0)+(wheel.IsRunning?1:0)+(treasure.IsRunning?1:0)+(lucky.IsRunning?1:0);
                    Assert.LessOrEqual(active,1,"Ball minigames must run serially throughout the awarded session.");
                    Claim(slot.Window.Popup.PlainButton);Claim(wheel.Window.CashPopup.PlainButton);
                    Claim(wheel.Window.JackpotPopup.PlainButton);Claim(treasure.Window.PlainButton);Claim(lucky.Popup.PlainButton);
                    var bonus=game.BonusFlow;var window=bonus.Window;
                    if(window.gameObject.activeInHierarchy&&!bonus.Transition.IsPlaying) {
                        Claim(window.RewardPopup.PlainButton);Claim(window.JackpotPopup.PlainButton);
                        if(!window.Selection.IsClicked&&!window.Selection.IsEnd) {
                            if(window.Selection.Round.ClickedCount<game.Rules.GetBonusFreeTimes())
                                Claim(window.Card(window.Selection.Round.ClickedCount).Button);
                            else Claim(window.CloseButton);
                        }
                    }
                }
                Assert.IsTrue(field.IsBusy);Assert.AreEqual(0,completed);
                yield return null;
            }
            Assert.IsNull(core.Error);Assert.IsNull(core.Exit.Error);Assert.IsNull(field.ModeView.FreeReels.Controller.Error);
            Assert.IsTrue(core.Exit.Window.IsShown);Assert.IsTrue(field.IsBusy);Assert.AreEqual(0,completed);
            Assert.AreEqual(awardedFree,rounds);Assert.AreEqual(rounds,stops);
            Assert.AreEqual(rounds,scans);
            Assert.AreEqual(freeLedger,reels.RewardCollect.FreeReward);
            Assert.AreEqual(coinLedger,reels.RewardCollect.CoinReward);
            Assert.AreEqual(freeLedger,player.TotalFreeSpinWin);
            Assert.IsNull(reels.CoinScan.Error);Assert.IsNull(reels.BallScan.Error);Assert.IsNull(reels.RewardCollect.Error);
            Assert.IsNull(slot.Error);Assert.IsNull(wheel.Error);Assert.IsNull(lucky.Error);
            Assert.AreEqual(0,player.FreeSpinCount);Assert.AreEqual(initialSpins-1,player.SpinCount);
            for(int frame=0;frame<120&&!core.Exit.Window.ContinueButton.gameObject.activeInHierarchy;frame++)yield return null;
            Assert.IsTrue(core.Exit.Window.ContinueButton.gameObject.activeInHierarchy);
            yield return ClickVisible(core.Exit.Window.ContinueButton,game);
            Assert.IsTrue(field.IsBusy,"Visible Continue must still wait for the return transition.");
            for(int frame=0;frame<250&&completed==0;frame++){RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.AreEqual(1,completed);Assert.IsFalse(field.IsBusy);Assert.IsFalse(field.AwaitingRewards);
            Assert.AreEqual(RecoveredSlotType.Base,player.GameSlotType);Assert.AreEqual("normalBg",game.CoreAudio.Manager.RequestedMusic);
            field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(initialSpins-2,player.SpinCount);
            TestContext.WriteLine("Mixed Base result seed: "+mixedSeed+"; full Free: "+fullFree+"; completed Free rounds: "+rounds+
                "; coins: "+coinCount+"; balls: "+ballCount+"; Free ledger: "+freeLedger+"; actual collection trigger: "+collectBonus);
        } finally {
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(UnityEngine.UI.Button button)
    {if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static IEnumerator ClickVisible(UnityEngine.UI.Button button,GameEntry game)
    {
        float deadline=Time.realtimeSinceStartup+5;
        var hits=new List<RaycastResult>();
        while(Time.realtimeSinceStartup<deadline) {
            if(button.gameObject.activeInHierarchy&&button.IsInteractable()) {
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                    position=RectTransformUtility.WorldToScreenPoint(game.GetComponent<Canvas>().worldCamera,rect.TransformPoint(rect.rect.center))};
                hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
                if(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject) {
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);yield break;
                }
            }
            yield return null;
        }
        Assert.Fail("Production button must become the top scene raycast target: "+button.name+
            "; top hit: "+(hits.Count>0?hits[0].gameObject.name:"none"));
    }
}
