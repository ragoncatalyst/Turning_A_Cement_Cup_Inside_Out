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
        transform.rotation = Quaternion.Euler(20f, -30f, 0f);
    }

    void LateUpdate()
    {
        if (player != null && playerMov != null)
        {
            // 调整距离
            float targetDistance = playerMov.isCrouching ? distance - crouchDistanceReduction : distance;
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * lerpSpeed);

            // 计算位置
            Vector3 offset = transform.rotation * new Vector3(0, 0, -currentDistance);
            transform.position = player.position + offset;

            // 固定旋转
            transform.rotation = Quaternion.Euler(20f, -30f, 0f);
        }
    }
}
