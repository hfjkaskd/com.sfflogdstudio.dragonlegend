using UnityEditor;
using UnityEngine;

public static class BuildFreeStartArt
{
    public static void Save()
    {
        // UIFreeSpinStart's ef_caidai node references ef_xjpl, not an ef_caidai skeleton.
        BuildJackpotPopupArt.Create("ef_xjpl", "xianjinpl", new Vector2(99.02252f,70.73037f),
            new Vector2(.5f,.5f), "Tools/Evidence/", "Artifacts/FreeStartAuthoring/");
        BuildJackpotPopupArt.Create("ef_starttc", "ef_starttc", new Vector2(953.99994f,838.45105f),
            new Vector2(.47798738f,.3499919f), "Tools/Evidence/", "Artifacts/FreeStartAuthoring/");
        AssetDatabase.SaveAssets();
    }
}
