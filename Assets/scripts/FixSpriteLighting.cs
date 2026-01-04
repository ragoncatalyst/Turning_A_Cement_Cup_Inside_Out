using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// FixSpriteLighting: 强制修复 SpriteRenderer 的光照问题
/// </summary>
public class FixSpriteLighting : MonoBehaviour
{
    [MenuItem("Tools/Fix Lighting/Force Player Sprite to Use Lit Shader")]
    private static void FixPlayerSpriteLighting()
    {
        // 找到 Player 的 SpriteDisplay
        GameObject player = GameObject.Find("Player_Lullaby");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Error", "找不到 Player_Lullaby", "OK");
            return;
        }
        
        Transform renderSquare = player.transform.Find("RenderSquare");
        if (renderSquare == null)
        {
            EditorUtility.DisplayDialog("Error", "找不到 RenderSquare", "OK");
            return;
        }
        
        Transform spriteDisplay = renderSquare.Find("SpriteDisplay");
        if (spriteDisplay == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "找不到 SpriteDisplay。请先执行 'Create Sprite Child for Player'", "OK");
            return;
        }
        
        SpriteRenderer sr = spriteDisplay.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            EditorUtility.DisplayDialog("Error", "SpriteDisplay 上没有 SpriteRenderer", "OK");
            return;
        }
        
        // 尝试找 Shader（按优先级）
        Shader shader = null;
        string shaderName = "";
        
        // 1. 尝试 2D Sprite 的 Lit 版本
        shader = Shader.Find("Sprites/Lit");
        shaderName = "Sprites/Lit";
        
        // 2. 如果不存在，尝试 URP 2D Lit
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            shaderName = "Universal Render Pipeline/2D/Sprite-Lit-Default";
        }
        
        // 3. 都不存在，使用 Unlit（总是存在，但不接受光照）
        if (shader == null)
        {
            shader = Shader.Find("Universal Render Pipeline/Unlit");
            shaderName = "Universal Render Pipeline/Unlit";
        }
        
        // 4. 最后备选：Sprites/Default
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
            shaderName = "Sprites/Default";
        }
        
        if (shader == null)
        {
            EditorUtility.DisplayDialog("Error", 
                "找不到任何可用的 Sprite Shader！\n" +
                "请检查 URP 是否正确安装。", "OK");
            return;
        }
        
        // 创建新材质
        Material mat = new Material(shader);
        mat.name = "SpriteMaterial_Player";
        
        // 设置颜色
        mat.SetColor("_Color", Color.white);
        
        // 如果是 Lit Shader，禁用自发光
        if (mat.HasProperty("_Emission"))
            mat.SetColor("_Emission", Color.black);
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", Color.black);
        
        // 应用材质
        sr.material = mat;
        
        EditorUtility.DisplayDialog("Success",
            $"已为 Player Sprite 应用 Shader:\n{shaderName}\n\n" +
            "现在应该能看到正确的贴图。",
            "OK");
        
        Debug.Log($"[FixSpriteLighting] 已应用 Shader: {shaderName}");
    }
    
    [MenuItem("Tools/Fix Lighting/Verify Environment Settings")]
    private static void VerifyEnvironmentSettings()
    {
        // 检查场景的光照设置
        string info = "场景光照设置：\n";
        info += $"AmbientIntensity: {RenderSettings.ambientIntensity}\n";
        info += $"AmbientLight: {RenderSettings.ambientLight}\n";
        info += $"AmbientSkyColor: {RenderSettings.ambientSkyColor}\n";
        info += $"AmbientMode: {RenderSettings.ambientMode}\n";
        
        // 检查光源
        var lights = FindObjectsOfType<Light>();
        info += $"\n场景中的光源数量: {lights.Length}\n";
        foreach (var light in lights)
        {
            if (light.enabled)
                info += $"✓ {light.name} ({light.type}) - Intensity: {light.intensity}\n";
            else
                info += $"✗ {light.name} ({light.type}) - 已禁用\n";
        }
        
        EditorUtility.DisplayDialog("Environment Settings", info, "OK");
        Debug.Log(info);
    }
}
#endif
