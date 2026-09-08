using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public sealed class RecoveredLuckySpinColumnTests
{
    [UnityTest]
    public IEnumerator ActualColumnsScrollWithOriginalDotCyclesAndFinishBeforeBounceSettles()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var columns=new RecoveredLuckySpinColumn[4];
        var host=new GameObject("Lucky column capture",typeof(RectTransform),typeof(Canvas));
        var cameraHost=new GameObject("Lucky column camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=camera;canvas.planeDistance=10;
        var texture=new RenderTexture(1080,480,24);camera.targetTexture=texture;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.02f;
            for(int column=0;column<4;column++) {
                var value=Object.Instantiate(Resources.Load<RecoveredLuckySpinColumn>("RecoveredUI/LuckySpinCol"+(column+1)),host.transform,false);columns[column]=value;
                value.Initialize(column,9);
                Assert.AreEqual(new Vector2(151,296),((RectTransform)value.transform).sizeDelta);
                Assert.IsNotNull(value.GetComponent<RectMask2D>());Assert.AreEqual("9",value.Digit(0).text);
                Assert.AreEqual(column==1||column==2?".":"0",value.Digit(1).text);
                for(int i=0;i<5;i++){Assert.AreEqual(new Vector2(0,296*i),value.Digit(i).rectTransform.anchoredPosition);Assert.IsNotNull(value.Digit(i).font);}
            }
            yield return null;
            foreach(var value in columns)value.StartSpin();
            Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
            foreach(var value in columns)Assert.AreEqual(0,value.RotNode.anchoredPosition.y);
            Time.timeScale=1;yield return null;
            float firstSpeed=10000*(1-Mathf.Cos(Mathf.PI/8));
            foreach(var value in columns){Assert.That(value.CurrentSpeed,Is.EqualTo(firstSpeed).Within(.01f));Assert.That(value.RotNode.anchoredPosition.y,Is.EqualTo(-firstSpeed*.02f).Within(.01f));}
            for(int i=0;i<8;i++)yield return null;
            // Every reachable digit, including the two interior decimal-point targets.
            for(int pass=0;pass<11;pass++) {
                int callbacks=0,bounces=0;
                for(int col=0;col<4;col++) {
                    var value=columns[col];int target=(col==0||col==3)?pass%10:pass;
                    if(pass>0)value.StartSpin();value.SetTarget(target);
                    value.StopRoll(stopped=>{callbacks++;Assert.IsTrue(stopped.IsStopped);Assert.IsFalse(stopped.IsSpinning);},()=>{
                        bounces++;Assert.IsTrue(value.IsSpinning,"Native bounce callback precedes clearing IsSpinning.");
                        Assert.That(value.RotNode.anchoredPosition.y,Is.EqualTo(0).Within(.01f));
                    });
                }
                for(int i=0;i<50&&callbacks<4;i++)yield return null;
                Assert.AreEqual(4,callbacks);Assert.Greater(bounces,0);
                for(int col=0;col<4;col++) {
                    string expected=pass==10&&(col==1||col==2)?".":(pass%10).ToString();
                    Assert.AreEqual(expected,columns[col].Digit(0).text,"Column "+col+", pass "+pass);
                }
                for(int i=0;i<15;i++)yield return null;
                foreach(var value in columns)Assert.That(value.RotNode.anchoredPosition.y,Is.EqualTo(0).Within(.001f));
            }
            Capture(camera,texture);
            foreach(var value in columns) {
                value.Initialize(value.ColumnIndex,9,true);int immediate=0;value.StartSpin();
                Assert.IsFalse(value.IsSpinning);for(int i=0;i<5;i++)Assert.IsFalse(value.Digit(i).gameObject.activeSelf);
                value.StopRoll(c=>immediate++,()=>Assert.Fail("Dot-only columns do not bounce."));Assert.AreEqual(1,immediate);
            }
        } finally {camera.targetTexture=null;Object.Destroy(texture);Object.Destroy(host);Object.Destroy(cameraHost);Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
    [UnityTest]
    public IEnumerator ImmediateStopCancelsMotionButCompletesExistingWaiter()
    {
        float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var value=Object.Instantiate(Resources.Load<RecoveredLuckySpinColumn>("RecoveredUI/LuckySpinCol1"));
        try {
            Time.timeScale=1;Time.captureDeltaTime=.02f;value.Initialize(0,9);value.StartSpin();yield return null;
            int completed=0;value.SetTarget(5);value.StopRoll(c=>completed++,()=>Assert.Fail("Cancelled acceleration must not bounce."));value.StopImmediate();
            Assert.IsFalse(value.IsSpinning);Assert.IsTrue(value.IsStopped);
            for(int i=0;i<20;i++)yield return null;
            Assert.AreEqual(1,completed);Assert.AreEqual(0,value.RotNode.anchoredPosition.y);
        } finally {Object.Destroy(value.gameObject);Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
    private static void Capture(Camera camera,RenderTexture target)
    {
        var prior=RenderTexture.active;var image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
        try {Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});RenderTexture.active=target;
            image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-lucky-spin-columns.png"),image.EncodeToPNG());
        } finally {RenderTexture.active=prior;Object.Destroy(image);}
    }
}
