using System;
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

public sealed class RecoveredBonusFlowTests
{
    [UnityTest]
    public IEnumerator RealSpinEntersBonusThroughTheAuthoredCameraStackAndWaitsForExit()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=UnityEngine.Random.state;
        Scene scene=default;RenderTexture target=null;Texture2D capture=null;Camera camera=null;AsyncOperation unload=null;
        var previous=RenderTexture.active;
        try{
            Time.timeScale=1;Time.captureDeltaTime=.025f;UnityEngine.Random.InitState(61);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;
            foreach(var root in scene.GetRootGameObjects()){var candidate=root.GetComponentInChildren<GameEntry>();if(candidate!=null)entry=candidate;}
            Assert.IsNotNull(entry);
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.BonusFlow==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            Assert.IsNotNull(entry.BonusFlow);var flow=entry.BonusFlow;var field=entry.Playfield;
            camera=entry.GetComponent<Canvas>().worldCamera;Assert.IsNotNull(camera);
            target=new RenderTexture(1080,1920,24);capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            camera.targetTexture=target;yield return null;Canvas.ForceUpdateCanvases();
            Assert.AreSame(field.transform.Find("QiPan"),field.Npc.Shake.Target);
            Exception failure=null;flow.Failed+=e=>failure=e;
            double began=0,secondCallback=0,completedAt=0;int completions=0;
            flow.PauseMusicRequested+=()=>{if(began==0)began=Time.timeAsDouble;};
            flow.ChangeMusicRequested+=s=>{if(s=="bonusBg")secondCallback=Time.timeAsDouble;};
            flow.Completed+=()=>{completions++;completedAt=Time.timeAsDouble;};
            entry.PlayerProgress.IsBonusGame=true;field.SpinButton.Button.onClick.Invoke();
            for(int i=0;i<1400&&!flow.IsRunning;i++){
                if(field.JackpotPopup.gameObject.activeInHierarchy)field.JackpotPopup.PlainButton.onClick.Invoke();
                if(field.BigWinPopup.gameObject.activeInHierarchy)field.BigWinPopup.PlainButton.onClick.Invoke();
                yield return null;
            }
            Assert.IsNull(field.Error);Assert.IsNull(failure);Assert.IsTrue(flow.IsRunning);
            field.SpinRecovery.MoreSpinButton.onClick.Invoke();Assert.IsTrue(entry.CoreRound.MoreSpins.gameObject.activeSelf);
            Assert.IsFalse(entry.PlayerProgress.IsBonusGame);CollectionAssert.AreEqual(new[]{0,0,0,0,0},entry.PlayerProgress.BonusArea);
            Assert.IsFalse(flow.Window.gameObject.activeSelf);Assert.AreEqual(2,field.Npc.State);
            for(int i=0;i<120&&!flow.Transition.IsPlaying;i++)yield return null;
            Assert.IsTrue(flow.Transition.IsPlaying);Assert.That(Time.timeAsDouble-began,Is.InRange(1.8,1.88));
            for(int i=0;i<16;i++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-flow-cover.png"),capture.EncodeToPNG());
            for(int i=0;i<120&&secondCallback==0;i++)yield return null;
            Assert.Greater(secondCallback,0);Assert.That(secondCallback-began,Is.InRange(4.8,4.95));
            Assert.IsTrue(flow.Window.gameObject.activeSelf);Assert.AreEqual(0,completions);
            Assert.Greater(flow.Window.GetComponent<Canvas>().sortingOrder,entry.CoreRound.MoreSpins.GetComponent<Canvas>().sortingOrder,"Bonus must open above the existing Popup window.");
            for(int i=0;i<25;i++)yield return null;
            for(int card=0;card<flow.Window.CardCount;card++){
                var position=RectTransformUtility.WorldToScreenPoint(camera,flow.Window.Card(card).transform.position);
                Assert.That(position.x,Is.InRange(0,1080));Assert.That(position.y,Is.InRange(0,1920));
            }
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-flow-window.png"),capture.EncodeToPNG());
            var reward=flow.Window.GetComponentInChildren<RecoveredBonusRewardPopup>(true);
            var jackpot=flow.Window.GetComponentInChildren<RecoveredJackpotPopup>(true);
            for(int card=0;card<entry.Rules.GetBonusFreeTimes();card++){
                Assert.IsTrue(ClickVisible(flow.Window.Card(card).Button,camera),"The top scene raycast must reach Bonus card "+card);
                Assert.AreEqual(card+1,flow.Window.Selection.Round.ClickedCount);
                float deadline=Time.realtimeSinceStartup+8;
                while(flow.Window.Selection.IsClicked&&Time.realtimeSinceStartup<deadline){
                    if(reward.gameObject.activeInHierarchy){Assert.Greater(reward.GetComponent<Canvas>().sortingOrder,flow.Window.GetComponent<Canvas>().sortingOrder);ClickVisible(reward.PlainButton,camera);}
                    if(jackpot.gameObject.activeInHierarchy){Assert.Greater(jackpot.GetComponent<Canvas>().sortingOrder,flow.Window.GetComponent<Canvas>().sortingOrder);ClickVisible(jackpot.PlainButton,camera);}
                    yield return null;
                }
                Assert.IsNull(failure);Assert.IsFalse(flow.Window.Selection.IsClicked);
            }
            float arrivalDeadline=Time.realtimeSinceStartup+5;
            while(entry.CashFlight.ActiveCashCount>0&&Time.realtimeSinceStartup<arrivalDeadline)yield return null;
            Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Assert.IsTrue(flow.Window.CloseButton.gameObject.activeInHierarchy);
            double closed=Time.timeAsDouble;Assert.IsTrue(ClickVisible(flow.Window.CloseButton,camera),"The top scene raycast must reach Bonus Close");
            for(int i=0;i<240&&completions==0;i++)yield return null;
            Assert.AreEqual(1,completions);Assert.IsFalse(flow.IsRunning);Assert.IsFalse(flow.Window.gameObject.activeSelf);
            Assert.That(completedAt-closed,Is.InRange(3.7,3.9));
            // Bonus completion precedes the native Bank and review Update waits.
            for(int i=0;i<6&&field.IsBusy;i++){RecoveredCorePromptDriver.ClaimAndClose(entry.CoreRound);yield return null;}
            Assert.IsFalse(field.IsBusy,"The production no-Free core continuation must release the round after its end-flow waits");
            Assert.IsNull(failure);Assert.IsNull(field.Error);
            // The no-Bonus branch returns synchronously without replaying presentation.
            began=0;flow.Begin();Assert.AreEqual(2,completions);Assert.AreEqual(0,began);
        }finally{
            Time.timeScale=scale;Time.captureDeltaTime=delta;UnityEngine.Random.state=random;RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;
            if(capture!=null)Object.Destroy(capture);if(target!=null)Object.Destroy(target);
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static bool ClickVisible(Button button,Camera camera)
    {
        if(!button.gameObject.activeInHierarchy||!button.IsInteractable())return false;
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
        var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
            position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);
        // A reward button can still be hidden by its authored reveal or entrance.
        // Deliver only to the actual top hit; never bypass a covering window.
        if(hits.Count==0||ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)!=button.gameObject)return false;
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);return true;
    }
}
