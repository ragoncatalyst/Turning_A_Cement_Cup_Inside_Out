using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private float interactRange = 0f; // serialized; do NOT write to this at runtime
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private Transform interactionOrigin;
    [Tooltip("Seconds before an interacted text automatically hides if still shown")]
    [SerializeField] private float textAutoHideSeconds = 5f;

    [Tooltip("Seconds to wait after the player leaves interaction range before hiding the text")]
    [SerializeField] private float leaveHideSeconds = 1.8f;

    // track currently shown interactables and their show/leave timestamps
    private class ShownInfo { public float shownAt; public float leftAt = -1f; }
    private Dictionary<ObjectInteractable, ShownInfo> shownInteractables = new Dictionary<ObjectInteractable, ShownInfo>();

    private ObjectInteractable currentTarget;

    private void Update()
    {
        UpdateCurrentTarget();
        // hide interactables if timed out or out of range
        CleanupShownInteractables();

        if (Input.GetKeyDown(KeyCode.F))
        {
            InteractWithNearby();
        }
    }

    private void Awake()
    {
        // do NOT assign to the serialized `interactRange` here — respect Inspector values.
        if (interactionOrigin == null) interactionOrigin = transform;
    }

    // Use this read-only property for runtime logic. It prefers the serialized value when > 0,
    // otherwise falls back to a sensible default.
    public float EffectiveInteractRange => (interactRange > 0f) ? interactRange : 2f;

    // When F is pressed, gather all colliders in range and call Interact() on every
    // ObjectInteractable that reports IsInteractable == true. Use a HashSet to avoid
    // duplicate calls when an object has multiple colliders.
    private void InteractWithNearby()
    {
        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, EffectiveInteractRange, interactableLayer);

        if (hits == null || hits.Length == 0) return;

        var called = new HashSet<ObjectInteractable>();
        foreach (var c in hits)
        {
            if (c == null) continue;
            var oi = c.GetComponent<ObjectInteractable>();
            if (oi == null) continue;
            if (!oi.IsInteractable) continue;
            if (called.Contains(oi)) continue;
            try
            {
                oi.Interact(gameObject);
            }
            catch (System.Exception ex)
            {
                Debug.LogException(ex, oi as Object);
            }
            called.Add(oi);
            // record shown time so we can auto-hide later; reset leave timer
            shownInteractables[oi] = new ShownInfo { shownAt = Time.time, leftAt = -1f };
        }
    }

    private void CleanupShownInteractables()
    {
        if (shownInteractables.Count == 0) return;

        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, EffectiveInteractRange, interactableLayer);
        var inRange = new HashSet<ObjectInteractable>();
        if (hits != null)
        {
            foreach (var c in hits)
            {
                if (c == null) continue;
                var oi = c.GetComponent<ObjectInteractable>();
                if (oi != null) inRange.Add(oi);
            }
        }

        // collect keys to remove to avoid modifying dictionary during iteration
        var toRemove = new List<ObjectInteractable>();
        foreach (var kv in shownInteractables)
        {
            var oi = kv.Key;
            var info = kv.Value;
            bool timedOut = (Time.time - info.shownAt) >= textAutoHideSeconds;

            if (inRange.Contains(oi))
            {
                // still in range -> reset leave timer
                info.leftAt = -1f;
            }
            else
            {
                // left range: start leave timer if not already started
                if (info.leftAt < 0f) info.leftAt = Time.time;
            }

            bool outOfRangeExpired = (info.leftAt >= 0f) && ((Time.time - info.leftAt) >= leaveHideSeconds);

            if (timedOut || outOfRangeExpired)
            {
                bool shouldHide = true;

                // If this ObjectInteractable uses a TMP that is shared by other shown interactables,
                // check whether any other shown interactable referencing the same TMP is still active
                // (i.e. not timed out and not out-of-range-expired). If so, do not hide the TMP now.
                var tb = oi != null ? oi.TextBox : null;
                if (tb != null)
                {
                    foreach (var otherKv in shownInteractables)
                    {
                        var otherOi = otherKv.Key;
                        if (otherOi == null || otherOi == oi) continue;
                        if (otherOi.TextBox != tb) continue;

                        var otherInfo = otherKv.Value;
                        bool otherTimedOut = (Time.time - otherInfo.shownAt) >= textAutoHideSeconds;
                        bool otherOutExpired = (otherInfo.leftAt >= 0f) && ((Time.time - otherInfo.leftAt) >= leaveHideSeconds);
                        if (!otherTimedOut && !otherOutExpired)
                        {
                            shouldHide = false;
                            break;
                        }
                    }
                }

                if (shouldHide)
                {
                    if (oi != null) oi.Hide();
                    toRemove.Add(oi);
                }
                else
                {
                    // If not hiding because another interactable keeps the TMP alive, still remove this entry
                    // so it doesn't later attempt to hide the shared TMP again. (The other live entry will manage hiding.)
                    toRemove.Add(oi);
                }
            }
        }

        foreach (var oi in toRemove)
        {
            shownInteractables.Remove(oi);
        }
    }

    private void UpdateCurrentTarget()
    {
        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Collider[] hits = Physics.OverlapSphere(origin, EffectiveInteractRange, interactableLayer);

        ObjectInteractable best = null;
        float bestDist = float.PositiveInfinity;

        foreach (var c in hits)
        {
            if (c == null) continue;
            var oi = c.GetComponent<ObjectInteractable>();
            if (oi != null && oi.IsInteractable)
            {
                float d = (c.transform.position - origin).sqrMagnitude;
                if (d < bestDist)
                {
                    best = oi;
                    bestDist = d;
                }
            }
        }

        currentTarget = best;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 origin = interactionOrigin != null ? interactionOrigin.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(origin, EffectiveInteractRange);
    }
}