using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// WindSystem: exposes a Y-axis direction (degrees) and a strength value
/// as serialized fields. It broadcasts the computed world-space direction
/// and strength to all active `BlizzardController` instances in the scene.
///
/// - `directionY` is a rotation around the Y axis in degrees (0 = forward/Z).
/// - `strength` is a scalar multiplier applied by BlizzardController.
///
/// The script updates at edit-time and runtime when values change.
/// </summary>
[ExecuteAlways]
public class WindSystem : MonoBehaviour
{
    [Tooltip("Y-axis rotation in degrees (0 = forward Z).")]
    public float directionY = 0f;
    [Tooltip("Wind strength (multiplier).")]
    public float strength = 5f;
    [Tooltip("If true, WindSystem will find all BlizzardController instances and call SetWind on them.")]
    public bool applyToBlizzardControllers = true;

    // Cached last-applied values to avoid redundant updates
    Vector3 _lastDirection = Vector3.zero;
    float _lastStrength = float.NaN;

    // global accessors (read-only)
    public static Vector3 CurrentDirection { get; private set; } = Vector3.forward;
    public static float CurrentStrength { get; private set; } = 0f;

    void OnEnable()
    {
        ApplyWind();
    }

    void OnValidate()
    {
        // Keep editor changes responsive
        ApplyWind();
    }

    void Update()
    {
        // Only reapply when values changed
        Vector3 dir = Quaternion.Euler(0f, directionY, 0f) * Vector3.forward;
        if (dir != _lastDirection || !Mathf.Approximately(strength, _lastStrength))
        {
            ApplyWind();
        }
    }

    void ApplyWind()
    {
        Vector3 dir = Quaternion.Euler(0f, directionY, 0f) * Vector3.forward;
        CurrentDirection = dir.normalized;
        CurrentStrength = strength;
        _lastDirection = dir;
        _lastStrength = strength;

        if (!applyToBlizzardControllers) return;

        // Broadcast to all BlizzardController instances in the scene
        var controllers = FindObjectsOfType<BlizzardController>();
        for (int i = 0; i < controllers.Length; i++)
        {
            var bc = controllers[i];
            if (bc != null) bc.SetWind(CurrentDirection, CurrentStrength);
        }
    }
}
