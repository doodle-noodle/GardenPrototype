using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(FarmPlot))]
public class FarmPlotVisual : MonoBehaviour
{
    private FarmPlot           plot;
    private FarmPlot.PlotState lastState;
    private int                lastStage = -1;

    private GameObject cropObject;
    private GameObject ghostObject;
    private GameObject timerLabel;

    private Renderer plotRenderer;
    private Color    defaultPlotColor;

    private Camera _mainCamera;
    private Canvas _canvas;

    private bool _burialAnimPlaying = false;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        plot             = GetComponent<FarmPlot>();
        lastState        = plot.State;
        plotRenderer     = GetComponent<Renderer>();
        _mainCamera      = Camera.main;
        _canvas          = FindFirstObjectByType<Canvas>();

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

        if (plot.State == FarmPlot.PlotState.Ready && cropObject != null)
        {
            GrowthStageVisual visual = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
            float pulse = 1f + Mathf.Sin(Time.time * 3f) * 0.08f;
            float s     = visual.scale * pulse;
            cropObject.transform.localScale = new Vector3(s, s, s);
        }

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

    // ── Burial animation ──────────────────────────────────────

    public void PlayBurialAnimation(CropData crop)
    {
        GrowthStageVisual stageVisual = GetStageVisual(crop, 0);
        _burialAnimPlaying = true;

        VFXManager.PlayBurial(
            transform.position,
            transform.lossyScale.y * 0.5f,
            stageVisual,
            onComplete: () =>
            {
                _burialAnimPlaying = false;
                RefreshVisual();
            });
    }

    // ── Timer label ───────────────────────────────────────────

    void ShowTimerLabel()
    {
        if (timerLabel != null || _canvas == null) return;

        timerLabel = new GameObject("TimerLabel", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        timerLabel.transform.SetParent(_canvas.transform, false);

        var tmp = timerLabel.GetComponent<TextMeshProUGUI>();
        tmp.fontSize  = 22;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color     = Color.white;

        timerLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 36f);
    }

    void UpdateTimerLabel()
    {
        if (timerLabel == null) return;
        if (plot.State != FarmPlot.PlotState.Growing || plot.ActiveCrop == null)
        {
            HideTimerLabel();
            return;
        }

        float remaining = plot.ActiveCrop.growthStages[plot.CurrentStage].duration
                          - plot.StageTimer;
        for (int i = plot.CurrentStage + 1; i < plot.ActiveCrop.growthStages.Length - 1; i++)
            remaining += plot.ActiveCrop.growthStages[i].duration;

        string timeText;
        if      (remaining >= 3600f) timeText = $"{(int)(remaining / 3600f)}h";
        else if (remaining >= 60f)   timeText = $"{(int)(remaining / 60f)}m";
        else                         timeText = $"{remaining:F1}s";

        timerLabel.GetComponent<TextMeshProUGUI>().text = timeText;

        Vector3 worldPos  = transform.position + Vector3.up * 1.5f;
        Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

        if (screenPos.z < 0f) { timerLabel.SetActive(false); return; }

        timerLabel.SetActive(true);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _canvas.GetComponent<RectTransform>(),
            screenPos, null, out Vector2 localPos);
        timerLabel.GetComponent<RectTransform>().anchoredPosition = localPos;
    }

    void HideTimerLabel()
    {
        if (timerLabel != null) { Destroy(timerLabel); timerLabel = null; }
    }

    // ── Ghost preview ─────────────────────────────────────────

    void ShowGhost()
    {
        HideGhost();
        CropData seed = Inventory.Instance.SelectedSeed;
        if (seed == null) return;

        ghostObject = SpawnVisual(GetStageVisual(seed, 0));

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

        if (_burialAnimPlaying) return;

        if (plot.ActiveCrop == null || plot.CurrentStage < 0) return;

        GrowthStageVisual visual = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
        ClearCropVisual();
        cropObject = SpawnVisual(visual);

        if (plot.State == FarmPlot.PlotState.Ready)
        {
            // Color tinting disabled — preserves real model materials
            // Re-enable when a proper highlight/outline system is added
        }
        else
        {
            StartCoroutine(ScalePop(cropObject, visual.scale));
        }
    }

    void ClearCropVisual()
    {
        if (cropObject == null) return;
        var rend = cropObject.GetComponent<Renderer>();
        if (rend != null) VFXManager.StopGlow(rend);
        Destroy(cropObject);
        cropObject = null;
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

    // ── Helpers ───────────────────────────────────────────────

    GrowthStageVisual GetStageVisual(CropData crop, int stageIndex)
    {
        if (crop.stageVisuals != null && stageIndex < crop.stageVisuals.Length)
            return crop.stageVisuals[stageIndex];
        return new GrowthStageVisual { stageColor = Color.green, scale = 0.3f };
    }

    GameObject SpawnVisual(GrowthStageVisual visual)
    {
        GameObject go = visual.visualPrefab != null
            ? Instantiate(visual.visualPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        go.transform.SetParent(null);

        foreach (var col in go.GetComponentsInChildren<Collider>())
            Destroy(col);

        float   s       = visual.scale;
        Vector3 plotTop = transform.position
                          + Vector3.up * (transform.lossyScale.y * 0.5f);

        go.transform.localScale = new Vector3(s, s, s);

        // Prefab origin sits on plot surface — sphere needs radius offset
        go.transform.position = visual.visualPrefab != null
            ? plotTop
            : plotTop + Vector3.up * (s * 0.5f);

        if (visual.visualPrefab == null)
        {
            var rend = go.GetComponent<Renderer>();
            rend.material = VFXManager.CreateMaterial(visual.stageColor);
        }

        return go;
    }

    public void ForceCleanup()
    {
        _burialAnimPlaying = false;
        ClearCropVisual();
        HideGhost();
        HideTimerLabel();
    }

    // ── Cleanup ───────────────────────────────────────────────

    void OnDestroy()
    {
        if (cropObject  != null) Destroy(cropObject);
        if (ghostObject != null) Destroy(ghostObject);
        if (timerLabel  != null) Destroy(timerLabel);
    }
}