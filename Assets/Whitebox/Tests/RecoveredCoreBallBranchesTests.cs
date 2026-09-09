using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCoreBallBranchesTests
{
    [UnityTest]
    public IEnumerator ActualFreeBallRoutesAllFourGamesAndReturnsTheirRewardsToSettlement()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);
            var core=game.CoreRound;var reels=game.Playfield.ModeView.FreeReels;
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
                bool seen=false;
                for(int frame=0;frame<1800&&!core.Exit.Window.IsShown;frame++) {
                    seen|=branch==0?slot.IsRunning:branch==1?wheel.IsRunning:branch==2?treasure.IsRunning:lucky.IsRunning;
                    Claim(slot.Window.Popup.PlainButton);Claim(wheel.Window.CashPopup.PlainButton);
                    Claim(wheel.Window.JackpotPopup.PlainButton);Claim(treasure.Window.PlainButton);Claim(lucky.Popup.PlainButton);
                    yield return null;
                }
                Assert.IsNull(core.Error);Assert.IsNull(reels.Controller.Error);Assert.IsNull(reels.BallScan.Error);Assert.IsNull(reels.RewardCollect.Error);
                Assert.IsNull(slot.Error);Assert.IsNull(wheel.Error);Assert.IsNull(lucky.Error);
                Assert.IsTrue(seen,"Actual requested game did not start: "+branch);
                Assert.AreEqual(branch+1,routed);Assert.IsTrue(core.Exit.Window.IsShown,"Free session did not reach its end window: "+branch);
                Assert.AreEqual(1,reels.CoinScan.Rewards.Count);float reward=0;foreach(var value in reels.CoinScan.Rewards.Values)reward+=value;
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
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button)
    {if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
}
