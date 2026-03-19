using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; 
using TMPro;

public static class FloatingText
{
    private static readonly Queue<GameObject> Pool          = new Queue<GameObject>();
    private static Canvas                     _floatCanvas;  // dedicated low-sort canvas
    private static Canvas                     _mainCanvas;
    private static Camera                     _cachedCamera;

    public static void Spawn(string text, Vector3 worldPos, Color color, int fontSize = 18)
    {
        EnsureCache();
        if (_floatCanvas == null) return;

        var go  = GetFromPool();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = ScaledFontSize(fontSize);
        tmp.fontStyle = FontStyles.Bold;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 80f);

        // Use main canvas for world-to-screen conversion
        Vector2 screenPos = _cachedCamera.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _floatCanvas.GetComponent<RectTransform>(),
            screenPos, null, out Vector2 localPos);
        rt.anchoredPosition = localPos;

        go.SetActive(true);
        go.GetComponent<FloatingTextMover>().Init(color);
    }

    public static int ScaledFontSize(int baseSize)
    {
        if (DioramaCamera.Instance == null) return baseSize;
        float t = Mathf.InverseLerp(
            DioramaCamera.Instance.maxZoom,
            DioramaCamera.Instance.minZoom,
            DioramaCamera.Instance.zoomHeight);
        return Mathf.RoundToInt(Mathf.Lerp(baseSize * 0.7f, baseSize * 1.1f, t));
    }

    public static void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        Pool.Enqueue(go);
    }

    // ── Cache / pool ──────────────────────────────────────────

    static void EnsureCache()
    {
        if (_cachedCamera == null) _cachedCamera = Camera.main;
        if (_floatCanvas != null) return;

        // Find main canvas — it keeps its default sort order (0)
        _mainCanvas = Object.FindFirstObjectByType<Canvas>();

        // Dedicated canvas for floating texts at sort order -1
        // This renders BEHIND the main UI canvas (sort order 0)
        var go = new GameObject("FloatingTextCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Object.DontDestroyOnLoad(go);

        _floatCanvas = go.GetComponent<Canvas>();
        _floatCanvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _floatCanvas.sortingOrder = -1; // always behind main UI

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        // No GraphicRaycaster — floating texts never need to receive clicks
    }

    static GameObject GetFromPool()
    {
        while (Pool.Count > 0)
        {
            var go = Pool.Dequeue();
            if (go != null) return go;
        }
        return CreateNew();
    }

    static GameObject CreateNew()
    {
        var go = new GameObject("FloatingText",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI), typeof(FloatingTextMover));
        go.transform.SetParent(_floatCanvas.transform, false);

        var tmp           = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize      = 18;
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.richText      = true;
        tmp.raycastTarget = false;

        go.SetActive(false);
        return go;
    }
}

public class FloatingTextMover : MonoBehaviour
{
    private float   elapsed  = 0f;
    private float   duration = 1.4f;
    private Vector2 startPos;
    private Color   startColor;

    public void Init(Color color)
    {
        elapsed    = 0f;
        startColor = color;
        startPos   = GetComponent<RectTransform>().anchoredPosition;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        GetComponent<RectTransform>().anchoredPosition =
            startPos + Vector2.up * (60f * t);

        var col = startColor;
        col.a = Mathf.Lerp(1f, 0f, t * t);
        GetComponent<TextMeshProUGUI>().color = col;

        if (elapsed >= duration) FloatingText.ReturnToPool(gameObject);
    }
}