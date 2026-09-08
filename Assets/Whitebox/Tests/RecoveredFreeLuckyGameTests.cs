using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public sealed class RecoveredFreeLuckyGameTests
{
    [UnityTest]
    public IEnumerator ActualLuckyPopupPaysThroughCashFlightAfterIconPulseAndPlainClaim()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;
        Scene scene=default;AsyncOperation unload=null;RecoveredFreeLuckyGame game=null;Camera camera=null;RenderTexture texture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry entry=null;foreach(var root in scene.GetRootGameObjects()){var value=root.GetComponentInChildren<GameEntry>();if(value!=null)entry=value;}
            Assert.IsNotNull(entry);{ float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.CashFlight==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            game=Object.Instantiate(Resources.Load<RecoveredFreeLuckyGame>("RecoveredUI/FreeLuckyGame"),entry.transform,false);
            game.Bind(entry.PlayerProgress,entry.Rules,entry.Ads,entry.CashFlight,entry.transform,entry.CurrentProfile.isA,entry.CurrentProfile.languageType);
            camera=entry.GetComponent<Canvas>().worldCamera;texture=new RenderTexture(1080,1920,24);camera.targetTexture=texture;
            yield return null;Canvas.ForceUpdateCanvases();float balance=entry.PlayerProgress.GreenCount;int completed=0;float received=0;
            var start=entry.transform.TransformPoint(new Vector3(0,100,0));double began=Time.timeAsDouble;
            game.Begin(start,reward=>{completed++;received=reward;Assert.AreEqual(balance,entry.PlayerProgress.GreenCount,"Callback precedes shared cash credit.");});
            Assert.AreEqual(start,game.Icon.position);Assert.AreEqual(new Vector2(498,368),game.Icon.sizeDelta);
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;Assert.IsTrue(game.Icon.gameObject.activeSelf);Assert.IsFalse(game.Popup.gameObject.activeSelf);
            Time.timeScale=1;for(int i=0;i<6;i++)yield return null;
            Assert.That(game.Icon.localScale.x,Is.EqualTo(.65f).Within(.0001f));Assert.That(game.Icon.localScale.z,Is.EqualTo(0).Within(.0001f));
            Capture(camera,texture,"current-free-lucky-icon.png");
            for(int i=0;i<12&&!game.Popup.gameObject.activeSelf;i++)yield return null;
            Assert.IsNull(game.Error);Assert.IsTrue(game.Popup.gameObject.activeSelf);Assert.IsFalse(game.Icon.gameObject.activeSelf);
            Assert.That(Time.timeAsDouble-began,Is.InRange(.599,.75));Assert.IsTrue(game.IsRunning);Assert.AreEqual(0,completed);
            float original=game.Popup.Claim.OriginalReward;
            for(int i=0;i<50&&!game.Popup.PlainButton.gameObject.activeInHierarchy;i++)yield return null;
            for(int i=0;i<8;i++)yield return null;
            Capture(camera,texture,"current-free-lucky-reward.png");
            game.Popup.PlainButton.onClick.Invoke();
            // Cash departures use unscaled time, independently of captureDeltaTime.
            float cashDeadline=Time.realtimeSinceStartup+5;
            while(completed==0&&Time.realtimeSinceStartup<cashDeadline)yield return null;
            Assert.AreEqual(1,completed);Assert.AreEqual(original*.5f,received);Assert.IsFalse(game.IsRunning);
            Assert.AreEqual(balance+received,entry.PlayerProgress.GreenCount);Assert.IsFalse(game.Popup.gameObject.activeSelf);Assert.IsNull(game.Error);
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
