using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialConsole : MonoBehaviour
{
    public static TutorialConsole Instance;

    private GameObject    panel;
    private TextMeshProUGUI logText;
    private Coroutine     fadeCoroutine;

    private const int   MaxLines    = 6;
    private const float DisplayTime = 4f;   // seconds before fading
    private const float FadeTime    = 1f;

    private System.Collections.Generic.Queue<string> lines = new();

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();

        // Outer panel — bottom left
        panel = new GameObject("TutorialConsole", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(16f, 16f);
        rt.sizeDelta        = new Vector2(360f, 160f);

        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        // Text inside the panel
        GameObject txtObj = new GameObject("Text", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(panel.transform, false);

        RectTransform trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin        = Vector2.zero;
        trt.anchorMax        = Vector2.one;
        trt.sizeDelta        = new Vector2(-16f, -12f);
        trt.anchoredPosition = new Vector2(4f, 0f);

        logText = txtObj.GetComponent<TextMeshProUGUI>();
        logText.fontSize  = 12;
        logText.color     = Color.white;
        logText.richText  = true;
        logText.alignment = TextAlignmentOptions.BottomLeft;

        panel.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────

    public static void Log(string message)
    {
        if (Instance == null) return;
        Instance.AddLine(message);
    }

    public static void Warn(string message)
    {
        if (Instance == null) return;
        Instance.AddLine($"<color=#FFB347>{message}</color>");
    }

    public static void Error(string message)
    {
        if (Instance == null) return;
        Instance.AddLine($"<color=#FF6B6B>{message}</color>");
    }

    // ── Internal ──────────────────────────────────────────────

    void AddLine(string message)
    {
        lines.Enqueue(message);
        if (lines.Count > MaxLines) lines.Dequeue();

        logText.text = string.Join("\n", lines);
        panel.SetActive(true);

        // Reset the CanvasGroup alpha
        var cg = panel.GetComponent<CanvasGroup>();
        if (cg == null) cg = panel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOut(cg));
    }

    IEnumerator FadeOut(CanvasGroup cg)
    {
        yield return new WaitForSeconds(DisplayTime);

        float elapsed = 0f;
        while (elapsed < FadeTime)
        {
            elapsed  += Time.deltaTime;
            cg.alpha  = Mathf.Lerp(1f, 0f, elapsed / FadeTime);
            yield return null;
        }

        panel.SetActive(false);
        lines.Clear();
    }
}