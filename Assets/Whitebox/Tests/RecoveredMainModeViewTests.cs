using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredMainModeViewTests
{
    [UnityTest]
    public IEnumerator ActualMainSwitchesFreeBoardFireworksAndBottomThenRestoresBaseAmount()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null;Texture2D capture=null;var previous=RenderTexture.active;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+5;while(game.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.Playfield);var field=game.Playfield;var mode=field.ModeView;Assert.IsNotNull(mode);
            Assert.IsTrue(mode.BaseRoll.activeSelf);Assert.IsTrue(mode.BaseResult.activeSelf);Assert.IsFalse(mode.FreeRoll.activeSelf);
            Assert.IsFalse(mode.Fireworks.gameObject.activeSelf);Assert.IsFalse(mode.FreeReels.IsInitialized);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            camera.targetTexture=target;yield return null;
            game.CoreRound.FirstSpinGuide.Hide();
            var adapt=field.GetComponent<RecoveredScreenAdapt>();Assert.IsNotNull(adapt);
            var fieldRect=(RectTransform)field.transform;var board=(RectTransform)field.transform.Find("QiPan");
            Vector3 boardBefore=board.position,spinBefore=field.SpinButton.transform.position;
            Vector3 backgroundBefore=game.Background.BaseBackground.transform.position;
            Assert.AreSame(field.transform,game.BalancePanel.transform.parent);
            Vector3 cashBefore=game.BalancePanel.CashTarget.position;
            Vector2 boardLocal=board.anchoredPosition;
            var scaler=game.GetComponentInParent<UnityEngine.UI.CanvasScaler>();
            float factor=scaler.matchWidthOrHeight*scaler.referenceResolution.y/1920-scaler.referenceResolution.x*(scaler.matchWidthOrHeight-1)/1080;
            adapt.Apply(1080,1920,new Rect(24,60,1032,1740));Canvas.ForceUpdateCanvases();
            Assert.AreEqual(60*factor,fieldRect.offsetMin.y,.0002f);Assert.AreEqual(-120*factor,fieldRect.offsetMax.y,.0002f);
            Assert.AreNotEqual(boardBefore,board.position);Assert.AreNotEqual(spinBefore,field.SpinButton.transform.position);
            Assert.AreEqual(boardLocal,board.anchoredPosition);Assert.AreEqual(backgroundBefore,game.Background.BaseBackground.transform.position);
            Assert.AreNotEqual(cashBefore,game.BalancePanel.CashTarget.position,"Top and the cash endpoint follow the common Node.");
            Render(camera,target,capture);File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-safe-area.png"),capture.EncodeToPNG());
            adapt.AdaptScreen();Canvas.ForceUpdateCanvases();
            for(int i=0;i<5;i++) {
                var effect=mode.SpeedEffectAt(i);Assert.IsFalse(effect.activeSelf);
                var rect=(RectTransform)effect.transform;
                Assert.AreEqual(new Vector2(267.50006f,606),rect.sizeDelta);
                Assert.AreEqual(new Vector2(.49532714f,.5f),rect.pivot);
                Assert.AreEqual(Vector2.zero,rect.anchoredPosition);
                Assert.AreEqual(new Vector2(-380+190*i,-1),((RectTransform)rect.parent).anchoredPosition);
                Assert.IsNotNull(rect.parent.GetComponent<UnityEngine.UI.RectMask2D>());
                Assert.IsNull(effect.GetComponent<Animation>(),"Original empty startingAnimation preserves setup pose.");
            }
            Canvas.ForceUpdateCanvases();Render(camera,target,capture);var withoutSpeed=capture.GetPixels32();
            mode.SetSpeedEffect(2,true);Canvas.ForceUpdateCanvases();Render(camera,target,capture);
            var withSpeed=capture.GetPixels32();int speedPixels=0;
            for(int i=0;i<withSpeed.Length;i++)if(!withSpeed[i].Equals(withoutSpeed[i]))speedPixels++;
            Assert.Greater(speedPixels,200,"Source setup-pose highlight must visibly render in the real board.");
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-reel-anticipation.png"),capture.EncodeToPNG());
            mode.HideSpeedEffects();camera.targetTexture=null;Object.Destroy(target);Object.Destroy(capture);target=null;capture=null;
            field.DownWin.SetTemporaryTotal(123.45f);game.PlayerProgress.GameSlotType=RecoveredSlotType.Free;game.PlayerProgress.FreeSpinCount=12;
            game.FreeSpinResult.Begin(new[]{0});int steps=0;while(game.FreeSpinResult.IsGenerating&&steps++<10000)game.FreeSpinResult.Step();
            Assert.IsFalse(game.FreeSpinResult.IsGenerating);
            mode.ApplyCurrent();Assert.IsFalse(mode.FreeReels.IsInitialized,"SetInitShow must not initialize reels early.");
            mode.InitializeFreeReels();mode.RefreshFreeCount();
            Assert.IsTrue(mode.FreeReels.IsInitialized);Assert.IsFalse(mode.BaseRoll.activeSelf);Assert.IsFalse(mode.BaseResult.activeSelf);
            Assert.IsTrue(mode.FreeRoll.activeSelf);Assert.IsTrue(mode.FreeReels.transform.Find("FreeResult").gameObject.activeInHierarchy);
            Assert.IsTrue(mode.Fireworks.gameObject.activeSelf);Assert.IsTrue(field.FreeBottom.Free.activeSelf);Assert.IsFalse(field.FreeBottom.Main.activeSelf);
            Assert.AreEqual("GOOD LUCK",field.DownWin.Label.text);Assert.AreEqual(123.45f,field.DownWin.TemporaryTotal);
            StringAssert.Contains(">12</gradient>",field.FreeBottom.Count.text);Assert.AreEqual(12,game.PlayerProgress.FreeSpinCount);
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)Assert.IsTrue(mode.FreeReels.At(col,row).gameObject.activeInHierarchy);
            var fireworksRect=(RectTransform)mode.Fireworks.transform;
            Assert.AreEqual(new Vector2(50,50),fireworksRect.sizeDelta);Assert.AreEqual(new Vector2(0,83),fireworksRect.anchoredPosition);
            Assert.AreEqual(4,mode.Fireworks.GetComponent<Animation>()["animation"].length,.00001f);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);yield return null;
            mode.Fireworks.Sample(0,1.27f);Canvas.ForceUpdateCanvases();Render(camera,target,capture);
            var withFireworks=capture.GetPixels32();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-free-mode.png"),capture.EncodeToPNG());
            mode.Fireworks.gameObject.SetActive(false);Canvas.ForceUpdateCanvases();Render(camera,target,capture);
            var without=capture.GetPixels32();int changed=0;for(int i=0;i<without.Length;i++)if(!without[i].Equals(withFireworks[i]))changed++;
            Assert.Greater(changed,200,"The active source fireworks must contribute visible pixels.");
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Base;mode.ApplyCurrent();
            Assert.IsFalse(mode.FreeRoll.activeSelf);Assert.IsFalse(mode.Fireworks.gameObject.activeSelf);Assert.IsTrue(mode.BaseRoll.activeSelf);Assert.IsTrue(mode.BaseResult.activeSelf);
            Assert.AreEqual(RecoveredCurrency.Format(123.45f,game.CurrentProfile.languageType,2),field.DownWin.Label.text);
            Assert.IsTrue(field.FreeBottom.Main.activeSelf);Assert.IsFalse(field.FreeBottom.Free.activeSelf);
            Canvas.ForceUpdateCanvases();Render(camera,target,capture);File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-base-return.png"),capture.EncodeToPNG());
            field.DownWin.SetTemporaryTotal(0);mode.ApplyCurrent();Assert.AreEqual("GOOD LUCK",field.DownWin.Label.text);
            game.PlayerProgress.GameSlotType=(RecoveredSlotType)2;mode.ApplyCurrent();Assert.IsTrue(mode.FreeRoll.activeSelf);Assert.IsTrue(mode.Fireworks.gameObject.activeSelf);
        } finally {
            Random.state=random;RenderTexture.active=previous;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Render(Camera camera,RenderTexture target,Texture2D capture)
    {
        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
    }
}
