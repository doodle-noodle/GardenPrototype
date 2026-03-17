using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Database")]
    public VFXDatabase vfxDatabase;

    [Header("Base material for runtime plants")]
    public Material basePlantMaterial;

    private readonly Dictionary<Renderer, Coroutine> _activeGlows
        = new Dictionary<Renderer, Coroutine>();

    void Awake()
    {
        Instance = this;
        if (vfxDatabase != null)
            vfxDatabase.Init();
    }

    public static void PlayDropAnim(VFXEvent e, GameObject target, Vector3 finalPosition)
    {
        if (!IsReady(out var mgr)) return;
        if (!mgr.vfxDatabase.TryGet(e, out var config)) return;
        mgr.StartCoroutine(mgr.DropAnimation(target, finalPosition, config));
    }

    public static void StartGlow(VFXEvent e, Renderer rend)
    {
        if (!IsReady(out var mgr) || rend == null) return;
        if (!mgr.vfxDatabase.TryGet(e, out var config)) return;
        if (!config.GlowEnabled) return;
        StopGlow(rend);
        Coroutine c = mgr.StartCoroutine(mgr.GlowPulse(rend, config));
        mgr._activeGlows[rend] = c;
    }

    public static void StopGlow(Renderer rend)
    {
        if (!IsReady(out var mgr) || rend == null) return;
        if (mgr._activeGlows.TryGetValue(rend, out Coroutine c))
        {
            if (c != null) mgr.StopCoroutine(c);
            mgr._activeGlows.Remove(rend);
        }
        if (rend != null)
            rend.material.DisableKeyword("_EMISSION");
    }

    public static Material CreateMaterial(Color color)
    {
        if (!IsReady(out var mgr) || mgr.basePlantMaterial == null)
        {
            Debug.LogError("VFXManager: basePlantMaterial not assigned in Inspector.");
            return null;
        }
        var mat = new Material(mgr.basePlantMaterial);
        mat.SetColor("_BaseColor", color);
        return mat;
    }

    IEnumerator DropAnimation(GameObject target, Vector3 finalPos,
        VFXDatabase.PlantAnimConfig config)
    {
        if (target == null) yield break;
        Vector3 startPos = finalPos + Vector3.up * config.DropHeight;
        target.transform.position = startPos;
        float elapsed = 0f;
        while (elapsed < config.DropDuration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / config.DropDuration);
            target.transform.position = Vector3.Lerp(startPos, finalPos, t);
            yield return null;
        }
        if (target != null)
            target.transform.position = finalPos;
    }

    IEnumerator GlowPulse(Renderer rend, VFXDatabase.PlantAnimConfig config)
    {
        if (rend == null) yield break;
        rend.material.EnableKeyword("_EMISSION");
        while (rend != null && rend.gameObject != null)
        {
            float pulse = (Mathf.Sin(Time.time * config.GlowPulseSpeed) + 1f) * 0.5f;
            Color glowColor = config.GlowColor * (config.GlowIntensity * pulse);
            rend.material.SetColor("_EmissionColor", glowColor);
            yield return null;
        }
        if (rend != null && _activeGlows.ContainsKey(rend))
            _activeGlows.Remove(rend);
    }

    private static bool IsReady(out VFXManager mgr)
    {
        mgr = Instance;
        return mgr != null && mgr.vfxDatabase != null;
    }
}