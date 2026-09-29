using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredMainUtilityTests
{
    [UnityTest]
    public IEnumerator MainButtonsOpenNativeWindowsAndSettingsPersist()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        Scene scene=default;AsyncOperation unload=null;Camera camera=null;RenderTexture target=null;Texture2D capture=null;var prior=RenderTexture.active;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var candidate=root.GetComponentInChildren<GameEntry>();if(candidate!=null)game=candidate;}
            float deadline=Time.realtimeSinceStartup+20;while(game.CoreAudio==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.MainUtility);game.CoreRound.FirstSpinGuide.Hide();
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);yield return null;
            var entry=game.MainUtility;AssertClickable(entry.HelpButton,camera);AssertClickable(entry.SettingsButton,camera);Shot("main-utility-entry",camera,target,capture);
            entry.HelpButton.onClick.Invoke();yield return null;
            var help=entry.Help;Assert.IsTrue(help.gameObject.activeSelf);Assert.AreEqual(0,help.CurrentPage);AssertClickable(help.RightButton,camera);
            help.LeftButton.onClick.Invoke();Assert.AreEqual(2,help.CurrentPage);help.RightButton.onClick.Invoke();Assert.AreEqual(0,help.CurrentPage);
            Shot("main-utility-help",camera,target,capture);AssertClickable(help.CloseButton,camera);help.CloseButton.onClick.Invoke();
            entry.SettingsButton.onClick.Invoke();yield return null;var settings=entry.Settings;
            AssertClickable(settings.MusicButton,camera);AssertClickable(settings.SoundButton,camera);
            settings.MusicButton.onClick.Invoke();Assert.IsFalse(game.PlayerStore.Data.IsMusic);Assert.IsFalse(game.CoreAudio.Manager.MusicSource.isPlaying);
            settings.SoundButton.onClick.Invoke();Assert.IsFalse(game.PlayerStore.Data.IsVibrate);
            var loaded=new RecoveredPlayerStore();loaded.Load(null);Assert.IsFalse(loaded.Data.IsMusic);Assert.IsFalse(loaded.Data.IsVibrate);
            settings.CloseButton.onClick.Invoke();entry.SettingsButton.onClick.Invoke();yield return null;
            Shot("main-utility-settings",camera,target,capture);settings.HelpButton.onClick.Invoke();Assert.IsFalse(settings.gameObject.activeSelf);Assert.IsTrue(help.gameObject.activeSelf);
            help.CloseButton.onClick.Invoke();entry.SettingsButton.onClick.Invoke();
            Assert.IsNull(settings.TermsButton);
            Assert.IsNull(settings.transform.Find("Content/ContractBtn"));Assert.IsNull(settings.transform.Find("Content/TermBtn"));
            entry.OpenPrivacy();yield return null;
            Assert.IsTrue(entry.Privacy.gameObject.activeSelf);AssertClickable(entry.Privacy.CloseButton,camera);
            var scroll=entry.Privacy.GetComponentInChildren<ScrollRect>();Assert.Greater(scroll.content.rect.height,scroll.viewport.rect.height);
            entry.Privacy.CloseButton.onClick.Invoke();AssertClickable(entry.SettingsButton,camera);
        } finally {
            RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;if(target!=null){target.Release();Object.Destroy(target);}if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    static void AssertClickable(Button button,Camera camera)
    {
        Canvas.ForceUpdateCanvases();Assert.IsTrue(button.IsInteractable());var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits,button.name);
        Assert.AreEqual(button,hits[0].gameObject.GetComponentInParent<Button>(),button.name+" must be the foremost click target.");
    }
    static void Shot(string name,Camera camera,RenderTexture target,Texture2D capture)
    {
        Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
        capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name+".png"),capture.EncodeToPNG());
    }
}
