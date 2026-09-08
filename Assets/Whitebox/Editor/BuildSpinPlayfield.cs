using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class BuildSpinPlayfield
{
    public static void Save()
    {
        const string destination = "Assets/Resources/RecoveredUI/SpinPlayfield.prefab";
        var root = new GameObject("SpinPlayfield", typeof(RectTransform), typeof(RecoveredSpinPlayfield),typeof(RecoveredOrdinaryWinSequence));
        // Author under the same parent-Canvas relationship used by GameEntry at runtime.
        var canvasContext=new GameObject("Authoring Canvas",typeof(RectTransform),typeof(Canvas));
        root.transform.SetParent(canvasContext.transform,false);
        try {
            root.layer = 5; var rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero;
            var jackpotPrefab=AssetDatabase.LoadAssetAtPath<RecoveredJackpotMeters>("Assets/Resources/RecoveredUI/JackpotMeters.prefab");
            var jackpot=jackpotPrefab==null?null:(RecoveredJackpotMeters)PrefabUtility.InstantiatePrefab(jackpotPrefab,root.transform);
            var jackpotSettings=new SerializedObject(root.GetComponent<RecoveredSpinPlayfield>());
            jackpotSettings.FindProperty("jackpotMeters").objectReferenceValue=jackpot;jackpotSettings.ApplyModifiedPropertiesWithoutUndo();
            var popup=(RecoveredJackpotPopup)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<RecoveredJackpotPopup>("Assets/Resources/RecoveredUI/JackpotPopup.prefab"),root.transform);
            popup.GetComponent<Canvas>().sortingOrder=300;popup.gameObject.SetActive(false);
            jackpotSettings.FindProperty("jackpotPopup").objectReferenceValue=popup;
            jackpotSettings.FindProperty("jackpotDelay").floatValue=1.5f;jackpotSettings.ApplyModifiedPropertiesWithoutUndo();
            var board = Rect("QiPan", root.transform, new Vector2(.5f, 0), new Vector2(.5f, .5f),
                new Vector2(-.003418f, 652), new Vector2(1080, 770.28f));
            var roll = Rect("Roll", board, new Vector2(.5f, .5f), new Vector2(.5f, .5f),
                new Vector2(-1.62f, -71), new Vector2(948, 515));
            var collectionPrefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/BonusCollection.prefab");
            var collection=collectionPrefab==null?null:(GameObject)PrefabUtility.InstantiatePrefab(collectionPrefab,board);
            var reelRoot = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredSymbols/BaseReels.prefab"), roll);
            // Sprite geometry is authored at 100 pixels/unit; Canvas local coordinates are pixels.
            reelRoot.transform.localScale = new Vector3(100, 100, 100);
            var stopPrefab=AssetDatabase.LoadAssetAtPath<RecoveredCoinStopEffect>("Assets/Resources/RecoveredSymbols/CoinStopEffect.prefab");
            RecoveredCoinStopPresenter coinStops=null;
            if(stopPrefab!=null) {
                var effects=new GameObject("CoinEffects",typeof(RecoveredCoinStopPresenter));effects.layer=5;
                effects.transform.SetParent(reelRoot.transform,false);coinStops=effects.GetComponent<RecoveredCoinStopPresenter>();
                var settings=new SerializedObject(coinStops);settings.FindProperty("effectPrefab").objectReferenceValue=stopPrefab;
                settings.FindProperty("flightPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlight>("Assets/Resources/RecoveredSymbols/LampFlight.prefab");
                settings.FindProperty("flashPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlash>("Assets/Resources/RecoveredUI/LampFlash.prefab");
                settings.ApplyModifiedPropertiesWithoutUndo();
            }
            foreach (var child in reelRoot.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 5;
            var shake=board.gameObject.AddComponent<RecoveredBoardShake>();var shakeSettings=new SerializedObject(shake);
            shakeSettings.FindProperty("target").objectReferenceValue=board;
            shakeSettings.FindProperty("duration").floatValue=.3f;shakeSettings.FindProperty("intensity").floatValue=20;
            shakeSettings.FindProperty("frequency").floatValue=20;shakeSettings.FindProperty("falloff").animationCurveValue=AnimationCurve.EaseInOut(0,1,1,0);
            shakeSettings.ApplyModifiedPropertiesWithoutUndo();
            var result=Rect("Result",board,new Vector2(.5f,.5f),new Vector2(.5f,.5f),new Vector2(.003418f,335.14f),new Vector2(1080,1919.72f));
            var worldEffects=new GameObject("WildEffects",typeof(RecoveredWildPresenter));worldEffects.layer=5;
            worldEffects.transform.SetParent(result,false);worldEffects.transform.localScale=new Vector3(100,100,100);
            var wildSettings=new SerializedObject(worldEffects.GetComponent<RecoveredWildPresenter>());
            wildSettings.FindProperty("columnPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredWildColumn>("Assets/Resources/RecoveredSymbols/Wild3.prefab");
            wildSettings.FindProperty("lightPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredWildLight>("Assets/Resources/RecoveredSymbols/Wild3Light.prefab");
            wildSettings.FindProperty("shake").objectReferenceValue=shake;wildSettings.ApplyModifiedPropertiesWithoutUndo();
            var symbolsRoot=new GameObject("SymbolEffects",typeof(RecoveredSymbolWinPresenter));symbolsRoot.layer=5;
            symbolsRoot.transform.SetParent(result,false);symbolsRoot.transform.localScale=new Vector3(100,100,100);
            var symbolSettings=new SerializedObject(symbolsRoot.GetComponent<RecoveredSymbolWinPresenter>());
            var effectPrefabs=symbolSettings.FindProperty("prefabs");effectPrefabs.arraySize=8;
            string[] effectNames={"A","10","J","Q","K","Yu","Gui","Wild1"};
            for(int i=0;i<effectNames.Length;i++)effectPrefabs.GetArrayElementAtIndex(i).objectReferenceValue=
                AssetDatabase.LoadAssetAtPath<RecoveredWildColumn>("Assets/Resources/RecoveredSymbols/Winning/"+effectNames[i]+".prefab");
            symbolSettings.ApplyModifiedPropertiesWithoutUndo();
            jackpotSettings.FindProperty("symbolEffects").objectReferenceValue=symbolsRoot.GetComponent<RecoveredSymbolWinPresenter>();
            jackpotSettings.ApplyModifiedPropertiesWithoutUndo();
            var bottom = Rect("Bottom", root.transform, new Vector2(.5f, 0), new Vector2(.5f, 0),
                new Vector2(-.003418f, 0), new Vector2(1080, 292.24f));
            var winRoot=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/DownWinText.prefab"),bottom);
            var main = Rect("Main", bottom, new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(100, 100));
            var buttonRoot = (GameObject)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/RecoveredUI/SpinButton.prefab"), main);
            ((RectTransform)buttonRoot.transform).anchoredPosition = new Vector2(417, -23.793f);
            var amount=(RecoveredSymbolWinAmount)PrefabUtility.InstantiatePrefab(
                AssetDatabase.LoadAssetAtPath<RecoveredSymbolWinAmount>("Assets/Resources/RecoveredUI/SymbolWinAmount.prefab"),root.transform);
            // Canvas ignores overrideSorting while it is a prefab root; configure the nested instance.
            var amountCanvas=amount.GetComponent<Canvas>();amountCanvas.overrideSorting=true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(amountCanvas);
            var data = new SerializedObject(root.GetComponent<RecoveredSpinPlayfield>());
            data.FindProperty("symbolAmount").objectReferenceValue=amount;
            var ordinary=root.GetComponent<RecoveredOrdinaryWinSequence>();
            var ordinarySettings=new SerializedObject(ordinary);
            ordinarySettings.FindProperty("amount").objectReferenceValue=amount;
            ordinarySettings.FindProperty("downWin").objectReferenceValue=winRoot.GetComponent<RecoveredDownWinText>();
            ordinarySettings.FindProperty("flightDuration").floatValue=.3f;ordinarySettings.FindProperty("flightHeight").floatValue=1;
            ordinarySettings.FindProperty("bottomCountDuration").floatValue=.5f;ordinarySettings.FindProperty("completionDelay").floatValue=.8f;
            ordinarySettings.FindProperty("flightEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,0,0),new Keyframe(1,1,2,2));
            ordinarySettings.FindProperty("countEase").animationCurveValue=new AnimationCurve(new Keyframe(0,0,2,2),new Keyframe(1,1,0,0));
            ordinarySettings.ApplyModifiedPropertiesWithoutUndo();data.FindProperty("ordinaryWin").objectReferenceValue=ordinary;
            var transfers=new GameObject("WinFlights",typeof(RecoveredDownWinFlight));transfers.layer=5;transfers.transform.SetParent(reelRoot.transform,false);
            var transferSettings=new SerializedObject(transfers.GetComponent<RecoveredDownWinFlight>());
            transferSettings.FindProperty("flightPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredLampFlight>("Assets/Resources/RecoveredSymbols/DownWinFlight.prefab");
            transferSettings.FindProperty("burstPrefab").objectReferenceValue=AssetDatabase.LoadAssetAtPath<RecoveredWinBurst>("Assets/Resources/RecoveredUI/WinBurst.prefab");
            transferSettings.FindProperty("burstParent").objectReferenceValue=bottom;
            transferSettings.FindProperty("destination").objectReferenceValue=winRoot.transform;transferSettings.ApplyModifiedPropertiesWithoutUndo();
            data.FindProperty("winFlight").objectReferenceValue=transfers.GetComponent<RecoveredDownWinFlight>();
            data.FindProperty("downWin").objectReferenceValue=winRoot.GetComponent<RecoveredDownWinText>();
            data.FindProperty("rewardDelay").floatValue = .5f;
            data.FindProperty("bonusCoinInterval").floatValue = .5f;
            data.FindProperty("wildColumnInterval").floatValue=.36f;
            data.FindProperty("wilds").objectReferenceValue=worldEffects.GetComponent<RecoveredWildPresenter>();
            data.FindProperty("coinStops").objectReferenceValue=coinStops;
            data.FindProperty("bonusCollection").objectReferenceValue = collection==null?null:collection.GetComponent<RecoveredBonusCollection>();
            data.FindProperty("spinButton").objectReferenceValue = buttonRoot.GetComponent<RecoveredSpinButton>();
            data.FindProperty("reels").objectReferenceValue = reelRoot.GetComponent<RecoveredBaseReelController>();
            data.FindProperty("symbols").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RecoveredSymbolCatalog>("Assets/Resources/RecoveredSymbols/OriginalSymbolCatalog.asset");
            data.ApplyModifiedPropertiesWithoutUndo();
            var saved = PrefabUtility.SaveAsPrefabAsset(root, destination);
            const string entryPath = "Assets/Resources/Whitebox/GameEntry.prefab";
            var entryRoot = PrefabUtility.LoadPrefabContents(entryPath);
            try {
                var entry = new SerializedObject(entryRoot.GetComponent<GameEntry>());
                entry.FindProperty("playfieldPrefab").objectReferenceValue = saved.GetComponent<RecoveredSpinPlayfield>();
                // Keep local GM controls accessible above the recovered board.
                var primary = (UnityEngine.UI.Button)entry.FindProperty("selectDefault").objectReferenceValue;
                var alternative = (UnityEngine.UI.Button)entry.FindProperty("selectAlternative").objectReferenceValue;
                foreach(var button in new[]{primary,alternative}) {
                    var gmCanvas=button.GetComponent<Canvas>();if(gmCanvas==null)gmCanvas=button.gameObject.AddComponent<Canvas>();
                    gmCanvas.overrideSorting=true;gmCanvas.sortingOrder=5000;
                    if(button.GetComponent<UnityEngine.UI.GraphicRaycaster>()==null)button.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                }
                ((RectTransform)primary.transform).anchoredPosition = new Vector2(0, 350);
                ((RectTransform)alternative.transform).anchoredPosition = new Vector2(0, 230);
                var status = (UnityEngine.UI.Text)entry.FindProperty("status").objectReferenceValue;
                status.gameObject.SetActive(false);
                entryRoot.transform.Find("Title").gameObject.SetActive(false);
                entryRoot.transform.Find("Scope").gameObject.SetActive(false);
                entry.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(entryRoot, entryPath);
            } finally { PrefabUtility.UnloadPrefabContents(entryRoot); }
        } finally { Object.DestroyImmediate(root);Object.DestroyImmediate(canvasContext); }
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 pivot, Vector2 position, Vector2 size)
    {
        var node = new GameObject(name, typeof(RectTransform)); node.layer = 5; node.transform.SetParent(parent, false);
        var rect = (RectTransform)node.transform; rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
    }
}
