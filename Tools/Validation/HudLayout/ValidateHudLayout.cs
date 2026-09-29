using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object=UnityEngine.Object;

// Isolated prefab rendering. Never instantiates GameEntry or calls game/SDK/store initialization.
public static class ValidateHudLayout
{
    const string Output="Artifacts/HudLayout";
    const int Width=1080;
    [Serializable] public sealed class Box {public string name;public float x,y,width,height; public Box(string n,Rect r){name=n;x=r.x;y=r.y;width=r.width;height=r.height;} }
    [Serializable] public sealed class Check {public string name;public bool passed;public string detail;}
    [Serializable] public sealed class CaseReport {public string name,mode,pipeline,error;public int width,height,topInset,bottomInset;public bool passed;public List<Box> bounds=new List<Box>();public List<Check> checks=new List<Check>();}
    [Serializable] public sealed class Report {public bool passed;public string error;public List<CaseReport> cases=new List<CaseReport>();}

    [MenuItem("Tools/Validation/HUD Layout")]
    public static void Run()
    {
        Directory.CreateDirectory(Output);var report=new Report();var random=UnityEngine.Random.state;
        try {
            foreach(var mode in new[]{RenderMode.ScreenSpaceCamera,RenderMode.WorldSpace})
                foreach(int height in new[]{1920,2160,2400,2640})
                    foreach(bool inset in new[]{false,true})report.cases.Add(Render(mode,height,inset?120:0,inset?60:0));
            report.passed=report.cases.Count==16;
            foreach(var result in report.cases)if(!result.passed)report.passed=false;
            if(report.passed)Debug.Log("HUD_LAYOUT_PASS: 16 isolated prefab renders and geometry checks.");
            else Debug.LogError("HUD_LAYOUT_FAILED: see Artifacts/HudLayout/report.json and PNGs.");
        } catch(Exception e){report.error=e.ToString();Debug.LogException(e);}
        finally {UnityEngine.Random.state=random;File.WriteAllText(Output+"/report.json",JsonUtility.ToJson(report,true));}
        if(Application.isBatchMode)EditorApplication.Exit(report.passed?0:1);
    }

    static CaseReport Render(RenderMode mode,int height,int top,int bottom)
    {
        var report=new CaseReport {name=(mode==RenderMode.ScreenSpaceCamera?"camera":"world")+"-1080x"+height+(top>0?"-safe":"-full"),mode=mode.ToString(),width=Width,height=height,topInset=top,bottomInset=bottom,pipeline=GraphicsSettings.currentRenderPipeline==null?"Built-in":GraphicsSettings.currentRenderPipeline.name};
        Scene scene=EditorSceneManager.NewPreviewScene();RenderTexture target=null;
        try {
            target=new RenderTexture(Width,height,24,RenderTextureFormat.ARGB32);target.Create();
            var camera=Node("HUD Preview Camera",scene,typeof(Camera)).GetComponent<Camera>();camera.enabled=false;
            camera.transform.position=new Vector3(0,0,-1000);camera.orthographic=true;camera.orthographicSize=height*.5f;
            camera.nearClipPlane=.1f;camera.farClipPlane=2000;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.08f,.03f,.02f,1);
            camera.scene=scene;camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.cullingMask=1<<5;camera.targetTexture=target;
            var host=Node("HUD Preview Canvas",scene,typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
            var canvas=host.GetComponent<Canvas>();canvas.renderMode=mode;canvas.worldCamera=camera;canvas.planeDistance=1000;canvas.scaleFactor=1;
            var root=(RectTransform)host.transform;root.sizeDelta=new Vector2(Width,height);
            var scaler=host.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1080,1920);scaler.matchWidthOrHeight=0;
            // Every test uses width 1080; freeze the editor scaler at its exact width-match result.
            // This prevents the unrelated user's Game View size from affecting a camera RT preview.
            scaler.enabled=false;canvas.scaleFactor=1;
            if(mode==RenderMode.WorldSpace){root.position=Vector3.zero;root.localScale=Vector3.one;root.sizeDelta=new Vector2(Width,height);}
            Canvas.ForceUpdateCanvases();

            var background=Prefab("MainSkin/Assets/RecoveredUI/MainBackground",scene,root).GetComponent<RecoveredMainBackground>();background.Bind(canvas);background.Apply(RecoveredSlotType.Base);
            var fieldObject=Node("Safe Playfield (layout fixture)",scene,typeof(RectTransform),typeof(RecoveredScreenAdapt));fieldObject.transform.SetParent(root,false);Stretch((RectTransform)fieldObject.transform);
            var field=(RectTransform)fieldObject.transform;var fieldAdapt=fieldObject.GetComponent<RecoveredScreenAdapt>();fieldAdapt.BindUiRootScaler(scaler);
            var balance=Prefab("MainSkin/Assets/RecoveredUI/BalancePanel",scene,field).GetComponent<RecoveredBalancePanel>();
            var balanceData=new SerializedObject(balance);
            TMP_Text money=Read<TMP_Text>(balanceData,"greenText"),level=Read<TMP_Text>(balanceData,"levelText"),progress=Read<TMP_Text>(balanceData,"progressText");
            money.text="$193.43";level.text="3";progress.text="0/2";Read<Image>(balanceData,"progressFill").fillAmount=0;
            var utility=Prefab("RecoveredUI/MainUtility/Entry",scene,field).GetComponent<RecoveredMainUtility>();
            var status=Prefab("RecoveredUI/MainCashOutStatus",scene,root).GetComponent<RecoveredMainCashOutStatus>();
            // Set the authored preview label directly; do not start its coroutine or create player data.
            string format=new SerializedObject(status).FindProperty("remainingFormat").stringValue;
            status.Label.text=string.Format(format,"$306.57","$500");status.Panel.gameObject.SetActive(true);status.Panel.localScale=Vector3.one;
            var cash=Prefab("RecoveredUI/CashOutEntry",scene,root).GetComponent<RecoveredCashOutEntry>();
            var sideRoot=cash.Button.transform.parent;
            var task=Prefab("RecoveredUI/DailyTasks/Entry",scene,sideRoot).GetComponent<RecoveredDailyTaskEntry>();
            // Exercise the maximum authored wallet visual pulse while its Button remains stable.
            var wallet=cash.Button.transform.Find("ApprovedSideIcon") as RectTransform;
            if(wallet==null)throw new InvalidOperationException("CashOutEntry ApprovedSideIcon missing.");
            foreach(var animation in cash.GetComponentsInChildren<Animation>(true))animation.enabled=false;
            wallet.localScale=Vector3.one*1.06f;
            var gameAsset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Whitebox/GameEntry.prefab");
            var gmAsset=gameAsset.GetComponent<RecoveredGmPanel>().ToggleButton;
            // Clone this leaf Button only. Never instantiate the GameEntry root.
            var gmObject=Object.Instantiate(gmAsset.gameObject,root,false);gmObject.name="GmToggle (leaf preview)";
            Canvas.ForceUpdateCanvases();gmObject.transform.SetParent(utility.transform,false);
            var gm=gmObject.GetComponent<Button>();gm.gameObject.SetActive(true);
            foreach(var text in gm.GetComponentsInChildren<Text>(true))text.text="GM";

            var safe=new Rect(0,bottom,Width,height-top-bottom);
            // Each real component owns its original adaptation exactly once.
            foreach(var adapt in host.GetComponentsInChildren<RecoveredScreenAdapt>(true)){adapt.BindUiRootScaler(scaler);adapt.Apply(Width,height,safe);}
            Canvas.ForceUpdateCanvases();
            foreach(var text in host.GetComponentsInChildren<TMP_Text>(true))if(text.gameObject.activeInHierarchy)text.ForceMeshUpdate(true,true);
            Canvas.ForceUpdateCanvases();

            Rect header=ScreenRect((RectTransform)balance.transform,camera,height),fieldBounds=ScreenRect(field,camera,height);
            Rect tip=ScreenRect(status.Panel,camera,height),tipText=TextRect(status.Label,camera,height),moneyInk=TextRect(money,camera,height);
            Rect gmBounds=ScreenRect((RectTransform)gm.transform,camera,height),paypal=ScreenRect((RectTransform)balance.WithdrawButton.transform,camera,height);
            Rect help=ScreenRect((RectTransform)utility.HelpButton.transform,camera,height),settings=ScreenRect((RectTransform)utility.SettingsButton.transform,camera,height);
            Rect taskClick=ScreenRect((RectTransform)task.Button.transform,camera,height),cashClick=ScreenRect((RectTransform)cash.Button.transform,camera,height);
            Rect taskVisual=GraphicsRect(task.transform,camera,height),walletVisual=GraphicsRect(cash.Button.transform,camera,height);
            Rect progressBounds=ScreenRect(progress.rectTransform,camera,height);
            foreach(var box in new[]{new Box("SafePlayfield",fieldBounds),new Box("Header",header),new Box("BalanceTextRect",ScreenRect(money.rectTransform,camera,height)),new Box("BalanceGlyphs",moneyInk),new Box("TipPanel",tip),new Box("TipGlyphs",tipText),new Box("GM",gmBounds),new Box("PayPal",paypal),new Box("Help",help),new Box("Settings",settings),new Box("ProgressLabel",progressBounds),new Box("CashOutButton",cashClick),new Box("WalletVisualAt1.06",walletVisual),new Box("TaskButton",taskClick),new Box("TaskVisual",taskVisual)})report.bounds.Add(box);
            Add(report,"Safe Playfield exact insets",Near(fieldBounds,new Rect(0,top,Width,height-top-bottom)),fieldBounds.ToString());
            Add(report,"Header aligns to safe top",Math.Abs(header.y-top)<.1f&&Math.Abs(header.width-1080)<.1f,header.ToString());
            Add(report,"Utility shares safe top",Math.Abs(ScreenRect((RectTransform)utility.transform,camera,height).y-top)<.1f,"Utility under Safe Playfield");
            var gmRect=(RectTransform)gm.transform;
            Add(report,"GM keeps authored position after Canvas to Utility reparent",(gmRect.anchoredPosition-new Vector2(610,-31)).sqrMagnitude<.01f,"anchoredPosition="+gmRect.anchoredPosition);
            Add(report,"Tip safely inside header",Contains(header,tip),tip.ToString());
            Separate(report,"Tip / balance text container",tip,ScreenRect(money.rectTransform,camera,height));
            Add(report,"Tip text contained",Contains(tip,tipText),tipText.ToString());
            Add(report,"Tip text not truncated",!status.Label.isTextOverflowing,"Font size "+status.Label.fontSize+", lines "+status.Label.textInfo.lineCount);
            Add(report,"Tip does not intercept clicks",NonInteractive(status.transform),"All tip Graphics raycastTarget=false");
            Separate(report,"Tip / PayPal",tip,paypal);Separate(report,"Tip / Help",tip,help);Separate(report,"Tip / Settings",tip,settings);Separate(report,"Tip / GM",tip,gmBounds);Separate(report,"Tip / balance glyphs",tip,moneyInk);Separate(report,"Tip / progress label",tip,progressBounds);
            Separate(report,"GM / balance glyphs",gmBounds,moneyInk);Separate(report,"GM / help",gmBounds,help);Separate(report,"GM / settings",gmBounds,settings);Separate(report,"GM / PayPal",gmBounds,paypal);
            Separate(report,"Wallet / Task interaction",cashClick,taskClick);Separate(report,"Wallet max pulse / Task artwork",walletVisual,taskVisual);
            Add(report,"Task Button fully contains visible artwork",Contains(taskClick,taskVisual),"Button="+taskClick+"; artwork="+taskVisual);
            foreach(var graphic in task.GetComponentsInChildren<Graphic>(true))if(graphic.gameObject.activeInHierarchy&&graphic.color.a>.01f)Add(report,"Task Button contains visual Rect "+graphic.name,Contains(taskClick,ScreenRect(graphic.rectTransform,camera,height)),ScreenRect(graphic.rectTransform,camera,height).ToString());
            var screenSafe=new Rect(0,top,Width,height-top-bottom);
            foreach(var box in new[]{new Box("GM",gmBounds),new Box("Help",help),new Box("Settings",settings),new Box("PayPal",paypal),new Box("Task",taskClick),new Box("CashOut",cashClick)})Add(report,box.name+" within safe screen",Contains(screenSafe,new Rect(box.x,box.y,box.width,box.height)),"Safe area "+screenSafe);
            // Capture despite geometry failures so the failing case remains visually reviewable.
            Capture(camera,target,Output+"/"+report.name+".png");
            report.passed=true;foreach(var check in report.checks)if(!check.passed)report.passed=false;
        } catch(Exception e){report.error=e.ToString();report.passed=false;}
        finally {if(target!=null){target.Release();Object.DestroyImmediate(target);}EditorSceneManager.ClosePreviewScene(scene);}
        File.WriteAllText(Output+"/"+report.name+".json",JsonUtility.ToJson(report,true));return report;
    }
    static T Read<T>(SerializedObject owner,string field)where T:Object{return owner.FindProperty(field).objectReferenceValue as T;}
    static GameObject Node(string name,Scene scene,params Type[] components){var node=new GameObject(name,components);node.layer=5;SceneManager.MoveGameObjectToScene(node,scene);return node;}
    static GameObject Prefab(string path,Scene scene,Transform parent){var asset=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/"+path+".prefab");if(asset==null)throw new InvalidOperationException("Missing prefab "+path);var result=(GameObject)PrefabUtility.InstantiatePrefab(asset,scene);result.transform.SetParent(parent,false);return result;}
    static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=Vector2.zero;rect.offsetMax=Vector2.zero;rect.localScale=Vector3.one;}
    static Rect ScreenRect(RectTransform rect,Camera camera,int height){var corners=new Vector3[4];rect.GetWorldCorners(corners);return ScreenPoints(corners,camera,height);}
    static Rect TextRect(TMP_Text text,Camera camera,int height){var points=new List<Vector3>();foreach(var c in text.textInfo.characterInfo){if(!c.isVisible)continue;points.Add(text.transform.TransformPoint(c.bottomLeft));points.Add(text.transform.TransformPoint(c.topRight));}return points.Count>0?ScreenPoints(points.ToArray(),camera,height):new Rect();}
    static Rect GraphicsRect(Transform root,Camera camera,int height){bool first=true;Rect result=new Rect();foreach(var graphic in root.GetComponentsInChildren<Graphic>(true)){if(!graphic.gameObject.activeInHierarchy||graphic.color.a<.01f)continue;var image=graphic as Image;if(image!=null&&image.sprite==null)continue;Rect rect=graphic is TMP_Text?TextRect((TMP_Text)graphic,camera,height):ScreenRect(graphic.rectTransform,camera,height);if(rect.width<=0||rect.height<=0)continue;if(first){result=rect;first=false;}else result=Rect.MinMaxRect(Math.Min(result.xMin,rect.xMin),Math.Min(result.yMin,rect.yMin),Math.Max(result.xMax,rect.xMax),Math.Max(result.yMax,rect.yMax));}return result;}
    static Rect ScreenPoints(Vector3[] points,Camera camera,int height){float left=float.MaxValue,top=float.MaxValue,right=float.MinValue,bottom=float.MinValue;foreach(var p in points){var s=camera.WorldToScreenPoint(p);left=Math.Min(left,s.x);right=Math.Max(right,s.x);top=Math.Min(top,height-s.y);bottom=Math.Max(bottom,height-s.y);}return Rect.MinMaxRect(left,top,right,bottom);}
    static bool NonInteractive(Transform root){foreach(var g in root.GetComponentsInChildren<Graphic>(true))if(g.raycastTarget)return false;return true;}
    static bool Near(Rect a,Rect b){return Math.Abs(a.x-b.x)<.2f&&Math.Abs(a.y-b.y)<.2f&&Math.Abs(a.width-b.width)<.2f&&Math.Abs(a.height-b.height)<.2f;}
    static bool Contains(Rect outer,Rect inner){const float tolerance=.25f;return inner.width>0&&inner.height>0&&inner.xMin>=outer.xMin-tolerance&&inner.yMin>=outer.yMin-tolerance&&inner.xMax<=outer.xMax+tolerance&&inner.yMax<=outer.yMax+tolerance;}
    static void Add(CaseReport report,string name,bool passed,string detail){report.checks.Add(new Check{name=name,passed=passed,detail=detail});}
    static void Separate(CaseReport report,string name,Rect a,Rect b){float w=Math.Min(a.xMax,b.xMax)-Math.Max(a.xMin,b.xMin),h=Math.Min(a.yMax,b.yMax)-Math.Max(a.yMin,b.yMin);Add(report,name,w<=.25f||h<=.25f,"overlap "+Math.Max(0,w)+" x "+Math.Max(0,h));}
    static void Capture(Camera camera,RenderTexture target,string path){Texture2D image=null;var previous=RenderTexture.active;try{Canvas.ForceUpdateCanvases();if(GraphicsSettings.currentRenderPipeline!=null)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=target});else camera.Render();RenderTexture.active=target;image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}finally{RenderTexture.active=previous;if(image!=null)Object.DestroyImmediate(image);}}
}
