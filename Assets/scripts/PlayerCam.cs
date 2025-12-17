using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCam : MonoBehaviour
{
    [Header("Camera Settings")]
    public float distance = 10f;
    public float crouchDistanceReduction = 3f;
    public float lerpSpeed = 5f;

    public PlayerMov playerMov;
    private Transform player;
    private float currentDistance;

    void Start()
    {
        player = GameObject.Find("Player_Lullaby").transform;
        currentDistance = distance;
        transform.rotation = Quaternion.Euler(30f, -30f, 0f);
    }

    void LateUpdate()
    {
        if (player != null)
        {
            // 如果没有通过 Inspector 连接 PlayerMov，则尝试自动查找
            if (playerMov == null)
            {
                var pm = player.GetComponent<PlayerMov>();
                if (pm != null) playerMov = pm;
            }

            if (playerMov != null)
            {
                // 调整距离：蹲伏时距离为原距离的 50%
                float targetDistance = playerMov.isCrouching ? distance * 0.5f : distance;
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * lerpSpeed);
            }

            // 计算位置并固定旋转
            transform.rotation = Quaternion.Euler(30f, -30f, 0f);
            Vector3 offset = transform.rotation * new Vector3(0, 0, -currentDistance);
            transform.position = player.position + offset;
        }
    }
}
