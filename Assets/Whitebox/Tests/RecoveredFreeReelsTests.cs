using System.Collections;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

public sealed class RecoveredFreeReelsTests
{
    private static RecoveredFreeSpinResult Result(int specials)
    {
        var weights=new List<int>();for(int i=0;i<=specials;i++)weights.Add(i==specials?1000000:0);
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig {Rrggiomg=new RrggiomgPoro {
            QoinOmoinrKgiitr=weights,RollOmoinrKgiitr=weights,RollRipgKgiitr=new List<int>{1,1,1}}});
        var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{0,1,2,3,4,5,6});
        int attempts=0;while(result.IsGenerating && attempts++<10000)result.Step();
        Assert.IsFalse(result.IsGenerating);return result;
    }

    [Test]
    public void FifteenMiniReelsInitializeBottomToTopWithInterleavedEffectsOnlyOnce()
    {
        var state=Random.state;
        var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        try {
            Random.InitState(39);var result=Result(5);
            Random.InitState(941);
            var expected=new int[15,7];var effectDraws=new int[15];
            for(int i=0;i<15;i++) {
                for(int slot=0;slot<7;slot++)expected[i,slot]=catalog.ModeId(RecoveredSlotType.Free,Random.Range(0,catalog.ModeCount(RecoveredSlotType.Free)));
                int id=result.GetSymbol(i/3,i%3);
                if(id==9 || id==11)effectDraws[i]=Random.Range(0,10000);
            }
            int next=Random.Range(0,10000);
            Random.InitState(941);int visits=0,last=-1;
            root.Initialize(catalog,result,(col,row,reel,id)=> {
                int i=col*3+row;Assert.Greater(i,last);last=i;visits++;
                Assert.AreEqual(result.GetSymbol(col,row),id);Assert.That(id==9 || id==11);
                for(int slot=0;slot<7;slot++)Assert.AreEqual(expected[i,slot],reel.SymbolId(slot));
                Assert.AreEqual(effectDraws[i],Random.Range(0,10000));
            });
            for(int col=0;col<5;col++)for(int row=0;row<3;row++) {
                int i=col*3+row;var reel=root.At(col,row);int id=result.GetSymbol(col,row);
                Assert.IsTrue(root.IsInitialized);Assert.AreEqual(7,reel.SlotCount);
                Assert.IsNull(reel.GetComponent<RecoveredBaseReelMotion>());
                Assert.IsNotNull(reel.GetComponent<SortingGroup>());
                Assert.AreEqual("Mask"+(3-row),reel.name);
                Assert.That(reel.transform.parent.localPosition.x,Is.EqualTo(-3.8f+col*1.9f).Within(.00001f));
                Assert.That(reel.transform.parent.localPosition.y,Is.EqualTo(-.01f).Within(.00001f));
                Assert.That(reel.transform.localPosition.y,Is.EqualTo(-1.72f+row*1.72f).Within(.00001f));
                for(int slot=0;slot<7;slot++) {
                    int target=slot==0 && id!=9 && id!=11?id:expected[i,slot];
                    Assert.AreEqual(target,reel.SymbolId(slot));
                    Assert.That(reel.transform.InverseTransformPoint(reel.SymbolAt(slot).Symbol.transform.position).y,
                        Is.EqualTo(slot*1.72f).Within(.00001f));
                }
                Assert.AreEqual(id!=9 && id!=11,reel.SymbolAt(0).Cover.gameObject.activeSelf);
            }
            Assert.AreEqual(10,visits);
            root.Initialize(catalog,result,(col,row,reel,id)=>Assert.Fail("Native initialization guard must skip subsequent calls."));
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
            Random.InitState(741);var result=Result(0);
            root.Initialize(catalog,result,(col,row,reel,id)=>Assert.Fail("This result contains no specials."));
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)for(int slot=0;slot<7;slot++) {
                root.At(col,row).SymbolAt(slot).Symbol.enabled=col==2 && row==0;
                root.At(col,row).SymbolAt(slot).Cover.enabled=col==2 && row==0;
            }
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
            for(int col=0;col<5;col++)for(int row=0;row<3;row++)for(int slot=0;slot<7;slot++) {
                root.At(col,row).SymbolAt(slot).Symbol.enabled=true;
                root.At(col,row).SymbolAt(slot).Cover.enabled=true;
            }
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
