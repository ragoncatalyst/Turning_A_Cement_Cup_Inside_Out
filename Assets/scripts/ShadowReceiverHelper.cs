using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShadowReceiverHelper : MonoBehaviour
{
    [Header("Auto-fix settings (edit in Inspector)")]
    public bool applyOnEnable = false;
    public bool includeAllMeshRenderers = false;
    public Color ambientColor = new Color(0.12f, 0.12f, 0.12f);

    void OnEnable()
    {
        if (applyOnEnable) ApplyFixes();
    }

    [ContextMenu("Apply Fixes Now")]
    public void ApplyFixes()
    {
        RenderSettings.ambientLight = ambientColor;

        // Ensure directional lights cast shadows
        Light[] lights = FindObjectsOfType<Light>();
        foreach (var l in lights)
        {
            if (l.type == LightType.Directional)
            {
                l.shadows = LightShadows.Soft;
                l.shadowStrength = 0.8f;
            }
        }

        // Enable receiveShadows on ground-like MeshRenderers
        if (includeAllMeshRenderers)
        {
            MeshRenderer[] mrs = FindObjectsOfType<MeshRenderer>();
            foreach (var mr in mrs)
            {
                mr.receiveShadows = true;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }
        else
        {
            // Try tag-based: objects tagged as "Ground"
            GameObject[] grounds = GameObject.FindGameObjectsWithTag("Ground");
            foreach (var g in grounds)
            {
                var mr = g.GetComponent<MeshRenderer>();
                if (mr != null) mr.receiveShadows = true;
            }
        }

        Debug.Log("ShadowReceiverHelper: applied fixes (ambient, lights, receivers)");
    }
}
