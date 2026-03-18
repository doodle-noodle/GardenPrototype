using System.Collections.Generic;
using UnityEngine;
using TMPro;

public static class FloatingText
{
    private static readonly Queue<GameObject> Pool = new Queue<GameObject>();
    private static Canvas _cachedCanvas;
    private static Camera _cachedCamera;

    // ── Public API ────────────────────────────────────────────

    public static void Spawn(string text, Vector3 worldPos, Color color, int fontSize = 18)
    {
        EnsureCache();
        if (_cachedCanvas == null) return;

        GameObject go  = GetFromPool();
        var        tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.color     = color;
        tmp.fontSize  = ScaledFontSize(fontSize);
        tmp.fontStyle = FontStyles.Bold;

        ApplyOutline(tmp, 0.2f);

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200f, 80f);

        Vector2 screenPos = _cachedCamera.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _cachedCanvas.GetComponent<RectTransform>(),
            screenPos, _cachedCanvas.worldCamera, out Vector2 localPos);
        rt.anchoredPosition = localPos;

        go.SetActive(true);
        go.GetComponent<FloatingTextMover>().Init(color);
    }

    // Scales font size with zoom so text stays readable at all distances
    public static int ScaledFontSize(int baseSize)
    {
        if (DioramaCamera.Instance == null) return baseSize;
        float t = Mathf.InverseLerp(
            DioramaCamera.Instance.maxZoom,
            DioramaCamera.Instance.minZoom,
            DioramaCamera.Instance.zoomHeight);
        return Mathf.RoundToInt(Mathf.Lerp(baseSize * 0.7f, baseSize * 1.1f, t));
    }

    // Applies a black outline via TMP's shader properties directly.
    // Creates an instanced material so changes don't affect other TMP objects.
    // Width is clamped to 0.4f max — above that the outline eats into the glyphs.
    public static void ApplyOutline(TextMeshProUGUI tmp, float width = 0.2f)
    {
        if (tmp == null) return;

        // Instance the material so this object's outline is independent
        tmp.fontMaterial = new Material(tmp.fontMaterial);

        var mat = tmp.fontMaterial;
        mat.EnableKeyword(ShaderUtilities.Keyword_Outline);
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, Mathf.Clamp(width, 0f, 0.4f));
        mat.SetColor(ShaderUtilities.ID_OutlineColor, Color.black);
        tmp.UpdateMeshPadding();
    }

    public static void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        Pool.Enqueue(go);
    }

    // ── Pool internals ────────────────────────────────────────

    static void EnsureCache()
    {
        if (_cachedCanvas == null)
            _cachedCanvas = Object.FindFirstObjectByType<Canvas>();
        if (_cachedCamera == null)
            _cachedCamera = Camera.main;
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
        GameObject go = new GameObject("FloatingText", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(FloatingTextMover));
        go.transform.SetParent(_cachedCanvas.transform, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.richText  = true;

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

        var rt = GetComponent<RectTransform>();
        rt.anchoredPosition = startPos + Vector2.up * (60f * t);

        var tmp   = GetComponent<TextMeshProUGUI>();
        var col   = startColor;
        col.a     = Mathf.Lerp(1f, 0f, t * t);
        tmp.color = col;

        if (elapsed >= duration)
            FloatingText.ReturnToPool(gameObject);
    }
}