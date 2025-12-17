using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ShadowCast : MonoBehaviour
{
    void Start()
    {
        // 设置SpriteRenderer投射阴影
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        // 确保有Directional Light，且Realtime
        Light[] lights = FindObjectsOfType<Light>();
        foreach (Light light in lights)
        {
            if (light.type == LightType.Directional)
            {
                light.shadows = LightShadows.Soft; // 或Hard
            }
        }

        // 接收阴影的物体需要MeshRenderer.receiveShadows = true，但这里不设置，因为是其他物体
    }
}