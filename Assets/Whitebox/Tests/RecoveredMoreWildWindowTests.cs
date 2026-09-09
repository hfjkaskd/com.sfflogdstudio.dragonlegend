using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredMoreWildWindowTests
{
    [UnityTest]
    public IEnumerator FirstSpinOpensFreeWildAndActualGuideClaimEnablesFollowingSpin()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=null;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            target=new RenderTexture(1080,1920,24);target.Create();
            prepare=(loaded,mode)=>{if(loaded.name!="GameEntry")return;foreach(var root in loaded.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null){camera=entry.GetComponent<Canvas>().worldCamera;camera.targetTexture=target;}}};
            SceneManager.sceneLoaded+=prepare;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;var field=game.Playfield;var window=core.MoreWild;
            var wildEntry=field.MoreWildEntry;
            Assert.IsTrue(wildEntry.Word.gameObject.activeSelf);Assert.IsFalse(wildEntry.Progress.activeSelf);
            int completed=0;core.CoreRoundCompleted+=()=>completed++;
            field.SpinButton.Button.onClick.Invoke();
            for(int frame=0;frame<1800&&completed==0;frame++){Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);RecoveredCorePromptDriver.ClaimAndClose(core);yield return null;}
            Assert.IsNull(core.Error);Assert.AreEqual(1,completed);Assert.IsFalse(field.IsBusy);
            Assert.IsTrue(window.gameObject.activeSelf);Assert.AreEqual(2,game.PlayerStore.Data.GuideStep);
            for(int frame=0;frame<80;frame++)yield return null;
            Assert.AreEqual("CLAIM",window.ClaimText.text);Assert.AreEqual("Tap here to unleash extra Wilds!",window.Guide.Text.Label.text);
            Assert.AreSame(window.ClaimButton.transform,window.Guide.Target);
            Assert.AreSame(window.Guide.transform,window.ClaimButton.transform.parent);
            Assert.IsTrue(window.Finger.gameObject.activeSelf);Assert.AreSame(window.ClaimText.transform,window.Finger.parent);
            Assert.IsFalse(game.Ads.Pending);Assert.IsFalse(field.IsBusy,"Native clears busy while the modal guide holds input.");
            Time.timeScale=0;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreSame(window.ClaimButton.gameObject,Hit(window.ClaimButton.transform,camera));
            Assert.AreSame(window.Guide.Background.gameObject,Hit(window.CloseButton.transform,camera));
            Assert.AreSame(window.Guide.Background.gameObject,Hit(field.SpinButton.transform,camera));
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-more-wild-guide.png"),capture.EncodeToPNG());
            Click(window.ClaimButton.gameObject);
            Assert.AreEqual(3,game.PlayerStore.Data.GuideStep);Assert.AreEqual(game.Rules.GetMoreWild(),game.PlayerProgress.MoreWild);
            Assert.IsFalse(window.Guide.gameObject.activeSelf);Assert.IsFalse(window.Finger.gameObject.activeSelf);
            Assert.AreSame(window.transform.Find("Content/Btn"),window.ClaimButton.transform.parent);
            Assert.IsFalse(game.Ads.Pending);
            Time.timeScale=1;for(int i=0;i<10;i++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);
            Assert.IsTrue(wildEntry.Progress.activeSelf);Assert.IsFalse(wildEntry.Word.gameObject.activeSelf);
            Assert.AreEqual(game.Rules.GetMoreWild()+"/"+game.Rules.GetMoreWild(),wildEntry.Label.text);
            Assert.AreEqual(1,wildEntry.Fill.fillAmount);
            game.PlayerProgress.SetMoreWild(2);
            Assert.AreEqual("2/"+game.Rules.GetMoreWild(),wildEntry.Label.text);
            Assert.AreEqual(Mathf.Clamp01(2f/game.Rules.GetMoreWild()),wildEntry.Fill.fillAmount);
            game.PlayerProgress.SetMoreWild(0);Assert.IsTrue(wildEntry.Word.gameObject.activeSelf);Assert.IsFalse(wildEntry.Progress.activeSelf);
            game.PlayerProgress.SetMoreWild(2);
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Free;field.ModeView.ApplyCurrent();
            Assert.IsTrue(wildEntry.Button.gameObject.activeInHierarchy,"Original Tubiao remains outside the mode-switched Bottom/Main.");
            Assert.AreEqual(2,game.PlayerProgress.MoreWild);
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Base;field.ModeView.ApplyCurrent();
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreSame(wildEntry.Button.gameObject,Hit(wildEntry.Button.transform,camera));
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-more-wild-entry.png"),capture.EncodeToPNG());
            Click(wildEntry.Button.gameObject);for(int i=0;i<10;i++)yield return null;
            Assert.IsFalse(window.Guide.gameObject.activeSelf);Assert.IsFalse(window.Finger.gameObject.activeSelf);
            Assert.AreEqual("<sprite name=\"tc_btn_bofang\">CLAIM",window.ClaimText.text);
            Click(window.ClaimButton.gameObject);Assert.IsTrue(game.Ads.Pending);game.Ads.Complete(AdOutcome.Failed);
            Click(window.ClaimButton.gameObject);game.Ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(3,game.PlayerStore.Data.GuideStep);for(int i=0;i<10;i++)yield return null;
            int balance=game.PlayerProgress.MoreWild,spins=game.PlayerProgress.SpinCount;
            Click(field.SpinButton.gameObject);Assert.IsTrue(field.IsBusy);Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);Assert.AreEqual(balance-1,game.PlayerProgress.MoreWild);
            Assert.AreEqual((balance-1)+"/"+game.Rules.GetMoreWild(),wildEntry.Label.text);
            Click(wildEntry.Button.gameObject);Assert.IsTrue(window.gameObject.activeSelf,"Native MoreWild entry has no Spin busy guard.");
            string oldLabel=wildEntry.Label.text;var oldPlayer=game.PlayerProgress;
            game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
            oldPlayer.SetMoreWild(1);
            Assert.AreEqual(oldLabel,wildEntry.Label.text,"GM teardown detaches the discarded entry immediately.");
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==core)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreNotSame(core,game.CoreRound);Assert.AreEqual(1,game.PlayerProgress.MoreWild);
            Assert.AreEqual("1/"+game.Rules.GetMoreWild(),game.Playfield.MoreWildEntry.Label.text);
        }
        finally
        {
            if(prepare!=null)SceneManager.sceneLoaded-=prepare;
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button){if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static void Click(GameObject obj)=>ExecuteEvents.Execute(obj,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
    private static GameObject Hit(Transform target,Camera camera)
    {
        var rect=(RectTransform)target;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
        var button=hits[0].gameObject.GetComponentInParent<Button>();return button==null?hits[0].gameObject:button.gameObject;
    }
}
