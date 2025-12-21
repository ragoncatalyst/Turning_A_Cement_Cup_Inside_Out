using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor utility: draws a SceneView gizmo/label for any ParticleSystem whose
/// velocityOverLifetime X/Y/Z axes are not all in the same ParticleSystemCurveMode.
/// Also provides menu items to select or fix those systems.
/// </summary>
[InitializeOnLoad]
public static class ParticleVelocityModeGizmo
{
    static ParticleVelocityModeGizmo()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    static void OnSceneGUI(SceneView sv)
    {
        if (Event.current == null) return;

        var systems = GameObject.FindObjectsOfType<ParticleSystem>(true);
        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            var vol = ps.velocityOverLifetime;
            var mx = vol.x.mode;
            var my = vol.y.mode;
            var mz = vol.z.mode;
            if (mx == my && my == mz) continue; // all same => OK

            // draw a red wire disc and a label at the PS position
            var pos = ps.transform.position;
            Handles.color = new Color(1f, 0.35f, 0.35f, 0.5f);
            float size = HandleUtility.GetHandleSize(pos) * 0.5f;
            Handles.DrawWireDisc(pos, Vector3.up, size);
            Handles.color = Color.red;
            string label = $"Mixed Velocity Modes: X={mx} Y={my} Z={mz}\nGameObject: {ps.gameObject.name}";
            GUIStyle style = new GUIStyle(EditorStyles.helpBox) { fontSize = 11, alignment = TextAnchor.UpperLeft };
            Handles.Label(pos + Vector3.up * (size * 0.6f), label, style);
        }
    }

    [MenuItem("Tools/Particle Velocity/Select Mixed Mode Systems")]
    public static void SelectMixedModeSystems()
    {
        var systems = GameObject.FindObjectsOfType<ParticleSystem>(true);
        List<GameObject> list = new List<GameObject>();
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            var vol = ps.velocityOverLifetime;
            var mx = vol.x.mode;
            var my = vol.y.mode;
            var mz = vol.z.mode;
            if (mx == my && my == mz) continue;
            list.Add(ps.gameObject);
        }
        Selection.objects = list.ToArray();
        if (list.Count == 0) Debug.Log("No mixed-mode ParticleSystems found in the active scenes.");
        else Debug.Log($"Selected {list.Count} ParticleSystem GameObjects with mixed velocity modes.");
    }

    [MenuItem("Tools/Particle Velocity/Fix Mixed Mode Systems (TwoConstants)")]
    public static void FixMixedModeSystems()
    {
        var systems = GameObject.FindObjectsOfType<ParticleSystem>(true);
        int fixedCount = 0;
        foreach (var ps in systems)
        {
            if (ps == null) continue;
            var vol = ps.velocityOverLifetime;
            var mx = vol.x.mode;
            var my = vol.y.mode;
            var mz = vol.z.mode;
            if (mx == my && my == mz) continue; // already consistent

            Undo.RecordObject(ps, "Fix ParticleSystem velocity mode");

            // helper: derive min/max for a MinMaxCurve
            float xmin, xmax, ymin, ymax, zmin, zmax;
            GetMinMax(vol.x, out xmin, out xmax);
            GetMinMax(vol.y, out ymin, out ymax);
            GetMinMax(vol.z, out zmin, out zmax);

            var vx = vol.x;
            vx.mode = ParticleSystemCurveMode.TwoConstants;
            vx.constantMin = xmin;
            vx.constantMax = xmax;
            vol.x = vx;

            var vy = vol.y;
            vy.mode = ParticleSystemCurveMode.TwoConstants;
            vy.constantMin = ymin;
            vy.constantMax = ymax;
            vol.y = vy;

            var vz = vol.z;
            vz.mode = ParticleSystemCurveMode.TwoConstants;
            vz.constantMin = zmin;
            vz.constantMax = zmax;
            vol.z = vz;

            EditorUtility.SetDirty(ps);
            fixedCount++;
        }
        Debug.Log($"Fixed {fixedCount} ParticleSystems to TwoConstants mode for velocityOverLifetime.");
    }

    static void GetMinMax(ParticleSystem.MinMaxCurve c, out float min, out float max)
    {
        min = 0f; max = 0f;
        switch (c.mode)
        {
            case ParticleSystemCurveMode.TwoConstants:
                min = c.constantMin; max = c.constantMax; break;
            case ParticleSystemCurveMode.Constant:
                min = max = c.constant; break;
            case ParticleSystemCurveMode.Curve:
                SampleCurveMinMax(c.curve, out min, out max); break;
            case ParticleSystemCurveMode.TwoCurves:
                float minA, maxA, minB, maxB;
                SampleCurveMinMax(c.curveMin, out minA, out maxA);
                SampleCurveMinMax(c.curveMax, out minB, out maxB);
                min = Mathf.Min(minA, minB); max = Mathf.Max(maxA, maxB);
                break;
            default:
                min = max = 0f; break;
        }
    }

    static void SampleCurveMinMax(AnimationCurve curve, out float min, out float max)
    {
        min = float.MaxValue; max = float.MinValue;
        if (curve == null)
        {
            min = max = 0f; return;
        }
        // sample 21 points across [0,1]
        for (int i = 0; i <= 20; i++)
        {
            float t = i / 20f;
            float v = curve.Evaluate(t);
            if (v < min) min = v;
            if (v > max) max = v;
        }
        if (min == float.MaxValue) min = 0f;
        if (max == float.MinValue) max = 0f;
    }
}
