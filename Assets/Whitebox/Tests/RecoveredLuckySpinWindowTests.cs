using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredLuckySpinWindowTests
{
    [UnityTest]
    public IEnumerator ActualWindowAutomaticallySpinsBothDecimalPositionsAndClaimsThroughCashFlight()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;
        Scene scene=default;AsyncOperation unload=null;RecoveredLuckySpinWindow game=null;Camera camera=null;RenderTexture texture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);float deadline=Time.realtimeSinceStartup+5;
            while(entry.CashFlight==null&&Time.realtimeSinceStartup<deadline)yield return null;Assert.IsNotNull(entry.CashFlight);
            game=Object.Instantiate(Resources.Load<RecoveredLuckySpinWindow>("RecoveredUI/LuckySpinWindow"),entry.transform,false);
            game.Bind(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,entry.transform,entry.CurrentProfile.isA,entry.CurrentProfile.languageType);
            camera=entry.GetComponent<Canvas>().worldCamera;texture=new RenderTexture(1080,1920,24);camera.targetTexture=texture;
            yield return null;
            var sounds=new List<string>();game.SoundRequested+=sounds.Add;
            for(int pass=0;pass<2;pass++) {
                sounds.Clear();float amount=pass==0?123:1234,balance=entry.PlayerProgress.GreenCount;int callbacks=0;
                float began=Time.time;var starts=new float[4];for(int i=0;i<4;i++)starts[i]=-1;
                game.Show(amount,value=>{callbacks++;Assert.AreEqual(amount*.5f,value);Assert.AreEqual(balance,entry.PlayerProgress.GreenCount);});
                Assert.IsTrue(game.Window.activeSelf);Assert.IsFalse(game.Window.transform.Find("Content/Btn").gameObject.activeSelf);
                Assert.AreEqual("jump",sounds[0]);for(int i=0;i<4;i++)Assert.AreEqual("9",game.Column(i).Digit(0).text);
                Time.timeScale=0;for(int i=0;i<4;i++)yield return null;Assert.AreEqual(0,game.Art.Selected);Assert.IsFalse(game.IsSpinning);
                Time.timeScale=1;
                for(int frame=0;frame<200&&game.StoppedCount<4;frame++) {
                    for(int i=0;i<4;i++)if(starts[i]<0&&game.Column(i).IsSpinning)starts[i]=Time.time-began;
                    yield return null;
                }
                Assert.IsNull(game.Error);Assert.AreEqual(4,game.StoppedCount);Assert.AreEqual(1,game.Art.Selected);
                Assert.That(starts[0],Is.InRange(1.299f,1.45f));
                for(int i=1;i<4;i++)Assert.That(starts[i]-starts[i-1],Is.InRange(.049f,.101f));
                CollectionAssert.AreEqual(new[]{"jump","slotsSpin","reelstop2","reelstop2","reelstop2","reelstop2"},sounds);
                CollectionAssert.AreEqual(pass==0?new[]{1,10,2,3}:new[]{1,2,10,3,4},game.Digits);
                for(int i=0;i<4;i++)Assert.AreEqual(game.Digits[i]==10?".":game.Digits[i].ToString(),game.Column(i).Digit(0).text);
                Assert.IsTrue(game.IsRunning);Assert.AreEqual(0,callbacks);Assert.IsFalse(game.Popup.gameObject.activeSelf);
                float allStopped=Time.time;for(int i=0;i<12;i++)yield return null;
                if(pass==0)Capture(camera,texture,"current-lucky-spin-window.png");
                for(int i=0;i<40&&!game.Popup.gameObject.activeSelf;i++)yield return null;
                Assert.IsTrue(game.Popup.gameObject.activeSelf);Assert.That(Time.time-allStopped,Is.InRange(.699f,.8f));
                Assert.IsTrue(game.Window.activeSelf,"Reward popup opens while Slot's exit is still running.");
                Assert.IsFalse(game.IsSpinning);Assert.AreEqual(amount,game.Popup.Claim.OriginalReward);
                for(int i=0;i<60&&!game.Popup.PlainButton.gameObject.activeInHierarchy;i++)yield return null;
                Assert.IsFalse(game.Window.activeSelf);Assert.IsTrue(game.Popup.PlainButton.gameObject.activeInHierarchy);
                for(int i=0;i<16;i++)yield return null; // Capture the plain-button reveal after its scale animation settles.
                if(pass==0)Capture(camera,texture,"current-lucky-spin-reward.png");
                game.Popup.PlainButton.onClick.Invoke();deadline=Time.realtimeSinceStartup+5;
                while(callbacks==0&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.IsNull(game.Error);Assert.AreEqual(1,callbacks);Assert.AreEqual(balance+amount*.5f,entry.PlayerProgress.GreenCount);
                Assert.IsFalse(game.IsRunning);Assert.IsFalse(game.Popup.gameObject.activeSelf);
            }
        } finally {
            if(camera!=null)camera.targetTexture=null;if(texture!=null)Object.Destroy(texture);if(game!=null)Object.Destroy(game.gameObject);
            if(scene.IsValid())unload=SceneManager.UnloadSceneAsync(scene);Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Capture(Camera camera,RenderTexture target,string name)
    {
        var prior=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try {Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+name),image.EncodeToPNG());
        } finally {RenderTexture.active=prior;Object.Destroy(image);}
    }
}
