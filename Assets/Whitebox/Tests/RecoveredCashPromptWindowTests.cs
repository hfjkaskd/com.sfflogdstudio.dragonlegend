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

public sealed class RecoveredCashPromptWindowTests
{
    [UnityTest]
    public IEnumerator AuthoredPromptClaimsImmediatelyBeforeWithdrawalAndHidesOnScaledTime()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        Scene scene=default;AsyncOperation unload=null;Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        RecoveredCashPromptWindow window=null;UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=null;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;target=new RenderTexture(1080,1920,24);target.Create();
            prepare=(loaded,mode)=>{if(loaded.name!="GameEntry")return;foreach(var root in loaded.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null){camera=entry.GetComponent<Canvas>().worldCamera;camera.targetTexture=target;}}};
            SceneManager.sceneLoaded+=prepare;yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            for(int i=0;i<200&&game.CoreRound==null;i++)yield return null;Assert.IsNotNull(game.CoreRound);
            game.CoreRound.FirstSpinGuide.Hide();
            window=Object.Instantiate(Resources.Load<RecoveredCashPromptWindow>("RecoveredUI/CashPromptWindow"),game.transform,false);
            foreach(var image in window.GetComponentsInChildren<Image>(true))if(image.name!="_WindowBg")Assert.IsNotNull(image.sprite,image.name);
            var order=new List<string>();window.SoundRequested+=sound=>order.Add(sound);
            window.Bind(camera,()=>{Assert.IsTrue(window.gameObject.activeSelf);Assert.IsFalse(window.Finger.gameObject.activeSelf);order.Add("withdraw");});
            window.Show(0,game.Rules,game.CurrentProfile.languageType,()=>{Assert.IsTrue(window.gameObject.activeSelf);order.Add("continue");});
            Assert.AreEqual(RecoveredCurrency.Format(game.Rules.GetCashOutCash(0),game.CurrentProfile.languageType,0),window.CashText.text);
            Assert.AreSame(window.ClaimButton.transform,window.Finger.parent);Assert.AreEqual(0,window.ClaimButton.onClick.GetPersistentEventCount());
            for(int i=0;i<16;i++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            var rect=(RectTransform)window.ClaimButton.transform;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);Assert.AreSame(window.ClaimButton,hits[0].gameObject.GetComponentInParent<Button>());
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-prompt.png"),capture.EncodeToPNG());
            Time.timeScale=0;ExecuteEvents.Execute(window.ClaimButton.gameObject,pointer,ExecuteEvents.pointerClickHandler);
            CollectionAssert.AreEqual(new[]{"congrats","click","continue","withdraw"},order);
            for(int i=0;i<6;i++)yield return null;Assert.IsTrue(window.gameObject.activeSelf);
            Time.timeScale=1;for(int i=0;i<10;i++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);Assert.AreEqual(4,order.Count);
            window.Show(1,game.Rules,game.CurrentProfile.languageType,()=>Assert.Fail("Cancelled window must not continue"));Assert.IsTrue(window.Finger.gameObject.activeSelf);
            window.Cancel();for(int i=0;i<10;i++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);
        }
        finally
        {
            if(window!=null)Object.Destroy(window.gameObject);if(prepare!=null)SceneManager.sceneLoaded-=prepare;
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
