using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// DynamicSorting (XZ distance mode)
///
/// For each GameObject with this component the script will determine a world point
/// used for occlusion (prefer a child BoxCollider with isTrigger=true; otherwise use transform.position).
/// It then computes the squared distance on the XZ plane from that point to the main camera's XZ position.
/// A central manager sorts all registered instances by that XZ distance and assigns SpriteRenderer.sortingOrder
/// so that objects closer in XZ to the camera are rendered in front of ones farther away.
///
/// Notes:
/// - The script targets SpriteRenderer components. If none is present the component is ignored.
/// - The default behaviour updates every frame; you can change updateEveryFrame or updateInterval.
/// - The occlusion probe prefers a local BoxCollider with isTrigger=true (child or self). This follows the user's
///   convention of having an isTrigger box used for ground/contact checks.
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class DynamicSorting : MonoBehaviour
{
    [Header("Sorting Settings")]
    [SerializeField] private bool updateEveryFrame = true;
    [Tooltip("If not updating every frame, the manager will update at this interval (seconds)")]
    [SerializeField] private float updateInterval = 0.12f;
    [Tooltip("Base sorting order offset applied to all objects.")]
    [SerializeField] private int baseSortingOrder = 100;
    [SerializeField] private int minSortingOrder = -30000;
    [SerializeField] private int maxSortingOrder = 32767;
    [Tooltip("Small tie-breaker factor that nudges ordering when two objects have very similar XZ distance."
        + " It's multiplied by local X difference and added to the squared distance used for sorting.")]
    [SerializeField] private float xTieBreaker = 0.0001f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private SpriteRenderer spriteRenderer;
    private BoxCollider probeCollider; // first child/self BoxCollider with isTrigger == true

    // manager registration
    private static readonly List<DynamicSorting> s_instances = new List<DynamicSorting>();
    private static float s_lastUpdateTime = 0f;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[DynamicSorting] {name} 缺少 SpriteRenderer，已禁用。即将移除组件。");
            enabled = false;
            return;
        }

        // find a BoxCollider used as an isTrigger probe (self or children)
        var colliders = GetComponentsInChildren<BoxCollider>();
        foreach (var c in colliders)
        {
            if (c.isTrigger)
            {
                probeCollider = c;
                break;
            }
        }

        // register
        if (!s_instances.Contains(this)) s_instances.Add(this);
    }

    private void OnEnable()
    {
        if (!s_instances.Contains(this)) s_instances.Add(this);
    }

    private void OnDisable()
    {
        s_instances.Remove(this);
    }

    private void OnDestroy()
    {
        s_instances.Remove(this);
    }

    private void Start()
    {
        // initial update
        UpdateSortingOrder();
    }

    private void Update()
    {
        if (updateEveryFrame)
        {
            // call manager to update all
            DynamicSortingManager_UpdateAllIfNeeded(updateInterval, true);
        }
        else
        {
            DynamicSortingManager_UpdateAllIfNeeded(updateInterval, false);
        }
    }

    /// <summary>
    /// Return the world point used as occlusion probe (center of isTrigger BoxCollider if present, otherwise transform.position)
    /// </summary>
    public Vector3 GetOcclusionPointWorld()
    {
        if (probeCollider != null)
        {
            return probeCollider.transform.TransformPoint(probeCollider.center);
        }
        return transform.position;
    }

    /// <summary>
    /// Compute the effective XZ squared distance to camera, including a tiny X-based tie breaker.
    /// </summary>
    private float ComputeXZDistanceSqrToCamera()
    {
        var cam = Camera.main;
        if (cam == null) return float.MaxValue;
        Vector3 camPos = cam.transform.position;
        Vector3 probe = GetOcclusionPointWorld();
        float dx = camPos.x - probe.x;
        float dz = camPos.z - probe.z;
        float dist2 = dx * dx + dz * dz;
        // tie-breaker: slightly bias by local x difference
        dist2 += Mathf.Abs(dx) * xTieBreaker;
        return dist2;
    }

    /// <summary>
    /// Force this instance to update its sortingOrder based on the globally computed ranking.
    /// (Prefer using the manager's batch update instead of calling this frequently per-object.)
    /// </summary>
    public void UpdateSortingOrder()
    {
        // noop: actual ordering assigned by manager. Provide this method so external callers can request a batch update.
        DynamicSortingManager_UpdateAllNow();
    }

    // ---------- Static manager helpers ----------

    private static void DynamicSortingManager_UpdateAllIfNeeded(float interval, bool immediateWhenEveryFrame)
    {
        float now = Time.time;
        if (immediateWhenEveryFrame)
        {
            DynamicSortingManager_UpdateAllNow();
            s_lastUpdateTime = now;
            return;
        }

        if (now - s_lastUpdateTime >= interval)
        {
            DynamicSortingManager_UpdateAllNow();
            s_lastUpdateTime = now;
        }
    }

    private static void DynamicSortingManager_UpdateAllNow()
    {
        var cam = Camera.main;
        if (cam == null) return;

        // build list of valid instances with distances
        List<(float dist, DynamicSorting ds)> list = new List<(float, DynamicSorting)>();
        foreach (var ds in s_instances)
        {
            if (ds == null || !ds.enabled || ds.spriteRenderer == null) continue;
            float d2 = ds.ComputeXZDistanceSqrToCamera();
            list.Add((d2, ds));
        }

        // sort ascending by distance (closest first)
        list.Sort((a, b) => a.dist.CompareTo(b.dist));

        int total = list.Count;
        for (int i = 0; i < total; ++i)
        {
            var ds = list[i].ds;
            // assign sorting order so that closest objects have larger sortingOrder (rendered on top)
            // e.g. closest gets baseSortingOrder + total, farthest gets baseSortingOrder
            int order = ds.baseSortingOrder + (total - 1 - i);
            order = Mathf.Clamp(order, ds.minSortingOrder, ds.maxSortingOrder);
            if (ds.spriteRenderer.sortingOrder != order)
            {
                ds.spriteRenderer.sortingOrder = order;
                if (ds.enableDebugLogs) Debug.Log($"[DynamicSorting] {ds.name} assigned sort {order} (rank {i}/{total})");
            }
        }
    }
}
