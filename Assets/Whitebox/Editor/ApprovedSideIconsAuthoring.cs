using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Offline prefab authoring only. No runtime static UI creation or reflection.
public static class ApprovedSideIconsAuthoring
{
    private const string Textures = "Assets/Resources/MainSkin/Textures/ApprovedSideIcons/";
    private const string CollectPath = "Assets/Resources/MainSkin/Assets/RecoveredUI/CollectEntry.prefab";
    private const string CashPath = "Assets/Resources/RecoveredUI/CashOutEntry.prefab";
    private const string TaskPath = "Assets/Resources/RecoveredUI/DailyTasks/Entry.prefab";
    private const string VisualName = "ApprovedSideIcon";

    [Serializable] private sealed class RectPose
    {
        public Vector2 anchorMin, anchorMax, pivot, sizeDelta, anchoredPosition;
        public Vector3 localPosition, localScale;
        public Quaternion localRotation;
        public RectPose(RectTransform r)
        {
            anchorMin=r.anchorMin; anchorMax=r.anchorMax; pivot=r.pivot;
            sizeDelta=r.sizeDelta; anchoredPosition=r.anchoredPosition;
            localPosition=r.localPosition; localScale=r.localScale; localRotation=r.localRotation;
        }
    }
    [Serializable] private sealed class Row
    {
        public string prefab, buttonPath, sprite;
        public Vector2 visualSize, visualCenter;
        public int existingRectTransformsPreserved, persistentCalls;
        public bool buttonPreserved, runtimeTargetPreserved, oldRigDisabled;
    }
    [Serializable] private sealed class Report { public bool passed; public List<Row> entries = new List<Row>(); }

    [MenuItem("Tools/Approved Art/Apply Side Icons")]
    public static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Side icon authoring requires Edit mode.");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ImportSprite("TreasureMap"); ImportSprite("CashOutWallet"); ImportSprite("TaskScroll");
        var report = new Report();
        report.entries.Add(ApplyRigIcon(CollectPath, "Tubiao/Treasure", "SkeletonGraphic (ef_shoucangicon)", "TreasureMap", true));
        report.entries.Add(ApplyRigIcon(CashPath, "Tubiao/CashOutb", "SkeletonGraphic (ef_tixianicon)", "CashOutWallet", false));
        report.entries.Add(ApplyTask());
        AssetDatabase.SaveAssets();
        report.passed=true;
        Directory.CreateDirectory("Artifacts/ApprovedSideIcons");
        File.WriteAllText("Artifacts/ApprovedSideIcons/authoring.json",JsonUtility.ToJson(report,true));
        Debug.Log("ApprovedSideIconsAuthoring PASS: original buttons, label transforms, destination and finger target retained.");
    }

    private static void ImportSprite(string name)
    {
        string path=Textures+name+".png";
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Missing side icon: "+path);
        importer.textureType=TextureImporterType.Sprite;
        importer.spriteImportMode=SpriteImportMode.Single;
        importer.spritePixelsPerUnit=100;
        importer.spritePivot=new Vector2(.5f,.5f);
        importer.alphaIsTransparency=true;
        importer.mipmapEnabled=false;
        importer.sRGBTexture=true;
        importer.wrapMode=TextureWrapMode.Clamp;
        importer.filterMode=FilterMode.Bilinear;
        importer.maxTextureSize=512;
        importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.isReadable=false;
        importer.SaveAndReimport();
    }

    private static Row ApplyRigIcon(string path,string buttonPath,string artName,string spriteName,bool collect)
    {
        var root=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var poses=CaptureRects(root);
            var texts=CaptureText(root);
            var buttonTransform=root.transform.Find(buttonPath);
            if(buttonTransform==null)throw new InvalidOperationException("Missing button: "+buttonPath);
            var button=buttonTransform.GetComponent<Button>();
            var art=buttonTransform.Find(artName);
            if(button==null||art==null)throw new InvalidOperationException("Incomplete original side entry: "+path);
            var rig=art.GetComponent<RecoveredRegionRig>();
            if(rig==null)throw new InvalidOperationException("Missing original rig: "+path);
            var originalTarget=collect ? (UnityEngine.Object)root.GetComponent<RecoveredCollectEntry>().Destination : root.GetComponent<RecoveredCashOutEntry>().FingerTarget;
            Button originalButton=collect ? root.GetComponent<RecoveredCollectEntry>().Button : root.GetComponent<RecoveredCashOutEntry>().Button;
            if(button!=originalButton)throw new InvalidOperationException("Button binding mismatch before authoring: "+path);
            RequireNoPersistentCalls(button,path);
            Rect bounds=BaseBounds(rig,(RectTransform)buttonTransform);
            Image image;
            var existing=buttonTransform.Find(VisualName);
            if(existing==null)
            {
                var visual=new GameObject(VisualName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                visual.layer=buttonTransform.gameObject.layer;
                visual.transform.SetParent(buttonTransform,false);
                image=visual.GetComponent<Image>();
            }
            else image=existing.GetComponent<Image>();
            if(image==null)throw new InvalidOperationException("Unexpected side visual component.");
            var r=image.rectTransform;
            r.anchorMin=r.anchorMax=new Vector2(.5f,.5f); r.pivot=new Vector2(.5f,.5f);
            r.localScale=Vector3.one; r.localRotation=Quaternion.identity;
            r.sizeDelta=bounds.size;
            r.anchoredPosition=bounds.center-((RectTransform)buttonTransform).rect.center;
            r.SetSiblingIndex(art.GetSiblingIndex()+1);
            image.sprite=LoadSprite(spriteName); image.color=Color.white;
            image.material=null; image.type=Image.Type.Simple; image.preserveAspect=false;
            // Standard Button keeps its original root Image/raycast rectangle.
            // Its authored visual lives in the same Button hierarchy and receives transition tint.
            image.raycastTarget=false; button.targetGraphic=image;
            foreach(var animator in art.GetComponentsInChildren<RecoveredRegionAnimator>(true)){animator.enabled=false;Persist(animator);}
            foreach(var animation in art.GetComponentsInChildren<Animation>(true)){animation.enabled=false;Persist(animation);}
            foreach(var oldGraphic in art.GetComponentsInChildren<RecoveredRegionRig>(true)){oldGraphic.enabled=false;Persist(oldGraphic);}
            if (!collect) ApprovedCashOutMotionAuthoring.Apply(image);
            Persist(button); Persist(image);
            // Keep the original GameObject active and its RectTransform reference intact:
            // TreasureDeparture uses this exact destination, CashOut uses its sibling finger target.
            var afterTarget=collect ? (UnityEngine.Object)root.GetComponent<RecoveredCollectEntry>().Destination : root.GetComponent<RecoveredCashOutEntry>().FingerTarget;
            var afterButton=collect ? root.GetComponent<RecoveredCollectEntry>().Button : root.GetComponent<RecoveredCashOutEntry>().Button;
            if(originalTarget!=afterTarget||originalButton!=afterButton)throw new InvalidOperationException("Runtime side binding changed.");
            VerifyRects(poses); VerifyText(texts); RequireNoPersistentCalls(button,path);
            PrefabUtility.SaveAsPrefabAsset(root,path);
            return new Row {prefab=path,buttonPath=buttonPath,sprite=spriteName,visualSize=bounds.size,visualCenter=bounds.center,
                existingRectTransformsPreserved=poses.Count,persistentCalls=0,buttonPreserved=true,runtimeTargetPreserved=true,oldRigDisabled=!rig.enabled};
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    private static Row ApplyTask()
    {
        var root=PrefabUtility.LoadPrefabContents(TaskPath);
        try
        {
            var poses=CaptureRects(root); var texts=CaptureText(root);
            var view=root.GetComponent<RecoveredDailyTaskEntry>();
            var button=root.GetComponent<Button>(); var image=root.GetComponent<Image>();
            if(view==null||view.Button!=button||image==null)throw new InvalidOperationException("Unexpected task button structure.");
            RequireNoPersistentCalls(button,TaskPath);
            var book=root.transform.Find("Book"); var pen=root.transform.Find("Pen");
            var label=root.transform.Find("Label") as RectTransform;
            if(book==null||pen==null||label==null)throw new InvalidOperationException("Missing original task Book/Pen/Label.");
            book.gameObject.SetActive(false); pen.gameObject.SetActive(false);
            var cashPrefab=AssetDatabase.LoadAssetAtPath<GameObject>(CashPath);
            var cashVisual=cashPrefab==null?null:cashPrefab.transform.Find("Tubiao/CashOutb/"+VisualName) as RectTransform;
            if(cashVisual==null)throw new InvalidOperationException("Author the CashOut icon before matching TASK alignment.");
            var existing=root.transform.Find(VisualName);
            Image icon;
            if(existing==null)
            {
                var visual=new GameObject(VisualName,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image));
                visual.layer=root.layer; visual.transform.SetParent(root.transform,false);
                icon=visual.GetComponent<Image>();
            }
            else icon=existing.GetComponent<Image>();
            if(icon==null)throw new InvalidOperationException("Unexpected TASK visual component.");
            var rect=icon.rectTransform;
            var rootRect=(RectTransform)root.transform;
            var cashButton=(RectTransform)cashVisual.parent;
            float ratioX=Mathf.Abs(cashButton.lossyScale.x/rootRect.lossyScale.x);
            float ratioY=Mathf.Abs(cashButton.lossyScale.y/rootRect.lossyScale.y);
            Vector2 cashOffset=(Vector2)cashVisual.localPosition-cashButton.rect.center;
            Vector2 iconSize=new Vector2(cashVisual.rect.width*Mathf.Abs(cashVisual.localScale.x)*ratioX,
                cashVisual.rect.height*Mathf.Abs(cashVisual.localScale.y)*ratioY);
            var labelCorners=new Vector3[4]; label.GetWorldCorners(labelCorners);
            float labelTop=float.MinValue;
            foreach(var corner in labelCorners)labelTop=Mathf.Max(labelTop,rootRect.InverseTransformPoint(corner).y);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f); rect.pivot=new Vector2(.5f,.5f);
            // TASK's existing root scale is .85. Match CashOut's displayed dimensions,
            // then position above the original label rather than overlapping its text rectangle.
            rect.sizeDelta=iconSize;
            rect.anchoredPosition=new Vector2(label.anchoredPosition.x+cashOffset.x*ratioX,
                labelTop+3f+iconSize.y*.5f-rootRect.rect.center.y);
            rect.localScale=Vector3.one; rect.localRotation=Quaternion.identity;
            // Keep the existing TASK label after the icon in draw order.
            rect.SetAsFirstSibling();
            icon.sprite=LoadSprite("TaskScroll"); icon.material=null; icon.color=Color.white;
            icon.type=Image.Type.Simple; icon.preserveAspect=false; icon.raycastTarget=false;
            // The authored Button bounds contain both the icon and label, without drawing a second badge.
            image.sprite=null; image.material=null; image.color=new Color(1,1,1,0);
            image.type=Image.Type.Simple; image.raycastTarget=true;
            button.targetGraphic=icon; Persist(image); Persist(icon); Persist(button);
            VerifyRects(poses); VerifyText(texts); RequireNoPersistentCalls(button,TaskPath);
            PrefabUtility.SaveAsPrefabAsset(root,TaskPath);
            return new Row {prefab=TaskPath,buttonPath="/",sprite="TaskScroll",visualSize=rect.sizeDelta,
                visualCenter=rect.localPosition,existingRectTransformsPreserved=poses.Count,persistentCalls=0,
                buttonPreserved=view.Button==button,runtimeTargetPreserved=true,oldRigDisabled=true};
        }
        finally {PrefabUtility.UnloadPrefabContents(root);}
    }

    private static Rect BaseBounds(RecoveredRegionRig rig,RectTransform button)
    {
        rig.RefreshPose();
        // Every authored side rig stores the 162x162 base as region 0.
        RecoveredRegionRig.Slot baseSlot=null;
        foreach(var slot in rig.slots)if(!slot.additive&&Mathf.RoundToInt(slot.attachment)==0){baseSlot=slot;break;}
        if(baseSlot==null)throw new InvalidOperationException("Side base slot was not found.");
        var region=rig.regions[0];
        if(region.vertices==null||region.vertices.Length!=4)throw new InvalidOperationException("Side base is no longer a quad.");
        Vector2 min=new Vector2(float.MaxValue,float.MaxValue),max=new Vector2(float.MinValue,float.MinValue);
        foreach(var vertex in region.vertices)
        {
            Vector3 local=rig.BoneMatrix(baseSlot.bone).MultiplyPoint3x4(vertex);
            Vector2 point=button.InverseTransformPoint(rig.transform.TransformPoint(local));
            min=Vector2.Min(min,point); max=Vector2.Max(max,point);
        }
        return Rect.MinMaxRect(min.x,min.y,max.x,max.y);
    }
    private static Sprite LoadSprite(string name)
    {
        var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Textures+name+".png");
        if(sprite==null)throw new InvalidOperationException("Sprite import failed: "+name);
        return sprite;
    }
    private static Dictionary<RectTransform,string> CaptureRects(GameObject root)
    {
        var result=new Dictionary<RectTransform,string>();
        foreach(var r in root.GetComponentsInChildren<RectTransform>(true))
        {
            // This generated visual is explicitly allowed to be adjusted between approved passes.
            // Original Button, label, targets and every other RectTransform stay protected.
            if(r.name==VisualName)continue;
            result.Add(r,JsonUtility.ToJson(new RectPose(r)));
        }
        return result;
    }
    private static void VerifyRects(Dictionary<RectTransform,string> before)
    {
        foreach(var pair in before)if(pair.Key==null||pair.Value!=JsonUtility.ToJson(new RectPose(pair.Key)))
            throw new InvalidOperationException("An original side RectTransform changed: "+(pair.Key==null?"missing":pair.Key.name));
    }
    private static Dictionary<TMP_Text,string> CaptureText(GameObject root)
    {
        var result=new Dictionary<TMP_Text,string>();
        foreach(var text in root.GetComponentsInChildren<TMP_Text>(true))result.Add(text,text.text);
        return result;
    }
    private static void VerifyText(Dictionary<TMP_Text,string> before)
    {
        foreach(var pair in before)if(pair.Key==null||pair.Key.text!=pair.Value)throw new InvalidOperationException("An existing side label changed.");
    }
    private static void RequireNoPersistentCalls(Button button,string path)
    {
        if(button.onClick.GetPersistentEventCount()!=0)throw new InvalidOperationException("Side Button has forbidden persistent event calls: "+path);
    }
    private static void Persist(Component component)
    {
        EditorUtility.SetDirty(component);
        if(PrefabUtility.IsPartOfPrefabInstance(component))PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    }
}
