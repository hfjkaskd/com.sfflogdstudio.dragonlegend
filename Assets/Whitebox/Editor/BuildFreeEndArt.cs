using UnityEditor;
using UnityEngine;

public static class BuildFreeEndArt
{
    public static void Save()
    {
        BuildJackpotPopupArt.Create("ef_overtc", "overtc", new Vector2(1090.0714f,1273.1414f),
            new Vector2(.50049144f,.49966273f), "Tools/Evidence/", "Artifacts/FreeEndAuthoring/");
        AssetDatabase.SaveAssets();
    }
}
