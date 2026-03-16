using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialConsole : MonoBehaviour
{
    public static TutorialConsole Instance;

    // ── Layout ────────────────────────────────────────────────
    private const int   FontBody    = 24;
    private const int   MaxLines    = 4;
    private const float PanelW      = 480f;
    private const float PanelH      = 220f;
    private const float BottomPad   = 20f;
    private const float LeftPad     = 16f;
    private const float DisplayTime = 4f;
    private const float FadeTime    = 1f;

    private GameObject      panel;
    private TextMeshProUGUI logText;
    private Coroutine       fadeCoroutine;
    private Queue<string>   lines = new Queue<string>();

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;
        BuildUI();
    }

    void BuildUI()
    {
        // Canvas found once at startup — no per-frame search
        Canvas canvas = FindFirstObjectByType<Canvas>();

        panel = new GameObject("TutorialConsole", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
        panel.transform.SetParent(canvas.transform, false);

        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin        = new Vector2(0f, 0f);
        rt.anchorMax        = new Vector2(0f, 0f);
        rt.pivot            = new Vector2(0f, 0f);
        rt.anchoredPosition = new Vector2(LeftPad, BottomPad);
        rt.sizeDelta        = new Vector2(PanelW, PanelH);

        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(panel.transform, false);

        var trt = txtObj.GetComponent<RectTransform>();
        trt.anchorMin        = Vector2.zero;
        trt.anchorMax        = Vector2.one;
        trt.sizeDelta        = new Vector2(-20f, -16f);
        trt.anchoredPosition = new Vector2(4f, 0f);

        logText                  = txtObj.GetComponent<TextMeshProUGUI>();
        logText.fontSize         = FontBody;
        logText.color            = Color.white;
        logText.richText         = true;
        logText.alignment        = TextAlignmentOptions.BottomLeft;
        logText.overflowMode     = TextOverflowModes.Truncate;
        logText.textWrappingMode = TextWrappingModes.Normal;

        panel.SetActive(false);
    }

    // ── Public API ────────────────────────────────────────────
    // Hex constants from UIColors — no ToHex() call per log

    public static void Log(string message)   => Instance?.AddLine(message);
    public static void Warn(string message)  => Instance?.AddLine(
        $"<color={UIColors.ConsoleWarning_Hex}>{message}</color>");
    public static void Error(string message) => Instance?.AddLine(
        $"<color={UIColors.ConsoleError_Hex}>{message}</color>");

    // ── Internal ──────────────────────────────────────────────

    void AddLine(string message)
    {
        lines.Enqueue(message);
        if (lines.Count > MaxLines) lines.Dequeue();

        logText.text = string.Join("\n", lines);
        panel.SetActive(true);

        var cg   = panel.GetComponent<CanvasGroup>();
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
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1f, 0f, elapsed / FadeTime);
            yield return null;
        }

        panel.SetActive(false);
        lines.Clear();
    }
}