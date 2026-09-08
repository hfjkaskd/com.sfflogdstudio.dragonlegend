using System.Collections;
using System.IO;
using DragonLegend.Whitebox;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

public sealed class RecoveredNpcArtTests
{
    [UnityTest]
    public IEnumerator CurrentNpcLayersRenderOriginalLayoutAndClips()
    {
        var host=new GameObject("NPC verification canvas",typeof(RectTransform),typeof(Canvas));
        var canvas=host.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;
        ((RectTransform)host.transform).sizeDelta=new Vector2(1080,1920);
        var root=Object.Instantiate(Resources.Load<GameObject>("RecoveredUI/Npc"),host.transform);
        var players=root.GetComponentsInChildren<RecoveredRegionAnimator>(true);
        var cameraHost=new GameObject("NPC verification camera",typeof(Camera));var camera=cameraHost.GetComponent<Camera>();
        camera.transform.position=new Vector3(0,0,-10);camera.orthographic=true;camera.orthographicSize=960;
        camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1;canvas.worldCamera=camera;
        var target=new RenderTexture(540,960,24);camera.targetTexture=target;var previous=RenderTexture.active;
        var capture=new Texture2D(540,960,TextureFormat.RGB24,false);float scale=Time.timeScale,delta=Time.captureDeltaTime;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;yield return null;
            Assert.AreEqual(2,players.Length);Assert.IsFalse(players[1].gameObject.activeSelf);
            Assert.AreEqual(new Vector2(0,355),((RectTransform)players[0].transform).anchoredPosition);
            Assert.AreEqual(new Vector2(-.0034179688f,19.859985f),((RectTransform)players[1].transform).anchoredPosition);
            var eventData=Resources.Load<RecoveredRigAnimation>("RecoveredUI/JackpotPopupArt/ef_long/win_penhuo");
            Assert.AreEqual(1,eventData.events.Length);Assert.AreEqual("huo",eventData.events[0].name);
            Assert.AreEqual(.766666710f,eventData.events[0].time,.000001f);
            Time.timeScale=0;yield return null;yield return null;
            var names=new[]{"idle","wind","fire"};
            for(int state=0;state<3;state++) {
                players[0].Sample(state==0?0:1,.95f);players[1].gameObject.SetActive(state!=0);
                if(state!=0)players[1].Sample(state==1?2:3,.95f);
                Canvas.ForceUpdateCanvases();RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});
                RenderTexture.active=target;capture.ReadPixels(new Rect(0,0,540,960),0,0);capture.Apply();
                File.WriteAllBytes(Path.Combine(Application.dataPath,"../Artifacts/current-npc-"+names[state]+".png"),capture.EncodeToPNG());
                int visible=0;foreach(var p in capture.GetPixels32())if(p.r>40||p.g>40||p.b>40)visible++;
                Assert.Greater(visible,10000,names[state]);
            }
            int completed=0;players[0].PlayOnce(1,()=>completed++);
            for(int i=0;i<8;i++)yield return null;Assert.AreEqual(0,completed);
            Time.timeScale=1;for(int i=0;i<70;i++)yield return null;Assert.AreEqual(1,completed);
        }finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;RenderTexture.active=previous;camera.targetTexture=null;
            target.Release();Object.Destroy(target);Object.Destroy(capture);Object.Destroy(host);Object.Destroy(cameraHost);
        }
    }
}
