using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ObjectCollider : MonoBehaviour
{
    [Tooltip("If true, attempt to ensure this object becomes a kinematic Rigidbody once it has landed on objects tagged as `groundTag`.")]
    public bool enforceKinematicSelf = true;

    [Tooltip("Tag used to identify ground objects.")]
    public string groundTag = "Ground";

    [Tooltip("When enforcing kinematic self, set CollisionDetectionMode for the added Rigidbody (Continuous recommended).")]
    public CollisionDetectionMode collisionModeToSet = CollisionDetectionMode.Continuous;

    [Tooltip("When enforcing kinematic self, set Rigidbody interpolation mode (recommended: Interpolate).")]
    public RigidbodyInterpolation interpolationModeToSet = RigidbodyInterpolation.Interpolate;

    // internal state
    private Rigidbody rb;
    private bool kinematicSet = false;

    void Awake()
    {
        if (!enforceKinematicSelf) return;

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        // If this object itself is tagged as ground, immediately make it kinematic
        if (CompareTag(groundTag))
        {
            SetKinematicNow();
            return;
        }

        // Otherwise, allow it to free-fall until it collides with an object tagged as ground.
        // Ensure it's dynamic for falling.
        rb.isKinematic = false;
        rb.useGravity = true;
        // use a safer collision detection while falling
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.interpolation = interpolationModeToSet;
    }

    void SetKinematicNow()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;
        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = collisionModeToSet;
        rb.interpolation = interpolationModeToSet;
        kinematicSet = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!enforceKinematicSelf || kinematicSet) return;
        if (other.CompareTag(groundTag))
        {
            SetKinematicNow();
        }
    }

    void OnCollisionEnter(Collision col)
    {
        if (!enforceKinematicSelf || kinematicSet) return;
        if (col.collider != null && col.collider.CompareTag(groundTag))
        {
            SetKinematicNow();
        }
    }
}
