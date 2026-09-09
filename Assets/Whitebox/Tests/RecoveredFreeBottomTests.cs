using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredFreeBottomTests
{
    [UnityTest]
    public IEnumerator ProductionCounterReadsLiveCountAfterGenerationAndRebuildsForGM()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null;Texture2D capture=null;var previous=RenderTexture.active;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+5;while(game.Playfield==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.Playfield);var bottom=game.Playfield.FreeBottom;Assert.IsNotNull(bottom);
            // Isolate the counter from the complete loop, which now starts reels on this event.
            game.CoreRound.Unbind();
            Assert.IsTrue(bottom.Main.activeSelf);Assert.IsFalse(bottom.Free.activeSelf);
            var bg=bottom.Free.transform.Find("Bg").GetComponent<Image>();
            Assert.AreEqual(Image.Type.Sliced,bg.type);Assert.AreEqual(new Vector2(640,86),bg.rectTransform.sizeDelta);
            Assert.AreEqual(new Vector2(0,-73),bg.rectTransform.anchoredPosition);
            Assert.AreEqual("zjm_s9g_spin_bg",bg.sprite.name);Assert.AreEqual(60,bottom.Count.fontSize);
            Assert.IsFalse(bottom.Count.enableAutoSizing);Assert.IsFalse(bottom.Count.enableWordWrapping);
            game.PlayerProgress.FreeSpinCount=12;bottom.Apply(RecoveredSlotType.Free);bottom.RefreshCount();
            game.Background.Apply(RecoveredSlotType.Free);
            Assert.IsFalse(bottom.Main.activeSelf);Assert.IsTrue(bottom.Free.activeSelf);
            StringAssert.Contains(">12</gradient>",bottom.Count.text);
            Assert.IsTrue(game.FreeSpinEntry.TryBegin(new[]{0}));Assert.AreEqual(11,game.PlayerProgress.FreeSpinCount);
            StringAssert.Contains(">12</gradient>",bottom.Count.text);
            int steps=0;while(game.FreeSpinResult.IsGenerating&&steps++<10000)game.FreeSpinResult.Step();
            Assert.IsFalse(game.FreeSpinResult.IsGenerating);StringAssert.Contains(">11</gradient>",bottom.Count.text);
            bottom.Count.ForceMeshUpdate();Assert.AreEqual("FREE SPIN 11 TIMES",bottom.Count.GetParsedText());
            Assert.Greater(bottom.Count.textInfo.materialCount,1,"The digit must use the source rich-text material.");
            foreach(var material in bottom.Count.fontSharedMaterials)Assert.IsTrue(material.shader.isSupported);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);yield return null;Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-free-bottom.png"),capture.EncodeToPNG());
            game.PlayerProgress.FreeSpinCount=0;bottom.RefreshCount();Assert.IsFalse(game.FreeSpinEntry.TryBegin(null));
            StringAssert.Contains(">0</gradient>",bottom.Count.text);
            bottom.Apply((RecoveredSlotType)2);Assert.IsTrue(bottom.Free.activeSelf);
            bottom.Apply(RecoveredSlotType.Base);Assert.IsTrue(bottom.Main.activeSelf);Assert.IsFalse(bottom.Free.activeSelf);
            var oldEntry=game.FreeSpinEntry;var oldPlayer=game.PlayerProgress;var oldResult=game.FreeSpinResult;
            var profile=game.CurrentProfile;game.transform.Find("SelectAlternative").GetComponent<Button>().onClick.Invoke();
            deadline=Time.realtimeSinceStartup+5;while((game.CurrentProfile==profile||game.Playfield==null)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;Assert.IsTrue(bottom==null);Assert.IsNotNull(game.Playfield.FreeBottom);
            oldPlayer.FreeSpinCount=1;Assert.IsTrue(oldEntry.TryBegin(new[]{0}));steps=0;
            while(oldResult.IsGenerating&&steps++<10000)oldResult.Step();Assert.IsFalse(oldResult.IsGenerating);
            Assert.IsTrue(game.Playfield.FreeBottom.Main.activeSelf);Assert.IsFalse(game.Playfield.FreeBottom.Free.activeSelf);
        } finally {
            Random.state=random;RenderTexture.active=previous;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
