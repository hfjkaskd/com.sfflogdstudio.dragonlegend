using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredCoinStopPresenterTests
{
    [UnityTest]
    public IEnumerator ActualReelStopsReplaceCoinsAndWrapReusesNativeEffectPool()
    {
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        var random=Random.state;float delta=Time.captureDeltaTime,scale=Time.timeScale;PlayerPrefs.DeleteKey(key);
        var root=Object.Instantiate(Resources.Load<GameObject>("Whitebox/GameEntry"));var entry=root.GetComponent<GameEntry>();
        var cameraHost=new GameObject("Coin stop camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(100,0,-10);camera.orthographic=true;camera.orthographicSize=5;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;
        var target=new RenderTexture(1080,1920,24);camera.targetTexture=target;root.GetComponent<Canvas>().worldCamera=camera;
        var previous=RenderTexture.active;Texture2D capture=null;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            { float resourceDeadline=Time.realtimeSinceStartup+5; while(entry.Playfield==null&&Time.realtimeSinceStartup<resourceDeadline)yield return null; }
            Assert.IsNotNull(entry.Playfield);var field=entry.Playfield;var presenter=field.CoinStops;
            Assert.IsNotNull(presenter);int sounds=0,vibrations=0;
            presenter.CoinShowSoundRequested+=()=>sounds++;
            presenter.VibrationRequested+=ms=>{Assert.AreEqual(200,ms);vibrations++;};
            var columns=new[]{new[]{9,0,9},new[]{1,2,3},new[]{4,5,6},new[]{0,1,2},new[]{3,9,4}};
            field.Reels.Begin(-1,index=>columns[index]);
            for(int i=0;i<200&&field.Reels.IsRunning;i++)yield return null;
            for(int i=0;i<15;i++)yield return null;
            Assert.AreEqual(3,presenter.CreatedCount);Assert.AreEqual(3,presenter.ActiveCount);
            Assert.AreEqual(3,sounds);Assert.AreEqual(3,vibrations);
            var original=new HashSet<RecoveredCoinStopEffect>();
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                var effect=presenter.CoinAt(col,row);var symbol=field.Reels.ReelAt(col).SymbolAt(row).Symbol;
                Assert.AreEqual(columns[col][row]!=9,symbol.gameObject.activeSelf);
                if(columns[col][row]!=9){Assert.IsNull(effect);continue;}
                original.Add(effect);Assert.IsNotNull(effect);
                Assert.Less(Vector3.Distance(symbol.transform.position,effect.transform.position),.00001f);
                Assert.IsFalse(effect.IsScaling);Assert.AreEqual(.7f,effect.transform.localScale.x,.0001f);
                // Only monetary information uses UI; the coin and its effects remain native renderers.
                Assert.AreEqual(1,effect.GetComponentsInChildren<Graphic>(true).Length);
                Assert.IsFalse(effect.RewardText.Label.gameObject.activeSelf);
                Assert.AreEqual(5,effect.GetComponentsInChildren<SpriteRenderer>().Length);
            }
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            var pixels=capture.GetPixels32();
            foreach(var effect in original) {
                var point=camera.WorldToScreenPoint(effect.transform.position);int gold=0;
                for(int y=(int)point.y-65;y<(int)point.y+65;y++)for(int x=(int)point.x-65;x<(int)point.x+65;x++) {
                    var pixel=pixels[y*1080+x];if(pixel.r>100&&pixel.g>70&&pixel.r>pixel.b)gold++;
                }
                Assert.Greater(gold,1500,"World-space coin effect must actually render.");
            }
            File.WriteAllBytes(Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-coin-stops.png")),capture.EncodeToPNG());
            var reel=field.Reels.ReelAt(0);reel.Refresh(10000,.1f,false);
            Assert.AreEqual(1,presenter.ActiveCount);Assert.IsNull(presenter.CoinAt(0,0));
            reel.SetOffsetPixels(0);reel.ApplyBaseColumn(columns[0]);presenter.ShowColumn(0);
            Assert.AreEqual(3,presenter.CreatedCount);Assert.AreEqual(3,presenter.ActiveCount);
            Assert.IsTrue(original.Contains(presenter.CoinAt(0,0)));Assert.IsTrue(original.Contains(presenter.CoinAt(0,2)));
            presenter.ShowColumn(0);Assert.AreEqual(3,presenter.ActiveCount);Assert.IsNull(presenter.CoinAt(0,0));
            Assert.AreEqual(5,sounds);Assert.AreEqual(5,vibrations);
            presenter.Unbind();Assert.AreEqual(0,presenter.ActiveCount);
        } finally {
            Object.DestroyImmediate(root);Object.Destroy(cameraHost);RenderTexture.active=previous;
            if(capture!=null)Object.Destroy(capture);target.Release();Object.Destroy(target);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);PlayerPrefs.Save();
            Random.state=random;Time.captureDeltaTime=delta;Time.timeScale=scale;
        }
    }
}
