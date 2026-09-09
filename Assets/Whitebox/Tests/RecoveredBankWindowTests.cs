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

public sealed class RecoveredBankWindowTests
{
    [UnityTest]
    public IEnumerator ActualWindowSelectsPaysContinuesAndClosesUsingAuthoredButtons()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;RecoveredBankWindow window=null;
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=null;
        try
        {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(83);
            target=new RenderTexture(1080,1920,24);target.Create();
            prepare=(loaded,mode)=>{if(loaded.name!="GameEntry")return;foreach(var root in loaded.GetRootGameObjects()){var entry=root.GetComponentInChildren<GameEntry>();if(entry!=null){camera=entry.GetComponent<Canvas>().worldCamera;camera.targetTexture=target;}}};
            SceneManager.sceneLoaded+=prepare;yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);game.CoreRound.FirstSpinGuide.Hide();
            window=game.CoreRound.Bank;Assert.IsNotNull(window);
            var meter=game.Playfield.BankProgress;Assert.IsNotNull(meter);
            Assert.AreEqual("0/"+game.Rules.GetBankSpinCD(),meter.Label.text);
            Assert.AreSame(meter.Button.transform,meter.Fill.transform.parent.parent);
            Assert.AreEqual(0,meter.Button.onClick.GetPersistentEventCount());
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreSame(meter.Button.gameObject,Hit(meter.Button.transform,camera));
            var beforeTip=Random.state;Click(meter.Button.gameObject);
            Assert.AreEqual("Need more spins to trigger the rewards.",game.CoreRound.Tips.Label.text);Assert.IsTrue(game.CoreRound.Tips.gameObject.activeSelf);
            Assert.IsFalse(window.gameObject.activeSelf);Assert.AreEqual(beforeTip,Random.state);
            game.CoreRound.Tips.Cancel();
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Free;game.Playfield.ModeView.ApplyCurrent();Assert.IsFalse(meter.gameObject.activeInHierarchy);
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Base;game.Playfield.ModeView.ApplyCurrent();Assert.IsTrue(meter.gameObject.activeInHierarchy);
            int flights=0,closed=0;float yAtClose=0;string sound=null;
            window.SoundRequested+=value=>sound=value;
            float bankStart=game.PlayerProgress.GreenCount,bankRewards=0;
            window.FlyRequested+=(amount,callback,source)=>{flights++;bankRewards+=amount;};
            game.PlayerProgress.SetBankCount(1);
            Assert.AreEqual("1/"+game.Rules.GetBankSpinCD(),meter.Label.text);Assert.AreEqual(Mathf.Clamp01(1f/game.Rules.GetBankSpinCD()),meter.Fill.fillAmount);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bank-progress.png"),capture.EncodeToPNG());
            window.Show(()=>{closed++;yAtClose=window.Item(0).transform.localPosition.y;});
            Assert.AreEqual(0,game.PlayerProgress.BankCount);Assert.AreEqual("jump",sound);
            Assert.AreEqual("0/"+game.Rules.GetBankSpinCD(),meter.Label.text);Assert.AreEqual(0,meter.Fill.fillAmount);
            Assert.AreEqual(4,window.Item(0).Ball.Selected);Assert.AreEqual(3,window.Item(1).Ball.Selected);Assert.AreEqual(5,window.Item(2).Ball.Selected);
            for(int i=0;i<12;i++)yield return null;
            Assert.IsTrue(window.Finger.gameObject.activeSelf);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreSame(window.Item(0).Button.gameObject,Hit(window.Item(0).Button.transform,camera));
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bank-window.png"),capture.EncodeToPNG());
            yield return ClickVisible(window.Item(0).Button,camera);Assert.IsFalse(window.Finger.gameObject.activeSelf);Assert.IsFalse(game.Ads.Pending);
            for(int i=0;i<160&&!window.Item(1).Ad.gameObject.activeSelf;i++)yield return null;
            Assert.AreEqual(1,flights);Assert.IsTrue(window.Item(1).Ad.gameObject.activeSelf);Assert.IsTrue(window.Item(2).Ad.gameObject.activeSelf);
            for(int i=0;i<25;i++)yield return null;
            Assert.AreSame(window.OpenButton.transform,window.Finger.parent);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreSame(window.OpenButton.gameObject,Hit(window.OpenButton.transform,camera));
            float beforeFailed=game.PlayerProgress.GreenCount;
            yield return ClickVisible(window.Item(1).Button,camera);Assert.IsTrue(game.Ads.Pending);
            Assert.AreEqual("itembank",game.Ads.Scene);
            yield return ClickVisible(game.AdControls.FailureButton,camera);
            CollectionAssert.AreEqual(new[]{0},window.Selection.Selected);
            Assert.AreEqual(beforeFailed,game.PlayerProgress.GreenCount);Assert.AreEqual(1,flights);
            yield return ClickVisible(window.Item(1).Button,camera);
            yield return ClickVisible(game.AdControls.RewardButton,camera);
            for(int i=0;i<160&&flights<2;i++)yield return null;
            for(int i=0;i<160&&game.CashFlight.ActiveCashCount>0;i++)yield return null;
            Assert.AreEqual(0,game.CashFlight.ActiveCashCount,"Wait for the second reward to arrive before opening the last ball.");
            Assert.AreEqual(2,flights);Assert.IsFalse(window.Selection.IsContinue);Assert.IsTrue(window.gameObject.activeSelf);
            beforeFailed=game.PlayerProgress.GreenCount;
            yield return ClickVisible(window.OpenButton,camera);Assert.IsTrue(game.Ads.Pending);
            Assert.AreEqual("Openbank",game.Ads.Scene);
            yield return ClickVisible(game.AdControls.FailureButton,camera);Assert.IsFalse(window.Selection.IsContinue);
            Assert.AreEqual(beforeFailed,game.PlayerProgress.GreenCount);Assert.AreEqual(2,flights);
            yield return ClickVisible(window.OpenButton,camera);
            yield return ClickVisible(game.AdControls.RewardButton,camera);
            for(int i=0;i<180&&closed==0;i++)yield return null;
            Assert.AreEqual(3,flights);Assert.AreEqual(1,closed);Assert.IsFalse(window.gameObject.activeSelf);
            Assert.AreEqual(bankStart+bankRewards,game.PlayerProgress.GreenCount,.001f,"Each bank flight pays once; failed ads pay nothing.");
            Assert.AreNotEqual(yAtClose,window.Item(0).transform.localPosition.y,"Native close continuation precedes float restoration.");
            window.Show(()=>closed++);for(int i=0;i<12;i++)yield return null;
            window.Item(0).Button.onClick.Invoke();for(int i=0;i<160&&!window.Item(1).Ad.gameObject.activeSelf;i++)yield return null;
            for(int i=0;i<15;i++)yield return null;
            Click(window.LeaveButton.gameObject);Assert.AreEqual(1,game.Ads.InterstitialCount);
            for(int i=0;i<10;i++)yield return null;Assert.AreEqual(2,closed);Assert.IsFalse(window.gameObject.activeSelf);
            int rounds=0;game.CoreRound.CoreRoundCompleted+=()=>rounds++;
            var reviewSounds=new List<string>();var audio=game.CoreAudio.Manager;
            // Clear before the production receiver, so old bank sounds cannot hide a missing binding.
            game.CoreAudio.Unbind();game.CoreRound.SoundRequested+=name=>audio.StopSound();game.CoreAudio.Bind(game);
            game.CoreRound.SoundRequested+=name=>{
                reviewSounds.Add(name);Assert.AreEqual(game.PlayerStore.Data.IsMusic,audio.SoundSource.isPlaying);
            };
            Assert.Less(game.PlayerProgress.Level,game.Rules.GetReview());
            game.PlayerStore.Data.Level=game.Rules.GetReview()-1;
            game.PlayerProgress.SetExperience(game.Rules.GetNeedPro(game.PlayerProgress.Level));
            Assert.IsNull(game.CoreRound.Review,"ReviewRequested only marks the pending event.");
            game.PlayerProgress.SetBankCount(game.Rules.GetBankSpinCD()-1);
            game.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(game.Rules.GetBankSpinCD()+"/"+game.Rules.GetBankSpinCD(),meter.Label.text);Assert.AreEqual(1,meter.Fill.fillAmount);
            Assert.IsTrue(game.Playfield.IsBusy);Assert.IsFalse(window.gameObject.activeSelf,"BankReady only marks the pending event during Spin entry.");
            game.Playfield.SpinRecovery.MoreSpinButton.onClick.Invoke();
            Assert.IsTrue(game.CoreRound.MoreSpins.gameObject.activeSelf);
            for(int i=0;i<1800&&!window.gameObject.activeSelf;i++)
            {
                Claim(game.Playfield.BigWinPopup.PlainButton);Claim(game.Playfield.JackpotPopup.PlainButton);yield return null;
            }
            Assert.IsNull(game.CoreRound.Error);Assert.IsTrue(window.gameObject.activeSelf);Assert.IsTrue(game.Playfield.IsBusy);Assert.AreEqual(0,rounds);
            Assert.Greater(window.GetComponent<Canvas>().sortingOrder,game.CoreRound.MoreSpins.GetComponent<Canvas>().sortingOrder,"Pending Bank opens above an existing More Spin window.");
            for(int i=0;i<12;i++)yield return null;
            Click(window.Item(0).Button.gameObject);for(int i=0;i<160&&!window.Item(1).Ad.gameObject.activeSelf;i++)yield return null;
            for(int i=0;i<15;i++)yield return null;Click(window.LeaveButton.gameObject);
            for(int i=0;i<20&&game.CoreRound.Review==null;i++)yield return null;
            var review=game.CoreRound.Review;Assert.IsNotNull(review);Assert.IsTrue(review.gameObject.activeSelf);
            Assert.AreEqual(0,rounds);Assert.IsTrue(game.Playfield.IsBusy);Assert.IsFalse(game.CoreRound.MoreWild.gameObject.activeSelf);
            CollectionAssert.AreEqual(new[]{"remind"},reviewSounds);
            Assert.Greater(review.GetComponent<Canvas>().sortingOrder,game.CoreRound.MoreSpins.GetComponent<Canvas>().sortingOrder);
            for(int i=0;i<10;i++)yield return null;
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreSame(review.Star(3).gameObject,Hit(review.Star(3).transform,camera));Click(review.Star(3).gameObject);
            Assert.AreEqual(4,review.StarCount);
            game.PlayerStore.Data.IsMusic=false;Click(review.Star(1).gameObject);Assert.AreEqual(2,review.StarCount);
            game.PlayerStore.Data.IsMusic=true;Click(review.Star(3).gameObject);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-review-window.png"),capture.EncodeToPNG());
            Assert.AreSame(review.ClaimButton.gameObject,Hit(review.ClaimButton.transform,camera));Click(review.ClaimButton.gameObject);
            CollectionAssert.AreEqual(new[]{"remind","click","click","click","click"},reviewSounds);
            for(int i=0;i<20&&rounds==0;i++){RecoveredCorePromptDriver.ClaimAndClose(game.CoreRound);yield return null;}
            Assert.AreEqual(1,rounds);Assert.IsFalse(game.Playfield.IsBusy);Assert.IsFalse(window.gameObject.activeSelf);
            Assert.IsTrue(game.CoreRound.MoreWild.gameObject.activeSelf,"The first-spin ExtraWild guide comes after Bank closes.");
            var oldPlayer=game.PlayerProgress;var oldCore=game.CoreRound;string oldLabel=meter.Label.text;
            int oldSoundCount=reviewSounds.Count;
            game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();oldPlayer.SetBankCount(2);
            review.Star(0).onClick.Invoke();Assert.AreEqual(oldSoundCount,reviewSounds.Count,"Discarded review must detach from core audio immediately.");
            Assert.AreEqual(oldLabel,meter.Label.text,"GM teardown detaches the old progress subscriptions immediately.");
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==oldCore)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.AreNotSame(oldCore,game.CoreRound);Assert.AreEqual("2/"+game.Rules.GetBankSpinCD(),game.Playfield.BankProgress.Label.text);
        }
        finally
        {
            if(window!=null)window.Cancel();if(prepare!=null)SceneManager.sceneLoaded-=prepare;
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Click(GameObject obj)=>ExecuteEvents.Execute(obj,new PointerEventData(EventSystem.current),ExecuteEvents.pointerClickHandler);
    private static IEnumerator ClickVisible(Button button,Camera camera)
    {
        float deadline=Time.realtimeSinceStartup+5;
        var hits=new List<RaycastResult>();
        while(Time.realtimeSinceStartup<deadline)
        {
            if(button.gameObject.activeInHierarchy&&button.IsInteractable())
            {
                Canvas.ForceUpdateCanvases();var rect=(RectTransform)button.transform;
                var pointer=new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left,
                    position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
                hits.Clear();EventSystem.current.RaycastAll(pointer,hits);
                if(hits.Count>0&&ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject)==button.gameObject)
                {
                    ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);yield break;
                }
            }
            yield return null;
        }
        Assert.Fail("Bank button must be the top scene raycast target: "+button.name);
    }
    private static void Claim(Button button){if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static GameObject Hit(Transform target,Camera camera)
    {
        var rect=(RectTransform)target;var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
        var button=hits[0].gameObject.GetComponentInParent<Button>();return button==null?hits[0].gameObject:button.gameObject;
    }
}
