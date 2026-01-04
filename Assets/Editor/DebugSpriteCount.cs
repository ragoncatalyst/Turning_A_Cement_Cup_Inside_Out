using UnityEngine;
using UnityEditor;

public class DebugSpriteCount
{
    [MenuItem("Tools/Debug - Count Sprites in Scene")]
    public static void CountSprites()
    {
        SpriteRenderer[] allSprites = Object.FindObjectsOfType<SpriteRenderer>();
        Debug.Log($"✓ Found {allSprites.Length} SpriteRenderer(s) in scene:");
        
        foreach (var sprite in allSprites)
        {
            Debug.Log($"  - {sprite.gameObject.name} (has DynamicSorting: {sprite.GetComponent<DynamicSorting>() != null})");
        }
    }
}
