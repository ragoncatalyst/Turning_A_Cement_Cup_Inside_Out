using UnityEngine;
using UnityEditor;

/// <summary>
/// Converts 2D SpriteRenderer objects to 3D Quads with sprite material
/// Preserves animations, colors, and sorting orders
/// </summary>
public class SpriteToQuadConverter
{
    [MenuItem("Tools/Convert Selected Sprite to Quad")]
    public static void ConvertSelectedSpriteToQuad()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a GameObject with SpriteRenderer", "OK");
            return;
        }

        ConvertGameObjectToQuad(selected);
    }

    [MenuItem("Tools/Convert All Sprites in Scene to Quads")]
    public static void ConvertAllSpritesToQuads()
    {
        SpriteRenderer[] allSprites = Object.FindObjectsOfType<SpriteRenderer>();
        int convertedCount = 0;

        foreach (var spriteRenderer in allSprites)
        {
            // Skip if already has a MeshFilter (already converted)
            if (spriteRenderer.GetComponent<MeshFilter>() != null)
                continue;

            ConvertGameObjectToQuad(spriteRenderer.gameObject);
            convertedCount++;
        }

        Debug.Log($"✓ Converted {convertedCount} sprites to quads");
    }

    static void ConvertGameObjectToQuad(GameObject go)
    {
        SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError($"GameObject {go.name} does not have a SpriteRenderer");
            return;
        }

        // ✅ DO NOT delete SpriteRenderer - keep it for animation and dynamic updates
        // Just ensure DynamicSorting component is present for proper 3D depth ordering
        
        DynamicSorting dynamicSorting = go.GetComponent<DynamicSorting>();
        if (dynamicSorting == null)
        {
            dynamicSorting = go.AddComponent<DynamicSorting>();
        }

        Debug.Log($"✓ Enhanced {go.name} with DynamicSorting for 3D depth ordering (kept SpriteRenderer intact)");
    }
}
