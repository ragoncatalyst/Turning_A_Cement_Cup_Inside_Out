using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SceneDebugger : MonoBehaviour
{
    public bool runOnEnable = false;

    void OnEnable()
    {
        if (runOnEnable) LogSceneInfo();
    }

    [ContextMenu("Log Scene Info")]
    public void LogSceneInfo()
    {
        var player = GameObject.Find("Player_Lullaby");
        if (player == null) Debug.LogWarning("Player_Lullaby not found in scene");
        else
        {
            Debug.Log("Player_Lullaby found: " + player.name);
            var rs = player.transform.Find("RenderSquare");
            if (rs == null) Debug.LogWarning("RenderSquare child not found under Player_Lullaby");
            else
            {
                Debug.Log("RenderSquare found: " + rs.name);
                var sr = rs.GetComponent<SpriteRenderer>();
                if (sr != null) Debug.Log("SpriteRenderer present. enabled=" + sr.enabled + ", sprite=" + (sr.sprite? sr.sprite.name : "null"));
                var quad = rs.Find("RenderQuad");
                if (quad != null)
                {
                    var mf = quad.GetComponent<MeshFilter>();
                    var mr = quad.GetComponent<MeshRenderer>();
                    if (mf != null) Debug.Log("RenderQuad mesh vertexCount=" + mf.sharedMesh.vertexCount);
                    if (mr != null)
                    {
                        Debug.Log("RenderQuad MeshRenderer: shader=" + (mr.sharedMaterial? mr.sharedMaterial.shader.name : "null") + ", renderQueue=" + (mr.sharedMaterial? mr.sharedMaterial.renderQueue.ToString() : "null") + ", keywords=" + (mr.sharedMaterial? string.Join(",", mr.sharedMaterial.shaderKeywords) : "null") );
                        Debug.Log("shadowCastingMode=" + mr.shadowCastingMode + ", receiveShadows=" + mr.receiveShadows + ", enabled=" + mr.enabled);
                        if (mr.sharedMaterial != null && mr.sharedMaterial.mainTexture != null) Debug.Log("mainTexture=" + mr.sharedMaterial.mainTexture.name);
                    }
                    else Debug.LogWarning("RenderQuad MeshRenderer missing");
                }
            }
        }
        // Lights: list Directional Lights
        Light[] lights = FindObjectsOfType<Light>();
        int dirCount = 0;
        foreach (var l in lights)
        {
            Debug.Log("Light: " + l.name + ", type=" + l.type + ", shadows=" + l.shadows + ", strength=" + l.shadowStrength);
            if (l.type == LightType.Directional) dirCount++;
        }
        if (dirCount == 0) Debug.LogWarning("No Directional Light found (required for large-scene realtime shadows)");

        // Quality and Graphics settings
        Debug.Log("Quality.shadowDistance=" + UnityEngine.QualitySettings.shadowDistance + ", pixelLightCount=" + UnityEngine.QualitySettings.pixelLightCount);
        var rp = UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset;
        Debug.Log("Graphics.renderPipelineAsset=" + (rp? rp.name : "Builtin"));

        // list ground objects with MeshRenderer (tagged)
        var grounds = GameObject.FindGameObjectsWithTag("Ground");
        Debug.Log("Ground tagged objects count=" + grounds.Length);
        foreach (var g in grounds)
        {
            var mr = g.GetComponent<MeshRenderer>();
            Debug.Log("Ground: " + g.name + ", hasMesh=" + (mr!=null) + ", receiveShadows=" + (mr? mr.receiveShadows.ToString() : "N/A"));
        }
    }
}
