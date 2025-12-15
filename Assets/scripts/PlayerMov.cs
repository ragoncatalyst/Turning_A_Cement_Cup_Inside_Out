using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMov : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpForce = 5f;
    public float smoothTime = 0.1f;

    [Header("State")]
    public bool isJumping = false;
    public bool isCrouching = false;
    public bool isMoving = false;
    public bool isRunning = false;
    public bool canJump = false;

    private Rigidbody rb;
    private float moveHorizontal;
    private float moveVertical;
    private Vector3 currentVelocity = Vector3.zero;
    private Vector3 velocityChange = Vector3.zero;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.drag = 0f;
        rb.angularDrag = 0f;
    }

    void Update()
    {
        // 输入
        moveHorizontal = Input.GetAxis("Horizontal");
        moveVertical = Input.GetAxis("Vertical");
        isRunning = Input.GetKey(KeyCode.Q);
        isCrouching = Input.GetKey(KeyCode.LeftShift);

        // 地面检测
        isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f);
        canJump = isGrounded;

        // 跳跃
        if (Input.GetKeyDown(KeyCode.Space) && canJump && !isJumping)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            isJumping = true;
        }

        // 重置跳跃状态
        if (isJumping && rb.velocity.y <= 0 && isGrounded)
        {
            isJumping = false;
        }

        // 计算目标速度
        float currentSpeed = isRunning ? speed * 1.6f : speed;
        Vector3 targetVelocity = new Vector3(moveHorizontal * currentSpeed, 0, moveVertical * currentSpeed);

        // 平滑过渡
        currentVelocity = Vector3.SmoothDamp(currentVelocity, targetVelocity, ref velocityChange, smoothTime);
        isMoving = currentVelocity.magnitude > 0.1f;
    }

    void FixedUpdate()
    {
        // 应用水平和垂直速度，保持Y速度
        rb.velocity = new Vector3(currentVelocity.x, rb.velocity.y, currentVelocity.z);
    }
}
