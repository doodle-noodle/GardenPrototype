using System.Collections;
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
    public static EffectsManager Instance;

    [Header("Database")]
    public EffectsDatabase effectsDatabase;

    [Header("Base material for runtime plants — drag PlantMaterial here")]
    public Material basePlantMaterial;

    void Awake()
    {
        Instance = this;
        if (effectsDatabase != null)
            effectsDatabase.Init();
    }

    // ── Public API ────────────────────────────────────────────

    public static void PlayDropAnim(EffectEvent e, GameObject target,
        Vector3 finalPosition)
    {
        if (Instance == null || Instance.effectsDatabase == null) return;
        if (!Instance.effectsDatabase.TryGet(e, out var config)) return;
        Instance.StartCoroutine(Instance.DropAnimation(target, finalPosition, config));
    }

    public static void StartGlow(EffectEvent e, Renderer rend)
    {
        if (Instance == null || Instance.effectsDatabase == null) return;
        if (!Instance.effectsDatabase.TryGet(e, out var config)) return;
        if (!config.GlowEnabled) return;
        Instance.StartCoroutine(Instance.GlowPulse(rend, config));
    }

    public static void StopGlow(Renderer rend)
    {
        if (rend == null) return;
        rend.material.DisableKeyword("_EMISSION");
    }

    // Creates a runtime material by cloning the base material with a new color.
    // Prevents pink materials in builds — base material holds the correct shader.
    public static Material CreateUrpMaterial(Color color)
    {
        if (Instance == null || Instance.basePlantMaterial == null)
        {
            Debug.LogError("EffectsManager: basePlantMaterial not assigned. " +
                "Drag a URP material into the Base Plant Material slot " +
                "on the EffectsManager GameObject.");
            return null;
        }

        // Clone so each plant gets its own color without affecting others
        var mat = new Material(Instance.basePlantMaterial);
        mat.SetColor("_BaseColor", color);
        return mat;
    }

    // ── Coroutines ────────────────────────────────────────────

    IEnumerator DropAnimation(GameObject target, Vector3 finalPos,
        EffectsDatabase.PlantAnimConfig config)
    {
        if (target == null) yield break;

        Vector3 startPos          = finalPos + Vector3.up * config.DropHeight;
        target.transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < config.DropDuration)
        {
            if (target == null) yield break;
            elapsed  += Time.deltaTime;
            float t   = Mathf.SmoothStep(0f, 1f, elapsed / config.DropDuration);
            target.transform.position = Vector3.Lerp(startPos, finalPos, t);
            yield return null;
        }

        if (target != null)
            target.transform.position = finalPos;
    }

    IEnumerator GlowPulse(Renderer rend, EffectsDatabase.PlantAnimConfig config)
    {
        if (rend == null) yield break;

        rend.material.EnableKeyword("_EMISSION");

        while (rend != null)
        {
            float pulse     = (Mathf.Sin(Time.time * config.GlowPulseSpeed) + 1f) * 0.5f;
            Color glowColor = config.GlowColor * (config.GlowIntensity * pulse);
            rend.material.SetColor("_EmissionColor", glowColor);
            yield return null;
        }
    }
}