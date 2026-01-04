using UnityEngine;
using UnityEditor;

/// <summary>
/// Helper script to ensure the DepthWrite shader is properly compiled and available
/// </summary>
public class ShaderCompileHelper
{
    [MenuItem("Tools/Verify Sprites/DepthWrite Shader")]
    public static void VerifyShader()
    {
        Shader shader = Shader.Find("Sprites/DepthWrite");
        if (shader != null)
        {
            Debug.Log("✓ Sprites/DepthWrite shader found and compiled!");
        }
        else
        {
            Debug.LogError("✗ Sprites/DepthWrite shader NOT found. Asset may not be compiled yet.");
            Debug.LogError("  - Check: Assets/Shaders/SpriteDepthWrite.shader");
            Debug.LogError("  - Try: Unity menu > Assets > Reimport All");
        }
    }

    [MenuItem("Tools/Force Shader Reimport")]
    public static void ForceShaderReimport()
    {
        string shaderPath = "Assets/Shaders/SpriteDepthWrite.shader";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        Debug.Log("Shader reimport forced for: " + shaderPath);
        
        EditorApplication.update += () =>
        {
            EditorApplication.update -= null;
            VerifyShader();
        };
    }

    [MenuItem("Tools/Apply DepthWrite to All Sprites")]
    public static void ApplyShaderToAllSprites()
    {
        Shader depthShader = Shader.Find("Sprites/DepthWrite");
        if (depthShader == null)
        {
            Debug.LogError("Cannot apply shader - Sprites/DepthWrite not found!");
            return;
        }

        var dynamicSortings = Object.FindObjectsOfType<DynamicSorting>();
        int count = 0;
        
        foreach (var sorting in dynamicSortings)
        {
            var renderer = sorting.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                Material mat = new Material(depthShader);
                if (renderer.sprite != null)
                {
                    mat.SetTexture("_MainTex", renderer.sprite.texture);
                }
                mat.SetColor("_Color", renderer.color);
                renderer.material = mat;
                count++;
            }
        }

        Debug.Log($"Applied DepthWrite shader to {count} sprites");
    }
}
