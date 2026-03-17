using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance;

    [Header("Database")]
    public VFXDatabase db;

    [Header("Base material for runtime plants")]
    public Material basePlantMaterial;

    private readonly Dictionary<Renderer, Coroutine> _activeGlows
        = new Dictionary<Renderer, Coroutine>();

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
    }

    // ── Public API ────────────────────────────────────────────

    public static void PlayBurial(Vector3 plotPosition, float plotHalfHeight,
        GrowthStageVisual visual, System.Action onComplete = null)
    {
        if (!IsReady(out var mgr)) { onComplete?.Invoke(); return; }
        mgr.StartCoroutine(mgr.BurialRoutine(
            plotPosition, plotHalfHeight, visual, onComplete));
    }

    public static void PlayDrop(GameObject target, Vector3 finalPosition)
    {
        if (!IsReady(out var mgr) || target == null) return;
        mgr.StartCoroutine(mgr.DropRoutine(target, finalPosition));
    }

    public static void StartGlow(Renderer rend)
    {
        if (!IsReady(out var mgr) || rend == null) return;
        StopGlow(rend);
        var c = mgr.StartCoroutine(mgr.GlowRoutine(rend));
        mgr._activeGlows[rend] = c;
    }

    public static void StopGlow(Renderer rend)
    {
        if (!IsReady(out var mgr) || rend == null) return;
        if (mgr._activeGlows.TryGetValue(rend, out var c))
        {
            if (c != null) mgr.StopCoroutine(c);
            mgr._activeGlows.Remove(rend);
        }
        rend.material.DisableKeyword("_EMISSION");
    }

    public static Material CreateMaterial(Color color)
    {
        if (!IsReady(out var mgr) || mgr.basePlantMaterial == null)
        {
            Debug.LogError("VFXManager: basePlantMaterial not assigned.");
            return null;
        }
        var mat = new Material(mgr.basePlantMaterial);
        mat.SetColor("_BaseColor", color);
        return mat;
    }

    // ── Coroutines ────────────────────────────────────────────

    IEnumerator BurialRoutine(Vector3 plotPos, float plotHalfHeight,
        GrowthStageVisual visual, System.Action onComplete)
    {
        GameObject seed = visual.visualPrefab != null
            ? Instantiate(visual.visualPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        seed.transform.localScale = Vector3.one * visual.scale;

        foreach (var col in seed.GetComponentsInChildren<Collider>())
            Destroy(col);

        var rend = seed.GetComponent<Renderer>();
        if (rend != null && visual.visualPrefab == null)
            rend.material = CreateMaterial(visual.stageColor);

        // Prefab origin sits on plot surface — sphere needs radius offset
        Vector3 finalPos = visual.visualPrefab != null
            ? new Vector3(plotPos.x, plotPos.y + plotHalfHeight,                        plotPos.z)
            : new Vector3(plotPos.x, plotPos.y + plotHalfHeight + visual.scale * 0.5f, plotPos.z);

        Vector3 hoverPos = finalPos + Vector3.up * db.BurialHoverHeight;
        Vector3 risePos  = hoverPos + Vector3.up * db.BurialRiseHeight;

        seed.transform.position = hoverPos;

        // Phase 1 — hover
        yield return new WaitForSeconds(db.BurialHoverDuration);

        // Phase 2 — anticipation rise
        yield return MoveRoutine(seed, hoverPos, risePos, db.BurialRiseDuration, smooth: true);

        // Phase 3 — plant down to final position
        yield return MoveRoutine(seed, risePos, finalPos, db.BurialPlantDuration, smooth: true);

        Destroy(seed);
        onComplete?.Invoke();
    }

    IEnumerator DropRoutine(GameObject target, Vector3 finalPos)
    {
        if (target == null) yield break;
        Vector3 startPos = finalPos + Vector3.up * db.DropHeight;
        target.transform.position = startPos;
        yield return MoveRoutine(target, startPos, finalPos, db.DropDuration, smooth: true);
    }

    IEnumerator GlowRoutine(Renderer rend)
    {
        if (rend == null) yield break;
        rend.material.EnableKeyword("_EMISSION");
        while (rend != null && rend.gameObject != null)
        {
            float pulse = (Mathf.Sin(Time.time * db.GlowPulseSpeed) + 1f) * 0.5f;
            rend.material.SetColor("_EmissionColor",
                db.GlowColor * (db.GlowIntensity * pulse));
            yield return null;
        }
        if (rend != null && _activeGlows.ContainsKey(rend))
            _activeGlows.Remove(rend);
    }

    // Reusable movement — used by burial and drop
    IEnumerator MoveRoutine(GameObject target, Vector3 from, Vector3 to,
        float duration, bool smooth)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (smooth) t = Mathf.SmoothStep(0f, 1f, t);
            target.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        if (target != null)
            target.transform.position = to;
    }

    // ── Helpers ───────────────────────────────────────────────

    private static bool IsReady(out VFXManager mgr)
    {
        mgr = Instance;
        return mgr != null && mgr.db != null;
    }
}