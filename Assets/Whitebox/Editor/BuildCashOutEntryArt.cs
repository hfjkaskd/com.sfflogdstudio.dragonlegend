using UnityEditor;
using UnityEngine;

public static class BuildCashOutEntryArt
{
    public static void Save()
    {
        // Original Main/CashOutb icon rectangle and its scaled-time idle clip.
        BuildJackpotPopupArt.Create("ef_tixianicon","按钮/tixian",
            new Vector2(187.14287f,187.14285f),new Vector2(.5f,.5f),"Tools/Evidence/");
        AssetDatabase.SaveAssets();
    }
}
