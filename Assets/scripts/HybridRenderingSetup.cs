using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// HybridRenderingSetup: 混合 3D Quad（投射阴影）+ 2D Sprite（显示动画）方案
/// Parent: 3D Quad - 仅用于投射阴影，自己不可见
/// Child: 2D SpriteRenderer - 原有动画系统
/// </summary>
public class HybridRenderingSetup : MonoBehaviour
{
    [MenuItem("Tools/Hybrid Rendering/Convert Scene to Hybrid Mode")]
    private static void ConvertToHybridMode()
    {
        var allMeshFilters = FindObjectsOfType<MeshFilter>();
        int count = 0;
        
        foreach (var mf in allMeshFilters)
        {
            if (mf.mesh == null) continue;
            
            GameObject quadObject = mf.gameObject;
            
            // 检查是否已经有 SpriteRenderer（可能已经转换过）
            if (quadObject.GetComponent<SpriteRenderer>() != null)
            {
                // 已经有 SpriteRenderer，说明可能已经转换过
                continue;
            }
            
            // 配置 Quad（仅投射阴影）
            var meshRenderer = quadObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // 设置为仅投射阴影，不接收光照
                meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                meshRenderer.receiveShadows = false;
                
                // 设置材质为不可见（黑色透明）
                Material shadowMat = new Material(Shader.Find("Standard"));
                shadowMat.SetColor("_Color", new Color(0, 0, 0, 0));
                shadowMat.renderQueue = -1;  // 放在渲染队列最前面
                meshRenderer.sharedMaterial = shadowMat;
                
                count++;
            }
        }
        
        EditorUtility.DisplayDialog("Success",
            $"已将 {count} 个 Quad 配置为仅投射阴影模式\n" +
            $"现在可以在每个 Quad 下添加 SpriteRenderer 子物体来显示动画",
            "OK");
        
        Debug.Log($"[HybridRenderingSetup] 已配置 {count} 个 Quad");
    }
    
    [MenuItem("Tools/Hybrid Rendering/Create Sprite Child for Player")]
    private static void CreateSpriteChildForPlayer()
    {
        // 找到 Player
        GameObject player = GameObject.Find("Player_Lullaby");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Error", "找不到 Player_Lullaby", "OK");
            return;
        }
        
        // 查找 RenderSquare（3D Quad）
        Transform renderSquare = player.transform.Find("RenderSquare");
        if (renderSquare == null)
        {
            EditorUtility.DisplayDialog("Error", "找不到 RenderSquare", "OK");
            return;
        }
        
        // 创建 2D Sprite 子物体
        GameObject spriteChild = new GameObject("SpriteDisplay");
        spriteChild.transform.parent = renderSquare;
        spriteChild.transform.localPosition = Vector3.zero;
        spriteChild.transform.localRotation = Quaternion.identity;
        spriteChild.transform.localScale = Vector3.one;
        
        // 添加 SpriteRenderer
        SpriteRenderer sr = spriteChild.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Arts/Lullaby_BottomR.png");
        sr.color = Color.white;
        sr.sortingOrder = 0;
        
        // 关键：设置 SpriteRenderer 的 Material 为 Sprite Lit Shader
        // URP Sprite Lit Shader 会正确响应光照
        Shader litShader = Shader.Find("Universal Render Pipeline/Sprite Lit");
        
        if (litShader == null)
        {
            Debug.LogWarning("[HybridRenderingSetup] Universal Render Pipeline/Sprite Lit 未找到，尝试其他 Shader");
            litShader = Shader.Find("Sprites/Lit");
            if (litShader == null)
            {
                litShader = Shader.Find("Standard");
            }
        }
        
        Material spriteLitMat = new Material(litShader);
        spriteLitMat.name = "SpriteLit_Player";
        
        // 确保不自发光
        spriteLitMat.SetFloat("_Metallic", 0f);
        spriteLitMat.SetFloat("_Glossiness", 0.5f);
        
        // 禁用 Emission（如果有的话）
        if (spriteLitMat.HasProperty("_Emission"))
        {
            spriteLitMat.SetColor("_Emission", Color.black);
        }
        
        sr.material = spriteLitMat;
        
        // 添加 Animator（用于动画）
        Animator animator = spriteChild.AddComponent<Animator>();
        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animation/RenderSquare.controller");
        if (controller != null)
        {
            animator.runtimeAnimatorController = controller;
        }
        
        EditorUtility.DisplayDialog("Success",
            "已为 Player 创建 2D Sprite 子物体\n" +
            $"使用 Shader: {spriteLitMat.shader.name}\n" +
            "Quad 将投射阴影，Sprite 将显示动画并接受光照",
            "OK");
        
        Debug.Log($"[HybridRenderingSetup] 已为 Player 创建 Sprite 子物体（使用 {spriteLitMat.shader.name} Shader）");
    }
    
    [MenuItem("Tools/Hybrid Rendering/Switch Back to Pure Sprite Mode")]
    private static void SwitchBackToSpriteMode()
    {
        // 还原为纯 2D Sprite 模式
        var allMeshRenderers = FindObjectsOfType<MeshRenderer>();
        int count = 0;
        
        foreach (var mr in allMeshRenderers)
        {
            // 禁用所有 MeshRenderer
            mr.enabled = false;
            
            // 启用相应的 SpriteRenderer（如果存在）
            var sr = mr.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
            {
                sr.enabled = true;
                count++;
            }
        }
        
        EditorUtility.DisplayDialog("Success",
            $"已切换回纯 2D Sprite 模式\n" +
            $"已启用 {count} 个 SpriteRenderer",
            "OK");
        
        Debug.Log($"[HybridRenderingSetup] 已切换回纯 Sprite 模式");
    }
}
#endif
