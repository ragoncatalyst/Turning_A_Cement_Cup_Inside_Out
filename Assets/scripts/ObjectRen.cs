using UnityEngine;

public class ObjectRen : MonoBehaviour
{
    public enum BillboardMode { YOnly, FaceCamera }

    [Tooltip("The child transform that will be rotated to face the camera. If null, the GameObject's own transform is used.")]
    [SerializeField] private Transform renderRoot;
    [Tooltip("YOnly: only rotate around Y to face camera (upright). FaceCamera: fully rotate to face camera on all axes")]
    [SerializeField] private BillboardMode billboardMode = BillboardMode.YOnly;

    private void LateUpdate()
    {
        if (renderRoot == null) renderRoot = transform;
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform t = renderRoot;
        Vector3 dir = cam.transform.position - t.position;
        if (dir.sqrMagnitude < 1e-8f) return;

        if (billboardMode == BillboardMode.YOnly)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 1e-8f) return;
        }

        Quaternion worldRot = Quaternion.LookRotation(dir.normalized, Vector3.up);

        if (t.parent != null)
        {
            Quaternion parentRot = t.parent.rotation;
            t.localRotation = Quaternion.Inverse(parentRot) * worldRot;
        }
        else
        {
            t.rotation = worldRot;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Transform t = renderRoot != null ? renderRoot : transform;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(t.position, 0.1f);
    }
}