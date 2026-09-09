using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredMainCashOutStatusTests
{
    [UnityTest]
    public IEnumerator MainStatusRepeatsWithFreshTextAndModeChangesCancelItsClock()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,previous=RenderTexture.active;Texture2D capture=null;
        try{
            Time.timeScale=1;Time.captureDeltaTime=.1f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var status=game.CoreRound.CashOutStatus;var mode=game.Playfield.ModeView;
            Assert.IsTrue(status.IsScheduled);Assert.IsFalse(status.Panel.gameObject.activeSelf);
            Assert.AreEqual(new Vector2(251,-116.04309f),status.Panel.anchoredPosition);
            Assert.AreEqual(new Vector2(359.5136f,113.80737f),status.Panel.sizeDelta);
            Assert.IsNotNull(status.Panel.GetComponent<Image>().sprite);
            var adapt=status.GetComponent<RecoveredScreenAdapt>();Assert.IsNotNull(adapt,"Original Node owns Adapt.");
            var statusRect=(RectTransform)status.transform;var scaler=game.GetComponentInParent<CanvasScaler>();
            Assert.IsNotNull(scaler);float factor=scaler.matchWidthOrHeight*scaler.referenceResolution.y/1920-scaler.referenceResolution.x*(scaler.matchWidthOrHeight-1)/1080;
            adapt.Apply(1080,1920,new Rect(24,60,1032,1740));
            Assert.AreEqual(24*factor,statusRect.offsetMin.x,.0002f);Assert.AreEqual(60*factor,statusRect.offsetMin.y,.0002f);
            Assert.AreEqual(-24*factor,statusRect.offsetMax.x,.0002f);Assert.AreEqual(-120*factor,statusRect.offsetMax.y,.0002f);
            Assert.AreEqual(new Vector2(251,-116.04309f),status.Panel.anchoredPosition,"Adapt changes the source parent, not child layout.");
            adapt.AdaptScreen();
            mode.ApplyCurrent();string original=status.Label.text;
            game.PlayerProgress.SetGreenCount(game.Rules.GetCashOutCash(0));
            for(int i=0;i<110;i++)yield return null;
            Assert.IsFalse(status.Panel.gameObject.activeSelf);Assert.AreEqual(original,status.Label.text);
            deadline=Time.realtimeSinceStartup+5;
            while(!status.Panel.gameObject.activeSelf&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(status.Panel.gameObject.activeSelf);Assert.AreEqual(Vector3.one,status.Panel.localScale,"First reveal retains source unit scale.");
            Assert.AreEqual(original,status.Label.text,"Balance changes during the delay do not refresh this cycle.");
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-cashout-status.png"),capture.EncodeToPNG());
            deadline=Time.realtimeSinceStartup+5;
            while(status.Panel.gameObject.activeSelf&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsFalse(status.Panel.gameObject.activeSelf);Assert.AreEqual(Vector3.zero,status.Panel.localScale);
            Assert.AreEqual("You Can Cash Out Now!",status.Label.text);Assert.IsTrue(status.IsScheduled);
            deadline=Time.realtimeSinceStartup+5;
            while(!status.Panel.gameObject.activeSelf&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(status.Panel.gameObject.activeSelf);Assert.Less(status.Panel.localScale.x,1,"Repeated reveal grows from the previous zero scale.");
            Vector3 interrupted=status.Panel.localScale;
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Free;mode.ApplyCurrent();
            Assert.IsFalse(status.Panel.gameObject.activeSelf);Assert.IsFalse(status.IsScheduled);
            for(int i=0;i<190;i++)yield return null;
            Assert.IsFalse(status.Panel.gameObject.activeSelf);Assert.AreEqual(interrupted,status.Panel.localScale);
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Base;mode.ApplyCurrent();Assert.IsTrue(status.IsScheduled);
            for(int tier=0;tier<game.Rules.GetCashOutCount();tier++)game.PlayerStore.Data.PlayerCashOutDatas.Add(new PlayerCashOutData{id=tier});
            mode.ApplyCurrent();Assert.IsFalse(status.IsScheduled);Assert.IsFalse(status.Panel.gameObject.activeSelf);
            game.PlayerStore.Data.PlayerCashOutDatas.Clear();mode.ApplyCurrent();Assert.IsTrue(status.IsScheduled);
            game.transform.Find("SelectAlternative").GetComponent<Button>().onClick.Invoke();Assert.IsFalse(status.IsScheduled);
            deadline=Time.realtimeSinceStartup+10;while(status!=null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsTrue(status==null);
        }finally{
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
