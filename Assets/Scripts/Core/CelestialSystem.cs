using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CelestialSystem : MonoBehaviour
{
    private const int   StarCount = 80;
    private const float MoonSize  = 80f;
    private const float StarMinSz = 3f;
    private const float StarMaxSz = 9f;

    private Canvas           _canvas;
    private GameObject       _moon;
    private List<GameObject> _stars = new List<GameObject>();

    void Awake()
    {
        var go = new GameObject("CelestialCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(go);

        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -10;

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        // No GraphicRaycaster — celestial elements never need to receive clicks
    }

    public void SetCelestials(bool hasMoon, Color moonColor, bool hasStars)
    {
        SetMoon(hasMoon, moonColor);
        SetStars(hasStars);
    }

    void SetMoon(bool active, Color color)
    {
        if (active && _moon == null) _moon = CreateMoon();
        if (_moon == null) return;
        _moon.SetActive(active);
        if (active) _moon.GetComponent<Image>().color = color;
    }

    GameObject CreateMoon()
    {
        var go = MakeImage(_canvas.transform, "Moon", MoonSize, MoonSize);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0.75f, 0.75f);
        rt.anchorMax        = new Vector2(0.75f, 0.75f);
        rt.anchoredPosition = Vector2.zero;
        go.SetActive(false);
        return go;
    }

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
            var   go   = MakeImage(_canvas.transform, "Star", size, size);
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

    // Creates an Image with raycastTarget OFF — celestial objects are purely visual
    GameObject MakeImage(Transform parent, string name, float w, float h)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        var img = go.GetComponent<Image>();
        img.color         = Color.white;
        img.raycastTarget = false; // never blocks clicks
        return go;
    }

    void OnDestroy()
    {
        if (_canvas != null) Destroy(_canvas.gameObject);
    }
}