using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Helper to setup Ground and other sprites as Quad meshes with depth shaders
/// </summary>
public class QuadSetupHelper
{
    [MenuItem("Tools/Setup Ground as 3D Quad")]
    public static void SetupGroundAsQuad()
    {
        GameObject ground = GameObject.Find("Ground");
        if (ground == null)
        {
            EditorUtility.DisplayDialog("Error", "Ground object not found in scene", "OK");
            return;
        }

        // Simply add DynamicSorting component
        DynamicSorting sorting = ground.GetComponent<DynamicSorting>();
        if (sorting == null)
        {
            sorting = ground.AddComponent<DynamicSorting>();
            Debug.Log("✓ Added DynamicSorting to Ground for proper depth ordering");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }
        else
        {
            Debug.Log("Ground already has DynamicSorting component");
        }
    }

    [MenuItem("Tools/Setup All Sprites as Quads")]
    public static void SetupAllSpritesAsQuads()
    {
        SpriteRenderer[] allSprites = Object.FindObjectsOfType<SpriteRenderer>();
        int count = 0;

        foreach (var renderer in allSprites)
        {
            // ✅ Keep SpriteRenderer - add DynamicSorting instead of converting to mesh
            DynamicSorting sorting = renderer.GetComponent<DynamicSorting>();
            if (sorting == null)
            {
                sorting = renderer.gameObject.AddComponent<DynamicSorting>();
                count++;
            }
        }

        Debug.Log($"✓ Added DynamicSorting to {count} sprite objects for 3D depth ordering");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }
}
