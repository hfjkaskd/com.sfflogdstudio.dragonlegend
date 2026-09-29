using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

// Author the recovered cash-out font from its bundled source. Imported PNG atlases
// can be compressed and cannot serve as writable TextMeshPro dynamic atlases.
public static class BuildCashOutFont
{
    private const string FontPath="Assets/Resources/RecoveredUI/CashOutItemArt/msyhbd SDF.asset";
    private const string MaterialPath="Assets/Resources/RecoveredUI/CashOutItemArt/msyhbd Atlas Material.mat";
    private const string SourcePath="Assets/Resources/RecoveredArt/Res/Font/msyhbd.ttf";

    [MenuItem("Tools/Repair/Cash Out Font")]
    public static void Save()
    {
        var target=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        var material=AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        var source=AssetDatabase.LoadAssetAtPath<Font>(SourcePath);
        if(target==null||material==null||source==null)throw new InvalidDataException("Cash-out font source or authored assets are missing.");
        var characters=new SortedSet<uint>();
        foreach(var character in target.characterTable)characters.Add(character.unicode);
        for(uint value=32;value<=126;value++)characters.Add(value);
        uint[] required=new uint[characters.Count];characters.CopyTo(required);
        var oldTextures=(Texture2D[])target.atlasTextures.Clone();
        TMP_FontAsset generated=null,backup=null;
        Material temporaryMaterial=null,materialBackup=null;
        bool committed=false;
        try
        {
            generated=TMP_FontAsset.CreateFontAsset(source,(int)target.faceInfo.pointSize,target.atlasPadding,
                (GlyphRenderMode)target.atlasRenderMode,target.atlasWidth,target.atlasHeight,AtlasPopulationMode.Dynamic,true);
            if(generated==null)throw new InvalidDataException("Unable to create the cash-out font from its source.");
            temporaryMaterial=generated.material;
            uint[] missing;
            if(!generated.TryAddCharacters(required,out missing,true))throw new InvalidDataException("Cash-out source is missing required characters: "+string.Join(",",missing));
            generated.name=target.name;
            generated.normalStyle=target.normalStyle;generated.normalSpacingOffset=target.normalSpacingOffset;
            generated.boldStyle=target.boldStyle;generated.boldSpacing=target.boldSpacing;
            generated.italicStyle=target.italicStyle;generated.tabSize=target.tabSize;
            generated.fallbackFontAssetTable=target.fallbackFontAssetTable;
            Array.Copy(target.fontWeightTable,generated.fontWeightTable,Math.Min(target.fontWeightTable.Length,generated.fontWeightTable.Length));
            var settings=target.creationSettings;settings.sourceFontFileGUID=AssetDatabase.AssetPathToGUID(SourcePath);
            generated.creationSettings=settings;
            generated.material=material;
            // Validate the full result before adding any persistent subresources.
            foreach(var texture in generated.atlasTextures)
                if(texture!=null&&texture.format!=TextureFormat.Alpha8)
                    throw new InvalidDataException("Dynamic font atlas must be Alpha8.");
            backup=ScriptableObject.CreateInstance<TMP_FontAsset>();
            EditorUtility.CopySerialized(target,backup);
            materialBackup=new Material(material){name=material.name};
            foreach(var texture in generated.atlasTextures)
            {
                if(texture==null)continue;
                texture.name=target.name+" Atlas";texture.filterMode=FilterMode.Bilinear;texture.wrapMode=TextureWrapMode.Clamp;
                AssetDatabase.AddObjectToAsset(texture,target);
            }
            // Copy into the existing objects so every prefab keeps its font/material GUID.
            EditorUtility.CopySerialized(generated,target);
            material.mainTexture=target.atlasTextures[0];
            material.SetFloat(ShaderUtilities.ID_TextureWidth,target.atlasWidth);
            material.SetFloat(ShaderUtilities.ID_TextureHeight,target.atlasHeight);
            material.SetFloat(ShaderUtilities.ID_GradientScale,target.atlasPadding+1);
            target.ReadFontAssetDefinition();
            EditorUtility.SetDirty(target);EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            committed=true;
            // Keep the old subresources alive until the replacement is saved successfully.
            if(oldTextures!=null)foreach(var texture in oldTextures)
                if(texture!=null&&AssetDatabase.GetAssetPath(texture)==FontPath&&
                    Array.IndexOf(generated.atlasTextures,texture)<0)
                    UnityEngine.Object.DestroyImmediate(texture,true);
            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(FontPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            AssetDatabase.ImportAsset(MaterialPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("CASH_OUT_FONT_REBUILT: "+required.Length+" pre-baked characters; writable Alpha8 atlas; existing prefab GUIDs retained.");
        }
        catch
        {
            if(!committed&&backup!=null)
            {
                EditorUtility.CopySerialized(backup,target);
                if(materialBackup!=null)EditorUtility.CopySerialized(materialBackup,material);
                target.ReadFontAssetDefinition();
                DestroyUncommittedTextures(generated);
                EditorUtility.SetDirty(target);EditorUtility.SetDirty(material);
                AssetDatabase.SaveAssets();
            }
            throw;
        }
        finally
        {
            if(!committed)DestroyUncommittedTextures(generated);
            UnityEngine.Object.DestroyImmediate(temporaryMaterial);
            UnityEngine.Object.DestroyImmediate(generated);
            UnityEngine.Object.DestroyImmediate(backup);
            UnityEngine.Object.DestroyImmediate(materialBackup);
            // TMP also caches its first atlas in a nonserialized field. Reload after the
            // current menu/build method completes so existing text sees the new texture.
            if(committed)EditorUtility.RequestScriptReload();
        }
    }

    private static void DestroyUncommittedTextures(TMP_FontAsset generated)
    {
        if(generated==null||generated.atlasTextures==null)return;
        foreach(var texture in generated.atlasTextures)
            if(texture!=null)UnityEngine.Object.DestroyImmediate(texture,AssetDatabase.Contains(texture));
    }
}
