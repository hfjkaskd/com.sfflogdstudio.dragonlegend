using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

public static class BuildMainUiHierarchy
{
    [MenuItem("Dragon Legend/Restore Main UI Hierarchy")]
    public static void Save()
    {
        const string path="Assets/Whitebox/Scenes/GameEntry.unity";
        var scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Single);
        Transform uiRoot=null;GameEntry entry=null;EventSystem events=null;
        foreach(var root in scene.GetRootGameObjects()) {
            if(root.name=="[UI]Main")uiRoot=root.transform;
            var candidate=root.GetComponentInChildren<GameEntry>(true);if(candidate!=null)entry=candidate;
            var eventCandidate=root.GetComponentInChildren<EventSystem>(true);if(eventCandidate!=null)events=eventCandidate;
        }
        if(uiRoot==null||entry==null||events==null)throw new InvalidDataException("Missing authored Main UI roots");
        var camera=entry.GetComponent<Canvas>().worldCamera;
        if(camera==null||camera.transform.parent!=uiRoot)throw new InvalidDataException("Unexpected UI camera hierarchy");
        // Original Main Transform 12 children: UICanvas/23, UICamera/13, EventSystem/11.
        entry.transform.SetParent(uiRoot,false);entry.transform.SetSiblingIndex(0);
        camera.transform.SetSiblingIndex(1);
        events.transform.SetParent(uiRoot,false);events.transform.SetSiblingIndex(2);
        events.transform.localPosition=Vector3.zero;events.transform.localRotation=Quaternion.identity;
        events.transform.localScale=Vector3.one;events.gameObject.layer=5;
        PrefabUtility.RecordPrefabInstancePropertyModifications(entry.transform);
        EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
    }
}
