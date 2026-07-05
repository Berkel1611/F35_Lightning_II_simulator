using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class GearAnimationFixer
{
    [MenuItem("Tools/Gear Fix/List Clip Paths")]
    static void ListPaths()
    {
        var clip = Selection.activeObject as AnimationClip;
        if (clip == null) { Debug.LogError("Zaznacz AnimationClip w Project window"); return; }

        foreach (var b in AnimationUtility.GetCurveBindings(clip))
            Debug.Log($"{b.path}  ({b.propertyName}, {b.type.Name})");
    }

    static readonly Dictionary<string, string> RootMap = new Dictionary<string, string>
    {
        { "nose_root",               "LandingGear/Nose/nose_root" },
        { "nose_doors_left",         "LandingGear/Nose/nose_doors_left" },
        { "nose_doors_right",        "LandingGear/Nose/nose_doors_right" },
        { "landing_gear_left_root",  "LandingGear/Left/landing_gear_left_root" },
        { "side_doors_left",         "LandingGear/Left/side_doors_left" },
        { "landing_gear_right_root", "LandingGear/Right/landing_gear_right_root" },
        { "side_doors_right",        "LandingGear/Right/side_doors_right" },
    };

    [MenuItem("Tools/Gear Fix/Remap Selected Clip")]
    static void RemapPaths()
    {
        var clip = Selection.activeObject as AnimationClip;
        if (clip == null) { Debug.LogError("Zaznacz AnimationClip w Project window"); return; }

        var newClip = Object.Instantiate(clip);
        newClip.name = clip.name + "_Fixed";

        int fixedCount = 0, skipped = 0;
        foreach (var binding in AnimationUtility.GetCurveBindings(clip))
        {
            var curve = AnimationUtility.GetEditorCurve(clip, binding);
            var newBinding = binding;

            // Pierwszy segment starej œcie¿ki (np. "landing_gear_left_root" z "landing_gear_left_root/lower_left_root")
            string oldPath = binding.path;
            int slash = oldPath.IndexOf('/');
            string rootSegment = slash >= 0 ? oldPath.Substring(0, slash) : oldPath;
            string remainder = slash >= 0 ? oldPath.Substring(slash) : ""; // zawiera wiod¹cy '/'

            if (RootMap.TryGetValue(rootSegment, out string newRoot))
            {
                newBinding.path = newRoot + remainder;
                fixedCount++;
            }
            else
            {
                Debug.LogWarning($"Brak mapowania dla root segmentu '{rootSegment}' (pe³na œcie¿ka: '{oldPath}') — krzywa skopiowana bez zmian");
                skipped++;
            }

            AnimationUtility.SetEditorCurve(newClip, newBinding, curve);
        }

        if (!AssetDatabase.IsValidFolder("Assets/Animations"))
            AssetDatabase.CreateFolder("Assets", "Animations");

        string savePath = $"Assets/Animations/{newClip.name}.anim";
        AssetDatabase.CreateAsset(newClip, savePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Naprawiono {fixedCount} krzywych, pominiêto {skipped}. Nowy klip: {savePath}");
    }
}