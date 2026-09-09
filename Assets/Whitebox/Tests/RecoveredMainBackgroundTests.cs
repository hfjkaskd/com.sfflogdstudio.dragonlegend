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

public sealed class RecoveredMainBackgroundTests
{
    [UnityTest]
    public IEnumerator ProductionBackgroundMatchesSourceRectsAndRebuildsWithGMProfile()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null;Texture2D capture=null;var previous=RenderTexture.active;
        try {
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            Assert.IsNotNull(game);yield return null;
            var background=game.Background;Assert.IsNotNull(background);Assert.AreEqual(0,background.transform.GetSiblingIndex());
            Assert.IsTrue(background.BackgroundCanvas.overrideSorting);
            Assert.AreEqual(game.GetComponent<Canvas>().sortingOrder-3,background.BackgroundCanvas.sortingOrder);
            var composition=game.Playfield.Composition;Assert.IsNotNull(composition);
            Assert.IsTrue(composition.DragonCanvas.overrideSorting);Assert.IsTrue(composition.BoardCanvas.overrideSorting);
            Assert.Less(background.BackgroundCanvas.sortingOrder,composition.DragonCanvas.sortingOrder);
            Assert.Less(composition.DragonCanvas.sortingOrder,composition.BoardCanvas.sortingOrder);
            Assert.Less(composition.BoardCanvas.sortingOrder,game.GetComponent<Canvas>().sortingOrder);
            Assert.AreEqual(new Vector2(1072,757),composition.BoardBackground.rectTransform.sizeDelta);
            Assert.AreEqual(new Vector2(0,-26),composition.BoardBackground.rectTransform.anchoredPosition);
            Assert.AreEqual("zjm_bg_qipan",composition.BoardBackground.sprite.name);
            Assert.AreEqual("US",game.CurrentProfile.countryCode);Assert.IsFalse(game.CurrentProfile.isA);
            Assert.IsTrue(background.BaseBackground.gameObject.activeSelf);Assert.IsFalse(background.FreeBackground.gameObject.activeSelf);
            foreach(var image in new[]{background.BaseBackground,background.FreeBackground}) {
                Assert.AreEqual(new Vector2(1080.4688f,2400),image.rectTransform.sizeDelta);
                Assert.AreEqual(Vector2.zero,image.rectTransform.anchoredPosition);
                Assert.AreEqual(Vector2.one*.5f,image.rectTransform.anchorMin);Assert.AreEqual(Vector2.one*.5f,image.rectTransform.anchorMax);
                Assert.AreEqual(Image.Type.Simple,image.type);Assert.IsFalse(image.preserveAspect);Assert.AreEqual(Color.white,image.color);
            }
            Assert.AreEqual("zjm_bg",background.BaseBackground.sprite.name);Assert.AreEqual("mfyx_bg",background.FreeBackground.sprite.name);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);yield return null;
            foreach(var mode in new[]{RecoveredSlotType.Base,RecoveredSlotType.Free}) {
                background.Apply(mode);Assert.AreEqual(mode==RecoveredSlotType.Base,background.BaseBackground.gameObject.activeSelf);
                Assert.AreEqual(mode!=RecoveredSlotType.Base,background.FreeBackground.gameObject.activeSelf);
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                var pixels=capture.GetPixels32();int dragonGold=0;
                for(int y=1100;y<1400;y++)for(int x=300;x<780;x++) {
                    var pixel=pixels[y*1080+x];if(pixel.r>120&&pixel.g>70&&pixel.r>pixel.b*1.5f)dragonGold++;
                }
                Assert.Greater(dragonGold,15000,"Dragon must remain visible between jackpot and board.");
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-main-background-"+(mode==RecoveredSlotType.Base?"base":"free")+".png"),capture.EncodeToPNG());
            }
            background.Apply(RecoveredSlotType.Base);Canvas.ForceUpdateCanvases();
            var button=game.Playfield.SpinButton.Button;var rect=(RectTransform)button.transform;
            var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
            var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits);
            Assert.AreEqual(button.gameObject,ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject));
            var profile=game.CurrentProfile;
            game.transform.Find("SelectAlternative").GetComponent<Button>().onClick.Invoke();
            float deadline=Time.realtimeSinceStartup+5;
            while((game.CurrentProfile==profile||game.Background==null)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return null;Assert.IsTrue(background==null);Assert.IsTrue(game.CurrentProfile.isA);
            Assert.AreEqual(1,game.GetComponentsInChildren<RecoveredMainBackground>(true).Length);
            Assert.IsTrue(game.Background.BaseBackground.gameObject.activeSelf);Assert.IsFalse(game.Background.FreeBackground.gameObject.activeSelf);
        } finally {
            Random.state=random;RenderTexture.active=previous;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
}
