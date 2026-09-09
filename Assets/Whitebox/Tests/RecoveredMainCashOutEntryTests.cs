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
using Object=UnityEngine.Object;

public sealed class RecoveredMainCashOutEntryTests
{
    [UnityTest]
    public IEnumerator MainButtonOpensCachedCashOutAndGmRemovesOldBinding()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,previous=RenderTexture.active;Texture2D capture=null;
        try{
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null)game=entry;}
            Assert.IsNotNull(game);yield return null;var core=game.CoreRound;Assert.IsNotNull(core);
            core.FirstSpinGuide.Hide();game.PlayerStore.Data.GuideStep=3;game.PlayerStore.Save();
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;yield return null;
            var entryView=core.CashOutEntry;var button=entryView.Button;
            Assert.AreEqual("Tubiao",button.transform.parent.name);
            var adapt=entryView.GetComponent<RecoveredScreenAdapt>();Assert.IsNotNull(adapt);
            var rootRect=(RectTransform)entryView.transform;var scaler=game.GetComponentInParent<CanvasScaler>();
            float factor=scaler.matchWidthOrHeight*scaler.referenceResolution.y/1920-scaler.referenceResolution.x*(scaler.matchWidthOrHeight-1)/1080;
            adapt.Apply(1080,1920,new Rect(24,60,1032,1740));
            Assert.AreEqual(24*factor,rootRect.offsetMin.x,.0002f);Assert.AreEqual(-120*factor,rootRect.offsetMax.y,.0002f);
            // Exercise the actual raycast while the source Node is inset.
            Click(button,camera);Assert.IsNotNull(core.CashOut);for(int i=0;i<10;i++)yield return null;
            Click(core.CashOut.BackButton,camera);for(int i=0;i<10;i++)yield return null;
            Assert.IsFalse(core.CashOut.gameObject.activeSelf);adapt.AdaptScreen();
            Assert.AreEqual("CashOutb",button.name);Assert.AreSame(button.transform,entryView.FingerTarget.parent);
            Assert.AreEqual(new Vector2(200,205),((RectTransform)button.transform).sizeDelta);
            Assert.AreEqual(0,button.onClick.GetPersistentEventCount());
            Assert.IsNull(entryView.Finger);
            float threshold=game.Rules.GetCashOutCash(0);
            game.PlayerProgress.SetGreenCount(threshold);Assert.IsNull(entryView.Finger,"Native handler observes the old balance.");
            game.PlayerProgress.SetGreenCount(threshold);var finger=entryView.Finger;Assert.IsNotNull(finger);
            Assert.IsTrue(finger.gameObject.activeSelf);Assert.AreSame(entryView.FingerTarget,finger.parent);
            Assert.AreEqual(Vector2.zero,finger.anchoredPosition);Assert.AreEqual(Vector3.one,finger.localScale);
            game.PlayerProgress.SetGreenCount(0);game.PlayerProgress.SetGreenCount(0);
            Assert.IsTrue(finger.gameObject.activeSelf,"Below threshold does not hide an existing finger.");
            var records=game.PlayerStore.Data.PlayerCashOutDatas;
            for(int tier=0;tier<game.Rules.GetCashOutCount();tier++)records.Add(new DragonLegend.Whitebox.Recovered.PlayerCashOutData{id=tier});
            game.PlayerProgress.SetGreenCount(0);Assert.IsFalse(finger.gameObject.activeSelf,"Every recorded tier hides the finger regardless of record state.");
            records.Clear();game.PlayerProgress.SetGreenCount(threshold);game.PlayerProgress.SetGreenCount(threshold);
            Assert.AreSame(finger,entryView.Finger);Assert.IsTrue(finger.gameObject.activeSelf);
            for(int frame=0;frame<6;frame++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-cashout-finger.png"),capture.EncodeToPNG());
            // Restore a fresh, empty local record for the independent window-open checks.
            for(int tier=0;tier<game.Rules.GetCashOutCount();tier++)records.Add(new DragonLegend.Whitebox.Recovered.PlayerCashOutData{id=tier});
            game.PlayerProgress.SetGreenCount(0);records.Clear();game.PlayerStore.Save();
            var sounds=new List<string>();core.SoundRequested+=sounds.Add;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-cashout-entry.png"),capture.EncodeToPNG());
            Click(button,camera);Assert.IsNotNull(core.CashOut);Assert.AreEqual("click",sounds[0]);
            var window=core.CashOut;Assert.IsTrue(window.gameObject.activeSelf);Assert.AreSame(core.PopupRoot,window.transform.parent);
            for(int i=0;i<10;i++)yield return null;
            int depth=window.GetComponent<Canvas>().sortingOrder;
            button.onClick.Invoke();Assert.AreSame(window,core.CashOut);Assert.AreEqual(depth,window.GetComponent<Canvas>().sortingOrder);
            Click(window.BackButton,camera);for(int i=0;i<10;i++)yield return null;Assert.IsFalse(window.gameObject.activeSelf);
            Click(button,camera);Assert.AreSame(window,core.CashOut);for(int i=0;i<10;i++)yield return null;
            game.transform.Find("SelectAlternative").GetComponent<Button>().onClick.Invoke();
            int count=sounds.Count;button.onClick.Invoke();Assert.AreEqual(count,sounds.Count,"Old profile must not react after release.");
            float deadline=Time.realtimeSinceStartup+10;
            while((game.CoreRound==null||game.CoreRound==core)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;Assert.IsTrue(core==null);Assert.IsTrue(window==null);Assert.IsTrue(entryView==null);
            Assert.IsNotNull(game.CoreRound.CashOutEntry);Assert.IsNull(game.CoreRound.CashOut);
        }finally{
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Click(Button button,Camera camera)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
        Assert.AreEqual(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
}
