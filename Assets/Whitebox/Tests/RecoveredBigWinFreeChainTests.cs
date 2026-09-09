using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RecoveredBigWinFreeChainTests
{
    [UnityTest]
    public IEnumerator OnePaidSpinRetriesBigWinThenFreeIntroAndReturnsToBase()=>Run(false);

    [UnityTest]
    public IEnumerator OnePaidSpinSettlesBigWinAndBonusBeforeItsFreeIntro()=>Run(true);

    private IEnumerator Run(bool includeBonus)
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key);var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)game=value;}
            Assert.IsNotNull(game);float deadline=Time.realtimeSinceStartup+10;
            while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;var field=game.Playfield;var player=game.PlayerProgress;
            core.FirstSpinGuide.Hide();game.PlayerStore.Data.GuideStep=3;
            int mixedSeed=-1;
            for(int seed=0;seed<10000;seed++) {
                Random.InitState(seed);var settlement=new RecoveredSlotSettlement(game.Rules);
                var probe=new RecoveredSpinResult(game.Rules,settlement){ForceFreeSpin=true};
                probe.Begin(false,field.Bet,Mathf.Max(0,player.MoreWild-1),game.PlayerStore.Data.BonusArea);
                int steps=0;while(probe.IsGenerating&&steps++<10000)probe.Step();
                if(!probe.IsGenerating&&probe.ScatterCount>=3&&game.Rules.GetBigWin(settlement.GetWinTotalLine(),field.Bet)!=RecoveredSlotWinType.None){mixedSeed=seed;break;}
            }
            Assert.GreaterOrEqual(mixedSeed,0,"Find a real BigWin plus Scatter result without replacing cells.");
            int freeSeed=-1;
            for(int seed=0;seed<10000;seed++){Random.InitState(seed);game.Rules.GetFreeCoinAmount();if(game.Rules.GetFreeBallAmount()==0){freeSeed=seed;break;}}
            Assert.GreaterOrEqual(freeSeed,0);
            // Bound the later Free loop to one actual round without a minigame. The
            // intro's native initial/extra count is checked before this fixture override.
            core.Entry.ChangeMusicRequested+=name=>{player.FreeSpinCount=1;Random.InitState(freeSeed);};
            int completed=0,baseFlights=0,credits=0;core.CoreRoundCompleted+=()=>completed++;
            player.GreenCountChanged+=(before,after)=>credits++;
            float initialCash=player.GreenCount;int initialSpins=player.SpinCount;
            field.FlyCoinRequested+=(amount,callback)=>{baseFlights++;Assert.IsFalse(core.Entry.Window.gameObject.activeSelf);};
            Random.InitState(mixedSeed);game.SpinResult.ForceFreeSpin=true;
            player.IsBonusGame=includeBonus;
            field.SpinButton.Button.onClick.Invoke();
            Assert.GreaterOrEqual(game.SpinResult.ScatterCount,3);
            Assert.AreNotEqual(RecoveredSlotWinType.None,game.Rules.GetBigWin(game.Settlement.GetWinTotalLine(),field.Bet));
            for(int frame=0;frame<1000&&!field.BigWinPopup.gameObject.activeSelf;frame++)yield return null;
            Assert.IsNull(field.Error);Assert.IsTrue(field.BigWinPopup.gameObject.activeSelf);
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
            core.Entry.Window.ClaimButton.onClick.Invoke();Assert.IsTrue(game.Ads.Pending);
            game.AdControls.FailureButton.onClick.Invoke();
            Assert.IsFalse(game.Ads.Pending);Assert.IsFalse(core.Entry.Window.IsClicked);
            Assert.AreEqual(initialFree,player.FreeSpinCount);Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);
            core.Entry.Window.ClaimButton.onClick.Invoke();game.AdControls.RewardButton.onClick.Invoke();
            Assert.AreEqual(initialFree+game.Rules.GetExtraFreeSpins(),player.FreeSpinCount);
            for(int frame=0;frame<1800&&!core.Exit.Window.IsShown;frame++)yield return null;
            Assert.IsNull(core.Error);Assert.IsNull(core.Exit.Error);Assert.IsNull(field.ModeView.FreeReels.Controller.Error);
            Assert.IsTrue(core.Exit.Window.IsShown);Assert.IsTrue(field.IsBusy);Assert.AreEqual(0,completed);
            Assert.AreEqual(0,player.FreeSpinCount);Assert.AreEqual(initialSpins-1,player.SpinCount);
            for(int frame=0;frame<120&&!core.Exit.Window.ContinueButton.gameObject.activeInHierarchy;frame++)yield return null;
            Assert.IsTrue(core.Exit.Window.ContinueButton.gameObject.activeInHierarchy);
            core.Exit.Window.ContinueButton.onClick.Invoke();
            for(int frame=0;frame<250&&completed==0;frame++){RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.AreEqual(1,completed);Assert.IsFalse(field.IsBusy);Assert.IsFalse(field.AwaitingRewards);
            Assert.AreEqual(RecoveredSlotType.Base,player.GameSlotType);Assert.AreEqual("normalBg",game.CoreAudio.Manager.RequestedMusic);
            field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(initialSpins-2,player.SpinCount);
            TestContext.WriteLine("Mixed Base result seed: "+mixedSeed+"; shortened Free seed: "+freeSeed);
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
