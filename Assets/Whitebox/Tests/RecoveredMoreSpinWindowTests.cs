using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class RecoveredMoreSpinWindowTests
{
    [UnityTest]
    public IEnumerator AuthoredWindowRetriesFailedAdAndCreditsOnSuccess()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);game.PlayerProgress.SetSpinCount(0);
            var window=Object.Instantiate(Resources.Load<RecoveredMoreSpinWindow>("RecoveredUI/MoreSpinWindow"),game.transform,false);
            window.Bind(game);window.Show();for(int frame=0;frame<10;frame++)yield return null;
            Assert.AreEqual("+"+game.Rules.GetAddSpins(),window.Amount.text);
            Assert.IsNotNull(window.ClaimButton.targetGraphic);Assert.AreEqual(0,window.ClaimButton.onClick.GetPersistentEventCount());
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Canvas.ForceUpdateCanvases();
            var background=window.transform.Find("_WindowBg").GetComponent<Button>();
            Assert.AreEqual(new Color(0,0,0,.65f),background.targetGraphic.color);
            Assert.AreEqual(background.gameObject,Hit(game.Playfield.SpinButton.transform,camera));
            ExecuteEvents.Execute(background.gameObject,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
            Assert.IsTrue(window.gameObject.activeSelf);Assert.IsFalse(game.Ads.Pending);
            Assert.AreEqual(window.ClaimButton.gameObject,Hit(window.ClaimButton.transform,camera));
            Assert.AreEqual(window.CloseButton.gameObject,Hit(window.CloseButton.transform,camera));
            Assert.AreEqual(2000,game.CoreRound.Tips.GetComponent<Canvas>().sortingOrder);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-more-spin.png"),capture.EncodeToPNG());
            window.ClaimButton.onClick.Invoke();window.CloseButton.onClick.Invoke();Assert.IsTrue(game.Ads.Pending);Assert.IsTrue(window.gameObject.activeSelf);
            game.Ads.Complete(AdOutcome.Failed);Assert.IsFalse(window.IsClicked);Assert.AreEqual(0,game.PlayerProgress.SpinCount);
            window.ClaimButton.onClick.Invoke();game.Ads.Complete(AdOutcome.Rewarded);
            Assert.AreEqual(Mathf.Min(game.Rules.GetAddSpins(),game.Rules.GetMaxSpinCount()),game.PlayerProgress.SpinCount);
            for(int frame=0;frame<10;frame++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);
            Assert.AreEqual(game.Playfield.SpinButton.Button.gameObject,Hit(game.Playfield.SpinButton.transform,camera));
            window.Show();for(int frame=0;frame<10;frame++)yield return null;window.CloseButton.onClick.Invoke();
            for(int frame=0;frame<10;frame++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);Assert.IsFalse(game.Ads.Pending);
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static GameObject Hit(Transform target,Camera camera)
    {
        var rect=(RectTransform)target;
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        Assert.IsNotEmpty(hits,"Expected an actual UI raycast hit at "+target.name);
        var button=hits[0].gameObject.GetComponentInParent<Button>();
        return button==null?hits[0].gameObject:button.gameObject;
    }
}
