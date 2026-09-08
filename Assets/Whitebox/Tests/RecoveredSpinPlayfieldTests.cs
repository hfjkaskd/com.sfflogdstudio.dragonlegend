using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public sealed class RecoveredSpinPlayfieldTests
{
    [UnityTest]
    public IEnumerator EntryPointerDebitsOnceGeneratesBoardAndKeepsRewardLock()
    {
        string key=RecoveredPlayerStore.OriginalKey; bool had=PlayerPrefs.HasKey(key); string previous=PlayerPrefs.GetString(key);
        var random=Random.state; float scale=Time.timeScale, delta=Time.captureDeltaTime;
        PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry")); var entry=root.GetComponent<GameEntry>();
        var cameraHost=new GameObject("Entry capture camera",typeof(Camera)); var eventHost=new GameObject("Entry pointer",typeof(EventSystem));
        var camera=cameraHost.GetComponent<Camera>(); camera.transform.position=new Vector3(100,0,-10);
        camera.orthographic=true;camera.orthographicSize=5;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;
        root.GetComponent<Canvas>().worldCamera=camera;
        Texture2D capture=null;var oldTarget=RenderTexture.active;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            Assert.IsTrue(entry.Playfield!=null);yield return null;
            var field=entry.Playfield;Assert.IsFalse(entry.CurrentProfile.isA);
            var bets=new List<int>();entry.Rules.GetBet(false,entry.PlayerProgress.Level,bets);Assert.AreEqual(bets[0],field.Bet);
            int before=entry.PlayerProgress.SpinCount;int rewards=0;field.RewardSequenceRequested+=()=>rewards++;
            Canvas.ForceUpdateCanvases();
            var pointer=new PointerEventData(eventHost.GetComponent<EventSystem>()) {
                position=RectTransformUtility.WorldToScreenPoint(camera,field.SpinButton.Button.targetGraphic.transform.position),
                button=PointerEventData.InputButton.Left
            };
            var hits=new List<RaycastResult>();root.GetComponent<GraphicRaycaster>().Raycast(pointer,hits);
            Assert.IsNotEmpty(hits);Assert.AreSame(field.SpinButton.Button.targetGraphic.gameObject,hits[0].gameObject);
            ExecuteEvents.ExecuteHierarchy(hits[0].gameObject,pointer,ExecuteEvents.pointerClickHandler);
            Assert.AreEqual(before-1,entry.PlayerProgress.SpinCount);Assert.IsTrue(field.IsBusy);
            Assert.IsFalse(entry.SpinResult.IsGenerating, "Native InitGameResult completes within the click handler.");
            Assert.IsTrue(field.Reels.MotionAt(0).IsSpinning, "First reel must start before click returns.");
            field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(before-1,entry.PlayerProgress.SpinCount);
            for(int i=0;i<250&&!field.AwaitingRewards;i++)yield return null;
            Assert.IsTrue(field.AwaitingRewards);Assert.IsTrue(field.IsBusy);Assert.AreEqual(1,rewards);
            Assert.IsFalse(entry.SpinResult.IsGenerating);Assert.AreEqual(5,field.Reels.StoppedCount);
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                Assert.AreEqual(entry.SpinResult.Board.GetSymbol(col,row),field.Reels.ReelAt(col).SymbolId(row));
                Assert.AreEqual(5,field.Reels.ReelAt(col).SymbolAt(row).Symbol.gameObject.layer);
            }
            field.SpinButton.Button.onClick.Invoke();Assert.AreEqual(before-1,entry.PlayerProgress.SpinCount);
            var stored=JsonUtility.FromJson<DragonLegend.Whitebox.Recovered.PlayerData>(PlayerPrefs.GetString(key));
            Assert.AreEqual(before-1,stored.SpinCount);
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();var pixels=capture.GetPixels32();
            for(int col=0;col<5;col++) {
                int visible=0;for(int y=350;y<800;y++)for(int x=70+190*col;x<240+190*col;x++) {
                    var c=pixels[y*1080+x];if(c.r>8||c.g>8||c.b>8)visible++;
                }
                Assert.Greater(visible,500,"Main UI camera must render reel "+col);
            }
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-entry-spin.png")),capture.EncodeToPNG());
        } finally {
            Object.DestroyImmediate(root);Object.Destroy(cameraHost);Object.Destroy(eventHost);
            RenderTexture.active=oldTarget;if(capture!=null)Object.Destroy(capture);target.Release();Object.Destroy(target);
            if(had)PlayerPrefs.SetString(key,previous);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;
        }
    }
    [UnityTest]
    public IEnumerator GmSwitchCancelsOldReelsAndEmptyClickDoesNotAnimateOrDebit()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string previous=PlayerPrefs.GetString(key);
        var random=Random.state;float delta=Time.captureDeltaTime,scale=Time.timeScale;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            Assert.IsTrue(entry.Playfield!=null);var old=entry.Playfield;int stale=0;
            old.Reels.ReelsStopped+=()=>stale++;
            old.Reels.Begin(0,index=>new[]{index,7,9});yield return null;
            var profiles=root.GetComponentsInChildren<Button>();Button alternate=null;
            foreach(var button in profiles)if(button.name=="SelectAlternative")alternate=button;
            Assert.IsTrue(alternate!=null);alternate.onClick.Invoke();
            Assert.IsFalse(old.Reels.IsRunning);
            for(int i=0;i<200&&entry.Playfield==null;i++)yield return null;
            Assert.IsTrue(entry.CurrentProfile.isA);Assert.IsTrue(entry.Playfield!=null);yield return null;
            Assert.AreEqual(0,entry.Playfield.Bet);
            entry.PlayerProgress.SetSpinCount(0);int more=0;entry.SpinEntry.MoreSpinsRequested+=()=>more++;
            entry.Playfield.SpinButton.Button.onClick.Invoke();
            Assert.AreEqual(1,more);Assert.AreEqual(0,entry.PlayerProgress.SpinCount);
            Assert.IsFalse(entry.Playfield.IsBusy);Assert.IsFalse(entry.Playfield.SpinButton.IsClickAnimationPlaying);
            for(int i=0;i<35;i++)yield return null;
            Assert.AreEqual(0,stale);Assert.IsFalse(entry.Playfield.Reels.IsRunning);
        } finally {
            Object.DestroyImmediate(root);if(had)PlayerPrefs.SetString(key,previous);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            Time.captureDeltaTime=delta;Time.timeScale=scale;Random.state=random;
        }
    }
}
