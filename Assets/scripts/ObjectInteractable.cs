using System.Collections;
using UnityEngine;
using TMPro;

public class ObjectInteractable : MonoBehaviour
{
    [SerializeField] private bool isInteractable = true;
    [SerializeField] private TMP_Text textBox;
    [TextArea] [SerializeField] private string displayText = "";

    [Header("Visual Pop Animation (does not affect collider)")]
    [Tooltip("The transform to scale for the pop/stretch animation. If null, the script will try to find a child Renderer/SpriteRenderer and use its transform. If none found, the object's own transform will be used (this may affect colliders).")]
    [SerializeField] private Transform visualRoot;
    [Tooltip("Maximum horizontal stretch multiplier during squash phase (applied to local X scale). e.g. 1.2 = 20% wider")]
    [SerializeField] private float maxStretchX = 1.2f;
    [Tooltip("Maximum vertical stretch multiplier during stretch phase (applied to local Y scale). e.g. 1.15 = 15% taller")]
    [SerializeField] private float maxStretchY = 1.15f;
    [Header("Phase Durations")]
    [Tooltip("Duration in seconds for phase 1 (squash)")]
    [SerializeField] private float phase1Duration = 0.08f;
    [Tooltip("Duration in seconds for phase 2 (stretch)")]
    [SerializeField] private float phase2Duration = 0.12f;
    [Tooltip("Duration in seconds for phase 3 (restore)")]
    [SerializeField] private float phase3Duration = 0.12f;

    [Header("Curves & Small Shrink")]
    [Tooltip("Curve that controls animation speed/ease for phase 1 (squash)")]
    [SerializeField] private AnimationCurve phase1Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Tooltip("Curve that controls animation speed/ease for phase 2 (stretch)")]
    [SerializeField] private AnimationCurve phase2Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Tooltip("Curve that controls animation speed/ease for phase 3 (restore)")]
    [SerializeField] private AnimationCurve phase3Curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
    [Tooltip("Horizontal slight shrink multiplier applied during stretch phase (e.g. 0.96 = 4% narrower)")]
    [SerializeField] private float smallShrinkX = 0.96f;
    [Tooltip("Vertical slight shrink multiplier applied during squash phase if needed (not typically used)")]
    [SerializeField] private float smallShrinkY = 0.98f;

    // internal
    private Coroutine popCoroutine = null;
    private Vector3 visualOriginalLocalScale;

    // cached renderer reference for bottom calculations
    private Renderer visualRenderer;

    public bool IsInteractable => isInteractable;

    // expose textBox for callers that need to coordinate shared TMP usage
    public TMP_Text TextBox => textBox;

    private void Start()
    {
        // Ensure dynamic objects use continuous collision detection and interpolation
        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // If visualRoot not set, try to find a child with a renderer to use for visual-only scaling
        if (visualRoot == null)
        {
            foreach (Transform child in transform)
            {
                if (child.GetComponent<Renderer>() != null || child.GetComponent<SpriteRenderer>() != null)
                {
                    visualRoot = child;
                    break;
                }
            }
        }

        if (visualRoot == null) visualRoot = transform;
        visualOriginalLocalScale = visualRoot.localScale;

        // cache renderer if present
        visualRenderer = visualRoot.GetComponentInChildren<Renderer>();
    }

    public void Interact(GameObject interactor)
    {
        if (!isInteractable) return;

        if (textBox != null)
        {
            textBox.text = displayText;
            textBox.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogFormat("{0} was interacted by {1}, but no textBox assigned.", name, interactor.name);
        }

        // trigger visual pop/stretch animation (non-physics visual only)
        if (popCoroutine != null) StopCoroutine(popCoroutine);
        popCoroutine = StartCoroutine(PopAnimation());
    }

    IEnumerator PopAnimation()
    {
        if (visualRoot == null) yield break;

        Vector3 start = visualOriginalLocalScale;

        // determine anchor bottom world Y so we can keep it fixed during vertical scaling
        float anchorBottomY = GetVisualBottomY();

        // Phase 1: squash (X up, Y down)
        float t = 0f;
        Vector3 targetPhase1 = new Vector3(start.x * maxStretchX, start.y * smallShrinkY, start.z);
        while (t < phase1Duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / phase1Duration);
            float eased = phase1Curve.Evaluate(f);
            visualRoot.localScale = Vector3.Lerp(start, targetPhase1, eased);
            KeepBottomAnchored(anchorBottomY);
            yield return null;
        }

        // Phase 2: stretch (X slightly down, Y up)
        t = 0f;
        Vector3 targetPhase2 = new Vector3(start.x * smallShrinkX, start.y * maxStretchY, start.z);
        Vector3 fromPhase2Start = visualRoot.localScale;
        while (t < phase2Duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / phase2Duration);
            float eased = phase2Curve.Evaluate(f);
            visualRoot.localScale = Vector3.Lerp(fromPhase2Start, targetPhase2, eased);
            KeepBottomAnchored(anchorBottomY);
            yield return null;
        }

        // Phase 3: restore to original
        t = 0f;
        Vector3 fromPhase3Start = visualRoot.localScale;
        while (t < phase3Duration)
        {
            t += Time.deltaTime;
            float f = Mathf.Clamp01(t / phase3Duration);
            float eased = phase3Curve.Evaluate(f);
            visualRoot.localScale = Vector3.Lerp(fromPhase3Start, start, eased);
            KeepBottomAnchored(anchorBottomY);
            yield return null;
        }

        visualRoot.localScale = start;
        KeepBottomAnchored(anchorBottomY);
        popCoroutine = null;
    }

    private float GetVisualBottomY()
    {
        if (visualRenderer != null)
        {
            return visualRenderer.bounds.min.y;
        }
        var sr = visualRoot.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            return sr.bounds.min.y;
        }
        // fallback: use visualRoot world position
        return visualRoot.position.y;
    }

    private void KeepBottomAnchored(float anchorBottomY)
    {
        float currentBottom = GetVisualBottomY();
        float delta = anchorBottomY - currentBottom;
        if (Mathf.Abs(delta) > 0.0001f)
        {
            visualRoot.position += new Vector3(0f, delta, 0f);
        }
    }

    /// <summary>
    /// Hide the associated text box (if any).
    /// </summary>
    public void Hide()
    {
        if (textBox != null && textBox.gameObject.activeSelf)
        {
            textBox.gameObject.SetActive(false);
        }
    }
}