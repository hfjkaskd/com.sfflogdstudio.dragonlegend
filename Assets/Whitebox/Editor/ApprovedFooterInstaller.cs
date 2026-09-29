using System;
using System.Collections.Generic;
using System.IO;
using DragonLegend.Whitebox;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Authoring only. Run once after staged textures are imported; no runtime UI creation.
public static class ApprovedFooterInstaller
{
    private const string BankPrefab = "Assets/Resources/MainSkin/Assets/RecoveredUI/JackpotPopupArt/ef_slyinhang.prefab";
    private const string ChestTexture = "Assets/Resources/MainSkin/Textures/approved-bank-chest.png";

    public static void Apply()
    {
        var importer = AssetImporter.GetAtPath(ChestTexture) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Missing approved chest texture.");
        importer.textureType = TextureImporterType.Default;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = false; // Source pixels are premultiplied for the existing rig material.
        importer.sRGBTexture = false; // Matches the existing PMA bank atlas/import convention.
        importer.mipmapEnabled = false;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 1024;
        importer.SaveAndReimport();

        var root = PrefabUtility.LoadPrefabContents(BankPrefab);
        try
        {
            var rig = root.GetComponent<RecoveredRegionRig>();
            if (rig == null) throw new InvalidOperationException("Bank visual rig missing.");
            var serializedRig = new SerializedObject(rig);
            var atlas = serializedRig.FindProperty("atlasPath");
            if (atlas.stringValue == "MainSkin/Textures/approved-bank-chest") return;
            if (atlas.stringValue != "MainSkin/Textures/bank")
                throw new InvalidOperationException("Bank visual was modified independently; refusing overwrite.");

            // Snapshot every existing transform. The chest replaces only the rendered geometry.
            var transforms = root.GetComponentsInChildren<RectTransform>(true);
            var positions = new Vector2[transforms.Length];
            var sizes = new Vector2[transforms.Length];
            for (int i = 0; i < transforms.Length; i++)
            {
                positions[i] = transforms[i].anchoredPosition;
                sizes[i] = transforms[i].sizeDelta;
            }

            rig.RefreshPose();
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            var region = rig.regions[0];
            var mainSlot = rig.slots[2];
            if (region.counts != null && region.counts.Length > 0)
            {
                int influence = 0;
                for (int v = 0; v < region.counts.Length; v++)
                {
                    Vector3 point = Vector3.zero;
                    for (int w = 0; w < region.counts[v]; w++, influence++)
                        point += rig.BoneMatrix(region.boneIndices[influence])
                            .MultiplyPoint3x4(region.vertices[influence]) * region.weights[influence];
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
            else
            {
                foreach (var vertex in region.vertices)
                {
                    var point = rig.BoneMatrix(mainSlot.bone).MultiplyPoint3x4(vertex);
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }
            if (max.x - min.x < 100 || max.y - min.y < 100)
                throw new InvalidOperationException("Unexpected bank visual bounds.");

            // The old animation bends bag fabric. It has no gameplay callbacks; bank progress
            // lives on its sibling and remains active. A rigid chest must not sample bag bones.
            var animator = root.GetComponent<RecoveredRegionAnimator>();
            if (animator != null) animator.enabled = false;
            var animation = root.GetComponent<Animation>();
            if (animation != null)
            {
                animation.Stop();
                animation.playAutomatically = false;
                animation.enabled = false;
            }

            // Preserve the existing Resources-based Graphic inside bankBtn. Its parent Button,
            // progress Image, label, click listeners and every RectTransform stay unchanged.
            rig.bones = new[] { new RecoveredRegionRig.Bone { name = "approved_chest", parent = -1 } };
            rig.slots = new[] { new RecoveredRegionRig.Slot { bone = 0, attachment = 0, tint = Color.white } };
            rig.regions = new[]
            {
                new RecoveredRegionRig.Region
                {
                    vertices = new[] { new Vector2(min.x,min.y),new Vector2(max.x,min.y),
                        new Vector2(max.x,max.y),new Vector2(min.x,max.y) },
                    uv = new[] { new Vector2(0,0),new Vector2(1,0),new Vector2(1,1),new Vector2(0,1) },
                    tint = Color.white,
                    triangles = Array.Empty<int>(), counts = Array.Empty<int>(),
                    boneIndices = Array.Empty<int>(), weights = Array.Empty<float>()
                }
            };
            rig.npcConstraints = null;
            serializedRig.Update();
            serializedRig.FindProperty("atlasPath").stringValue = "MainSkin/Textures/approved-bank-chest";
            serializedRig.ApplyModifiedPropertiesWithoutUndo();
            rig.RefreshPose();
            for (int i = 0; i < transforms.Length; i++)
                if (transforms[i].anchoredPosition != positions[i] || transforms[i].sizeDelta != sizes[i])
                    throw new InvalidOperationException("Footer authoring unexpectedly changed transform geometry.");
            PrefabUtility.SaveAsPrefabAsset(root, BankPrefab);
            Debug.Log("Approved footer: rigid chest uses original bounds " + min + " to " + max
                + "; bank Button/progress and spin Button animation/callbacks remain unchanged.");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        AssetDatabase.SaveAssets();
    }
}

