using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DragonLegend.Whitebox;

// Offline authoring/validation utility, not an Editor-specific runtime fallback.
public static class BuildFixture
{
    public static void ValidateArtifacts()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Whitebox/MockFlow.prefab");
        if (!prefab) throw new InvalidOperationException("Fixture Prefab is missing.");
        var buttons=prefab.GetComponentsInChildren<Button>(true);
        if (buttons.Length!=4) throw new InvalidOperationException("Expected four standard Buttons.");
        foreach(var button in buttons)
        {
            if (!button.GetComponent<Image>() || button.targetGraphic.gameObject!=button.gameObject)
                throw new InvalidOperationException("Visual/interaction component mismatch.");
            if (button.onClick.GetPersistentEventCount()!=0)
                throw new InvalidOperationException("Serialized UnityEvent binding found.");
        }
        if (System.IO.Directory.GetFiles(Application.dataPath,"*.dll",System.IO.SearchOption.AllDirectories).Length!=0)
            throw new InvalidOperationException("Unexpected binary plugin in project Assets.");
        Debug.Log("WHITEBOX_ARTIFACTS_VALID: four Buttons with same-object graphics, no serialized callbacks, no plugin DLLs.");
    }

    public static void Build()
    {
        const string folder = "Assets/Resources/Whitebox";
        System.IO.Directory.CreateDirectory(folder);
        var config = ScriptableObject.CreateInstance<MockFlowSettings>();
        config.placement = "mock_reward"; config.scene = "fixture";
        config.requestId = "local-test-order"; config.amount = 50000;
        config.adOutcome = AdOutcome.Rewarded; config.cashOutcome = CashOutcome.SimulatedApproved;
        AssetDatabase.CreateAsset(config, "Assets/Whitebox/Settings/MockFlow.asset");
        var root = new GameObject("MockFlow", typeof(RectTransform));
        root.SetActive(false);
        var canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        root.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        root.GetComponent<CanvasScaler>().referenceResolution = new Vector2(720,1280);
        root.AddComponent<GraphicRaycaster>();
        var view = root.AddComponent<MockFlowView>();
        var ad = Button(root.transform,"RequestAd","1. Request mock ad",240);
        var done = Button(root.transform,"CompleteAd","2. Complete mock ad",80);
        var cash = Button(root.transform,"SubmitCash","3. Submit simulated cash-out",-80);
        var resolved = Button(root.transform,"ResolveCash","4. Resolve simulated cash-out",-240);
        view.Configure(ad,done,cash,resolved,config);
        var prefab = PrefabUtility.SaveAsPrefabAsset(root,folder+"/MockFlow.prefab");
        UnityEngine.Object.DestroyImmediate(root);
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var instance=(GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.SetActive(true);
        new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
        EditorSceneManager.SaveScene(scene,"Assets/Whitebox/Scenes/MockFlow.unity");
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene("Assets/Whitebox/Scenes/MockFlow.unity",true)};
        AssetDatabase.SaveAssets();
        Debug.Log("WHITEBOX_FIXTURE_AUTHORED: native Buttons, code-bound events, serialized Prefab and settings.");
    }

    private static Button Button(Transform parent,string name,string label,float y)
    {
        var go=new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image),typeof(Button));
        go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform; rect.sizeDelta=new Vector2(600,112);rect.anchoredPosition=new Vector2(0,y);
        var image=go.GetComponent<Image>();image.color=new Color(0.12f,0.2f,0.3f,1);
        var button=go.GetComponent<Button>();button.targetGraphic=image;
        var textGo=new GameObject("Label",typeof(RectTransform),typeof(CanvasRenderer),typeof(Text));
        textGo.transform.SetParent(go.transform,false);
        var textRect=(RectTransform)textGo.transform;textRect.anchorMin=Vector2.zero;textRect.anchorMax=Vector2.one;textRect.sizeDelta=Vector2.zero;
        var text=textGo.GetComponent<Text>();text.text=label;text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize=24;text.color=Color.white;text.alignment=TextAnchor.MiddleCenter;text.raycastTarget=false;
        return button;
    }
}
