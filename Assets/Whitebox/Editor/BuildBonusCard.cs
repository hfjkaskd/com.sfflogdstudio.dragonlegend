using UnityEditor;
using UnityEngine;

public static class BuildBonusCard
{
    public static void Save()
    {
        // UIBonusView's body and glow share this source skeleton and RectTransform.
        BuildJackpotPopupArt.Create("ef_jinbi","棋子/jinbi",new Vector2(187,172),
            new Vector2(.49999967f,.5f),"Artifacts/BonusSource/","Artifacts/BonusAuthoring/");
        AssetDatabase.SaveAssets();
    }
}
