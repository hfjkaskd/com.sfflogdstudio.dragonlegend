using System.Collections;
using System.IO;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public sealed class RecoveredBalancePanelTests
{
    [UnityTest]
    public IEnumerator OriginalBalanceAndLevelResourcesImportAndRender()
    {
        var prefab=Resources.Load<GameObject>("RecoveredUI/BalancePanel");
        Assert.IsTrue(prefab != null);
        var cameraHost=new GameObject("BalancePreviewCamera",typeof(Camera));
        var canvasHost=new GameObject("BalancePreviewCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        canvasHost.layer=5; // Screen-space Canvas draw calls use the root Canvas layer.
        var render=new RenderTexture(1080,1920,24);
        GameObject panel=null; Texture2D capture=null;
        var previous=RenderTexture.active;
        try {
            var camera=cameraHost.GetComponent<Camera>();
            camera.transform.position=new Vector3(100,0,-10);
            camera.orthographic=true; camera.orthographicSize=5;
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
            camera.cullingMask=1<<5;camera.targetTexture=render;
            var canvas=canvasHost.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;
            canvas.worldCamera=camera;canvas.planeDistance=10;
            var scaler=canvasHost.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
            panel=Object.Instantiate(prefab,canvas.transform,false);
            var texts=panel.GetComponentsInChildren<TextMeshProUGUI>(true);
            Assert.AreEqual(3,texts.Length);
            foreach(var text in texts){Assert.IsTrue(text.font != null,text.name);Assert.IsTrue(text.fontSharedMaterial != null,text.name);}
            var images=panel.GetComponentsInChildren<Image>(true);
            Assert.AreEqual(7,images.Length);
            foreach(var image in images) {
                Assert.IsTrue(image.sprite != null,image.name+" requires its original Sprite asset.");
                Assert.IsTrue(image.sprite.texture != null,image.name+" requires its original texture.");
                Assert.Greater(image.sprite.rect.width,0,image.name);
            }
            Canvas.ForceUpdateCanvases();
            yield return null; yield return null;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest {destination=render});
            RenderTexture.active=render;
            capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
            int colored=0;
            foreach(var pixel in capture.GetPixels32()) if(pixel.r>8 || pixel.g>8 || pixel.b>8) colored++;
            Assert.Greater(colored,1000,"The current prefab render must contain visible content.");
            string path=Path.GetFullPath(Path.Combine(Application.dataPath,"../Artifacts/current-balance-panel.png"));
            Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllBytes(path,capture.EncodeToPNG());
        } finally {
            RenderTexture.active=previous;
            if(capture!=null)Object.Destroy(capture);
            if(panel!=null)Object.Destroy(panel);
            Object.Destroy(canvasHost);Object.Destroy(cameraHost);
            render.Release();Object.Destroy(render);
        }
        yield return null;
    }
}