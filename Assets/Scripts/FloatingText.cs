using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    public static void Spawn(string text, Vector3 worldPos, Color color)
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        GameObject go = new GameObject("FloatingText", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(canvas.transform, false);

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = 18;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color     = color;
        tmp.richText  = true;
        tmp.alignment = TextAlignmentOptions.Center;

        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 50);

        // Convert world position to screen position
        Vector2 screenPos = Camera.main.WorldToScreenPoint(worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(),
            screenPos, canvas.worldCamera, out Vector2 localPos);
        rt.anchoredPosition = localPos;

        go.AddComponent<FloatingTextMover>().Init(color);
    }
}

public class FloatingTextMover : MonoBehaviour
{
    private float   elapsed  = 0f;
    private float   duration = 1.4f;
    private Vector2 startPos;
    private Color   color;

    public void Init(Color c)
    {
        color    = c;
        startPos = GetComponent<RectTransform>().anchoredPosition;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = elapsed / duration;

        var rt  = GetComponent<RectTransform>();
        rt.anchoredPosition = startPos + Vector2.up * (60f * t);

        var tmp    = GetComponent<TextMeshProUGUI>();
        var col    = color;
        col.a      = Mathf.Lerp(1f, 0f, t * t);
        tmp.color  = col;

        if (elapsed >= duration) Destroy(gameObject);
    }
}