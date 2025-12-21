using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public static class ReplaceMagentaMaterials
{
    [MenuItem("Tools/Replace Magenta Materials With White")]
    public static void ReplaceAll()
    {
        var sceneRenderers = Object.FindObjectsOfType<Renderer>();
        Shader fallback = FindCompatibleParticleShader();
        if (fallback == null) fallback = Shader.Find("Sprites/Default");
        if (fallback == null) fallback = Shader.Find("Unlit/Texture");
        if (fallback == null) fallback = Shader.Find("Unlit/Color");
        if (fallback == null) fallback = Shader.Find("Standard");
        int replaced = 0;

        foreach (var r in sceneRenderers)
        {
            var mat = r.sharedMaterial;
            bool bad = false;
            if (mat == null) bad = true;
            else
            {
                var s = mat.shader;
                if (s == null) bad = true;
                else if (!s.isSupported) bad = true;
                else
                {
                    string n = s.name.ToLower();
                    if (n.Contains("hidden") || n.Contains("magenta") ) bad = true;
                }
            }

            if (bad)
            {
                Material newMat = new Material(fallback);
                newMat.color = Color.white;
                // assign as sharedMaterial to modify scene object
                Undo.RecordObject(r, "Replace Magenta Material");
                r.sharedMaterial = newMat;
                replaced++;
            }
        }

        Debug.Log($"ReplaceMagentaMaterials: replaced {replaced} renderer materials in scene with white fallback.");
    }

    static Shader FindCompatibleParticleShader()
    {
        string[] candidates = new string[] {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Particles/Standard Unlit",
            "Particles/Standard Surface",
            "Sprites/Default",
            "Unlit/Texture"
        };
        foreach (var name in candidates)
        {
            var s = Shader.Find(name);
            if (s != null) return s;
        }
        return null;
    }
}
