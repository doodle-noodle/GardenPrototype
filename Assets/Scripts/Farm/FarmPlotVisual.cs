using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(FarmPlot))]
public class FarmPlotVisual : MonoBehaviour
{
    private FarmPlot           plot;
    private FarmPlot.PlotState lastState;
    private int                lastStage = -1;

    private GameObject      cropObject;
    private GameObject      ghostObject;
    private GameObject      timerLabel;
    private Renderer        plotRenderer;
    private Color           defaultPlotColor;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        plot             = GetComponent<FarmPlot>();
        lastState        = plot.State;
        plotRenderer     = GetComponent<Renderer>();

        if (plotRenderer != null)
            defaultPlotColor = plotRenderer.material.color;
    }

    // ── Update ────────────────────────────────────────────────

    void Update()
    {
        if (plot.StateChanged || plot.CurrentStage != lastStage || plot.State != lastState)
        {
            lastState = plot.State;
            lastStage = plot.CurrentStage;
            RefreshVisual();
        }

        // Pulse when ready
        if (plot.State == FarmPlot.PlotState.Ready && cropObject != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.08f;
            GrowthStage stage = plot.ActiveCrop.growthStages[plot.CurrentStage];
            float s = stage.scale * pulse;
            cropObject.transform.localScale = new Vector3(s, s, s);
        }

        // Update timer label text every frame while visible
        UpdateTimerLabel();
    }

    // ── Hover ─────────────────────────────────────────────────

    public void OnHoverEnter()
    {
        if (ShopUI.IsOpen) return;

        if (plot.State == FarmPlot.PlotState.Empty)
        {
            if (Inventory.Instance.SelectedSeed == null) return;
            if (Inventory.Instance.GetSeedCount(Inventory.Instance.SelectedSeed) <= 0) return;
            if (plotRenderer != null)
                plotRenderer.material.color = defaultPlotColor * 1.3f;
            ShowGhost();
        }
        else if (plot.State == FarmPlot.PlotState.Growing)
        {
            ShowTimerLabel();
        }
    }

    public void OnHoverStay()
    {
        if (plot.State == FarmPlot.PlotState.Empty && ghostObject == null)
            OnHoverEnter();

        if (plot.State == FarmPlot.PlotState.Growing && timerLabel == null)
            ShowTimerLabel();
    }

    public void OnHoverExit()
    {
        if (plotRenderer != null)
            plotRenderer.material.color = defaultPlotColor;

        HideGhost();
        HideTimerLabel();
    }

    // ── Timer label ───────────────────────────────────────────

    void ShowTimerLabel()
    {
        if (timerLabel != null) return;

        Canvas canvas = FindFirstObjectByType<Canvas>();

        timerLabel = new GameObject("TimerLabel", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        timerLabel.transform.SetParent(canvas.transform, false);

        var tmp = timerLabel.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        var rt = timerLabel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(120f, 36f);
    }

    void UpdateTimerLabel()
    {
        if (timerLabel == null) return;
        if (plot.State != FarmPlot.PlotState.Growing || plot.ActiveCrop == null)
        {
            HideTimerLabel();
            return;
        }

        // Format time remaining
        float remaining = plot.ActiveCrop.growthStages[plot.CurrentStage].duration - plot.StageTimer;
        for (int i = plot.CurrentStage + 1; i < plot.ActiveCrop.growthStages.Length - 1; i++)
            remaining += plot.ActiveCrop.growthStages[i].duration;

        string timeText;
        if (remaining >= 3600f)
            timeText = $"{(int)(remaining / 3600f)}h";
        else if (remaining >= 60f)
            timeText = $"{(int)(remaining / 60f)}m";
        else
            timeText = $"{remaining:F1}s";

        timerLabel.GetComponent<TextMeshProUGUI>().text = timeText;

        // Position above the crop in world space
        Vector3 worldPos   = transform.position + Vector3.up * 1.5f;
        Vector3 screenPos  = Camera.main.WorldToScreenPoint(worldPos);

        // Only show if in front of camera
        if (screenPos.z < 0f)
        {
            timerLabel.SetActive(false);
            return;
        }

        timerLabel.SetActive(true);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            FindFirstObjectByType<Canvas>().GetComponent<RectTransform>(),
            screenPos, null, out Vector2 localPos);

        timerLabel.GetComponent<RectTransform>().anchoredPosition = localPos;
    }

    void HideTimerLabel()
    {
        if (timerLabel != null)
        {
            Destroy(timerLabel);
            timerLabel = null;
        }
    }

    // ── Ghost ─────────────────────────────────────────────────

    void ShowGhost()
    {
        HideGhost();

        CropData seed = Inventory.Instance.SelectedSeed;
        if (seed == null || seed.growthStages.Length == 0) return;

        ghostObject = SpawnVisual(seed.growthStages[0]);

        if (PlacementController.GhostValid != null)
            ghostObject.GetComponent<Renderer>().material = PlacementController.GhostValid;
    }

    void HideGhost()
    {
        if (ghostObject != null) { Destroy(ghostObject); ghostObject = null; }
    }

    // ── Crop visual ───────────────────────────────────────────

    void RefreshVisual()
    {
        if (plot.State == FarmPlot.PlotState.Empty)
        {
            ClearCropVisual();
            return;
        }

        if (plot.ActiveCrop == null || plot.CurrentStage < 0) return;

        GrowthStage stage = plot.ActiveCrop.growthStages[plot.CurrentStage];
        ClearCropVisual();
        cropObject = SpawnVisual(stage);

        if (plot.State == FarmPlot.PlotState.Ready)
        {
            var rend = cropObject.GetComponent<Renderer>();
            if (rend != null)
                rend.material.color = Color.Lerp(stage.stageColor, Color.yellow, 0.4f);
        }

        StartCoroutine(ScalePop(cropObject, stage.scale));
    }

    void ClearCropVisual()
    {
        if (cropObject != null) { Destroy(cropObject); cropObject = null; }
    }

    // ── Scale pop ─────────────────────────────────────────────

    IEnumerator ScalePop(GameObject target, float finalScale)
    {
        if (target == null) yield break;

        float elapsed  = 0f;
        float duration = 0.25f;

        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t     = elapsed / duration;
            float curve = Mathf.Sin(t * Mathf.PI);
            float s     = finalScale + curve * finalScale * 0.35f;
            target.transform.localScale = new Vector3(s, s, s);
            yield return null;
        }

        if (target != null)
            target.transform.localScale = Vector3.one * finalScale;
    }

    // ── Shared spawn helper ───────────────────────────────────

    GameObject SpawnVisual(GrowthStage stage)
    {
        GameObject go = stage.visualPrefab != null
            ? Instantiate(stage.visualPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        if (stage.visualPrefab == null)
            go.GetComponent<Renderer>().material.color = stage.stageColor;

        go.transform.SetParent(null);

        foreach (var col in go.GetComponentsInChildren<Collider>())
            Destroy(col);

        float s = stage.scale;
        go.transform.localScale = new Vector3(s, s, s);

        Vector3 plotTop = transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);
        go.transform.position = plotTop + Vector3.up * (s * 0.5f);

        return go;
    }

    // ── Cleanup ───────────────────────────────────────────────

    void OnDestroy()
    {
        if (cropObject  != null) Destroy(cropObject);
        if (ghostObject != null) Destroy(ghostObject);
        if (timerLabel  != null) Destroy(timerLabel);
    }
}