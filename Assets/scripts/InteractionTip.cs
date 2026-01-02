using System.Reflection;
using UnityEngine;

[DisallowMultipleComponent]
public class InteractionTip : MonoBehaviour
{
    [Header("Tip Sprites")]
    [Tooltip("Sprite (GameObject) that appears when any interactable is in camera view.")]
    [SerializeField] private GameObject viewTipSprite;

    [Tooltip("Sprite (GameObject) that appears when any interactable is within interaction range. This has higher priority than view-tip.")]
    [SerializeField] private GameObject rangeTipSprite;

    [Header("Detection (optional, will try to read from PlayerInteraction if present)")]
    [Tooltip("Interaction range (meters) used for near detection. If left <= 0 a sensible default will be used. If a PlayerInteraction component exists on the same GameObject, this value will be read from it automatically.")]
    [SerializeField] private float interactRange = 0f;

    [Tooltip("Layer mask used for overlap checks. If left empty (Nothing) all layers will be considered when scanning scene objects.")]
    [SerializeField] private LayerMask interactableLayer = ~0;

    [Tooltip("Origin used for range checks. If null, this GameObject's transform will be used.")]
    [SerializeField] private Transform interactionOrigin;

    // optional link to player interaction to sync settings
    private Component playerInteractionComp;
    private FieldInfo fi_range;
    private FieldInfo fi_layer;
    private FieldInfo fi_origin;

    void Start()
    {
        // ensure sprites start hidden
        if (viewTipSprite != null) viewTipSprite.SetActive(false);
        if (rangeTipSprite != null) rangeTipSprite.SetActive(false);

        if (interactionOrigin == null) interactionOrigin = transform;

        // try to find PlayerInteraction on same object and bind to its serialized fields via reflection
        playerInteractionComp = GetComponent("PlayerInteraction");
        if (playerInteractionComp != null)
        {
            var t = playerInteractionComp.GetType();
            fi_range = t.GetField("interactRange", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            fi_layer = t.GetField("interactableLayer", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);
            fi_origin = t.GetField("interactionOrigin", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public);

            // If InteractionTip's inspector value is not set (<=0), try to read the player's value as a sensible default.
            if (interactRange <= 0f && fi_range != null)
            {
                try
                {
                    object v = fi_range.GetValue(playerInteractionComp);
                    if (v is float && (float)v > 0f) interactRange = (float)v;
                }
                catch { }
            }
        }

        // If still not set, apply local default
        if (interactRange <= 0f) interactRange = 2f;
    }

    void Update()
    {
        // sync settings from PlayerInteraction if present
        if (playerInteractionComp != null)
        {
            try
            {
                // Only copy the player's interactRange if this InteractionTip has not been configured in the Inspector (<=0)
                if (fi_range != null && interactRange <= 0f)
                {
                    object v = fi_range.GetValue(playerInteractionComp);
                    if (v is float && (float)v > 0f) interactRange = (float)v;
                }
                if (fi_layer != null)
                {
                    object v = fi_layer.GetValue(playerInteractionComp);
                    if (v is LayerMask) interactableLayer = (LayerMask)v;
                }
                if (fi_origin != null)
                {
                    object v = fi_origin.GetValue(playerInteractionComp);
                    if (v is Transform && v != null) interactionOrigin = (Transform)v;
                }
            }
            catch { /* swallow reflection exceptions */ }
        }

        bool anyInView = false;
        bool anyInRange = false;

        Camera cam = Camera.main;
        Plane[] planes = null;
        if (cam != null) planes = GeometryUtility.CalculateFrustumPlanes(cam);

        // Find all interactables in scene and test. This is simple and robust for small scenes.
        var all = GameObject.FindObjectsOfType<ObjectInteractable>();
        if (all != null && all.Length > 0)
        {
            foreach (var oi in all)
            {
                if (oi == null) continue;
                if (!oi.IsInteractable) continue;

                // get bounds (collider preferred)
                Bounds b = new Bounds(oi.transform.position, Vector3.zero);
                var col = oi.GetComponent<Collider>();
                if (col != null) b = col.bounds;
                else
                {
                    var rend = oi.GetComponent<Renderer>();
                    if (rend != null) b = rend.bounds;
                    else b = new Bounds(oi.transform.position, Vector3.one * 0.1f);
                }

                // in-range check (distance from origin to closest point on bounds)
                Vector3 originPos = interactionOrigin != null ? interactionOrigin.position : transform.position;
                float sqDist = (b.ClosestPoint(originPos) - originPos).sqrMagnitude;
                if (sqDist <= (interactRange * interactRange))
                {
                    anyInRange = true;
                }

                // in-view check using camera frustum + simple occlusion bypass (frustum only)
                if (!anyInView && planes != null)
                {
                    if (GeometryUtility.TestPlanesAABB(planes, b))
                    {
                        anyInView = true;
                    }
                }

                // early exit if high-priority condition met
                if (anyInRange && anyInView) break;
            }
        }

        // priority: near (rangeTip) over view (viewTip)
        if (anyInRange)
        {
            if (rangeTipSprite != null && !rangeTipSprite.activeSelf) rangeTipSprite.SetActive(true);
            if (viewTipSprite != null && viewTipSprite.activeSelf) viewTipSprite.SetActive(false);
        }
        else if (anyInView)
        {
            if (viewTipSprite != null && !viewTipSprite.activeSelf) viewTipSprite.SetActive(true);
            if (rangeTipSprite != null && rangeTipSprite.activeSelf) rangeTipSprite.SetActive(false);
        }
        else
        {
            if (viewTipSprite != null && viewTipSprite.activeSelf) viewTipSprite.SetActive(false);
            if (rangeTipSprite != null && rangeTipSprite.activeSelf) rangeTipSprite.SetActive(false);
        }
    }
}
