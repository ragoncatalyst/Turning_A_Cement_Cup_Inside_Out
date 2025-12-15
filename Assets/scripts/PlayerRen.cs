using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRen : MonoBehaviour
{
    private GameObject shadowObject;

    void Start()
    {
        // 创建影子
        shadowObject = new GameObject("Shadow");
        shadowObject.transform.parent = transform;
        shadowObject.transform.localPosition = Vector3.zero;

        MeshRenderer shadowMr = shadowObject.AddComponent<MeshRenderer>();
        MeshFilter shadowMf = shadowObject.AddComponent<MeshFilter>();
        MeshRenderer playerMr = GetComponent<MeshRenderer>();
        if (playerMr != null)
        {
            shadowMr.material = playerMr.material;
            shadowMr.material.color = new Color(0, 0, 0, 0.3f);
            shadowMf.mesh = GetComponent<MeshFilter>().mesh; // 假设有MeshFilter
        }
        shadowObject.transform.rotation = Quaternion.Euler(90, 0, 0);
    }

    void Update()
    {
        if (shadowObject != null)
        {
            // 影子位置在地面
            shadowObject.transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            shadowObject.transform.rotation = Quaternion.Euler(90, 0, 0);
        }

        // 广告牌技术：让玩家物体始终面向摄像头
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
        }
    }
}