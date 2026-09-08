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

public sealed class RecoveredSymbolTests
{
    [Test]
    public void OriginalCatalogRetainsOrderedBaseAndFreePoolsAndAllSprites()
    {
        var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        Assert.IsTrue(catalog != null); Assert.AreEqual(11,catalog.Count);
        var ids=new List<int>();
        for(int i=0;i<catalog.ModeCount(RecoveredSlotType.Base);i++) ids.Add(catalog.ModeId(RecoveredSlotType.Base,i));
        CollectionAssert.AreEqual(new[]{0,1,2,3,4,5,6,7,9,10},ids);
        ids.Clear();
        for(int i=0;i<catalog.ModeCount(RecoveredSlotType.Free);i++) ids.Add(catalog.ModeId(RecoveredSlotType.Free,i));
        CollectionAssert.AreEqual(new[]{0,1,2,3,4,5,6},ids);
        for(int i=0;i<11;i++) {
            Assert.AreEqual(i,catalog.At(i).id);
            foreach(bool blur in new[]{false,true}) {
                var sprite=catalog.At(i).LoadSprite(blur);
                Assert.IsTrue(sprite != null,catalog.At(i).originalName);
                Assert.IsTrue(sprite.texture != null,sprite.name);
                Assert.AreEqual(100,sprite.pixelsPerUnit);
            }
        }
    }

    [Test]
    public void NativeSymbolSwitchesSharpBlurAndFreeCoverWithoutUiObjects()
    {
        var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var view=Object.Instantiate(Resources.Load<RecoveredSymbolView>("RecoveredSymbols/SymbolItem"));
        try {
            Assert.IsNull(view.GetComponentInChildren<Graphic>(true));
            Assert.IsNull(view.GetComponentInChildren<RectTransform>(true));
            Assert.AreEqual(new Vector3(0,0.86f,0),view.Symbol.transform.localPosition);
            view.Show(catalog,7,RecoveredSlotType.Base,true);
            Assert.AreSame(catalog.Find(7).LoadSprite(true),view.Symbol.sprite);
            Assert.AreEqual(Vector3.one*2,view.Symbol.transform.localScale); Assert.IsFalse(view.Cover.gameObject.activeSelf);
            view.Show(catalog,9,RecoveredSlotType.Base,false,true);
            Assert.AreSame(catalog.Find(9).LoadSprite(false),view.Symbol.sprite);
            Assert.AreEqual(Vector3.one,view.Symbol.transform.localScale); Assert.IsTrue(view.Cover.gameObject.activeSelf);
            view.Show(catalog,4,RecoveredSlotType.Free,true);
            Assert.AreSame(catalog.Find(4).LoadSprite(false),view.Symbol.sprite); Assert.IsTrue(view.Cover.gameObject.activeSelf);
            view.Show(catalog,4,RecoveredSlotType.Free,false,true);
            Assert.IsFalse(view.Cover.gameObject.activeSelf,"Free mode cover follows blur, ignoring hide.");
        } finally {Object.DestroyImmediate(view.gameObject);}
    }

    [UnityTest]
    public IEnumerator RenderCurrentSharpAndBlurSymbolSheet()
    {
        var catalog=Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog");
        var prefab=Resources.Load<RecoveredSymbolView>("RecoveredSymbols/SymbolItem");
        var host=new GameObject("SymbolPreviewCamera",typeof(Camera));
        var camera=host.GetComponent<Camera>(); camera.orthographic=true;camera.orthographicSize=5;
        camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=new Color(0.05f,0.05f,0.05f);camera.cullingMask=1;
        var render=new RenderTexture(1500,1000,24);camera.targetTexture=render;
        var views=new List<RecoveredSymbolView>(22); Texture2D capture=null; var previous=RenderTexture.active;
        try {
            for(int i=0;i<22;i++) {
                int slot=i<11?i:i+1;
                var view=Object.Instantiate(prefab);
                view.transform.position=new Vector3(-5.75f+(slot%6)*2.3f,3.3f-(slot/6)*2.2f-0.86f,0);
                view.Show(catalog,i%11,RecoveredSlotType.Base,i>=11); views.Add(view);
            }
            yield return null;
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=render});
            RenderTexture.active=render; capture=new Texture2D(1500,1000,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1500,1000),0,0);capture.Apply();
            var pixels=capture.GetPixels32();
            for(int i=0;i<22;i++) {
                int slot=i<11?i:i+1; int cx=175+(slot%6)*230,cy=830-(slot/6)*220;int visible=0;
                for(int y=cy-75;y<cy+75;y++) for(int x=cx-75;x<cx+75;x++) {
                    var pixel=pixels[y*1500+x];if(pixel.r>40||pixel.g>40||pixel.b>40)visible++;
                }
                Assert.Greater(visible,100,"Symbol "+i+" must render in the current URP.");
            }
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-symbols.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,capture.EncodeToPNG());
        } finally {
            RenderTexture.active=previous;
            foreach(var view in views) Object.Destroy(view.gameObject);
            Object.Destroy(host);if(capture!=null)Object.Destroy(capture);render.Release();Object.Destroy(render);
        }
    }
}
