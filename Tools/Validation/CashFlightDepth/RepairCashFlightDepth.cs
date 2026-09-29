using System;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class RepairCashFlightDepth
{
    const string Output="Artifacts/CashFlightDepth";
    [Serializable] sealed class Report
    {
        public bool passed, mainParent, sourcePreserved, popupParent, nonInteractive;
        public int canvasOrder, coverOrder, testedPixels;
        public float beforeError, afterError, popupError;
        public string error;
    }
    public static void Run()
    {
        Directory.CreateDirectory(Output);var report=new Report();
        var loop=UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop();var random=UnityEngine.Random.state;
        Scene preview=default;RecoveredCashFlightPresenter flight=null;
        try
        {
            const string path="Assets/Resources/RecoveredUI/CashFlight/CashFlight.prefab";
            var root=PrefabUtility.LoadPrefabContents(path);
            try {BuildCashFlight.ConfigureMainFlightLayer(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
            AssetDatabase.SaveAssets();preview=EditorSceneManager.NewPreviewScene();
            var host=Node("Cash preview",preview,typeof(RectTransform),typeof(Canvas));
            var main=host.GetComponent<Canvas>();main.renderMode=RenderMode.WorldSpace;
            ((RectTransform)host.transform).sizeDelta=new Vector2(320,240);
            var camera=Node("Camera",preview,typeof(Camera)).GetComponent<Camera>();
            camera.orthographic=true;camera.orthographicSize=120;camera.transform.position=new Vector3(0,0,-100);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.16f,.08f,.025f);
            camera.scene=preview;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(preview);camera.cullingMask=1<<5;camera.enabled=false;main.worldCamera=camera;
            var balance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/MainSkin/Assets/RecoveredUI/BalancePanel.prefab"),preview);
            balance.transform.SetParent(host.transform,false);balance.transform.localPosition=new Vector3(0,1000,0);
            var balanceView=balance.GetComponent<RecoveredBalancePanel>();balanceView.InitializePlacement((RectTransform)host.transform,camera);
            var cash=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path),preview);
            cash.transform.SetParent(host.transform,false);flight=cash.GetComponent<RecoveredCashFlightPresenter>();
            var settings=new SerializedObject(flight);settings.FindProperty("preload").intValue=0;settings.ApplyModifiedPropertiesWithoutUndo();
            flight.Bind(null,balanceView,false);var layer=cash.transform.Find("MainFlightLayer").GetComponent<Canvas>();
            var source=Node("Source",preview,typeof(RectTransform));source.transform.SetParent(host.transform,false);
            Canvas.ForceUpdateCanvases();source.transform.position=Vector3.zero;
            flight.Begin(0,null,host.transform,true,source.transform);
            var items=host.GetComponentsInChildren<RecoveredCashFlightItem>();Require(items.Length==10,"Missing authored cash batch");
            report.mainParent=true;report.sourcePreserved=true;report.nonInteractive=true;
            for(int i=0;i<items.Length;i++)
            {
                report.mainParent&=items[i].transform.parent==layer.transform;
                report.sourcePreserved&=Vector3.Distance(items[i].transform.position,source.transform.position)<.001f;
                report.nonInteractive&=!items[i].Image.raycastTarget;
                items[i].transform.position=new Vector3((i%3-1)*24,(i/3-1)*16,0);
            }
            Canvas.ForceUpdateCanvases();
            foreach(var item in items)
            {
                item.Image.SetAllDirty();
                Debug.Log("CASH_DIAGNOSTIC "+item.name+" sprite="+item.Image.sprite+" position="+item.transform.position+" scale="+item.transform.lossyScale+" rect="+item.Image.rectTransform.rect+" scene="+item.gameObject.scene.name+" main="+main.renderMode+" nested="+layer.renderMode+" cull="+item.Image.canvasRenderer.cull);
            }
            var symbol=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/FreeSymbolItem.prefab").GetComponent<RecoveredSymbolView>();
            var cover=Node("Actual free cover",preview,typeof(SpriteRenderer)).GetComponent<SpriteRenderer>();
            cover.sprite=symbol.Cover.sprite;cover.sharedMaterial=symbol.Cover.sharedMaterial;cover.color=symbol.Cover.color;
            cover.sortingOrder=symbol.Cover.sortingOrder;report.coverOrder=cover.sortingOrder;report.canvasOrder=layer.sortingOrder;
            cover.transform.position=new Vector3(-55,0,0);cover.transform.localScale=new Vector3(110/cover.sprite.bounds.size.x,180/cover.sprite.bounds.size.y,1);
            cover.enabled=false;var reference=Capture(camera,"reference.png");
            cover.enabled=true;layer.overrideSorting=false;var before=Capture(camera,"before.png");
            layer.overrideSorting=true;var after=Capture(camera,"after.png");
            var popup=Node("Popup",preview,typeof(RectTransform),typeof(Canvas),typeof(CanvasRenderer),typeof(Image));
            popup.transform.SetParent(host.transform,false);((RectTransform)popup.transform).sizeDelta=new Vector2(320,240);
            var popupCanvas=popup.GetComponent<Canvas>();popupCanvas.overrideSorting=true;popupCanvas.sortingOrder=300;
            popup.GetComponent<Image>().color=new Color(0,0,0,.65f);popup.GetComponent<Image>().raycastTarget=false;
            Canvas.ForceUpdateCanvases();popup.transform.position=Vector3.zero;
            var underPopup=Capture(camera,"popup.png");
            for(int i=0;i<reference.Length;i++)
            {
                var p=reference[i];if(p.g<120||p.g<p.r+30||p.g<p.b+40)continue;
                report.testedPixels++;report.beforeError+=Difference(p,before[i]);report.afterError+=Difference(p,after[i]);report.popupError+=Difference(after[i],underPopup[i]);
            }
            Require(report.testedPixels>100,"No cash pixels rendered");
            report.beforeError/=report.testedPixels;report.afterError/=report.testedPixels;report.popupError/=report.testedPixels;
            Require(report.beforeError>8&&report.afterError<report.beforeError*.1f,"Cash still darkened by reel cover");
            Require(report.popupError>20,"Cash escaped the normal popup mask");
            Require(report.mainParent&&report.sourcePreserved&&report.nonInteractive,"Main flight hierarchy/input/origin mismatch");
            flight.Begin(0,null,popup.transform,false,source.transform);report.popupParent=true;
            foreach(var item in popup.GetComponentsInChildren<RecoveredCashFlightItem>())report.popupParent&=item.transform.parent==popup.transform;
            Require(popup.GetComponentsInChildren<RecoveredCashFlightItem>().Length==10&&report.popupParent,"Popup flight parent changed");
            report.passed=true;Debug.Log("CASH_FLIGHT_DEPTH_PASS");
        }
        catch(Exception error){report.error=error.ToString();Debug.LogException(error);}
        finally
        {
            if(preview.IsValid())
            {
                foreach(var root in preview.GetRootGameObjects())foreach(var item in root.GetComponentsInChildren<RecoveredCashFlightItem>(true))Object.DestroyImmediate(item.gameObject);
                if(flight!=null)flight.Unbind();EditorSceneManager.ClosePreviewScene(preview);
            }
            UnityEngine.LowLevel.PlayerLoop.SetPlayerLoop(loop);UnityEngine.Random.state=random;
            File.WriteAllText(Output+"/report.json",JsonUtility.ToJson(report,true));
        }
        if(Application.isBatchMode)EditorApplication.Exit(report.passed?0:1);
    }
    static GameObject Node(string name,Scene scene,params Type[] components)
    {var go=new GameObject(name,components){layer=5,hideFlags=HideFlags.HideAndDontSave};SceneManager.MoveGameObjectToScene(go,scene);return go;}
    static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
    static float Difference(Color32 a,Color32 b)=> (Math.Abs(a.r-b.r)+Math.Abs(a.g-b.g)+Math.Abs(a.b-b.b))/3f;
    static Color32[] Capture(Camera camera,string name)
    {
        var previous=RenderTexture.active;var target=new RenderTexture(640,480,24);Texture2D capture=null;
        try
        {
            target.Create();camera.targetTexture=target;Canvas.ForceUpdateCanvases();
            if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});else camera.Render();
            RenderTexture.active=target;capture=new Texture2D(640,480,TextureFormat.RGBA32,false);capture.ReadPixels(new Rect(0,0,640,480),0,0);capture.Apply();
            File.WriteAllBytes(Output+"/"+name,capture.EncodeToPNG());return capture.GetPixels32();
        }
        finally{camera.targetTexture=null;RenderTexture.active=previous;if(capture!=null)Object.DestroyImmediate(capture);target.Release();Object.DestroyImmediate(target);}
    }
}
