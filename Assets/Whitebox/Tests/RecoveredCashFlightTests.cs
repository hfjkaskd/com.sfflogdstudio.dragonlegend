using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredCashFlightTests
{
    [UnityTest]
    public IEnumerator BothVersionsUseNativeSizesUnscaledDeparturesScaledFlightAndCreditAfterCallback()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        float scale=Time.timeScale,delta=Time.captureDeltaTime;var random=Random.state;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        var host=new GameObject("Current flight camera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.transform.position=new Vector3(100,0,-10);camera.orthographic=true;camera.orthographicSize=5;camera.cullingMask=32;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        var render=new RenderTexture(1080,1920,24);camera.targetTexture=render;root.GetComponent<Canvas>().worldCamera=camera;
        var previous=RenderTexture.active;var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
        try {
            Time.timeScale=1;Time.captureDeltaTime=.025f;
            for(int version=0;version<2;version++) {
                if(version==1)root.transform.Find("SelectAlternative").GetComponent<Button>().onClick.Invoke();
                { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.CashFlight==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
                Assert.IsNotNull(entry.CashFlight);Assert.AreEqual(version==1,entry.CurrentProfile.isA);
                var flight=entry.CashFlight;var player=entry.PlayerProgress;int sounds=0,callbacks=0;
                flight.SoundRequested+=name=>{Assert.AreEqual("fly",name);sounds++;};
                float before=player.GreenCount;
                flight.Begin(75,()=>{Assert.AreEqual(before,player.GreenCount);callbacks++;player.SetGreenCount(321);},root.transform,true);
                Assert.AreEqual(10,flight.ActiveCashCount);
                var items=root.GetComponentsInChildren<RecoveredCashFlightItem>();Assert.AreEqual(10,items.Length);
                foreach(var item in items) {
                    Assert.AreEqual(new Vector2(version==1?73:92,version==1?76:79),item.Image.rectTransform.sizeDelta);
                    Assert.AreEqual(version==1?"zjm_hb_a":"zjm_hb_b",item.Image.sprite.name);
                }
                // The initial scatter and .3-second wait pause together.
                Time.timeScale=0;var position=items[0].transform.position;
                for(int i=0;i<6;i++)yield return null;
                Assert.AreEqual(position,items[0].transform.position);Assert.IsFalse(items[9].IsScheduled);
                Time.timeScale=1;
                for(int i=0;i<30&&!items[9].IsScheduled;i++)yield return null;
                Assert.IsTrue(items[9].IsScheduled);
                Time.timeScale=0;
                yield return new WaitForSecondsRealtime(.35f);
                foreach(var item in items)Assert.IsTrue(item.IsFlying,"Stagger advances while flight is paused");
                Assert.AreEqual(0,callbacks);Assert.AreEqual(0,sounds);
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=render});
                RenderTexture.active=render;capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-cash-flight-"+(version==1?"a":"b")+".png"),capture.EncodeToPNG());
                position=items[0].transform.position;
                for(int i=0;i<5;i++)yield return null;
                Assert.AreEqual(position,items[0].transform.position);
                foreach(var item in items) {
                    Assert.Less(Vector3.Distance(item.EndPoint,entry.BalancePanel.CashTarget.position),.0001f);
                    var expected=(item.StartPoint+item.EndPoint)*.5f+Vector3.up*(Vector3.Distance(item.StartPoint,item.EndPoint)*.3f);
                    Assert.Less(Vector3.Distance(expected,item.ControlPoint),.0001f);
                }
                Time.timeScale=1;
                for(int i=0;i<30&&callbacks==0;i++)yield return null;
                Assert.AreEqual(1,callbacks);Assert.AreEqual(10,sounds);Assert.AreEqual(396,player.GreenCount);
                Assert.AreEqual(0,flight.ActiveCashCount);Assert.AreEqual(10,flight.ActiveEffectCount);
                for(int i=0;i<30;i++)yield return null;
                Assert.AreEqual(0,flight.ActiveEffectCount);
                // Reused cash/effects must not retain their old endpoint or animation completion.
                flight.Begin(25,()=>callbacks++,root.transform,true);
                float deadline=Time.realtimeSinceStartup+3;
                while(callbacks==1&&Time.realtimeSinceStartup<deadline)yield return null;
                Assert.AreEqual(2,callbacks);Assert.AreEqual(421,player.GreenCount);
                for(int i=0;i<25;i++)yield return null;
                Assert.AreEqual(0,flight.ActiveEffectCount);
            }
            var oldPlayer=entry.PlayerProgress;float oldBalance=oldPlayer.GreenCount;int cancelled=0;
            entry.CashFlight.Begin(100,()=>cancelled++,root.transform,true);
            root.transform.Find("SelectUS").GetComponent<Button>().onClick.Invoke();
            for(int i=0;i<100;i++)yield return null;
            Assert.AreEqual(0,cancelled);Assert.AreEqual(oldBalance,oldPlayer.GreenCount);
            Assert.AreEqual(0,entry.CashFlight.ActiveCashCount);Assert.AreEqual(0,entry.CashFlight.ActiveEffectCount);
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;camera.targetTexture=null;
            Object.Destroy(capture);Object.Destroy(render);Object.Destroy(host);Object.Destroy(root);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
        }
    }
}
