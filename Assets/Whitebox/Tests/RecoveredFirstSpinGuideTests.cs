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

public sealed class RecoveredFirstSpinGuideTests
{
    [UnityTest]
    public IEnumerator ActualGuideBlocksOtherButtonsRevealsTextAndRestoresSpinForPlayAndGm()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);PlayerPrefs.DeleteKey(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;Scene scene=default;AsyncOperation unload=null;
        Camera camera=null;RenderTexture target=null,prior=RenderTexture.active;Texture2D capture=null;
        UnityEngine.Events.UnityAction<Scene,LoadSceneMode> prepare=null;
        try {
            Time.timeScale=0;Time.captureDeltaTime=.05f;
            target=new RenderTexture(1080,1920,24);
            // Establish the capture viewport before the guide records world positions.
            prepare=(loaded,mode)=>{
                if(loaded.name!="GameEntry")return;
                foreach(var root in loaded.GetRootGameObjects()){
                    var entry=root.GetComponentInChildren<GameEntry>();if(entry==null)continue;
                    camera=entry.GetComponent<Canvas>().worldCamera;camera.targetTexture=target;
                }
            };
            SceneManager.sceneLoaded+=prepare;
            yield return SceneManager.LoadSceneAsync("GameEntry",LoadSceneMode.Additive);scene=SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            float deadline=Time.realtimeSinceStartup+10;while(game.CoreRound==null&&Time.realtimeSinceStartup<deadline)yield return null;
            Assert.IsNotNull(game.CoreRound);var guide=game.CoreRound.FirstSpinGuide;var spin=game.Playfield.SpinButton.Button;
            Assert.IsTrue(guide.gameObject.activeSelf);Assert.AreSame(spin.transform,guide.Target);Assert.AreSame(guide.transform,spin.transform.parent);
            Assert.AreEqual("C",guide.Text.Label.text);Assert.IsTrue(game.Playfield.SpinHint.Finger.gameObject.activeSelf);
            Assert.AreEqual(.5019608f,guide.GetComponent<Image>().color.a,.000001f);
            Assert.IsNotNull(camera);
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-first-spin-guide-initial.png"),capture.EncodeToPNG());
            Assert.AreEqual(spin.gameObject,Hit((RectTransform)spin.transform,camera));
            var more=game.Playfield.SpinRecovery.MoreSpinButton;
            Assert.AreEqual(guide.Background.gameObject,Hit((RectTransform)more.transform,camera));
            Click(guide.Background.gameObject);Assert.AreEqual(1,game.PlayerStore.Data.GuideStep);
            Assert.AreEqual("C",guide.Text.Label.text,"The source background button is an input blocker with no listener.");
            Time.timeScale=1;
            for(int frame=0;frame<160&&guide.Text.Label.text!="Click SPIN and let the dragon breathe fire into your wins!";frame++)yield return null;
            Time.timeScale=0;
            Assert.AreEqual("Click SPIN and let the dragon breathe fire into your wins!",guide.Text.Label.text);
            Assert.IsFalse(game.CoreRound.MoreSpins.gameObject.activeSelf);
            RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-first-spin-guide.png"),capture.EncodeToPNG());
            // GM while the original button is lifted must restore it before disposing the old view.
            var oldField=game.Playfield;var oldCore=game.CoreRound;
            game.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
            Assert.AreSame(oldField.transform.Find("Bottom/Main"),spin.transform.parent);Assert.IsFalse(guide.gameObject.activeSelf);
            deadline=Time.realtimeSinceStartup+10;while((game.CoreRound==null||game.CoreRound==oldCore)&&Time.realtimeSinceStartup<deadline)yield return null;
            guide=game.CoreRound.FirstSpinGuide;spin=game.Playfield.SpinButton.Button;Assert.IsTrue(guide.gameObject.activeSelf);
            yield return null;
            var position=spin.transform.position;int count=game.PlayerProgress.SpinCount;
            Click(spin.gameObject);Assert.AreEqual(2,game.PlayerStore.Data.GuideStep);Assert.AreEqual(count-1,game.PlayerProgress.SpinCount);
            Assert.IsFalse(guide.gameObject.activeSelf);Assert.IsTrue(game.Playfield.IsBusy);
            Assert.AreSame(game.Playfield.transform.Find("Bottom/Main"),spin.transform.parent);
            Assert.Less(Vector3.Distance(position,spin.transform.position),.01f);
            Assert.IsFalse(game.Playfield.SpinHint.Finger.gameObject.activeSelf);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            Assert.AreEqual(game.Playfield.SpinRecovery.MoreSpinButton.gameObject,
                Hit((RectTransform)game.Playfield.SpinRecovery.MoreSpinButton.transform,camera));
        } finally {
            if(prepare!=null)SceneManager.sceneLoaded-=prepare;
            Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=prior;if(camera!=null)camera.targetTexture=null;
            if(target!=null)Object.Destroy(target);if(capture!=null)Object.Destroy(capture);
            if(scene.IsValid()&&scene.isLoaded)unload=SceneManager.UnloadSceneAsync(scene);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
        if(unload!=null)yield return unload;
    }
    private static void Click(GameObject target)=>ExecuteEvents.Execute(target,new PointerEventData(EventSystem.current){button=PointerEventData.InputButton.Left},ExecuteEvents.pointerClickHandler);
    private static GameObject Hit(RectTransform rect,Camera camera)
    {
        var pointer=new PointerEventData(EventSystem.current){position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        var hits=new List<RaycastResult>();EventSystem.current.RaycastAll(pointer,hits);Assert.IsNotEmpty(hits,"No hit: "+rect.name+" screen="+pointer.position+" world="+rect.position+" local="+rect.anchoredPosition+" parent="+((RectTransform)rect.parent).rect);
        var button=hits[0].gameObject.GetComponentInParent<Button>();return button==null?hits[0].gameObject:button.gameObject;
    }
}
