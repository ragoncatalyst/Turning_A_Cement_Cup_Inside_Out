using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

public class SetupQuadWithSpritesheet : MonoBehaviour
{
    [MenuItem("Tools/Setup Quad Spritesheet")]
    private static void SetupPlayerQuadWithSpritesheet()
    {
        GameObject player = GameObject.Find("Player_Lullaby");
        if (player == null)
        {
            Debug.LogError("[SetupQuadWithSpritesheet] 找不到 Player_Lullaby");
            return;
        }
        
        Transform renderSquare = player.transform.Find("RenderSquare");
        if (renderSquare == null)
        {
            Debug.LogError("[SetupQuadWithSpritesheet] 找不到 RenderSquare");
            return;
        }
        
        GameObject quad = renderSquare.gameObject;
        
        // 确保有 MeshFilter 和 MeshRenderer
        MeshFilter meshFilter = quad.GetComponent<MeshFilter>();
        if (meshFilter == null) meshFilter = quad.AddComponent<MeshFilter>();
        if (meshFilter.sharedMesh == null)
        {
            meshFilter.sharedMesh = CreateQuadMesh();
        }
        
        MeshRenderer meshRenderer = quad.GetComponent<MeshRenderer>();
        if (meshRenderer == null) meshRenderer = quad.AddComponent<MeshRenderer>();
        
        // 删除 SpriteRenderer
        SpriteRenderer spriteRenderer = quad.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null) DestroyImmediate(spriteRenderer);
        
        // 加载贴图
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Arts/Lullaby_BottomR.png");
        if (texture == null)
        {
            Debug.LogError("[SetupQuadWithSpritesheet] 找不到 Lullaby_BottomR.png");
            return;
        }
        
        // 加载或创建 Shader
        Shader shader = Shader.Find("Custom/SimpleQuadSpritesheet");
        if (shader == null)
        {
            Debug.LogError("[SetupQuadWithSpritesheet] 找不到 Custom/SimpleQuadSpritesheet");
            return;
        }
        
        // 创建材质
        Material mat = new Material(shader);
        mat.name = "Mat_QuadSpritesheet_Player";
        mat.SetTexture("_MainTex", texture);
        mat.SetColor("_Color", Color.white);
        mat.SetFloat("_FrameCountX", 4f);
        mat.SetFloat("_FrameCountY", 4f);
        mat.SetFloat("_CurrentFrame", 0f);
        
        meshRenderer.material = mat;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        meshRenderer.receiveShadows = true;
        
        // 添加或更新动画脚本
        QuadSpritesheetAnimator animator = quad.GetComponent<QuadSpritesheetAnimator>();
        if (animator == null)
        {
            animator = quad.AddComponent<QuadSpritesheetAnimator>();
        }
        
        // 标记场景为脏以保存更改
        EditorSceneManager.MarkSceneDirty(player.scene);
        
        Debug.Log("[SetupQuadWithSpritesheet] 完成！Quad 已配置 Spritesheet 动画");
    }
    
    private static Mesh CreateQuadMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "QuadMesh_Spritesheet";
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, -0.5f, 0f),
            new Vector3(0.5f, -0.5f, 0f),
            new Vector3(0.5f, 0.5f, 0f),
            new Vector3(-0.5f, 0.5f, 0f)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(1, 1),
            new Vector2(0, 1)
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,
            0, 3, 2
        };
        
        Vector3[] normals = new Vector3[4]
        {
            Vector3.forward,
            Vector3.forward,
            Vector3.forward,
            Vector3.forward
        };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.normals = normals;
        
        return mesh;
    }
}
#endif
