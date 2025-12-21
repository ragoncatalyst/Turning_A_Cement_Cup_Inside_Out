using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// BlizzardController: produces a dense falling-white-particle (snow) effect.
/// - If `snowPS` is not assigned, one will be created with sensible defaults.
/// - When snow particles collide with world geometry, the script spawns a short
///   fade emitter at the collision point which fades alpha to 0 over `fadeDuration` seconds.
/// - The fade emitters are pooled for performance.
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class BlizzardController : MonoBehaviour
{
    [Header("Snow (main) particle system")]
    public ParticleSystem snowPS; // optional: assign a ParticleSystem you configured
    [Tooltip("Area size (X,Z) where snow is spawned; Y is height of spawn box")]
    public Vector3 areaSize = new Vector3(30f, 15f, 30f);
    [Tooltip("Particles emitted per second (total across area)")]
    public float emissionRate = 600f;
    [Tooltip("If true, only emit particles when they are visible from the configured camera (line-of-sight).")]
    public bool cullByCameraVisibility = true;
    [Tooltip("Camera used for visibility tests. If null, Camera.main will be used.")]
    public Camera visibilityCamera;
    [Tooltip("Maximum number of raycasts used per frame for visibility tests (safety cap).")]
    public int maxRaycastsPerFrame = 200;
    [Tooltip("Maximum random sampling attempts per frame to find visible spawn points.")]
    public int maxSpawnAttemptsPerFrame = 1000;
    [Tooltip("Particle fall speed range (unused when startSpeed set to 0 and velocity over lifetime drives motion)")]
    public Vector2 speedRange = new Vector2(0f, 0f);
    [Tooltip("Particle size range")]
    public Vector2 sizeRange = new Vector2(0.03f, 0.12f);
    [Header("Wind")]
    [SerializeField] private float windStrength = 5f; // 风力强度
    [SerializeField] private Vector3 windDirection = new Vector3(1f, 0f, 0f); // 风向

    [Header("Collision fade prefab (one-shot particle) -- optional")]
    public ParticleSystem fadePrefab; // if null will be created automatically
    [Tooltip("How long fade takes (in seconds) before the hit particle is gone")]
    public float fadeDuration = 1.5f;
    [Tooltip("Pool size for fade emitters")]
    public int fadePoolSize = 24;

    // pool
    List<ParticleSystem> fadePool = new List<ParticleSystem>();
    int fadePoolIndex = 0;

    // collision event buffer
    List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();
    // cached generated dot texture for particles
    Texture2D _cachedDotTexture = null;
    // emission accumulator for script-driven emission
    float _emitAccumulator = 0f;
    int _raycastsThisFrame = 0;

    void Awake()
    {
        if (snowPS == null)
        {
            snowPS = CreateSnowParticleSystem("Blizzard_SnowPS");
            snowPS.transform.SetParent(transform, false);
        }

        if (fadePrefab == null)
        {
            fadePrefab = CreateFadePrefab();
        }

        // create pool
        for (int i = 0; i < fadePoolSize; i++)
        {
            var go = Instantiate(fadePrefab.gameObject, transform);
            go.name = "Blizzard_FadePool_" + i;
            var ps = go.GetComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            fadePool.Add(ps);
        }

        // Ensure assigned particle renderers use valid materials (fix magenta)
        EnsureParticleSystemRendererHasValidMaterial(snowPS);
        if (fadePrefab != null) EnsureParticleSystemRendererHasValidMaterial(fadePrefab);
        // Also fix any ParticleSystemRenderer under this GameObject
        var childRenderers = GetComponentsInChildren<ParticleSystemRenderer>(true);
        foreach (var r in childRenderers) EnsureRendererHasValidMaterial(r);

        // Configure the main snow particle system to behave like a dense blizzard
        ConfigureBlizzard();

        // Start with emission disabled; we'll drive emission manually to allow visibility culling
        var emission = snowPS.emission;
        emission.rateOverTime = 0f;

        // Remove any stray MeshRenderers (default square gizmos) under this object
        var meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
        foreach (var mr in meshRenderers)
        {
            // if it's not explicitly authored (e.g., created by our pools), disable it
            if (mr.gameObject != this.gameObject)
            {
                mr.enabled = false;
            }
        }
    }

    // Configure the existing snowPS to match the EffectBlissard settings
    void ConfigureBlizzard()
    {
        if (snowPS == null) return;

        var main = snowPS.main;
        main.startSpeed = 0f; // motion controlled by velocity over lifetime
        main.startSize = 0.05f;
        main.startLifetime = 20f;
        main.maxParticles = Mathf.Max(5000, (int)(emissionRate * main.startLifetime.constant));
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = snowPS.emission;
        emission.rateOverTime = emissionRate;
        emission.SetBursts(new ParticleSystem.Burst[0]);

        var shape = snowPS.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;
        shape.randomDirectionAmount = 0.2f;

        var velocityOverLifetime = snowPS.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        // Ensure all three axes use TwoConstants mode so curves behave consistently
        // X axis
        var vx = velocityOverLifetime.x;
        vx.mode = ParticleSystemCurveMode.TwoConstants;
        vx.constantMin = windStrength * windDirection.x - 1f;
        vx.constantMax = windStrength * windDirection.x + 1f;
        velocityOverLifetime.x = vx;
        // Y axis (downward drift)
        var vy = velocityOverLifetime.y;
        vy.mode = ParticleSystemCurveMode.TwoConstants;
        vy.constantMin = -1f;
        vy.constantMax = -0.5f;
        velocityOverLifetime.y = vy;
        // Z axis
        var vz = velocityOverLifetime.z;
        vz.mode = ParticleSystemCurveMode.TwoConstants;
        vz.constantMin = windStrength * windDirection.z - 1f;
        vz.constantMax = windStrength * windDirection.z + 1f;
        velocityOverLifetime.z = vz;

        var collision = snowPS.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.bounce = 0f;
        collision.dampen = 1f;
        // Kill particle immediately on collision so it doesn't continue below ground.
        // We still send collision messages to spawn fade effects in OnParticleCollision.
        collision.lifetimeLoss = 1f;

        // Ensure renderer has a valid material
        EnsureParticleSystemRendererHasValidMaterial(snowPS);
    }

    ParticleSystem CreateSnowParticleSystem(string name)
    {
        var go = new GameObject(name);
        go.transform.position = transform.position;
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.startLifetime = Mathf.Max(2f, areaSize.y / speedRange.y * 1.2f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(speedRange.x, speedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(sizeRange.x, sizeRange.y);
        main.startColor = Color.white;
        main.maxParticles = Mathf.CeilToInt(emissionRate * main.startLifetime.constant);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        // we'll drive emission from script so set to zero here
        emission.rateOverTime = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = areaSize;
        shape.position = new Vector3(0f, areaSize.y * 0.5f, 0f);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader sh = FindCompatibleParticleShader();
        var mat = CreateSafeMaterial(sh);
        Texture2D dotTex = GetOrCreateParticleDotTexture();
        if (mat != null)
        {
            // enforce white fallback so missing textures/shaders don't show magenta
            mat.color = Color.white;
            if (mat.mainTexture == null) mat.mainTexture = dotTex;
            renderer.material = mat;
        }
        else
        {
            // if we couldn't create a material, disable renderer to avoid crashes/magenta
            renderer.enabled = false;
            Debug.LogWarning("BlizzardController: no compatible shader found; particle renderer has been disabled to avoid runtime errors.");
        }
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (renderer.material != null && renderer.material.mainTexture == null) renderer.material.mainTexture = dotTex;

        // collision module: enable world collisions and send collision messages
        var collision = ps.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.sendCollisionMessages = true;
        // Kill particle on collision so it won't be rendered under the ground.
        collision.lifetimeLoss = 1f;

        // some randomness
        var velocity = ps.velocityOverLifetime;
        velocity.enabled = false;

        return ps;
    }

    void Update()
    {
        if (snowPS == null || emissionRate <= 0f) return;

        _raycastsThisFrame = 0;
        _emitAccumulator += emissionRate * Time.deltaTime;
        int toEmit = Mathf.FloorToInt(_emitAccumulator);
        if (toEmit <= 0) return;
        _emitAccumulator -= toEmit;

        Camera cam = visibilityCamera != null ? visibilityCamera : Camera.main;

        int attempts = 0;
        int emitted = 0;
        int maxAttempts = Mathf.Clamp(maxSpawnAttemptsPerFrame, 64, 5000);
        int maxRaycasts = Mathf.Max(1, maxRaycastsPerFrame);

        while (emitted < toEmit && attempts < maxAttempts)
        {
            attempts++;
            // random point inside box (centered at transform + (0, areaSize.y*0.5f, 0))
            Vector3 localOffset = new Vector3(
                Random.Range(-areaSize.x * 0.5f, areaSize.x * 0.5f),
                Random.Range(-areaSize.y * 0.5f, areaSize.y * 0.5f),
                Random.Range(-areaSize.z * 0.5f, areaSize.z * 0.5f)
            );
            Vector3 center = transform.position + new Vector3(0f, areaSize.y * 0.5f, 0f);
            Vector3 spawnPos = center + localOffset;

            bool visible = true;
            if (cullByCameraVisibility && cam != null && _raycastsThisFrame < maxRaycasts)
            {
                Vector3 dir = spawnPos - cam.transform.position;
                float dist = dir.magnitude;
                if (dist <= 0.001f) visible = true;
                else
                {
                    RaycastHit hit;
                    _raycastsThisFrame++;
                    if (Physics.Raycast(cam.transform.position, dir.normalized, out hit, dist - 0.01f))
                    {
                        // something blocks the line of sight
                        visible = false;
                    }
                }
            }

            if (!visible)
            {
                // skip emitting at this sampled point
                continue;
            }

            // Emit one particle at spawnPos
            var ep = new ParticleSystem.EmitParams();
            ep.position = spawnPos;
            ep.applyShapeToPosition = false;
            snowPS.Emit(ep, 1);
            emitted++;
        }
    }

    ParticleSystem CreateFadePrefab()
    {
        var go = new GameObject("Blizzard_FadePrefab");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = false;
        main.startLifetime = fadeDuration;
        main.startSpeed = 0f;
        main.startSize = 0.08f;
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 1;

        var emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = 0f;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.01f;

        // color over lifetime: fade alpha to 0
        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient g = new Gradient();
        g.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = new ParticleSystem.MinMaxGradient(g);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        Shader sh = FindCompatibleParticleShader();
        var mat = CreateSafeMaterial(sh);
        Texture2D dotTex = GetOrCreateParticleDotTexture();
        if (mat != null)
        {
            mat.color = Color.white;
            if (mat.mainTexture == null) mat.mainTexture = dotTex;
            renderer.material = mat;
        }
        else
        {
            renderer.enabled = false;
            Debug.LogWarning("BlizzardController: no compatible shader found for fade prefab; fade renderer disabled.");
        }
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        if (renderer.material != null && renderer.material.mainTexture == null) renderer.material.mainTexture = dotTex;

        return ps;
    }

    // Try to find a shader that works with the current render pipeline for particles.
    Shader FindCompatibleParticleShader()
    {
        string[] candidates = new string[] {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Unlit",
            "Particles/Standard Unlit",
            "Particles/Standard Surface",
            "Sprites/Default",
            "Unlit/Texture"
        };
        foreach (var name in candidates)
        {
            var s = Shader.Find(name);
            if (s != null) return s;
        }
        return null;
    }

    Material CreateSafeMaterial(Shader s)
    {
        Shader fallback = s;
        if (fallback == null) fallback = Shader.Find("Sprites/Default");
        if (fallback == null) fallback = Shader.Find("Unlit/Texture");
        if (fallback == null) fallback = Shader.Find("Unlit/Color");
        if (fallback == null) fallback = Shader.Find("Standard");
        if (fallback == null)
        {
            // as last resort, return null so caller can disable renderer instead of crashing
            return null;
        }

        try
        {
            var mat = new Material(fallback);
            mat.hideFlags = HideFlags.DontSave;
            return mat;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("BlizzardController: failed to create material from shader " + fallback.name + ", exception: " + e.Message);
            return null;
        }
    }

    Texture2D GetOrCreateParticleDotTexture()
    {
        if (_cachedDotTexture != null) return _cachedDotTexture;
        int size = 32;
        var tex = new Texture2D(size, size, TextureFormat.ARGB32, false);
        Color32[] cols = new Color32[size * size];
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size * 0.45f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), center);
                byte alpha = (byte)(Mathf.Clamp01(1f - (d / radius)) * 255f);
                cols[y * size + x] = new Color32(255, 255, 255, alpha);
            }
        }
        tex.SetPixels32(cols);
        tex.Apply();
        tex.hideFlags = HideFlags.DontSave;
        _cachedDotTexture = tex;
        return _cachedDotTexture;
    }

    void OnParticleCollision(GameObject other)
    {
        if (snowPS == null) return;
        int num = snowPS.GetCollisionEvents(other, collisionEvents);
        for (int i = 0; i < num; i++)
        {
            var ce = collisionEvents[i];
            SpawnFadeAt(ce.intersection);
        }
    }

    void SpawnFadeAt(Vector3 pos)
    {
        if (fadePool.Count == 0) return;
        var ps = fadePool[fadePoolIndex];
        fadePoolIndex = (fadePoolIndex + 1) % fadePool.Count;
        ps.transform.position = pos;
        ps.transform.rotation = Quaternion.identity;
        ps.Play(true);
    }

    void EnsureParticleSystemRendererHasValidMaterial(ParticleSystem ps)
    {
        if (ps == null) return;
        var rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend != null) EnsureRendererHasValidMaterial(rend);
    }

    /// <summary>
    /// Public API for external systems (e.g. WindSystem) to inform this
    /// BlizzardController of the current wind direction and strength.
    /// Direction is expected to be a normalized world-space vector (x,y,z),
    /// strength is a scalar multiplier. This updates the velocityOverLifetime
    /// module immediately so particles respond in real-time.
    /// </summary>
    public void SetWind(Vector3 direction, float strength)
    {
        windDirection = direction;
        windStrength = strength;
        if (snowPS == null) return;
        var vol = snowPS.velocityOverLifetime;
        vol.enabled = true;
        var vx2 = vol.x;
        vx2.mode = ParticleSystemCurveMode.TwoConstants;
        vx2.constantMin = windStrength * windDirection.x - 1f;
        vx2.constantMax = windStrength * windDirection.x + 1f;
        vol.x = vx2;
        var vz2 = vol.z;
        vz2.mode = ParticleSystemCurveMode.TwoConstants;
        vz2.constantMin = windStrength * windDirection.z - 1f;
        vz2.constantMax = windStrength * windDirection.z + 1f;
        vol.z = vz2;
    }

    void EnsureRendererHasValidMaterial(Renderer rend)
    {
        if (rend == null) return;
        var mat = rend.sharedMaterial;
        bool bad = false;
        if (mat == null) bad = true;
        else
        {
            var s = mat.shader;
            if (s == null || !s.isSupported) bad = true;
        }

        if (bad)
        {
            Shader sh = FindCompatibleParticleShader();
            Material newMat = (sh != null) ? new Material(sh) : new Material(Shader.Find("Sprites/Default"));
            newMat.color = Color.white;
            // assign safely
            rend.sharedMaterial = newMat;
        }
    }
}
