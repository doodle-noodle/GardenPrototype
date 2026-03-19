using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class FloatingText
{
    private static readonly Queue<GameObject> Pool = new Queue<GameObject>();
    private static Camera  _camera;

    // Dedicated canvas at sort order -1 — always renders behind main UI (sort 0)
    private static Canvas _canvas;

    // Exposed so FarmPlotVisual can parent its world labels here too
    public static Canvas WorldLabelCanvas
    {
        get { EnsureCanvas(); return _canvas; }
    }

    // ── Public API ────────────────────────────────────────────

    public static void Spawn(string text, Vector3 worldPos, Color color, int fontSize = 18)
    {
        EnsureCanvas();
        if (_canvas == null) return;

        var go  = GetFromPool();
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = ScaledFontSize(fontSize);
        tmp.fontStyle = FontStyles.Bold;

        PositionAtWorld(go.GetComponent<RectTransform>(), worldPos);

        go.SetActive(true);
        go.GetComponent<FloatingTextMover>().Init(color);
    }

    // Positions any RectTransform on the world label canvas at a world position
    public static void PositionOnCanvas(RectTransform rt, Vector3 worldPos)
    {
        EnsureCanvas();
        if (_camera == null || _canvas == null) return;
        Vector2 screen = _camera.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(), screen, null, out var local);
        rt.anchoredPosition = local;
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

    // ── Internals ─────────────────────────────────────────────

    static void PositionAtWorld(RectTransform rt, Vector3 worldPos)
    {
        rt.sizeDelta = new Vector2(200f, 80f);
        PositionOnCanvas(rt, worldPos);
    }

    static void EnsureCanvas()
    {
        if (_camera == null) _camera = Camera.main;
        if (_canvas != null) return;

        var go = new GameObject("WorldLabelCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Object.DontDestroyOnLoad(go);

        _canvas = go.GetComponent<Canvas>();
        _canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = -1; // behind main UI canvas (sort order 0)

        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight  = 0.5f;
        // No GraphicRaycaster — world labels never receive clicks
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
        EnsureCanvas();
        var go = new GameObject("FloatingText",
            typeof(RectTransform), typeof(CanvasRenderer),
            typeof(TextMeshProUGUI), typeof(FloatingTextMover));
        go.transform.SetParent(_canvas.transform, false);

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