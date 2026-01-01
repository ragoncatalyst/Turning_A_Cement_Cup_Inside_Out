using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// EasyShader: a simple blob-shadow implementation.
/// Attach to `Player_Lullaby`. It will create a soft circular quad under the player
/// and keep it stuck to the nearest ground below the player (so it doesn't fly up with jumps).
/// The quad can align to the ground normal (so it lies on stairs) and fades slightly with height.
/// </summary>
[ExecuteAlways]
public class EasyShader : MonoBehaviour
{
    [Header("Shadow Settings")]
    public float radius = 0.25f;
    [Tooltip("During jump, shadow radius will shrink toward this value at apex")]
    public float RadiusJumpMax = 0.08f;
    [Tooltip("Alpha value at jump apex (0 = invisible, 1 = unchanged)")]
    public float AlphaJumpMax = 0.08f;
    [Tooltip("Base color (alpha controls darkness)")]
    public Color color = new Color(0f, 0f, 0f, 0.6f);
    [Range(0f, 1f)]
    public float softness = 0.6f; // 0 = hard edge, 1 = very soft
    public float groundOffset = 0.02f; // lift above ground to avoid z-fighting
    public LayerMask groundLayers = ~0;
    public float maxCastDistance = 10f;

    [Header("Height-based fading")]
    [Tooltip("Distance above ground where fading starts")]
    public float fadeStart = 0.5f;
    [Tooltip("Distance above ground where shadow is mostly faded")]
    public float fadeEnd = 3f;

    [Header("Jump Behaviour")]
    [Tooltip("How quickly the shadow restores to original size/alpha after landing")]
    public float restoreSpeed = 8f;
    [Tooltip("Minimum height above recorded ground to be considered airborne")]
    public float airborneThreshold = 0.05f;

    [Header("Texture settings")]
    public int textureSize = 128;

    // runtime objects
    GameObject shadowQuad;
    Material shadowMaterial;
    Texture2D shadowTexture;

    // stored originals and runtime blended values
    float originalRadius;
    float currentRadius;
    float originalAlpha;
    float currentAlpha;

    // keep last valid ground position when ray misses
    Vector3 lastGroundPos;
    Quaternion lastGroundRot = Quaternion.identity;
    float lastGroundY = float.NaN;

    // airborne tracking
    bool wasGroundedPrev = true;
    bool isAirborne = false;
    float airApexY = 0f; // highest player Y observed during current airborne
    float airStartGroundY = 0f;

    void OnEnable()
    {
        CreateShadowQuad();
        // initialize originals
        originalRadius = radius;
        currentRadius = radius;
        originalAlpha = color.a;
        currentAlpha = color.a;
    }

    void OnDisable()
    {
        if (shadowQuad != null)
        {
            if (Application.isPlaying) Destroy(shadowQuad);
            else DestroyImmediate(shadowQuad);
        }
        if (shadowTexture != null)
        {
            if (Application.isPlaying) Destroy(shadowTexture);
            else DestroyImmediate(shadowTexture);
        }
        if (shadowMaterial != null)
        {
            if (Application.isPlaying) Destroy(shadowMaterial);
            else DestroyImmediate(shadowMaterial);
        }
    }

    void CreateShadowQuad()
    {
        if (shadowQuad != null) return;

        // create texture
        shadowTexture = GenerateRadialTexture(textureSize, softness);

        // create material using a simple transparent/unlit shader
        Shader s = Shader.Find("Sprites/Default");
        shadowMaterial = new Material(s);
        shadowMaterial.mainTexture = shadowTexture;
        shadowMaterial.color = color;
        // Ensure it renders after geometry
        shadowMaterial.renderQueue = 3000;

        // quad
        shadowQuad = new GameObject("EasyShadowQuad");
        shadowQuad.hideFlags = HideFlags.DontSaveInBuild | HideFlags.DontSaveInEditor;
        shadowQuad.transform.SetParent(null, true);

        MeshFilter mf = shadowQuad.AddComponent<MeshFilter>();
        mf.sharedMesh = BuildQuadMesh();

        MeshRenderer mr = shadowQuad.AddComponent<MeshRenderer>();
        mr.sharedMaterial = shadowMaterial;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        // initial transform
        shadowQuad.transform.localScale = Vector3.one * (radius * 2f);
    }

    void Update()
    {
        if (shadowQuad == null) CreateShadowQuad();

        Vector3 origin = transform.position + Vector3.up * 0.1f;
        RaycastHit hit;
        bool hitGround = Physics.Raycast(origin, Vector3.down, out hit, maxCastDistance, groundLayers, QueryTriggerInteraction.Ignore);
        if (hitGround)
        {
            // Ensure we place the shadow slightly above the detected ground point.
            // Use absolute(groundOffset) so designers don't accidentally set a negative offset.
            Vector3 groundPos = hit.point + hit.normal * Mathf.Abs(groundOffset);
            // As an extra safety, ensure Y is at least slightly above hit.point.y to avoid being buried.
            float minAbove = 0.005f;
            if (groundPos.y < hit.point.y + minAbove)
            {
                groundPos.y = hit.point.y + minAbove;
            }
            lastGroundPos = groundPos;
            Quaternion groundRot = Quaternion.FromToRotation(Vector3.up, hit.normal);
            lastGroundRot = groundRot;

            shadowQuad.transform.position = groundPos;
            shadowQuad.transform.rotation = groundRot;

            // scale will be applied below based on currentRadius (handles jump interpolation)

            // alpha fade based on height between player and ground (base fade)
            float height = transform.position.y - hit.point.y;
            float baseFade = 1f;
            if (height <= fadeStart) baseFade = 1f;
            else if (height >= fadeEnd) baseFade = 0.05f;
            else baseFade = Mathf.SmoothStep(1f, 0.05f, (height - fadeStart) / Mathf.Max(0.0001f, fadeEnd - fadeStart));

            // update ground Y baseline
            lastGroundY = hit.point.y;

            // airborne detection and apex tracking
            bool grounded = (height <= airborneThreshold);
            if (!grounded && !isAirborne && wasGroundedPrev)
            {
                // left ground -> start airborne
                isAirborne = true;
                airApexY = transform.position.y;
                airStartGroundY = lastGroundY;
            }

            if (isAirborne)
            {
                // update apex
                if (transform.position.y > airApexY) airApexY = transform.position.y;

                // compute normalized ascent towards apex
                float denom = Mathf.Max(0.0001f, airApexY - airStartGroundY);
                float t = Mathf.Clamp01((transform.position.y - airStartGroundY) / denom);

                // radius and alpha blend toward jump targets based on t
                float desiredRadius = Mathf.Lerp(originalRadius, RadiusJumpMax, t);
                float desiredAlpha = Mathf.Lerp(originalAlpha, AlphaJumpMax, t) * baseFade;

                currentRadius = desiredRadius;
                currentAlpha = desiredAlpha;
            }
            else
            {
                // grounded or not considered airborne -> restore toward originals (with base fade applied)
                float targetRadius = originalRadius;
                float targetAlpha = originalAlpha * baseFade;
                currentRadius = Mathf.Lerp(currentRadius, targetRadius, Time.deltaTime * restoreSpeed);
                currentAlpha = Mathf.Lerp(currentAlpha, targetAlpha, Time.deltaTime * restoreSpeed);
            }

            // apply material alpha
            Color c = color;
            c.a = currentAlpha;
            shadowMaterial.color = c;
        }
        else
        {
            // no ground found — keep last known position so shadow doesn't spring to player
            if (lastGroundPos != Vector3.zero)
            {
                shadowQuad.transform.position = lastGroundPos;
                shadowQuad.transform.rotation = lastGroundRot;
            }
        }

        // apply current radius scale (applies whether hit or not)
        float scale = currentRadius * 2f;
        if (shadowQuad != null) shadowQuad.transform.localScale = new Vector3(scale, 1f, scale);

        // landing detection: if we were airborne and now near ground, end airborne state
        if (isAirborne)
        {
            bool nowGrounded = (!float.IsNaN(lastGroundY) && transform.position.y - lastGroundY <= airborneThreshold);
            if (nowGrounded)
            {
                isAirborne = false;
            }
        }
        wasGroundedPrev = !isAirborne;
    }

    // Helper: generate a circular radial alpha texture (soft edge)
    Texture2D GenerateRadialTexture(int size, float softness01)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.ARGB32, false, true);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] cols = new Color32[size * size];
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float maxR = (size * 0.5f);
        float hard = Mathf.Clamp01(1f - softness01);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - cx);
                float dy = (y - cy);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float t = Mathf.Clamp01(dist / maxR);
                // alpha: 1 at center, 0 at edges with softness
                float alpha = 1f;
                if (t <= hard) alpha = 1f;
                else alpha = Mathf.SmoothStep(1f, 0f, (t - hard) / Mathf.Max(0.0001f, 1f - hard));
                byte a = (byte)(Mathf.Clamp01(alpha) * 255f);
                cols[y * size + x] = new Color32(0, 0, 0, a);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        return tex;
    }

    Mesh BuildQuadMesh()
    {
        Mesh m = new Mesh();
        m.name = "EasyShadowQuadMesh";
        Vector3[] v = new Vector3[4]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f)
        };
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0f,0f), new Vector2(1f,0f), new Vector2(0f,1f), new Vector2(1f,1f)
        };
        int[] tri = new int[6] { 0, 2, 1, 1, 2, 3 };
        m.vertices = v;
        m.uv = uv;
        m.triangles = tri;
        m.RecalculateNormals();
        return m;
    }
}
