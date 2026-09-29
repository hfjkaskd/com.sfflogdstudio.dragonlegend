using System.Collections;
using System.Collections.Generic;
using DragonLegend.Whitebox;
using DragonLegend.Whitebox.Recovered;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class RecoveredFreeBallFlightTests
{
    [Test]
    public void ActivationKeepsWorldPoseAndRendersAboveFire()
    {
        var source=new GameObject("Source");
        var fire=new GameObject("Fire",typeof(Canvas));
        var ball=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"),source.transform);
        try {
            source.transform.localScale=Vector3.one*1.5f;
            ball.transform.position=new Vector3(2,3,0);
            fire.transform.position=new Vector3(-5,7,0);
            fire.transform.localScale=Vector3.one*.01f;
            var canvas=fire.GetComponent<Canvas>();canvas.sortingOrder=2;
            var position=ball.transform.position;var size=ball.transform.lossyScale;
            ball.AttachForActivation(fire.transform);
            Assert.That(Vector3.Distance(position,ball.transform.position),Is.LessThan(.0001f));
            Assert.That(Vector3.Distance(size,ball.transform.lossyScale),Is.LessThan(.0001f));
            var sorting=ball.GetComponent<UnityEngine.Rendering.SortingGroup>();
            Assert.IsTrue(sorting.sortAtRoot);
            Assert.Greater(sorting.sortingOrder,canvas.sortingOrder);
            Assert.IsTrue(ball.gameObject.activeInHierarchy);
            ball.gameObject.SetActive(false);
            Assert.AreEqual(0,sorting.sortingOrder);
            Assert.IsFalse(sorting.sortAtRoot);
        } finally {Object.DestroyImmediate(source);Object.DestroyImmediate(fire);}
    }

    [UnityTest]
    public IEnumerator FlightIsVisibleAboveTheBoardInTheAuthoredMainScene()
    {
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        string key=RecoveredPlayerStore.OriginalKey;bool had=PlayerPrefs.HasKey(key);string saved=PlayerPrefs.GetString(key);
        PlayerPrefs.SetString(key,JsonUtility.ToJson(new PlayerData{GuideStep=3,Level=1,SpinCount=50}));
        UnityEngine.SceneManagement.Scene scene=default;RenderTexture target=null;Camera camera=null;
        var previous=RenderTexture.active;
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;
            yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("GameEntry",UnityEngine.SceneManagement.LoadSceneMode.Additive);
            scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameEntry");
            GameEntry game=null;
            foreach(var root in scene.GetRootGameObjects()){var found=root.GetComponentInChildren<GameEntry>();if(found!=null)game=found;}
            for(int i=0;i<300&&game.CoreRound==null;i++)yield return null;
            Assert.IsNotNull(game.CoreRound);
            camera=game.GetComponent<Canvas>().worldCamera;target=new RenderTexture(1080,1920,24);target.Create();camera.targetTexture=target;
            var mode=game.Playfield.ModeView;
            game.FreeSpinResult.Begin(new[]{3});while(game.FreeSpinResult.IsGenerating)game.FreeSpinResult.Step();
            game.PlayerProgress.GameSlotType=RecoveredSlotType.Free;mode.InitializeFreeReels();mode.ApplyCurrent();
            var reels=mode.FreeReels;
            var source=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"),reels.At(2,0).SymbolAt(0).transform);
            source.transform.position=reels.At(2,0).transform.position;source.Initialize(2);source.Clipping.Bind(null);
            for(int i=0;i<5;i++)yield return null;
            var flight=reels.Specials.FlyBall(source,game.Playfield.BallDestination,null);
            for(int i=0;i<3;i++)yield return null;
            Time.timeScale=0;yield return null;
            var renderer=flight.Art.Rig.GetComponent<MeshRenderer>();
            Assert.Greater(flight.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder,game.GetComponent<Canvas>().sortingOrder);
            Assert.IsTrue(flight.GetComponent<UnityEngine.Rendering.SortingGroup>().sortAtRoot);
            Debug.Log("BALL_FLIGHT start="+source.transform.position+" current="+flight.transform.position+" end="+flight.FlightEnd+" bounds="+renderer.bounds+" sourceScale="+source.transform.lossyScale+" flightScale="+flight.transform.lossyScale);
            Canvas.ForceUpdateCanvases();
            UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
            RenderTexture.active=target;var capture=new Texture2D(1080,1920,TextureFormat.RGB24,false);
            try{
                capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();
                System.IO.File.WriteAllBytes(System.IO.Path.Combine(Application.dataPath,"../Artifacts/current-main-ball-flight.png"),capture.EncodeToPNG());
                var visible=capture.GetPixels32();renderer.enabled=false;
                UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera,new UnityEngine.Rendering.RenderPipeline.StandardRequest{destination=target});
                capture.ReadPixels(new Rect(0,0,1080,1920),0,0);capture.Apply();var hidden=capture.GetPixels32();
                int changed=0;
                for(int i=0;i<visible.Length;i++)if(System.Math.Abs(visible[i].r-hidden[i].r)+System.Math.Abs(visible[i].g-hidden[i].g)+System.Math.Abs(visible[i].b-hidden[i].b)>30)changed++;
                renderer.enabled=true;
                Assert.Greater(changed,1000,"Flight mesh must contribute visible pixels, not just move behind the board.");
            }
            finally{Object.Destroy(capture);}
            Assert.IsTrue(flight.IsFlying);Assert.Greater(flight.transform.position.y,source.transform.position.y);
            reels.Specials.ReleaseFlightBall(flight);Object.Destroy(source.gameObject);
        } finally {
            Time.timeScale=scale;Time.captureDeltaTime=delta;Random.state=random;RenderTexture.active=previous;
            if(camera!=null)camera.targetTexture=null;if(target!=null)Object.Destroy(target);
            if(had)PlayerPrefs.SetString(key,saved);else PlayerPrefs.DeleteKey(key);
            if(scene.IsValid())UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);
        }
    }

    [UnityTest]
    public IEnumerator FlightClonesTypeThroughSharedPoolAndRetainsBothObjectsAtArrival()
    {
        var random=Random.state;float scale=Time.timeScale,delta=Time.captureDeltaTime;
        var weights=new List<int>();for(int i=0;i<=15;i++)weights.Add(i==15?1000000:0);
        var rules=new RecoveredGameplayRules(new GoldenDragonAutoGenConfig{Rrggiomg=new RrggiomgPoro{
            QoinOmoinrKgiitr=weights,RollOmoinrKgiitr=new List<int>{1000000},RollRipgKgiitr=new List<int>{1}}});
        var result=new RecoveredFreeSpinResult(rules);result.Begin(new[]{3});while(result.IsGenerating)result.Step();
        var root=Object.Instantiate(Resources.Load<RecoveredFreeReels>("RecoveredSymbols/FreeReels"));
        root.Initialize(Resources.Load<RecoveredSymbolCatalog>("RecoveredSymbols/OriginalSymbolCatalog"),result);
        var parent=new GameObject("Flight test parent");parent.transform.position=new Vector3(2,1,0);parent.transform.localScale=Vector3.one*1.5f;
        var source=Object.Instantiate(Resources.Load<RecoveredFreeBall>("RecoveredSymbols/FreeBall"),parent.transform);
        var target=new GameObject("Flight test target");target.transform.position=new Vector3(-2,4,0);
        try {
            Time.timeScale=1;Time.captureDeltaTime=.05f;RecoveredFreeBall previous=null;
            for(int type=0;type<3;type++) {
                source.Initialize(type);int arrived=0;var start=source.transform.position;var end=target.transform.position;
                var flight=root.Specials.FlyBall(source,target.transform,(copy,original)=>{
                    Assert.AreSame(source,original);Assert.AreNotSame(source,copy);Assert.IsTrue(copy.gameObject.activeInHierarchy);
                    Assert.AreEqual(1,root.Specials.FlightBallCount);arrived++;
                });
                if(previous!=null)Assert.AreSame(previous,flight);
                Assert.AreEqual(type,flight.BallType);Assert.AreEqual(string.Empty,flight.Reward.text);
                Assert.AreSame(parent.transform,flight.transform.parent);Assert.AreEqual(parent.transform.childCount-1,flight.transform.GetSiblingIndex());
                Assert.AreEqual(Vector3.one*.8f,flight.transform.localScale);Assert.AreEqual(start,flight.transform.position);
                Assert.AreEqual(end,flight.FlightEnd);Assert.AreEqual(1,root.Specials.CreatedBalls);
                Time.timeScale=0;for(int i=0;i<3;i++)yield return null;
                Assert.AreEqual(start,flight.transform.position);Assert.AreEqual(0,arrived);
                Time.timeScale=1;for(int i=0;i<3;i++)yield return null;
                var control=(start+end)*.5f+Vector3.up*(Vector3.Distance(start,end)*.3f);
                var midpoint=start*.25f+control*.5f+end*.25f;
                Assert.That(Vector3.Distance(midpoint,flight.transform.position),Is.LessThan(.001f));
                target.transform.position+=Vector3.right; // Native snapshots its destination.
                for(int i=0;i<10&&arrived==0;i++)yield return null;
                Assert.AreEqual(1,arrived);Assert.IsFalse(flight.IsFlying);Assert.That(Vector3.Distance(end,flight.transform.position),Is.LessThan(.001f));
                Assert.AreEqual(start,source.transform.position);Assert.IsTrue(source.gameObject.activeSelf);
                Assert.AreEqual(1,root.Specials.ActiveBalls);root.Specials.ReleaseFlightBall(flight);
                Assert.AreEqual(0,root.Specials.ActiveBalls);Assert.AreEqual(0,root.Specials.FlightBallCount);
                Assert.AreEqual(0,flight.GetComponent<UnityEngine.Rendering.SortingGroup>().sortingOrder);
                Assert.IsFalse(flight.GetComponent<UnityEngine.Rendering.SortingGroup>().sortAtRoot);previous=flight;
            }
        } finally {Object.Destroy(root.gameObject);Object.Destroy(parent);Object.Destroy(target);Random.state=random;Time.timeScale=scale;Time.captureDeltaTime=delta;}
    }
}
