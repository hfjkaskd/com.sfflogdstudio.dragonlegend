using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object=UnityEngine.Object;

public static class BuildLuckySpinColumns
{
    public static void Save()
    {
        string source=File.ReadAllText("ReferenceOriginal/Res/ViewPrefabs/UILuckySpinView.prefab").Replace("\r","");
        var blocks=new Dictionary<string,string>();
        foreach(Match m in Regex.Matches(source,@"^--- !u!\d+ &(\d+)\n.*?(?=^--- !u!|\z)",RegexOptions.Multiline|RegexOptions.Singleline))blocks.Add(m.Groups[1].Value,m.Value);
        var map=new Dictionary<string,string>();
        Script<Text>(map,"04f84fc2003509a5e7e068ec1271cc40");
        Script<RectMask2D>(map,"69dacd7a039c12e90cde90bbae65247a");
        Script<RecoveredLuckySpinColumn>(map,"38862240cc02cff2d334471a5c7e518c");
        map["36977c4faccb97c4ebe0b4fdeea88b25"]=AssetDatabase.AssetPathToGUID("Assets/Resources/RecoveredUI/CoinRewardText/Green.asset");
        foreach(var block in blocks.Values) {
            if(!block.Contains("_digitTexts:"))continue;
            string go=Regex.Match(block,@"m_GameObject: \{fileID: (\d+)").Groups[1].Value;
            string rect=Regex.Match(blocks[go],@"component: \{fileID: (\d+)").Groups[1].Value;
            string name=Regex.Match(blocks[go],@"m_Name: (.+)").Groups[1].Value.Trim();
            var result=new StringBuilder("%YAML 1.1\n%TAG !u! tag:unity3d.com,2011:\n");
            Append(rect,rect,blocks,result);
            string yaml=result.ToString();
            foreach(var pair in map)yaml=yaml.Replace(pair.Key,pair.Value);
            yaml=yaml.Replace("_rotNode:","rotNode:").Replace("_digitTexts:","digitTexts:").Replace("_itemHeight:","itemHeight:")
                .Replace("  _visibleRowIndex: 0","  maxSpeed: 10000\n  accelTime: 0.08\n  minSnapTime: 0.05\n  maxSnapTime: 0.3\n  bounceHeight: 35\n  bounceUpTime: 0.12\n  bounceDownTime: 0.08");
            string path="Assets/Resources/RecoveredUI/LuckySpin"+name+".prefab";
            File.WriteAllText(path,yaml,new UTF8Encoding(false));AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            if(AssetDatabase.LoadAssetAtPath<GameObject>(path)==null)throw new InvalidDataException(path);
        }
        AssetDatabase.SaveAssets();
    }
    private static void Append(string rect,string root,Dictionary<string,string> blocks,StringBuilder result)
    {
        string transform=blocks[rect];string go=Regex.Match(transform,@"m_GameObject: \{fileID: (\d+)").Groups[1].Value;
        result.Append(blocks[go]);
        foreach(Match m in Regex.Matches(blocks[go],@"component: \{fileID: (\d+)")) {
            string id=m.Groups[1].Value;string b=blocks[id];
            if(id==root)b=Regex.Replace(b,@"m_Father: \{fileID: \d+\}","m_Father: {fileID: 0}");
            result.Append(b);
        }
        string children=Regex.Match(transform,@"m_Children:\n(.*?)  m_Father:",RegexOptions.Singleline).Groups[1].Value;
        foreach(Match child in Regex.Matches(children,@"fileID: (\d+)"))Append(child.Groups[1].Value,root,blocks,result);
    }
    private static void Script<T>(Dictionary<string,string> map,string original) where T:MonoBehaviour
    {
        var host=new GameObject("Script lookup",typeof(RectTransform));
        try {map[original]=AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(host.AddComponent<T>())));}
        finally {Object.DestroyImmediate(host);}
    }
}
