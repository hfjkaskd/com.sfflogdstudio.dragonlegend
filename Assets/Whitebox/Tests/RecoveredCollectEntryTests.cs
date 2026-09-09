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
using Object = UnityEngine.Object;

public sealed class RecoveredCollectEntryTests
{
    [UnityTest]
    public IEnumerator MainPointerOpensSharedCollectionAndGmReplacesWindowOwnership()
    {
        string key = RecoveredPlayerStore.OriginalKey; bool had = PlayerPrefs.HasKey(key); string saved = PlayerPrefs.GetString(key); PlayerPrefs.DeleteKey(key);
        var random = Random.state; float scale = Time.timeScale, delta = Time.captureDeltaTime;
        Scene scene = default; AsyncOperation unload = null; Camera camera = null; RenderTexture target = null;
        try {
            Time.timeScale = 1; Time.captureDeltaTime = .025f;
            yield return SceneManager.LoadSceneAsync("GameEntry", LoadSceneMode.Additive); scene = SceneManager.GetSceneByName("GameEntry");
            GameEntry entry = null; foreach (var root in scene.GetRootGameObjects()) { var value = root.GetComponentInChildren<GameEntry>(); if (value != null) entry = value; }
            Assert.IsNotNull(entry); yield return null;
            camera = entry.GetComponent<Canvas>().worldCamera; target = new RenderTexture(1080,1920,24); camera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); yield return null;
            var collect = entry.CollectEntry; Assert.IsNotNull(collect); Assert.IsNull(collect.Window);
            Assert.AreEqual(1, entry.GetComponentsInChildren<RecoveredCollectEntry>(true).Length);
            Assert.IsTrue(collect.Button.gameObject.activeInHierarchy); Assert.AreEqual("Treasure", collect.Button.name);
            Assert.AreSame(collect.Button.transform, collect.Destination.parent);
            int sounds = 0; collect.SoundRequested += name => { Assert.AreEqual("click", name); sounds++; };
            yield return Click(collect.Button, camera); var window = collect.Window; Assert.IsNotNull(window);
            Assert.AreEqual(1, sounds); Assert.IsTrue(window.Green.activeSelf); Assert.IsFalse(window.Gold.activeSelf);
            for (int i=0;i<14;i++) yield return null;
            Assert.AreEqual(0, window.List.Scroll.content.anchoredPosition.y, .01f, "First opening must not manufacture scroll during zero-scale entry.");
            Capture(camera, target, "current-main-collect-entry.png");
            var list = window.List; list.Scroll.enabled = false; list.Scroll.content.anchoredPosition = new Vector2(0,150);
            yield return Click(window.CloseButton, camera); Assert.AreEqual(2, sounds);
            for (int i=0;i<14;i++) yield return null;
            Assert.IsFalse(window.gameObject.activeSelf);
            entry.PlayerProgress.SetCollectData(entry.Rules.GetCollectInfos()[0].id, 1);
            yield return Click(collect.Button, camera); Assert.AreSame(window, collect.Window); Assert.AreSame(list, window.List);
            Assert.AreEqual(150, list.Scroll.content.anchoredPosition.y); Assert.AreEqual("1/15", window.ProgressText.text);
            var gm = entry.GetComponent<RecoveredGmPanel>();
            yield return Click(gm.ToggleButton, camera);
            yield return Click(entry.transform.Find("SelectAlternative").GetComponent<Button>(), camera);
            float deadline = Time.realtimeSinceStartup + 5;
            while(entry.CollectEntry == null && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null; yield return null;
            Assert.IsTrue(collect == null); Assert.IsTrue(window == null);
            collect = entry.CollectEntry; Assert.IsNotNull(collect); Assert.IsTrue(entry.CurrentProfile.isA);
            Assert.IsNull(collect.Window); Assert.AreEqual(1, entry.GetComponentsInChildren<RecoveredCollectEntry>(true).Length);
            yield return Click(collect.Button, camera); window = collect.Window;
            Assert.IsTrue(window.Gold.activeSelf); Assert.IsFalse(window.Green.activeSelf);
            for (int i=0;i<14;i++) yield return null;
            Capture(camera, target, "current-main-collect-entry-a.png");
            yield return Click(gm.ToggleButton, camera);
            yield return Click(entry.transform.Find("SelectUS").GetComponent<Button>(), camera);
            deadline = Time.realtimeSinceStartup + 5;
            while(entry.CollectEntry == null && Time.realtimeSinceStartup < deadline) yield return null;
            yield return null; yield return null;
            Assert.IsTrue(window == null); Assert.IsFalse(entry.CurrentProfile.isA); Assert.AreEqual("US", entry.CurrentProfile.countryCode);
            Assert.AreEqual(1, entry.GetComponentsInChildren<RecoveredCollectEntry>(true).Length);
        }
        finally {
            if(camera != null) camera.targetTexture = null; if(target != null) Object.Destroy(target);
            if(scene.IsValid()) unload = SceneManager.UnloadSceneAsync(scene); Random.state = random; Time.timeScale = scale; Time.captureDeltaTime = delta;
            if(had) PlayerPrefs.SetString(key,saved); else PlayerPrefs.DeleteKey(key);
        }
        if(unload != null) yield return unload;
    }
    private static IEnumerator Click(Button button, Camera camera)
    {
        var system = EventSystem.current;
        var previous = system.GetComponents<BaseInputModule>(); var enabled = new bool[previous.Length];
        for (int i=0;i<previous.Length;i++) { enabled[i] = previous[i].enabled; previous[i].enabled = false; }
        var input = system.gameObject.AddComponent<CollectEntryInputModule>();
        input.Click = () => DispatchClick(button, camera);
        try {
            float deadline = Time.realtimeSinceStartup + 5;
            while (!input.Completed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(input.Completed, "EventSystem must process the queued pointer click.");
            if (input.Error != null) throw input.Error;
        }
        finally {
            if (input != null) { input.enabled = false; Object.Destroy(input); }
            for (int i=0;i<previous.Length;i++) if (previous[i] != null) previous[i].enabled = enabled[i];
        }
    }
    private static void DispatchClick(Button button, Camera camera)
    {
        Canvas.ForceUpdateCanvases(); var hits = new List<RaycastResult>();
        var rect = (RectTransform)button.transform;
        var pointer = new PointerEventData(EventSystem.current) {button=PointerEventData.InputButton.Left,position=RectTransformUtility.WorldToScreenPoint(camera,rect.TransformPoint(rect.rect.center))};
        EventSystem.current.RaycastAll(pointer,hits); Assert.IsNotEmpty(hits,button.name);
        Assert.AreSame(button.gameObject, ExecuteEvents.GetEventHandler<IPointerClickHandler>(hits[0].gameObject), "Top hit must reach " + button.name);
        ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
    }
    private static void Capture(Camera camera, RenderTexture target, string filename)
    {
        var previous=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try {
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest {destination=target});RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/"+filename),image.EncodeToPNG());
        } finally {RenderTexture.active=previous;Object.Destroy(image);}
    }
}

// EventSystem itself calls Process in its actual Update ordering.
public sealed class CollectEntryInputModule : BaseInputModule
{
    public System.Action Click;
    public bool Completed;
    public System.Exception Error;
    public override bool ShouldActivateModule() => true;
    public override void Process()
    {
        if (Completed || Click == null) return;
        try { Click(); } catch (System.Exception error) { Error = error; }
        Completed = true;
    }
}
