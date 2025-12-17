using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRen : MonoBehaviour
{
    private GameObject renderSquare;
    private GameObject renderQuad; // runtime mesh quad used for shadow casting
    public enum BillboardMode { YOnly, FaceCamera }
    [Tooltip("YOnly: only rotate around Y to face camera (upright). FaceCamera: fully rotate to face camera on all axes")]
    public BillboardMode billboardMode = BillboardMode.YOnly;

    void Start()
    {
        // 硬编码查找 Player_Lullaby 并使用其子物体 RenderSquare 的 SpriteRenderer
        GameObject player = GameObject.Find("Player_Lullaby");
        if (player == null)
        {
            Debug.LogError("PlayerRen: 找不到名为 Player_Lullaby 的物体，请确认场景中存在此对象（区分大小写）。");
            return;
        }

        var rs = player.transform.Find("RenderSquare");
        if (rs == null)
        {
            Debug.LogError("PlayerRen: 在 Player_Lullaby 下找不到子物体 RenderSquare。请检查层级。");
            return;
        }

        renderSquare = rs.gameObject;
        var sr = renderSquare.GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            Debug.LogError("PlayerRen: RenderSquare 上需要有 SpriteRenderer。请添加后重试。");
            return;
        }

        // 确保使用 2D SpriteRenderer 渲染（严格遵守 2D 渲染要求）
        sr.enabled = true;
    }

    void Update()
    {
        // 广告牌行为：RenderSquare 实时面向 Camera.main（完整朝向），但绝不修改父物体 Transform
        if (renderSquare == null) return;
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform t = renderSquare.transform;

        // 方向向量（世界空间），从渲染面指向摄像机
        Vector3 dir = cam.transform.position - t.position;
        if (dir.sqrMagnitude < 1e-8f) return;

        // 世界旋转：使物体的正面(+Z)指向摄像机
        Quaternion worldRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        // 将世界旋转转为相对于父物体的局部旋转，写入 localRotation，确保 Inspector 显示 X/Z 变化
        if (t.parent != null)
        {
            Quaternion parentRot = t.parent.rotation;
            t.localRotation = Quaternion.Inverse(parentRot) * worldRot;
        }
        else
        {
            t.rotation = worldRot;
        }
    }
}