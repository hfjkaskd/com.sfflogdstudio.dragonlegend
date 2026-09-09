using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCoreRepeatedFreeTests
{
    [UnityTest]
    public IEnumerator TwoFreeRoundsSerializeMultipleBallsAndPreserveNativeCumulativeCoinCredit()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;var reels=game.Playfield.ModeView.FreeReels;
            var slot=core.GetComponentInChildren<RecoveredFreeSlotGame>(true);var wheel=core.GetComponentInChildren<RecoveredFreeWheelGame>(true);
            var treasure=core.GetComponentInChildren<RecoveredFreeTreasureGame>(true);var lucky=core.GetComponentInChildren<RecoveredFreeLuckyGame>(true);
            int finished=0;core.CoreRoundCompleted+=()=>finished++;
            game.Playfield.SpinButton.Button.onClick.Invoke();
            for(int frame=0;frame<1800&&finished==0;frame++) {
                Claim(game.Playfield.BigWinPopup.PlainButton);Claim(game.Playfield.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;
            }
            Assert.AreEqual(1,finished);
            int seed=-1;var beforeSearch=Random.state;
            for(int candidate=0;candidate<10000;candidate++) {
                Random.InitState(candidate);if(game.Rules.GetFreeCoinAmount()==1&&game.Rules.GetFreeBallAmount()==2){seed=candidate;break;}
            }
            Random.state=beforeSearch;Assert.GreaterOrEqual(seed,0);
            core.Entry.ChangeMusicRequested+=name=>{game.PlayerProgress.FreeSpinCount=2;Random.InitState(seed);};
            int rounds=0,scans=0,balls=0,games=0;float total=0,coins=0,credit=0;
            reels.Controller.RoundStarted+=()=>{
                rounds++;Assert.LessOrEqual(rounds,2);Assert.AreEqual(2-rounds,game.PlayerProgress.FreeSpinCount);
                Assert.AreEqual(0,reels.CoinScan.Rewards.Count,"Round ledger must clear before a new spin.");
                Assert.AreEqual(total,game.PlayerProgress.TotalFreeSpinWin);
                Assert.AreEqual(total,reels.RewardCollect.FreeReward);Assert.AreEqual(coins,reels.RewardCollect.CoinReward);
                Assert.IsTrue(game.Playfield.IsBusy);Assert.IsFalse(core.Exit.Window.IsShown);
            };
            reels.BallScan.Completed+=()=>{
                scans++;float roundCoins=0,roundBalls=0;int count=0;
                for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                    int id=game.FreeSpinResult.GetSymbol(col,row);if(id!=9&&id!=11)continue;
                    Assert.IsTrue(reels.CoinScan.Rewards.TryGetValue(reels.At(col,row).gameObject,out float reward));
                    count++;if(id==9)roundCoins+=reward;else {roundBalls+=reward;balls++;}
                }
                Assert.AreEqual(3,count);Assert.AreEqual(3,reels.CoinScan.Rewards.Count);
                coins+=roundCoins;total+=roundCoins+roundBalls;
                // Native CheckRewardCollect credits cumulative CoinReward again at each round end.
                credit+=roundBalls+coins;
                Assert.AreEqual(total,game.PlayerProgress.TotalFreeSpinWin);
            };
            game.SpinResult.ForceFreeSpin=true;game.Playfield.SpinButton.Button.onClick.Invoke();
            for(int frame=0;frame<1800&&!core.Entry.Window.gameObject.activeSelf;frame++) {
                Claim(game.Playfield.BigWinPopup.PlainButton);Claim(game.Playfield.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;
            }
            Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);for(int frame=0;frame<20;frame++)yield return null;
            for(int frame=0;frame<200&&game.CashFlight.ActiveCashCount>0;frame++)yield return null;
            Assert.AreEqual(0,game.CashFlight.ActiveCashCount);float balance=game.PlayerProgress.GreenCount;
            core.Entry.Window.PlainButton.onClick.Invoke();bool wasRunning=false;
            for(int frame=0;frame<3600&&!core.Exit.Window.IsShown;frame++) {
                int active=(slot.IsRunning?1:0)+(wheel.IsRunning?1:0)+(treasure.IsRunning?1:0)+(lucky.IsRunning?1:0);
                Assert.LessOrEqual(active,1,"Ball games must run serially.");if(active>0&&!wasRunning)games++;wasRunning=active>0;
                Claim(slot.Window.Popup.PlainButton);Claim(wheel.Window.CashPopup.PlainButton);Claim(wheel.Window.JackpotPopup.PlainButton);
                Claim(treasure.Window.PlainButton);Claim(lucky.Popup.PlainButton);
                Assert.IsTrue(game.Playfield.IsBusy);Assert.AreEqual(1,finished);
                // Test fixture controls the next generated board without replacing generation or exit callbacks.
                if(reels.RewardCollect.IsRunning)Random.InitState(seed);
                yield return null;
            }
            Assert.IsNull(core.Error);Assert.IsNull(core.Exit.Error);Assert.IsNull(reels.Controller.Error);
            Assert.IsNull(reels.CoinScan.Error);Assert.IsNull(reels.BallScan.Error);Assert.IsNull(reels.RewardCollect.Error);
            Assert.IsNull(slot.Error);Assert.IsNull(wheel.Error);Assert.IsNull(lucky.Error);
            Assert.IsTrue(core.Exit.Window.IsShown);Assert.AreEqual(2,rounds);Assert.AreEqual(2,scans);Assert.AreEqual(4,balls);Assert.AreEqual(4,games);
            Assert.AreEqual(total,reels.RewardCollect.FreeReward);Assert.AreEqual(coins,reels.RewardCollect.CoinReward);
            Assert.AreEqual(total,game.PlayerProgress.TotalFreeSpinWin);Assert.AreEqual(balance+credit,game.PlayerProgress.GreenCount);
            for(int frame=0;frame<120&&!core.Exit.Window.ContinueButton.gameObject.activeInHierarchy;frame++)yield return null;
            Assert.IsTrue(core.Exit.Window.ContinueButton.gameObject.activeInHierarchy);core.Exit.Window.ContinueButton.onClick.Invoke();Assert.IsTrue(game.Playfield.IsBusy);
            for(int frame=0;frame<150&&finished<2;frame++){RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.AreEqual(2,finished);Assert.IsFalse(game.Playfield.IsBusy);Assert.AreEqual(RecoveredSlotType.Base,game.PlayerProgress.GameSlotType);
            int spins=game.PlayerProgress.SpinCount;game.Playfield.SpinButton.Button.onClick.Invoke();Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);
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
