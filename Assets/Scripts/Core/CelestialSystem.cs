using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CelestialSystem : MonoBehaviour
{
    private const int   StarCount = 80;
    private const float MoonSize  = 80f;
    private const float StarMinSz = 3f;
    private const float StarMaxSz = 9f;

    private Transform        _celestialRoot;
    private GameObject       _moon;
    private List<GameObject> _stars = new List<GameObject>();

    void Awake()
    {
        Canvas mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null)
        {
            Debug.LogError("CelestialSystem: main Canvas not found. Celestials disabled.");
            return;
        }

        // Safety check — main canvas must keep its GraphicRaycaster for all UI interaction.
        // CelestialRoot intentionally has NO GraphicRaycaster (visual-only).
        if (mainCanvas.GetComponent<GraphicRaycaster>() == null)
            Debug.LogWarning("CelestialSystem: main Canvas is missing a GraphicRaycaster. " +
                "UI clicks may not work correctly.");

        // Child canvas override — the safe alternative to a standalone Screen Space Overlay canvas.
        // A child canvas with overrideSorting does NOT create a separate hit-test surface,
        // so it cannot block raycasts on the main canvas beneath it. This was the root
        // cause of the previous UI click breakage and must not be reverted to a root canvas.
        var go = new GameObject("CelestialRoot", typeof(RectTransform), typeof(Canvas));
        go.transform.SetParent(mainCanvas.transform, false);

        // Stretch to cover the full canvas rect so anchored children position correctly
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var childCanvas             = go.GetComponent<Canvas>();
        childCanvas.overrideSorting = true;
        childCanvas.sortingOrder    = -5; // behind world labels (-1) and main UI (0)
        // No GraphicRaycaster — celestials are purely visual and must never receive or block input

        _celestialRoot = go.transform;
    }

    // ── Public API ────────────────────────────────────────────

    public void SetCelestials(bool hasMoon, Color moonColor, bool hasStars)
    {
        if (_celestialRoot == null) return; // Awake failed silently — safe no-op
        SetMoon(hasMoon, moonColor);
        SetStars(hasStars);
    }

    // ── Moon ──────────────────────────────────────────────────

    void SetMoon(bool active, Color color)
    {
        if (active && _moon == null) _moon = CreateMoon();
        if (_moon == null) return;
        _moon.SetActive(active);
        if (active) _moon.GetComponent<Image>().color = color;
    }

    GameObject CreateMoon()
    {
        var go = MakeImage(_celestialRoot, "Moon", MoonSize, MoonSize);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.75f, 0.75f);
        rt.anchorMax        = new Vector2(0.75f, 0.75f);
        rt.anchoredPosition = Vector2.zero;
        go.SetActive(false);
        return go;
    }

    // ── Stars ─────────────────────────────────────────────────

    void SetStars(bool active)
    {
        if (active && _stars.Count == 0) CreateStars();
        foreach (var s in _stars) if (s != null) s.SetActive(active);
    }

    void CreateStars()
    {
        for (int i = 0; i < StarCount; i++)
        {
            float size = Random.Range(StarMinSz, StarMaxSz);
            var   go   = MakeImage(_celestialRoot, "Star", size, size);
            var   rt   = go.GetComponent<RectTransform>();
            rt.anchorMin        = new Vector2(Random.value, Random.Range(0.35f, 0.95f));
            rt.anchorMax        = rt.anchorMin;
            rt.anchoredPosition = Vector2.zero;
            float b = Random.Range(0.6f, 1f);
            go.GetComponent<Image>().color = new Color(b, b, b * 0.9f, 1f);
            go.SetActive(false);
            _stars.Add(go);
        }
    }

    // ── Cleanup ───────────────────────────────────────────────

    void OnDestroy()
    {
        // CelestialRoot is parented to the main Canvas, not to this GameObject.
        // Destroy it explicitly so it doesn't linger when WorldEventManager is torn down.
        if (_celestialRoot != null) Destroy(_celestialRoot.gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────

    // All celestial Image components have raycastTarget = false — they must never block clicks.
    GameObject MakeImage(Transform parent, string name, float w, float h)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt       = go.GetComponent<RectTransform>();
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        var img           = go.GetComponent<Image>();
        img.color         = Color.white;
        img.raycastTarget = false;
        return go;
    }
}