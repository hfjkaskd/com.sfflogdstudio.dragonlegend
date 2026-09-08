using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredBonusCardArtTests
{
    [UnityTest]
    public IEnumerator CurrentCardsRenderEveryFaceAndNativeTurnSpeed()
    {
        var host=new GameObject("Bonus card verification canvas",typeof(RectTransform),typeof(Canvas));
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;host.layer=5;
        ((RectTransform)host.transform).sizeDelta=new Vector2(1280,800);
        var cameraHost=new GameObject("Bonus card verification camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=400;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=32;canvas.worldCamera=camera;
        var target=new RenderTexture(1280,800,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(1280,800,TextureFormat.RGB24,false);
        var players=new RecoveredRegionAnimator[15];float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=0;Time.captureDeltaTime=.025f;
            var prefab=Resources.Load<RecoveredRegionAnimator>("RecoveredUI/JackpotPopupArt/ef_jinbi");
            Assert.IsNotNull(prefab);
            for(int i=0;i<players.Length;i++) {
                players[i]=Object.Instantiate(prefab,host.transform);
                var rect=(RectTransform)players[i].transform;rect.anchoredPosition=new Vector2((i%5-2)*240,(1-i/5)*240);
                Assert.AreEqual(new Vector2(187,172),rect.sizeDelta);
            }
            yield return null;yield return null;
            var idle=new[]{5,2,4,1,3};var turn=new[]{10,7,9,6,8};
            for(int i=0;i<players.Length;i++)players[i].Sample(i<5?idle[i%5]:turn[i%5],i<5?.173f:i<10?.173f:.55f);
            Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,1280,800),0,0);capture.Apply();
            File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-bonus-card-faces.png"),capture.EncodeToPNG());
            int visible=0;foreach(var p in capture.GetPixels32())if(p.r>40||p.g>40||p.b>40)visible++;
            Assert.Greater(visible,50000);
            var player=players[0];int completed=0;
            player.PlaybackSpeed=3;player.PlayOnce(10,()=>{completed++;player.Play(5);});
            for(int i=0;i<6;i++)yield return null;Assert.AreEqual(0,completed);
            Time.timeScale=1;
            for(int i=0;i<7;i++)yield return null;Assert.AreEqual(0,completed);
            for(int i=0;i<5;i++)yield return null;Assert.AreEqual(1,completed);Assert.AreEqual(5,player.Selected);
            Assert.AreEqual(3,player.GetComponent<Animation>()["idle_zhao"].speed);
            player.PlaybackSpeed=1;Assert.AreEqual(1,player.GetComponent<Animation>()["idle_zhao"].speed);
            foreach(var region in player.Rig.regions)Assert.IsFalse(region.deformActive,"Turn deformation must not leak into idle.");
        }finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);Object.Destroy(cameraHost);
        }
    }
}
