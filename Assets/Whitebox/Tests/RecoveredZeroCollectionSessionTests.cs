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
    public IEnumerator ActualPaidSpinsCollectFromZeroAndReturnAfterBonus()
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
            core.FirstSpinGuide.Hide();game.PlayerStore.Data.GuideStep=3;
            CollectionAssert.AreEqual(new[]{0,0,0,0,0},player.BonusArea);Assert.IsFalse(player.IsBonusGame);
            var slot=core.GetComponentInChildren<RecoveredFreeSlotGame>(true);
            var wheel=core.GetComponentInChildren<RecoveredFreeWheelGame>(true);
            var treasure=core.GetComponentInChildren<RecoveredFreeTreasureGame>(true);
            var lucky=core.GetComponentInChildren<RecoveredFreeLuckyGame>(true);
            int completed=0,paid=0,bonusShown=0;bool inBonus=false;
            core.CoreRoundCompleted+=()=>completed++;
            System.Exception bonusError=null;game.BonusFlow.Failed+=error=>bonusError=error;
            float sessionDeadline=Time.realtimeSinceStartup+120;
            while((bonusShown==0||field.IsBusy)&&Time.realtimeSinceStartup<sessionDeadline&&paid<60) {
                if(!field.IsBusy&&!core.MoreSpins.gameObject.activeSelf) {
                    int before=player.SpinCount;field.SpinButton.Button.onClick.Invoke();
                    if(before>0){paid++;Assert.AreEqual(before-1,player.SpinCount);Assert.IsTrue(field.IsBusy);}
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
                    if(!inBonus){bonusShown++;inBonus=true;CollectionAssert.AreEqual(new[]{0,0,0,0,0},player.BonusArea);Assert.IsFalse(player.IsBonusGame);}
                    if(!bonus.Transition.IsPlaying) {
                        Claim(window.RewardPopup.PlainButton);Claim(window.JackpotPopup.PlainButton);
                        if(!window.Selection.IsClicked&&!window.Selection.IsEnd) {
                            if(window.Selection.Round.ClickedCount<game.Rules.GetBonusFreeTimes())Claim(window.Card(window.Selection.Round.ClickedCount).Button);
                            else Claim(window.CloseButton);
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
            Assert.Greater(bonusShown,0,"Actual generated collection must reach Bonus within the bounded session.");
            Assert.IsFalse(field.IsBusy);Assert.AreEqual(paid,completed);
            Assert.AreEqual(RecoveredSlotType.Base,player.GameSlotType);
            Assert.AreEqual(JsonUtility.ToJson(game.PlayerStore.Data),PlayerPrefs.GetString(key));
            TestContext.WriteLine("Paid Spins: "+paid+"; completed: "+completed+"; Bonus windows: "+bonusShown);
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
