using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredCoreCashPromptTests
{
    [UnityTest] public IEnumerator QualifyingSpinWaitsForClaimBeforeOpeningCashAndContinuingGuide()=>Run(false);
    [UnityTest] public IEnumerator GmSwitchCancelsThePendingPromptAndItsDiscardedCallbacks()=>Run(true);
    private static IEnumerator Run(bool switchProfile)
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var core=game.CoreRound;var field=game.Playfield;int completed=0;string timeline="";core.CoreRoundCompleted+=()=>{completed++;timeline+=" end="+game.PlayerProgress.GreenCount;};game.BonusFlow.Completed+=()=>timeline+=" bonus="+game.PlayerProgress.GreenCount;game.PlayerProgress.GreenCountChanged+=(before,after)=>timeline+=" credit="+before+"->"+after;
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;yield return null;
            // Qualify before settlement; some native reward flights credit only after the end check.
            game.PlayerProgress.SetGreenCount(game.Rules.GetCashOutCash(0));
            field.SpinButton.Button.onClick.Invoke();
            for(int frame=0;frame<1800&&!core.IsCashPromptPending;frame++) {
                Claim(field.BigWinPopup.PlainButton);Claim(field.JackpotPopup.PlainButton);yield return null;
            }
            Assert.IsNull(core.Error);Assert.IsTrue(core.IsCashPromptPending,timeline+" balance="+game.PlayerProgress.GreenCount+" threshold="+game.Rules.GetCashOutCash(0)+" completed="+completed+" busy="+field.IsBusy+" store="+JsonUtility.ToJson(game.PlayerStore.Data));Assert.IsNotNull(core.CashPrompt);
            Assert.IsNull(core.CashOut);Assert.AreEqual(0,completed);Assert.IsTrue(field.IsBusy);Assert.IsTrue(field.AwaitingRewards);
            Assert.IsFalse(core.MoreWild.gameObject.activeSelf);Assert.AreEqual(2,game.PlayerStore.Data.GuideStep);
            Assert.AreEqual(RecoveredCurrency.Format(game.Rules.GetCashOutCash(0),game.CurrentProfile.languageType,0),core.CashPrompt.CashText.text);
            CollectionAssert.AreEqual(new[]{0},game.PlayerStore.Data.CashOutTipIndexs);
            deadline=Time.realtimeSinceStartup+8;while(game.CashFlight.ActiveCashCount>0&&Time.realtimeSinceStartup<deadline)yield return null;
            for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(0,game.CashFlight.ActiveCashCount);
            string beforeClaim=PlayerPrefs.GetString(key);game.PlayerStore.Data.IsVibrate=!game.PlayerStore.Data.IsVibrate;
            for(int i=0;i<10;i++)yield return null;
            Assert.AreEqual(beforeClaim,PlayerPrefs.GetString(key));Assert.AreEqual(0,completed);Assert.IsTrue(field.IsBusy);
            var prompt=core.CashPrompt;
            if(switchProfile)
            {
                game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
                Assert.IsFalse(prompt.gameObject.activeSelf);Assert.IsFalse(core.IsCashPromptPending);
                prompt.ClaimButton.onClick.Invoke(); // Even an already queued click cannot open the old context.
                Assert.IsNull(core.CashOut);
                deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==core)&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.AreNotSame(core,game.CoreRound);for(int i=0;i<10;i++)yield return null;
                Assert.AreEqual(0,completed);Assert.IsNull(game.CoreRound.CashPrompt);Assert.IsNull(game.CoreRound.CashOut);
            }
            else
            {
                Canvas.ForceUpdateCanvases();Assert.AreSame(prompt.ClaimButton,Hit(prompt.ClaimButton.transform,camera));
                int clicks=0;core.SoundRequested+=name=>{if(name=="click")clicks++;};
                prompt.ClaimButton.onClick.Invoke();
                Assert.AreEqual(1,clicks);Assert.IsFalse(core.IsCashPromptPending);
                Assert.IsNotNull(core.CashOut);Assert.IsTrue(core.CashOut.gameObject.activeSelf);
                Assert.AreEqual(0,completed,"The claim opens CashOut synchronously; the wait resumes on Update.");
                Assert.IsTrue(field.IsBusy);Assert.IsFalse(core.MoreWild.gameObject.activeSelf);
                for(int i=0;i<10&&completed==0;i++)yield return null;
                Assert.AreEqual(1,completed);Assert.IsFalse(field.IsBusy);Assert.IsFalse(field.AwaitingRewards);
                Assert.IsTrue(core.MoreWild.gameObject.activeSelf);
                Assert.Greater(core.MoreWild.transform.GetSiblingIndex(),core.CashOut.transform.GetSiblingIndex());
                Assert.AreEqual(JsonUtility.ToJson(game.PlayerStore.Data),PlayerPrefs.GetString(key));
                Assert.AreEqual(-1,game.PlayerProgress.PrepareCashOutPrompt(),"The first unrecorded tier already prompted blocks later tiers.");
                for(int i=0;i<80;i++)yield return null;Assert.IsFalse(prompt.gameObject.activeSelf);
                RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
                capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-core-cash-guide.png"),capture.EncodeToPNG());
                Assert.AreSame(core.MoreWild.ClaimButton,Hit(core.MoreWild.ClaimButton.transform,camera),lastHit);
                core.MoreWild.ClaimButton.onClick.Invoke();for(int i=0;i<15;i++)yield return null;
                Assert.AreSame(core.CashOut.BackButton,Hit(core.CashOut.BackButton.transform,camera));
                core.CashOut.BackButton.onClick.Invoke();for(int i=0;i<15;i++)yield return null;
                Assert.IsFalse(core.CashOut.gameObject.activeSelf);Assert.AreEqual(3,game.PlayerStore.Data.GuideStep);
                int spins=game.PlayerProgress.SpinCount;field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(spins-1,game.PlayerProgress.SpinCount);
            }
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;
            RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(capture!=null)Object.Destroy(capture);if(target!=null){target.Release();Object.Destroy(target);}
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button){if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static string lastHit;
    private static Button Hit(Transform target,Camera camera)
    {
        Canvas.ForceUpdateCanvases();var rect=(RectTransform)target;
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
        lastHit="";foreach(var hit in hits){lastHit+=hit.gameObject.name+" parent="+hit.gameObject.transform.parent.name+" order="+hit.sortingOrder+" depth="+hit.depth+"; ";}
        return hits[0].gameObject.GetComponentInParent<Button>();
    }
}
