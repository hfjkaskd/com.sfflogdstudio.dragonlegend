using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredFreeGMTeardownTests
{
    [UnityTest]
    public IEnumerator GMRebuildCancelsFreeAccelerationAndPartiallyStoppedColumns()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);
            for(int phase=0;phase<2;phase++) {
                var core=game.CoreRound;var player=game.PlayerProgress;var reels=game.Playfield.ModeView.FreeReels;var controller=reels.Controller;
                core.Entry.Begin(3);
                for(int frame=0;frame<150&&!core.Entry.Window.gameObject.activeSelf;frame++)yield return null;
                Assert.IsTrue(core.Entry.Window.gameObject.activeSelf);for(int i=0;i<20;i++)yield return null;
                core.Entry.Window.PlainButton.onClick.Invoke();
                for(int frame=0;frame<250&&!controller.IsRunning;frame++)yield return null;
                Assert.IsTrue(controller.IsRunning);Assert.IsTrue(reels.IsInitialized);
                if(phase==1)for(int frame=0;frame<100&&controller.StoppedCount==0;frame++)yield return null;
                Assert.Less(controller.StoppedCount,5);if(phase==1)Assert.Greater(controller.StoppedCount,0);
                var oldShake=game.Playfield.Wilds.Shake;
                if(phase==1)Assert.IsTrue(oldShake.IsShaking,"Partial Free stop must have started its board shake.");
                int completions=0,sounds=0;controller.ReelsStopped+=()=>completions++;controller.SoundRequested+=name=>sounds++;
                float cash=player.GreenCount,total=player.TotalFreeSpinWin;int count=player.FreeSpinCount;
                var motions=new RecoveredBaseReelMotion[15];for(int col=0;col<5;col++)for(int row=0;row<3;row++)motions[col*3+row]=reels.MotionAt(col,row).Movement;
                game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(oldShake.IsShaking);Assert.AreEqual(oldShake.OriginalPosition,oldShake.Target.anchoredPosition);
                Assert.IsFalse(controller.IsRunning,"GM must abort the old Free controller immediately.");
                foreach(var motion in motions){Assert.IsFalse(motion.IsSpinning);Assert.IsFalse(motion.StopRequested);}
                deadline=Time.realtimeSinceStartup+10;while((game.PlayerProgress==player||game.CoreRound==null)&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNotNull(game.CoreRound);Assert.AreNotSame(core,game.CoreRound);
                for(int frame=0;frame<100;frame++)yield return null;
                Assert.AreEqual(0,completions);Assert.AreEqual(0,sounds);Assert.AreEqual(cash,player.GreenCount);Assert.AreEqual(total,player.TotalFreeSpinWin);Assert.AreEqual(count,player.FreeSpinCount);
                Assert.AreEqual(RecoveredSlotType.Base,game.PlayerProgress.GameSlotType);Assert.IsFalse(game.Playfield.IsBusy);
            }
            int spins=game.PlayerProgress.SpinCount;game.Playfield.SpinButton.Button.onClick.Invoke();Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.IsTrue(game.Playfield.Reels.IsRunning);
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
