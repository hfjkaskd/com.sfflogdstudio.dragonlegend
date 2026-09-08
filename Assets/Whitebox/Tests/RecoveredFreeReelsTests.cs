using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public sealed class RecoveredFreeReelsTests
{
    [Test]
    public void FifteenMiniReelsInitializeBottomToTopWithInterleavedEffectsOnlyOnce()
    {
        var state=Random.state;
        var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        try {
            Random.InitState(941);
            var expected=new int[15,7];var effectDraws=new int[15];
            for(int i=0;i<15;i++) {
                for(int slot=0;slot<7;slot++)expected[i,slot]=catalog.ModeId(RecoveredSlotType.Free,Random.Range(0,catalog.ModeCount(RecoveredSlotType.Free)));
                effectDraws[i]=Random.Range(0,10000);
            }
            int next=Random.Range(0,10000);
            Random.InitState(941);int visits=0;
            root.Initialize(catalog,(col,row,reel)=> {
                int i=col*3+row;Assert.AreEqual(visits++,i);
                Assert.IsTrue(root.IsInitialized);Assert.AreEqual(7,reel.SlotCount);
                Assert.IsNull(reel.GetComponent<RecoveredBaseReelMotion>());
                Assert.IsNotNull(reel.GetComponent<SortingGroup>());
                Assert.AreEqual("Mask"+(3-row),reel.name);
                Assert.That(reel.transform.parent.localPosition.x,Is.EqualTo(-3.8f+col*1.9f).Within(.00001f));
                Assert.That(reel.transform.parent.localPosition.y,Is.EqualTo(-.01f).Within(.00001f));
                Assert.That(reel.transform.localPosition.y,Is.EqualTo(-1.72f+row*1.72f).Within(.00001f));
                for(int slot=0;slot<7;slot++) {
                    Assert.AreEqual(expected[i,slot],reel.SymbolId(slot));
                    Assert.That(reel.transform.InverseTransformPoint(reel.SymbolAt(slot).Symbol.transform.position).y,
                        Is.EqualTo(slot*1.72f).Within(.00001f));
                }
                Assert.AreEqual(effectDraws[i],Random.Range(0,10000));
            });
            Assert.AreEqual(15,visits);
            root.Initialize(catalog,(col,row,reel)=>Assert.Fail("Native initialization guard must skip subsequent calls."));
            Assert.AreEqual(next,Random.Range(0,10000));
        } finally {Object.DestroyImmediate(root.gameObject);Random.state=state;}
    }

    [UnityTest]
    public IEnumerator IndependentMasksKeepHiddenSlotsOutOfAdjacentRows()
    {
        var state=Random.state;var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        var host=new GameObject("FreeReelPreviewCamera",typeof(Camera));var camera=host.GetComponent<Camera>();
        camera.orthographic=true;camera.orthographicSize=3.6f;camera.transform.position=new Vector3(0,0,-10);
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1;
        var target=new RenderTexture(1080,720,24);camera.targetTexture=target;
        var previous=RenderTexture.active;Texture2D capture=null;
        try {
            Random.InitState(741);root.Initialize(catalog,(col,row,reel)=>{});
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)for(int slot=0;slot<7;slot++)
                root.At(col,row).SymbolAt(slot).Symbol.enabled=col==2 && row==0;
            yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture=new Texture2D(1080,720,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();
            var pixels=capture.GetPixels32();int visible=0,outside=0;
            for(int y=0;y<720;y++)for(int x=0;x<1080;x++) {
                var pixel=pixels[y*1080+x];if(pixel.r<=8 && pixel.g<=8 && pixel.b<=8)continue;
                if(x>=445 && x<=635 && y>=100 && y<=274)visible++;else outside++;
            }
            Assert.Greater(visible,500);Assert.AreEqual(0,outside,"Sibling masks must not reveal this reel's upper hidden slots.");
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)for(int slot=0;slot<7;slot++)
                root.At(col,row).SymbolAt(slot).Symbol.enabled=true;
            yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1080,720),0,0);capture.Apply();
            pixels=capture.GetPixels32();
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                visible=0;
                for(int y=110+row*172;y<265+row*172;y++)for(int x=75+col*190;x<245+col*190;x++) {
                    var pixel=pixels[y*1080+x];if(pixel.r>8||pixel.g>8||pixel.b>8)visible++;
                }
                Assert.Greater(visible,500,"Every authored Free cell must display its own first symbol.");
            }
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-free-reels.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,capture.EncodeToPNG());
        } finally {
            RenderTexture.active=previous;Object.Destroy(root.gameObject);Object.Destroy(host);
            if(capture!=null)Object.Destroy(capture);target.Release();Object.Destroy(target);Random.state=state;
        }
    }
}
