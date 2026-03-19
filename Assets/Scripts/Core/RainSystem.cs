using UnityEngine;
using UnityEngine.Rendering;

public class RainSystem : MonoBehaviour
{
    private ParticleSystem _rainPS;
    private ParticleSystem _splashPS;
    private Color          _color = new Color(0.7f, 0.85f, 1f, 0.8f);
    private bool           _built;

    void Awake()
    {
        BuildSystems();
    }

    // ── Public API ────────────────────────────────────────────

    public void SetRain(bool active, Color color)
    {
        _color = color;
        if (!_built) BuildSystems();

        if (active)
        {
            UpdateColors();
            if (!_rainPS.isPlaying) _rainPS.Play();
        }
        else
        {
            _rainPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // ── Build ─────────────────────────────────────────────────

    void BuildSystems()
    {
        if (_built) return;
        _built = true;

        _splashPS = BuildSplashPS();
        _rainPS   = BuildRainPS(_splashPS);

        // Ensure both are stopped after construction
        _splashPS.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        _rainPS.Stop(true,   ParticleSystemStopBehavior.StopEmittingAndClear);
    }

    // ── Rain drop PS ──────────────────────────────────────────

    ParticleSystem BuildRainPS(ParticleSystem splash)
    {
        var go = new GameObject("RainPS");
        go.transform.SetParent(transform);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        float gw = 12f, gh = 12f;
        if (GridManager.Instance != null)
        {
            gw = GridManager.Instance.gridWidth  * GridManager.Instance.cellSize;
            gh = GridManager.Instance.gridHeight * GridManager.Instance.cellSize;
        }
        go.transform.position = new Vector3(gw * 0.5f, 18f, gh * 0.5f);

        var ps = go.AddComponent<ParticleSystem>();

        // Stop immediately before configuring to avoid "still playing" errors
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.loop            = true;
        main.playOnAwake     = false;
        main.duration        = 10f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(24f, 28f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2.5f;
        main.startColor      = _color;
        main.maxParticles    = 800;

        var emission          = ps.emission;
        emission.rateOverTime = 120f;

        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(gw + 6f, gh + 6f, 0.1f);

        // Force rain straight down — zero horizontal velocity
        var vel     = ps.velocityOverLifetime;
        vel.enabled = true;
        vel.space   = ParticleSystemSimulationSpace.World;
        vel.x       = new ParticleSystem.MinMaxCurve(0f);
        vel.z       = new ParticleSystem.MinMaxCurve(0f);

        var col                    = ps.collision;
        col.enabled                = true;
        col.type                   = ParticleSystemCollisionType.World;
        col.mode                   = ParticleSystemCollisionMode.Collision3D;
        col.lifetimeLoss           = 1f;
        col.bounceMultiplier       = 0f;
        col.quality                = ParticleSystemCollisionQuality.Low;
        col.enableDynamicColliders = false;

        var sub     = ps.subEmitters;
        sub.enabled = true;
        sub.AddSubEmitter(splash,
            ParticleSystemSubEmitterType.Collision,
            ParticleSystemSubEmitterProperties.InheritColor);

        var rend               = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode        = ParticleSystemRenderMode.Stretch;
        rend.velocityScale     = 0.025f;
        rend.lengthScale       = 2.5f;
        rend.material          = CreateParticleMat(_color);
        rend.sortingOrder      = 10;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        return ps;
    }

    // ── Splash PS ─────────────────────────────────────────────

    ParticleSystem BuildSplashPS()
    {
        var go = new GameObject("SplashPS");
        go.transform.SetParent(transform);

        var ps = go.AddComponent<ParticleSystem>();

        // Stop immediately before configuring
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main             = ps.main;
        main.loop            = false;
        main.playOnAwake     = false;
        main.duration        = 1f;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(0.12f, 0.25f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(1.5f, 4f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.02f, 0.05f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 4f;
        main.startColor      = new Color(_color.r, _color.g, _color.b, 0.55f);
        main.maxParticles    = 400;

        var emission          = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2, 4) });

        var shape       = ps.shape;
        shape.enabled   = true;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 0.01f;

        var rend               = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode        = ParticleSystemRenderMode.Stretch;
        rend.velocityScale     = 0.06f;
        rend.lengthScale       = 2f;
        rend.material          = CreateParticleMat(new Color(_color.r, _color.g, _color.b, 0.55f));
        rend.sortingOrder      = 10;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows    = false;

        return ps;
    }

    // ── Color update ──────────────────────────────────────────

    void UpdateColors()
    {
        if (_rainPS != null)
        {
            var m = _rainPS.main; m.startColor = _color;
            _rainPS.GetComponent<ParticleSystemRenderer>().material =
                CreateParticleMat(_color);
        }
        if (_splashPS != null)
        {
            var m = _splashPS.main;
            m.startColor = new Color(_color.r, _color.g, _color.b, 0.55f);
        }
    }

    // ── Material ──────────────────────────────────────────────

    Material CreateParticleMat(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Particles/Standard Unlit")
            ?? Shader.Find("Universal Render Pipeline/Unlit")
            ?? Shader.Find("Standard");

        var mat = new Material(shader);
        mat.SetColor("_BaseColor", color);
        mat.SetColor("_Color",     color);
        mat.SetFloat("_Surface",   1f);
        mat.SetFloat("_ZWrite",    0f);
        mat.SetFloat("_SrcBlend",  (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend",  (float)BlendMode.OneMinusSrcAlpha);
        mat.renderQueue = 3000;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        return mat;
    }
}