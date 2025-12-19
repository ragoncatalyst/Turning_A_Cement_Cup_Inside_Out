using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRen : MonoBehaviour
{
    private GameObject renderSquare;
    private GameObject renderQuad; // runtime mesh quad used for shadow casting
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private Rigidbody playerRigidbody;
    private bool lastFlip = false;
    [Tooltip("Threshold of horizontal velocity to trigger flip (world X)" )]
    public float flipThreshold = 0.05f;
    [Tooltip("If true, flip direction is inverted (useful if sprite art faces left by default).")]
    public bool invertFlip = true;
    [Tooltip("Default flip state when the special animation is not playing")]
    public bool defaultFlip = false;
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
        spriteRenderer = renderSquare.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogError("PlayerRen: RenderSquare 上需要有 SpriteRenderer。请添加后重试。");
            return;
        }

        // 确保使用 2D SpriteRenderer 渲染（严格遵守 2D 渲染要求）
        spriteRenderer.enabled = true;

        // initialize flip state
        spriteRenderer.flipX = defaultFlip;
        lastFlip = defaultFlip;

        // cache animator and player's rigidbody if present
        animator = player.GetComponentInChildren<Animator>();
        playerRigidbody = player.GetComponent<Rigidbody>();
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

        // --- Animation flip logic ---
        if (spriteRenderer != null)
        {
            bool handled = false;
            if (animator != null)
            {
                var clips = animator.GetCurrentAnimatorClipInfo(0);
                if (clips != null && clips.Length > 0 && clips[0].clip != null)
                {
                    string clipName = clips[0].clip.name;
                    if (clipName == "PlayerIdle_BottomRight")
                    {

                        // Determine camera-relative horizontal movement
                        float camDot = 0f;
                        Vector3 camRight = cam.transform.right;
                        camRight.y = 0f; camRight.Normalize();
                        if (playerRigidbody != null)
                        {
                            Vector3 horVel = playerRigidbody.velocity;
                            horVel.y = 0f;
                            camDot = Vector3.Dot(horVel, camRight);
                        }
                        else
                        {
                            float inH = Input.GetAxisRaw("Horizontal");
                            float inV = Input.GetAxisRaw("Vertical");
                            Vector3 camForward = cam.transform.forward;
                            camForward.y = 0f; camForward.Normalize();
                            Vector3 inputVec = camRight * inH + camForward * inV;
                            camDot = Vector3.Dot(inputVec, camRight);
                        }

                        bool flip = spriteRenderer.flipX;
                        // If invertFlip==true then moving to camera-right should set flip=true; else flip=false
                        bool flipWhenMovingRight = invertFlip;
                        if (camDot > flipThreshold) flip = flipWhenMovingRight;
                        else if (camDot < -flipThreshold) flip = !flipWhenMovingRight;

                        if (flip != lastFlip)
                        {
                            spriteRenderer.flipX = flip;
                            lastFlip = flip;
                        }

                        handled = true;
                    }
                }
            }

            // when not handled by the special animation, ensure default flip is enforced
            if (!handled)
            {
                if (spriteRenderer.flipX != defaultFlip)
                {
                    spriteRenderer.flipX = defaultFlip;
                    lastFlip = defaultFlip;
                }
            }
        }
    }
}