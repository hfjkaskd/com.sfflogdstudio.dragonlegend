using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class RecoveredSpinHintTests
{
    [UnityTest]
    public IEnumerator ScaledIdleHintRendersHidesOnSpinAndReusesAfterSettlement()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;Random.InitState(71);
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var hint=game.Playfield.SpinHint;Assert.IsTrue(hint.IsWaiting);
            Assert.AreEqual(1,game.PlayerStore.Data.GuideStep);Assert.IsNotNull(hint.Finger);Assert.IsTrue(hint.Finger.gameObject.activeSelf);
            var initialFinger=hint.Finger;
            hint.Hide();hint.Begin();for(int frame=0;frame<20;frame++)yield return null;Assert.IsFalse(hint.Finger.gameObject.activeSelf);
            Time.timeScale=0;for(int frame=0;frame<30;frame++)yield return null;Assert.IsFalse(hint.Finger.gameObject.activeSelf);
            Time.timeScale=1;for(int frame=0;frame<25;frame++)yield return null;
            var finger=hint.Finger;Assert.IsNotNull(finger);Assert.IsTrue(finger.gameObject.activeInHierarchy);Assert.IsFalse(hint.IsWaiting);
            Assert.AreSame(initialFinger,finger);
            Assert.AreEqual(game.Playfield.SpinButton.transform,finger.parent);Assert.AreEqual(Vector2.zero,finger.anchoredPosition);
            Assert.AreEqual(Vector3.one,finger.localScale);Assert.AreEqual(Vector2.one*100,finger.sizeDelta);
            var art=finger.GetComponentInChildren<RecoveredRegionAnimator>();Assert.IsNotNull(art);Assert.IsFalse(art.Rig.raycastTarget);
            Assert.AreEqual(new Vector2(160.88226f,159.71727f),((RectTransform)art.transform).sizeDelta);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);art.Sample(0,.55f);Canvas.ForceUpdateCanvases();
            Render(camera,target,capture);var shown=capture.GetPixels32();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-spin-finger.png"),capture.EncodeToPNG());
            finger.gameObject.SetActive(false);Canvas.ForceUpdateCanvases();Render(camera,target,capture);
            var hidden=capture.GetPixels32();int changed=0;for(int i=0;i<shown.Length;i++)if(!shown[i].Equals(hidden[i]))changed++;
            Assert.Greater(changed,200,"The authored finger must contribute visible pixels.");finger.gameObject.SetActive(true);
            int completed=0;game.CoreRound.CoreRoundCompleted+=()=>completed++;
            game.Playfield.SpinButton.Button.onClick.Invoke();Assert.IsFalse(finger.gameObject.activeSelf);Assert.IsFalse(hint.IsWaiting);
            for(int frame=0;frame<1800&&completed==0;frame++) {
                Assert.IsFalse(finger.gameObject.activeSelf);Claim(game.Playfield.BigWinPopup.PlainButton);Claim(game.Playfield.JackpotPopup.PlainButton);yield return null;
            }
            Assert.AreEqual(1,completed);Assert.IsTrue(hint.IsWaiting);Assert.IsFalse(finger.gameObject.activeSelf);
            for(int frame=0;frame<45;frame++)yield return null;
            Assert.AreSame(finger,hint.Finger);Assert.IsTrue(finger.gameObject.activeInHierarchy);
            game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();Assert.IsFalse(hint.IsWaiting);Assert.IsFalse(finger.gameObject.activeSelf);
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.Playfield.SpinHint==hint)&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.GreaterOrEqual(game.PlayerStore.Data.GuideStep,2);
            Assert.IsTrue(game.Playfield.SpinHint.IsWaiting);Assert.IsNull(game.Playfield.SpinHint.Finger,"Returning player only starts the idle timer.");
        } finally {
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Claim(Button button){if(button!=null&&button.gameObject.activeInHierarchy&&button.IsInteractable())button.onClick.Invoke();}
    private static void Render(Camera camera,RenderTexture target,Texture2D capture)
    {
        RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
        RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
    }
}
