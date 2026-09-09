using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredGmPanelTests
{
    [UnityTest]
    public IEnumerator DefaultHiddenControlsOpenByPointerSwitchProfilesAndCollapseWithoutReloadingOnToggle()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);float deadline=Time.realtimeSinceStartup+5;
            while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;Assert.IsNotNull(entry.Playfield);
            var panel=entry.GetComponent<RecoveredGmPanel>();var camera=entry.GetComponent<Canvas>().worldCamera;
            var primary=entry.transform.Find("SelectUS").GetComponent<Button>();var alternate=entry.transform.Find("SelectAlternative").GetComponent<Button>();
            var configTest=entry.transform.Find("SelectConfigTest").GetComponent<Button>();
            var bundledDefault=entry.transform.Find("SelectBundledDefault").GetComponent<Button>();
            var bundledOrganic=entry.transform.Find("SelectBundledOrganic").GetComponent<Button>();
            Assert.IsFalse(panel.IsOpen);Assert.IsFalse(entry.CurrentProfile.isA);Assert.AreEqual("US",entry.CurrentProfile.countryCode);
            foreach(var button in new[]{primary,alternate,configTest,bundledDefault,bundledOrganic}) {
                var group=button.GetComponent<CanvasGroup>();Assert.AreEqual(0,group.alpha);Assert.IsFalse(group.blocksRaycasts);Assert.IsFalse(button.IsInteractable());
                Assert.IsEmpty(Hits(button,camera),"Hidden GM controls must not intercept touches over the game.");
            }
            var oldField=entry.Playfield;var oldProgress=entry.PlayerProgress;int spins=oldProgress.SpinCount;
            Click(panel.ToggleButton,camera);yield return null;Assert.IsTrue(panel.IsOpen);
            Assert.AreSame(oldField,entry.Playfield);Assert.AreSame(oldProgress,entry.PlayerProgress);Assert.AreEqual(spins,oldProgress.SpinCount);
            Click(panel.ToggleButton,camera);yield return null;Assert.IsFalse(panel.IsOpen);Assert.AreSame(oldField,entry.Playfield);
            Click(panel.ToggleButton,camera);yield return null;Click(alternate,camera);Assert.IsFalse(panel.IsOpen);
            deadline=Time.realtimeSinceStartup+5;while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(entry.Playfield);Assert.IsTrue(entry.CurrentProfile.isA);Assert.AreNotSame(oldField,entry.Playfield);
            Assert.IsEmpty(Hits(alternate,camera));
            Click(panel.ToggleButton,camera);yield return null;Click(primary,camera);Assert.IsFalse(panel.IsOpen);
            deadline=Time.realtimeSinceStartup+5;while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(entry.Playfield);Assert.IsFalse(entry.CurrentProfile.isA);Assert.AreEqual("US",entry.CurrentProfile.countryCode);
            oldField=entry.Playfield;
            Click(panel.ToggleButton,camera);yield return null;Click(configTest,camera);Assert.IsFalse(panel.IsOpen);
            deadline=Time.realtimeSinceStartup+5;while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(entry.Playfield);Assert.AreNotSame(oldField,entry.Playfield);
            Assert.AreEqual("RecoveredConfig/Remote/cp_test.json",entry.CurrentProfile.snapshotPath);
            Assert.IsFalse(entry.CurrentProfile.isA);Assert.IsEmpty(Hits(configTest,camera));
            int testSpins=entry.PlayerProgress.SpinCount;
            entry.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(testSpins-1,entry.PlayerProgress.SpinCount);Assert.IsTrue(entry.Playfield.IsBusy);
            Click(panel.ToggleButton,camera);yield return null;Click(primary,camera);
            deadline=Time.realtimeSinceStartup+5;while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(entry.Playfield);Assert.AreEqual("US_Default",entry.CurrentProfile.profileId);
            foreach(var button in new[]{bundledDefault,bundledOrganic}) {
                Click(panel.ToggleButton,camera);yield return null;Click(button,camera);Assert.IsFalse(panel.IsOpen);
                deadline=Time.realtimeSinceStartup+5;while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNotNull(entry.Playfield);
                string type=button==bundledDefault?"default":"organic";
                Assert.AreEqual("RecoveredConfig/Bundled/GoldenDragon_"+type+".json",entry.CurrentProfile.snapshotPath);
                Assert.AreEqual(type,entry.Rules.GetConfigType());
                entry.PlayerProgress.SetSpinCount(entry.Rules.GetMaxSpinCount());
                int beforeSpin=entry.PlayerProgress.SpinCount;
                entry.Playfield.SpinButton.Button.onClick.Invoke();
                Assert.AreEqual(beforeSpin-1,entry.PlayerProgress.SpinCount);Assert.IsTrue(entry.Playfield.IsBusy);
                var recovery=entry.Playfield.SpinRecovery.Recovery;
                Assert.AreEqual(type=="default",recovery.IsRunning);
                if(type=="default")Assert.AreEqual(entry.Rules.GetSpinCD(entry.PlayerProgress.Level),recovery.RemainingSeconds);
                Click(panel.ToggleButton,camera);yield return null;Click(primary,camera);
                Assert.IsFalse(recovery.IsRunning,"Switching must dispose the previous profile's countdown.");
                deadline=Time.realtimeSinceStartup+5;while(entry.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNotNull(entry.Playfield);Assert.AreEqual("US_Default",entry.CurrentProfile.profileId);
            }
        } finally {
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static List<RaycastResult> Hits(Button button,Camera camera)
    {
        Canvas.ForceUpdateCanvases();var hits=new List<RaycastResult>();
        var rect=(RectTransform)button.transform;
        button.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))},hits);
        return hits;
    }
    private static void Click(Button button,Camera camera)
    {
        var hits=Hits(button,camera);Assert.IsNotEmpty(hits,"Expected reachable button "+button.name);
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left};
        Assert.IsTrue(ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler)!=null);
    }
}
