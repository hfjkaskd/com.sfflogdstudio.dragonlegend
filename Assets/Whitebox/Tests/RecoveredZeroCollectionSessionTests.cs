using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredZeroCollectionSessionTests
{
    [UnityTest]
    public IEnumerator ActualPaidSpinsCollectFromZeroAndReturnAfterBonus()=>Run(false);

    [UnityTest]
    public IEnumerator FreshGuideContinuesThroughFreeWildAndNaturalBonus()=>Run(true);

    [UnityTest]
    public IEnumerator TwoNaturalBonusesReuseWindowAndRetainNativeAdState()=>Run(false,2);

    private IEnumerator Run(bool keepGuide,int targetBonuses=1)
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        PlayerPrefs.DeleteKey(key);var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)game=value;}
            Assert.IsNotNull(game);float deadline=Time.realtimeSinceStartup+10;
            while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;var field=game.Playfield;var player=game.PlayerProgress;
            if(!keepGuide){core.FirstSpinGuide.Hide();game.PlayerStore.Data.GuideStep=3;}
            else {Assert.AreEqual(1,game.PlayerStore.Data.GuideStep);Assert.IsTrue(core.FirstSpinGuide.gameObject.activeInHierarchy);}
            CollectionAssert.AreEqual(new[]{0,0,0,0,0},player.BonusArea);Assert.IsFalse(player.IsBonusGame);
            var slot=core.GetComponentInChildren<RecoveredFreeSlotGame>(true);
            var wheel=core.GetComponentInChildren<RecoveredFreeWheelGame>(true);
            var treasure=core.GetComponentInChildren<RecoveredFreeTreasureGame>(true);
            var lucky=core.GetComponentInChildren<RecoveredFreeLuckyGame>(true);
            int completed=0,paid=0,bonusShown=0;bool inBonus=false,wildClaimed=false;
            RecoveredBonusWindow firstWindow=null;RecoveredBonusSelection firstSelection=null;int bonusAds=0;
            core.CoreRoundCompleted+=()=>completed++;
            System.Exception bonusError=null;game.BonusFlow.Failed+=error=>bonusError=error;
            float sessionDeadline=Time.realtimeSinceStartup+120;
            while((bonusShown<targetBonuses||field.IsBusy)&&Time.realtimeSinceStartup<sessionDeadline&&paid<60) {
                if(!field.IsBusy&&!core.MoreSpins.gameObject.activeSelf&&!core.MoreWild.gameObject.activeSelf) {
                    int before=player.SpinCount;field.SpinButton.Button.onClick.Invoke();
                    if(before>0){paid++;Assert.AreEqual(before-1,player.SpinCount);Assert.IsTrue(field.IsBusy);}
                    if(keepGuide&&paid==1){Assert.AreEqual(2,game.PlayerStore.Data.GuideStep);Assert.IsFalse(core.FirstSpinGuide.gameObject.activeSelf);}
                }
                if(keepGuide&&!wildClaimed&&core.MoreWild.Guide.gameObject.activeInHierarchy) {
                    Assert.AreEqual(2,game.PlayerStore.Data.GuideStep);Assert.AreEqual(1,completed);
                    Claim(core.MoreWild.ClaimButton);wildClaimed=true;
                    Assert.AreEqual(3,game.PlayerStore.Data.GuideStep);
                    Assert.AreEqual(game.Rules.GetMoreWild(),player.MoreWild);Assert.IsFalse(game.Ads.Pending);
                }
                if(core.MoreSpins.gameObject.activeInHierarchy) {
                    Claim(core.MoreSpins.ClaimButton);
                    if(game.Ads.Pending)game.AdControls.RewardButton.onClick.Invoke();
                }
                Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);
                if(core.Entry.Window.gameObject.activeInHierarchy&&!core.Entry.Window.IsTransitioning)Claim(core.Entry.Window.PlainButton);
                Claim(core.Exit.Window.ContinueButton);
                Claim(slot.Window.Popup.PlainButton);Claim(wheel.Window.CashPopup.PlainButton);
                Claim(wheel.Window.JackpotPopup.PlainButton);Claim(treasure.Window.PlainButton);Claim(lucky.Popup.PlainButton);
                var bonus=game.BonusFlow;var window=bonus.Window;
                if(window.gameObject.activeInHierarchy) {
                    if(!inBonus){
                        bonusShown++;inBonus=true;CollectionAssert.AreEqual(new[]{0,0,0,0,0},player.BonusArea);Assert.IsFalse(player.IsBonusGame);
                        Assert.AreEqual(0,window.Selection.Round.ClickedCount);Assert.IsFalse(window.Selection.IsClicked);
                        if(bonusShown==1){firstWindow=window;firstSelection=window.Selection;}
                        else {Assert.AreSame(firstWindow,window);Assert.AreSame(firstSelection,window.Selection);Assert.IsTrue(window.Selection.NeedsAd);}
                    }
                    if(!bonus.Transition.IsPlaying) {
                        Claim(window.RewardPopup.PlainButton);Claim(window.JackpotPopup.PlainButton);
                        if(!window.Selection.IsClicked&&!window.Selection.IsEnd) {
                            if(window.Selection.Round.ClickedCount<game.Rules.GetBonusFreeTimes())Claim(window.Card(window.Selection.Round.ClickedCount).Button);
                            else Claim(window.CloseButton);
                        }
                        if(game.Ads.Pending) {
                            Assert.AreEqual("bonusCoin",game.Ads.Placement);Assert.Greater(bonusShown,1);
                            int before=window.Selection.Round.ClickedCount;
                            Assert.IsTrue(window.Selection.IsClicked);
                            if(game.AdControls.RewardButton.gameObject.activeInHierarchy) {
                                Claim(game.AdControls.RewardButton);bonusAds++;
                                Assert.IsFalse(game.Ads.Pending);Assert.AreEqual(before+1,window.Selection.Round.ClickedCount);
                            }
                        }
                    }
                } else inBonus=false;
                if(core.Bank.gameObject.activeInHierarchy) {
                    if(core.Bank.Selection.Selected.Count==0)Claim(core.Bank.Item(0).Button);
                    Claim(core.Bank.LeaveButton);
                }
                if(core.Review!=null)Claim(core.Review.CloseButton);
                RecoveredCorePromptDriver.ClaimAndClose(core);
                Assert.IsNull(field.Error);Assert.IsNull(core.Error);Assert.IsNull(bonusError);
                yield return null;
            }
            Assert.AreEqual(targetBonuses,bonusShown,"Actual generated collection must reach each Bonus within the bounded session.");
            Assert.AreEqual((targetBonuses-1)*game.Rules.GetBonusFreeTimes(),bonusAds);
            Assert.IsFalse(field.IsBusy);Assert.AreEqual(paid,completed);
            Assert.AreEqual(RecoveredSlotType.Base,player.GameSlotType);
            if(keepGuide){Assert.IsTrue(wildClaimed);Assert.AreEqual(3,game.PlayerStore.Data.GuideStep);Assert.IsFalse(core.MoreWild.Guide.gameObject.activeSelf);}
            Assert.AreEqual(JsonUtility.ToJson(game.PlayerStore.Data),PlayerPrefs.GetString(key));
            string completedSave=PlayerPrefs.GetString(key);
            var previousProgress=player;
            yield return SceneManager.UnloadSceneAsync(scene);scene=default;
            Assert.IsTrue(game==null,"The old GameEntry must be destroyed before loading again.");
            Assert.AreEqual(completedSave,PlayerPrefs.GetString(key));
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry restored=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)restored=value;}
            Assert.IsNotNull(restored);deadline=Time.realtimeSinceStartup+10;
            while(restored.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(restored.CoreRound);Assert.AreNotSame(previousProgress,restored.PlayerProgress);
            Assert.AreEqual(completedSave,JsonUtility.ToJson(restored.PlayerStore.Data),"Reload must deserialize the completed gameplay record.");
            Assert.AreEqual(completedSave,PlayerPrefs.GetString(key));
            Assert.IsFalse(restored.CoreRound.FirstSpinGuide.gameObject.activeSelf);
            Assert.IsFalse(restored.CoreRound.MoreWild.gameObject.activeSelf);
            Assert.IsFalse(restored.Playfield.IsBusy);Assert.IsFalse(restored.BonusFlow.IsRunning);
            int restoredSpins=restored.PlayerProgress.SpinCount;
            if(restoredSpins==0) {
                restored.Playfield.SpinButton.Button.onClick.Invoke();
                Assert.IsFalse(restored.Playfield.IsBusy);Assert.AreEqual(0,restored.PlayerProgress.SpinCount);
                deadline=Time.realtimeSinceStartup+5;
                while(!restored.CoreRound.MoreSpins.ClaimButton.gameObject.activeInHierarchy&&Time.realtimeSinceStartup<deadline)yield return null;
                Claim(restored.CoreRound.MoreSpins.ClaimButton);Assert.IsTrue(restored.Ads.Pending);
                restored.AdControls.RewardButton.onClick.Invoke();Assert.IsFalse(restored.Ads.Pending);
                restoredSpins=restored.PlayerProgress.SpinCount;
            }
            Assert.Greater(restoredSpins,0);
            restored.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(restoredSpins-1,restored.PlayerProgress.SpinCount);Assert.IsTrue(restored.Playfield.IsBusy);
            Assert.AreEqual(3,restored.PlayerStore.Data.GuideStep);
            TestContext.WriteLine("Paid Spins: "+paid+"; completed: "+completed+"; Bonus windows: "+bonusShown+"; original guide: "+keepGuide);
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
