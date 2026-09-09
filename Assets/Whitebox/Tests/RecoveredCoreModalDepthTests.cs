using System.Collections;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RecoveredCoreModalDepthTests
{
    [UnityTest]
    public IEnumerator PopupGroupExcludesTipsAndOrdersNewWindowsAboveExistingOnes()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;core.FirstSpinGuide.Hide();
            var wild=core.MoreWild;var spins=core.MoreSpins;
            core.Tips.Show("Depth fixture");game.Playfield.MoreWildEntry.Button.onClick.Invoke();
            Assert.IsTrue(wild.gameObject.activeSelf);Assert.AreEqual(300,wild.GetComponent<Canvas>().sortingOrder,"Top3 tips must not raise Popup group depth.");
            Assert.Greater(core.Tips.GetComponent<Canvas>().sortingOrder,wild.GetComponent<Canvas>().sortingOrder);
            game.Playfield.SpinRecovery.MoreSpinButton.onClick.Invoke();
            Assert.IsTrue(spins.gameObject.activeSelf);Assert.AreEqual(301,spins.GetComponent<Canvas>().sortingOrder);
            game.Playfield.SpinRecovery.MoreSpinButton.onClick.Invoke();Assert.AreEqual(301,spins.GetComponent<Canvas>().sortingOrder,"Already shown does not move again.");
            spins.CloseButton.onClick.Invoke();for(int i=0;i<10;i++)yield return null;
            wild.CloseButton.onClick.Invoke();for(int i=0;i<10;i++)yield return null;
            Assert.IsFalse(spins.gameObject.activeSelf);Assert.IsFalse(wild.gameObject.activeSelf);
            game.Playfield.SpinRecovery.MoreSpinButton.onClick.Invoke();Assert.AreEqual(300,spins.GetComponent<Canvas>().sortingOrder,"Inactive cached windows do not reserve depths.");
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
