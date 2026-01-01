using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMov : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f;
    public float jumpForce = 5f;

    [Header("State")]
    public bool isCrouching = false;
    [Tooltip("按住 Q 为跑步")]
    public bool isRunning = false;

    [Header("Runtime Status (Debug)")]
    [SerializeField] private bool debugIsGrounded = false;
    [SerializeField] private bool debugShortJumpBuffered = false;
    [SerializeField] private bool debugLongPressBuffered = false;
    [SerializeField] private float debugSpaceHoldTime = 0f;
    [SerializeField] private float debugJumpBufferRemaining = 0f;
    [SerializeField] private float debugLastJumpTime = -1f;
    [SerializeField] private Vector3 debugLastDesiredHorizontal = Vector3.zero;
    [SerializeField] private int debugWaitlistCount = 0;
    [SerializeField] private int debugGroundContacts = 0;

    private Rigidbody rb;
    private int groundContacts = 0;
    private bool canJump { get { return groundContacts > 0; } }

    [Header("Ground Detection")]
    [Tooltip("Tag used to identify ground objects for jump/landing detection.")]
    public string groundTag = "Ground";

    // Jump waitlist
    private class JumpRequest { public bool isLongPress; public float expireAt; }
    private List<JumpRequest> waitlist = new List<JumpRequest>();
    private JumpRequest activeLongPressRequest = null; // reference created while holding Space
    private float spaceHoldTime = 0f;

    // smoothing
    private Vector3 lastDesiredHorizontal = Vector3.zero;
    private Coroutine smoothCoroutine = null;
    private float lastJumpTime = -1f;
    // Physics-tick desired velocity applied in FixedUpdate to avoid Update/physics mismatch
    private Vector3 fixedDesiredHorizontal = Vector3.zero;
    private bool jumpRequested = false;
    private float pendingJumpForce = 0f;
    private bool wasGroundedPrev = false;
    // Prevent multiple consumptions on a single landing (OnTriggerEnter + frame-guard race)
    private bool consumedOnThisLanding = false;
    // Minimum time between applied jump impulses to avoid double-impulse glitches
    [Header("Jump Timing")]
    [Tooltip("Ignore subsequent jump impulses that occur within this many seconds of the previous jump.")]
    public float minJumpSpacing = 0.08f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        // Movement states
        isCrouching = Input.GetKey(KeyCode.LeftShift);
        isRunning = Input.GetKey(KeyCode.Q);

        // Camera-relative basis
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;
        cameraForward.y = 0f; cameraRight.y = 0f;
        cameraForward.Normalize(); cameraRight.Normalize();

        // Read raw WASD keys so we can implement the strict axis-aligned diagonal rules
        bool keyW = Input.GetKey(KeyCode.W);
        bool keyA = Input.GetKey(KeyCode.A);
        bool keyS = Input.GetKey(KeyCode.S);
        bool keyD = Input.GetKey(KeyCode.D);
        float speedMultiplier = isCrouching ? 0.6f : (isRunning ? 1.5f : 1f);

        // Priority: when exactly the specified adjacent two-key combos are held, move along world axes
        // WA -> X- ; SD -> X+ ; WD -> Z+ ; AS -> Z-
        if (keyW && keyA && !keyS && !keyD)
        {
            float v = speed * speedMultiplier;
            lastDesiredHorizontal = new Vector3(-v, 0f, 0f);
            MovXNeg();
        }
        else if (keyS && keyD && !keyW && !keyA)
        {
            float v = speed * speedMultiplier;
            lastDesiredHorizontal = new Vector3(v, 0f, 0f);
            MovXPos();
        }
        else if (keyW && keyD && !keyA && !keyS)
        {
            float v = speed * speedMultiplier;
            lastDesiredHorizontal = new Vector3(0f, 0f, v);
            MovZPos();
        }
        else if (keyA && keyS && !keyW && !keyD)
        {
            float v = speed * speedMultiplier;
            lastDesiredHorizontal = new Vector3(0f, 0f, -v);
            MovZNeg();
        }
        else
        {
            // Default: camera-relative movement using input axes (preserves original behaviour)
            float mvX = Input.GetAxis("Horizontal");
            float mvZ = Input.GetAxis("Vertical");

            Vector3 desired = cameraRight * mvX + cameraForward * mvZ;
            if (desired.sqrMagnitude > 1f) desired.Normalize();
            desired *= speed * speedMultiplier;
            lastDesiredHorizontal = desired;

            // store desired horizontal velocity for FixedUpdate application
            fixedDesiredHorizontal = desired;
        }

        // -------- Jump input handling (strict waitlist rules) --------
        // KeyDown: immediate jump if grounded; otherwise add a short buffered request (0.15s) if none exists
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (canJump)
            {
                DoJump();
            }
            else
            {
                bool shortExists = waitlist.Exists(r => !r.isLongPress);
                if (!shortExists)
                {
                    waitlist.Add(new JumpRequest { isLongPress = false, expireAt = Time.time + 0.15f });
                }
            }
        }

        // Hold handling: accumulate hold time and add a persistent long-press request once >= 0.5s
        if (Input.GetKey(KeyCode.Space))
        {
            spaceHoldTime += Time.deltaTime;
            if (spaceHoldTime >= 0.5f && activeLongPressRequest == null)
            {
                var jr = new JumpRequest { isLongPress = true, expireAt = float.PositiveInfinity };
                waitlist.Add(jr);
                activeLongPressRequest = jr;
                // If currently grounded when long press becomes active, trigger immediately
                if (canJump) TryConsumeWaitlistOnLanding();
            }
        }
        else
        {
            // On release: remove the long-press request created by this hold (if any)
            if (activeLongPressRequest != null)
            {
                waitlist.Remove(activeLongPressRequest);
                activeLongPressRequest = null;
            }
            spaceHoldTime = 0f;
        }

        // Remove expired short requests
        CleanExpiredShortRequests();

        // Frame-based landing guard: if we just transitioned false->true, consume waitlist
        bool groundedNow = canJump;
        if (!wasGroundedPrev && groundedNow)
        {
            TryConsumeWaitlistOnLanding();
        }
        wasGroundedPrev = groundedNow;

        // Debug fields
        debugIsGrounded = canJump;
        debugShortJumpBuffered = waitlist.Exists(r => !r.isLongPress);
        debugLongPressBuffered = (activeLongPressRequest != null);
        debugSpaceHoldTime = spaceHoldTime;
        float soonest = float.PositiveInfinity;
        foreach (var r in waitlist) if (!r.isLongPress) soonest = Mathf.Min(soonest, r.expireAt);
        debugJumpBufferRemaining = (soonest < float.PositiveInfinity) ? Mathf.Max(0f, soonest - Time.time) : 0f;
        debugWaitlistCount = waitlist.Count;
        debugLastJumpTime = lastJumpTime;
        debugLastDesiredHorizontal = lastDesiredHorizontal;
        debugGroundContacts = groundContacts;
    }

    private void DoJump()
    {
        // guard: avoid applying a second impulse too quickly after a previous jump
        if (lastJumpTime > 0f && Time.time - lastJumpTime < minJumpSpacing) return;
        // Request a jump to be executed in FixedUpdate (physics tick)
        pendingJumpForce = jumpForce;
        jumpRequested = true;
        lastJumpTime = Time.time;
        if (smoothCoroutine != null) StopCoroutine(smoothCoroutine);
        smoothCoroutine = StartCoroutine(SmoothHorizontalTo(lastDesiredHorizontal, 0.18f));
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        // Apply horizontal desired velocity on physics step
        rb.velocity = new Vector3(fixedDesiredHorizontal.x, rb.velocity.y, fixedDesiredHorizontal.z);

        // Execute pending jump impulses on physics step
        if (jumpRequested)
        {
            // normalize vertical velocity before applying impulse
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * pendingJumpForce, ForceMode.Impulse);
            jumpRequested = false;
            pendingJumpForce = 0f;
        }
    }

    private void CleanExpiredShortRequests()
    {
        if (waitlist.Count == 0) return;
        waitlist.RemoveAll(r => !r.isLongPress && r.expireAt <= Time.time);
    }

    // Axis-aligned helper methods required by the project
    // These respect run / crouch multipliers so behaviour matches single-key movement
    // They now write into `fixedDesiredHorizontal` so movement is applied in FixedUpdate
    void MovXNeg() { float v = speed * (isCrouching ? 0.6f : (isRunning ? 1.5f : 1f)); fixedDesiredHorizontal = new Vector3(-v, 0f, 0f); lastDesiredHorizontal = fixedDesiredHorizontal; }
    void MovXPos() { float v = speed * (isCrouching ? 0.6f : (isRunning ? 1.5f : 1f)); fixedDesiredHorizontal = new Vector3(v, 0f, 0f); lastDesiredHorizontal = fixedDesiredHorizontal; }
    void MovZPos() { float v = speed * (isCrouching ? 0.6f : (isRunning ? 1.5f : 1f)); fixedDesiredHorizontal = new Vector3(0f, 0f, v); lastDesiredHorizontal = fixedDesiredHorizontal; }
    void MovZNeg() { float v = speed * (isCrouching ? 0.6f : (isRunning ? 1.5f : 1f)); fixedDesiredHorizontal = new Vector3(0f, 0f, -v); lastDesiredHorizontal = fixedDesiredHorizontal; }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(groundTag)) return;
        bool wasZero = groundContacts == 0;
        groundContacts++;
        if (wasZero)
        {
            // first contact for this landing
            consumedOnThisLanding = false;
            TryConsumeWaitlistOnLanding();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(groundTag)) return;
        groundContacts = Mathf.Max(0, groundContacts - 1);
        if (groundContacts == 0) consumedOnThisLanding = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        // treat non-trigger collisions with ground as landing
        var other = collision.collider;
        if (!other.CompareTag(groundTag)) return;
        bool wasZero = groundContacts == 0;
        groundContacts++;
        if (wasZero)
        {
            consumedOnThisLanding = false;
            TryConsumeWaitlistOnLanding();
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        var other = collision.collider;
        if (!other.CompareTag(groundTag)) return;
        groundContacts = Mathf.Max(0, groundContacts - 1);
        if (groundContacts == 0) consumedOnThisLanding = false;
    }

    // Landing consumer: clears expired short requests, then consumes head of queue
    private void TryConsumeWaitlistOnLanding()
    {
        if (consumedOnThisLanding) return;
        CleanExpiredShortRequests();
        if (waitlist.Count == 0) return;

        // Process queue head: short -> consume and remove; long -> trigger but keep until release
        var req = waitlist[0];
        if (!req.isLongPress)
        {
            if (req.expireAt > Time.time)
            {
                DoJump();
                waitlist.RemoveAt(0);
                consumedOnThisLanding = true;
            }
            else waitlist.RemoveAt(0);
        }
        else
        {
            DoJump();
            consumedOnThisLanding = true;
        }
    }

    private IEnumerator SmoothHorizontalTo(Vector3 targetHorizontal, float duration)
    {
        float t = 0f;
        Vector3 start = new Vector3(fixedDesiredHorizontal.x, 0f, fixedDesiredHorizontal.z);
        Vector3 end = new Vector3(targetHorizontal.x, 0f, targetHorizontal.z);
        while (t < duration)
        {
            t += Time.deltaTime;
            float f = Mathf.SmoothStep(0f, 1f, t / duration);
            Vector3 cur = Vector3.Lerp(start, end, f);
            fixedDesiredHorizontal = cur;
            yield return null;
        }
        fixedDesiredHorizontal = end;
        smoothCoroutine = null;
    }
}