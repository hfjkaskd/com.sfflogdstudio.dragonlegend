using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCashOutWindowTests
{
    [UnityTest]
    public IEnumerator CurrentMainContextShowsClosesAndReopensCashWindowWithReusedLists()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;RecoveredCashOutWindow window=null;
        try {
            Time.timeScale=0;Time.captureDeltaTime=.025f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var canvas=game.GetComponent<Canvas>();camera=canvas.worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;yield return null;
            window=Object.Instantiate(Resources.Load<RecoveredCashOutWindow>("RecoveredUI/CashOutWindow"),game.transform,false);
            window.Bind(game.Rules,game.PlayerProgress,game.CurrentProfile.languageType,canvas,()=>100);
            var blocker=window.transform.Find("_WindowBg").GetComponent<Button>();
            Assert.AreEqual(Color.clear,blocker.targetGraphic.color);Assert.IsTrue(blocker.targetGraphic.raycastTarget);
            Assert.AreEqual(0,blocker.onClick.GetPersistentEventCount());
            var header=window.GetComponent<RecoveredCashOutPaymentHeader>();int clicks=0;window.SoundRequested+=name=>{Assert.AreEqual("click",name);clicks++;};
            Time.timeScale=1;window.Show();Assert.IsTrue(window.gameObject.activeSelf);Assert.AreEqual(Vector3.zero,window.Content.localScale);
            yield return null;var pausedScale=window.Content.localScale;Assert.Greater(pausedScale.x,0);Time.timeScale=0;
            for(int i=0;i<3;i++)yield return null;Assert.AreEqual(pausedScale,window.Content.localScale);
            Assert.IsTrue(window.List.IsCreateFinished);var first=window.List.ItemAt(0);Assert.IsNotNull(first,"list="+((RectTransform)window.List.transform).rect+" parent="+((RectTransform)window.List.transform.parent).rect+" scroll="+window.List.Scroll.content.localPosition+" created="+window.List.CreatedCount);
            Time.timeScale=1;for(int i=0;i<30;i++)yield return null;
            Assert.AreEqual(Vector3.one,window.Content.localScale);Assert.AreEqual(Vector3.one,first.transform.localScale);
            int created=window.List.CreatedCount;header.RefreshPaymentType(3);window.Mode.GiftButton.onClick.Invoke();yield return null;
            var gift=window.GetComponentInChildren<RecoveredGiftList>().Item;Assert.IsNotNull(gift);
            window.BackButton.onClick.Invoke();Assert.IsTrue(window.gameObject.activeSelf);
            Time.timeScale=0;for(int i=0;i<4;i++)yield return null;Assert.IsTrue(window.gameObject.activeSelf);Assert.AreEqual(Vector3.one,window.Content.localScale);
            Time.timeScale=1;for(int i=0;i<20;i++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);
            window.Show();Assert.IsFalse(window.Mode.IsGift);Assert.AreEqual(1,header.PaymentType);
            Assert.AreSame(window.transform.Find("Content/Node/Layout/PaypalBtn"),header.PaymentFrame.parent);
            for(int i=0;i<30;i++)yield return null;
            Assert.AreEqual(created,window.List.CreatedCount);Assert.AreSame(first,window.List.ItemAt(0));Assert.AreSame(first.transform,window.List.SelectionFrame.parent);
            Assert.AreEqual(2,clicks);window.Show();Assert.AreEqual(Vector3.one,window.Content.localScale,"Showing an already open window must not restart it.");
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-window.png"),capture.EncodeToPNG());
            window.Mode.GiftButton.onClick.Invoke();yield return null;Assert.AreSame(gift,window.GetComponentInChildren<RecoveredGiftList>().Item);
            window.Unbind();Assert.IsFalse(window.gameObject.activeSelf);int previousClicks=clicks;window.Mode.CashButton.onClick.Invoke();Assert.AreEqual(previousClicks,clicks,"Unbind detaches discarded-window audio.");
        } finally {
            if(window!=null)Object.Destroy(window.gameObject);Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(capture!=null)Object.Destroy(capture);if(target!=null){target.Release();Object.Destroy(target);}
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
