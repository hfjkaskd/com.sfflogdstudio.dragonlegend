using UnityEditor;
using UnityEngine;

public static class BuildTopWithdrawArt
{
    public static void Save()
    {
        // Original Main/Node/Top/CashOut skeleton rectangle and pivot.
        BuildJackpotPopupArt.Create("ef_sltxan", "按钮/sltxan",
            new Vector2(456.99997f, 147), new Vector2(.5f, .5034014f), "Tools/Evidence/");
        AssetDatabase.SaveAssets();
    }
}
