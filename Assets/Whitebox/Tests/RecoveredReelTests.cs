using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public sealed class RecoveredReelTests
{
    [Test]
    public void BoundaryCarriesLastThreeSlotsAndConsumesExactlyFourRandomDraws()
    {
        var state=Random.state;
        var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var reel=Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        try {
            Random.InitState(87123); var expected=new int[11];
            for(int i=0;i<11;i++)expected[i]=catalog.ModeId(RecoveredSlotType.Base,Random.Range(0,10));
            int next=Random.Range(0,100000);
            Random.InitState(87123);reel.Initialize(catalog,RecoveredSlotType.Base);
            var originals=new RecoveredSymbolView[7];
            for(int i=0;i<7;i++){Assert.AreEqual(expected[i],reel.SymbolId(i));originals[i]=reel.SymbolAt(i);}
            var events=new List<string>();reel.EffectsClearRequested+=()=>events.Add("effects");
            reel.CoinsClearRequested+=()=>events.Add("coins");reel.BallsClearRequested+=()=>events.Add("balls");
            reel.Refresh(688,1,false);Assert.AreEqual(-688,reel.OffsetPixels);Assert.IsEmpty(events);
            reel.Refresh(1,1,true);Assert.AreEqual(-1,reel.OffsetPixels);
            CollectionAssert.AreEqual(new[]{"effects","coins","balls"},events);
            for(int i=0;i<7;i++) {
                Assert.AreEqual(expected[i+4],reel.SymbolId(i));Assert.AreSame(originals[i],reel.SymbolAt(i));
                Assert.AreSame(catalog.Find(expected[i+4]).LoadSprite(true),reel.SymbolAt(i).Symbol.sprite);
            }
            Assert.AreEqual(next,Random.Range(0,100000));
            events.Clear();reel.Refresh(2000,1,false);
            Assert.AreEqual(-1313,reel.OffsetPixels,"One wrap only, even with a large delta.");Assert.AreEqual(3,events.Count);
            reel.Initialize(catalog,RecoveredSlotType.Base);
            Assert.AreEqual(0,reel.OffsetPixels);for(int i=0;i<7;i++)Assert.AreSame(originals[i],reel.SymbolAt(i));
        } finally {Object.DestroyImmediate(reel.gameObject);Random.state=state;}
    }

    [Test]
    public void FreeCycleRequestsFakeCoinAfterSecondWrapThenSamplesFourOrFive()
    {
        var state=Random.state;var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var reel=Object.Instantiate(Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel"));
        try {
            Random.InitState(6633);
            for(int i=0;i<15;i++)Random.Range(0,7); // Seven initial + four on each of two wraps.
            int interval=Random.Range(4,6);int next=Random.Range(0,100000);
            Random.InitState(6633);reel.Initialize(catalog,RecoveredSlotType.Free);int fake=0;reel.FakeCoinRequested+=()=>fake++;
            reel.Refresh(689,1,true);Assert.AreEqual(0,fake);
            reel.Refresh(688,1,true);Assert.AreEqual(1,fake);Assert.AreEqual(next,Random.Range(0,100000));
            for(int i=1;i<=interval;i++){reel.Refresh(688,1,true);Assert.AreEqual(i==interval?2:1,fake);}
        } finally {Object.DestroyImmediate(reel.gameObject);Random.state=state;}
    }

    [UnityTest]
    public IEnumerator CurrentFiveReelsRenderOnlyInsideOriginalClipRectangles()
    {
        var state=Random.state;var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var prefab=Resources.Load<RecoveredReelView>("RecoveredSymbols/Reel");
        var host=new GameObject("ReelPreviewCamera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.6f;camera.transform.position=new Vector3(0,0,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1;
        var render=new RenderTexture(1080,720,24);camera.targetTexture=render;
        var reels=new List<RecoveredReelView>(5);Texture2D capture=null;var previous=RenderTexture.active;
        try {
            Random.InitState(777);
            for(int i=0;i<5;i++) {
                var reel=Object.Instantiate(prefab);reel.transform.position=new Vector3(-3.8f+i*1.9f,-0.01f,0);
                reel.Initialize(catalog,RecoveredSlotType.Base);reel.Refresh(344,1,false);reels.Add(reel);
            }
            yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=render});
            RenderTexture.active=render;capture=new Texture2D(1080,720,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();var pixels=capture.GetPixels32();
            int outside=0;
            for(int y=625;y<720;y++)for(int x=0;x<1080;x++){var c=pixels[y*1080+x];if(c.r>8||c.g>8||c.b>8)outside++;}
            Assert.AreEqual(0,outside,"Offscreen slots must be clipped by native SpriteMask.");
            for(int i=0;i<5;i++) {
                int visible=0;for(int y=120;y<600;y++)for(int x=75+i*190;x<245+i*190;x++) {
                    var c=pixels[y*1080+x];if(c.r>8||c.g>8||c.b>8)visible++;
                }
                Assert.Greater(visible,500,"Reel "+i+" must contain visible symbols.");
            }
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-reels.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,capture.EncodeToPNG());
        } finally {
            RenderTexture.active=previous;foreach(var reel in reels)Object.Destroy(reel.gameObject);
            Object.Destroy(host);if(capture!=null)Object.Destroy(capture);render.Release();Object.Destroy(render);Random.state=state;
        }
    }
}
