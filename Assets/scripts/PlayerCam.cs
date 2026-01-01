using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class PlayerCam : MonoBehaviour
{
    [Header("Sway")]
    [Tooltip("Horizontal sway amplitude in local units (meters)")]
    public float swayAmplitude = 0.1f;
    [Tooltip("Sway speed in cycles per second")]
    public float swaySpeed = 0.5f;
    [Tooltip("Sway frequency phase offset")]
    public float swayPhase = 0f;

    [Header("Edge Blur (screen)")]
    [Tooltip("Enable edge blur effect")]
    public bool enableEdgeBlur = true;
    [Tooltip("Overall blur strength (0-1)")]
    [Range(0f, 1f)] public float blurStrength = 0.6f;
    [Tooltip("Radius (0-1) fraction from center where blur starts")]
    [Range(0.1f, 1f)] public float blurRadius = 0.65f;
    [Tooltip("Number of blur samples (performance cost). 2-12 recommended")]
    [Range(2, 12)] public int blurSamples = 6;
    [Tooltip("Vignette intensity")]
    [Range(0f, 2f)] public float vignette = 0.7f;
    [Tooltip("Dynamic wobble amount added to blur (animated)")]
    public float blurDynamic = 1.0f;
    [Tooltip("Dynamic speed for blur wobble")]
    public float blurDynamicSpeed = 0.8f;

    [Header("Camera Settings")]
    public float distance = 10f;
    public float crouchDistanceReduction = 3f;
    public float lerpSpeed = 5f;

    public PlayerMov playerMov;
    private Transform player;
    private float currentDistance;

    Camera _cam;
    Vector3 _initialLocalPos;
    float _swayTime = 0f;

    // runtime material for the post effect
    Material _blurMat;
    Shader _blurShader;

    void Awake()
    {
        _cam = GetComponent<Camera>();
        _initialLocalPos = transform.localPosition;
        _swayTime = Random.value * 10f + swayPhase;

        // find shader and create material
        _blurShader = Shader.Find("Hidden/EdgeRadialBlur");
        if (_blurShader != null)
        {
            _blurMat = new Material(_blurShader);
            _blurMat.hideFlags = HideFlags.DontSave;
        }
        else
        {
            Debug.LogWarning("PlayerCam: EdgeRadialBlur shader not found. Edge blur disabled.");
            enableEdgeBlur = false;
        }

        // find the player transform if present
        var go = GameObject.Find("Player_Lullaby");
        if (go != null) player = go.transform;
    }

    void Start()
    {
        currentDistance = distance;
        if (player == null)
        {
            var go = GameObject.Find("Player_Lullaby");
            if (go != null) player = go.transform;
        }
        transform.rotation = Quaternion.Euler(30f, -30f, 0f);
    }

    void OnDestroy()
    {
        if (_blurMat != null)
        {
            DestroyImmediate(_blurMat);
            _blurMat = null;
        }
    }

    void LateUpdate()
    {
        // camera follow logic
        if (player != null)
        {
            // If playerMov not assigned, try to get it
            if (playerMov == null)
            {
                var pm = player.GetComponent<PlayerMov>();
                if (pm != null) playerMov = pm;
            }

            if (playerMov != null)
            {
                float targetDistance = playerMov.isCrouching ? distance * 0.5f : distance;
                currentDistance = Mathf.Lerp(currentDistance, targetDistance, Time.deltaTime * lerpSpeed);
            }

            transform.rotation = Quaternion.Euler(30f, -30f, 0f);
            Vector3 offset = transform.rotation * new Vector3(0, 0, -currentDistance);
            Vector3 basePos = player.position + offset;

            // Sway: horizontal sinusoidal offset applied in camera right direction
            _swayTime += Time.deltaTime * swaySpeed * 2f * Mathf.PI; // rad/sec
            float swayOffset = Mathf.Sin(_swayTime) * swayAmplitude;
            Vector3 swayWorld = transform.right * swayOffset;

            transform.position = basePos + swayWorld;
        }
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (!enableEdgeBlur || _blurMat == null)
        {
            Graphics.Blit(src, dest);
            return;
        }

        // animate dynamic offset
        float dyn = Mathf.Sin(Time.time * blurDynamicSpeed) * blurDynamic;

        _blurMat.SetFloat("_BlurStrength", blurStrength);
        _blurMat.SetFloat("_Radius", blurRadius);
        _blurMat.SetInt("_Samples", Mathf.Clamp(blurSamples, 2, 32));
        _blurMat.SetFloat("_DynamicOffset", dyn);
        _blurMat.SetFloat("_Vignette", vignette);

        Graphics.Blit(src, dest, _blurMat);
    }
}
