using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
    private GameObject      readyLabel;
    private TextMeshProUGUI readyNameTmp;
    private TextMeshProUGUI readyMutTmp;

    private Renderer plotRenderer;
    private Color    _originalPlotColor;
    private Color    _currentBaseColor;

    private Camera _mainCamera;

    private bool _burialAnimPlaying = false;
    private bool _isHovering        = false;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        plot         = GetComponent<FarmPlot>();
        lastState    = plot.State;
        plotRenderer = GetComponent<Renderer>();
        _mainCamera  = Camera.main;
        // Use WorldLabelCanvas instead of main canvas —
        // labels render behind UI at sort order -1

        if (plotRenderer != null)
        {
            _originalPlotColor = plotRenderer.material.color;
            _currentBaseColor  = _originalPlotColor;
        }
    }

    // Helper — parent a label to the world label canvas
    Canvas GetLabelCanvas() => FloatingText.WorldLabelCanvas;

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
        UpdateReadyLabel();
    }

    // ── Hover ─────────────────────────────────────────────────

    public void OnHoverEnter()
    {
        _isHovering = true;
        if (ShopUI.IsOpen) return;

        if (plot.State == FarmPlot.PlotState.Empty)
        {
            if (Inventory.Instance.SelectedSeed == null) return;
            if (Inventory.Instance.GetSeedCount(Inventory.Instance.SelectedSeed) <= 0) return;
            if (plotRenderer != null)
                plotRenderer.material.color = _currentBaseColor * 1.3f;
            ShowGhost();
        }
        else if (plot.State == FarmPlot.PlotState.Growing)
        {
            ShowTimerLabel();
        }
    }

    public void OnHoverStay()
    {
        _isHovering = true;
        if (plot.State == FarmPlot.PlotState.Empty && ghostObject == null) OnHoverEnter();
        if (plot.State == FarmPlot.PlotState.Growing && timerLabel == null) ShowTimerLabel();
    }

    public void OnHoverExit()
    {
        _isHovering = false;
        if (plotRenderer != null)
            plotRenderer.material.color = _currentBaseColor;
        HideGhost();
        HideTimerLabel();
        HideReadyLabel();
    }

    // ── Watering ──────────────────────────────────────────────

    public void ShowWateringEffect()
    {
        StartCoroutine(WateringColorRoutine());
    }

    IEnumerator WateringColorRoutine()
    {
        if (plotRenderer == null) yield break;
        float elapsed    = 0f;
        float duration   = 3f;
        Color startColor = _currentBaseColor;
        Color wetSoil    = new Color(0.25f, 0.12f, 0.04f);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _currentBaseColor = Color.Lerp(startColor, wetSoil, t);
            if (!_isHovering)
                plotRenderer.material.color = _currentBaseColor;
            yield return null;
        }

        _currentBaseColor = wetSoil;
        if (!_isHovering)
            plotRenderer.material.color = _currentBaseColor;
    }

    public void ResetWateringColor()
    {
        _currentBaseColor = _originalPlotColor;
        if (plotRenderer != null && !_isHovering)
            plotRenderer.material.color = _currentBaseColor;
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
        if (timerLabel != null) return;
        var canvas = GetLabelCanvas();
        if (canvas == null) return;

        timerLabel = new GameObject("TimerLabel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        timerLabel.transform.SetParent(canvas.transform, false);

        var tmp           = timerLabel.GetComponent<TextMeshProUGUI>();
        tmp.fontSize      = FloatingText.ScaledFontSize(22);
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = Color.white;
        tmp.raycastTarget = false;

        timerLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 40f);
    }

    void UpdateTimerLabel()
    {
        if (timerLabel == null) return;
        if (plot.State != FarmPlot.PlotState.Growing || plot.ActiveCrop == null)
        {
            HideTimerLabel(); return;
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
        FloatingText.PositionOnCanvas(
            timerLabel.GetComponent<RectTransform>(),
            transform.position + Vector3.up * 1.5f);
    }

    void HideTimerLabel()
    {
        if (timerLabel != null) { Destroy(timerLabel); timerLabel = null; }
    }

    // ── Ready label — hover only ──────────────────────────────

    void ShowReadyLabel()
    {
        if (readyLabel != null || plot.ActiveCrop == null) return;
        var canvas = GetLabelCanvas();
        if (canvas == null) return;

        int fontSize = FloatingText.ScaledFontSize(28);

        readyLabel = new GameObject("ReadyLabel", typeof(RectTransform));
        readyLabel.transform.SetParent(canvas.transform, false);
        readyLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 80f);

        readyNameTmp = MakeTMP(readyLabel.transform, "Name",
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), fontSize);
        readyMutTmp  = MakeTMP(readyLabel.transform, "Muts",
            new Vector2(0f, 0f),   new Vector2(1f, 0.5f), fontSize);

        RefreshReadyLabel();
    }

    void RefreshReadyLabel()
    {
        if (readyLabel == null || plot.ActiveCrop == null) return;
        if (readyNameTmp == null || readyMutTmp == null) return;

        int fontSize = FloatingText.ScaledFontSize(28);

        // Name in rarity color
        Color  rarityColor = RarityUtility.RarityColor(plot.ActiveCrop.rarity);
        string rarityHex   = "#" + ColorUtility.ToHtmlStringRGB(rarityColor);
        readyNameTmp.text     = $"<color={rarityHex}>{plot.ActiveCrop.cropName}</color>";
        readyNameTmp.fontSize = fontSize;

        if (plot.Mutations != null && plot.Mutations.Count > 0)
        {
            var parts = new List<string>();
            foreach (var m in plot.Mutations)
            {
                string hex = "#" + ColorUtility.ToHtmlStringRGB(m.tintColor);
                parts.Add($"<color={hex}>{m.mutationName}</color>");
            }
            readyMutTmp.text     = string.Join(" + ", parts);
            readyMutTmp.fontSize = fontSize;
            readyMutTmp.gameObject.SetActive(true);
            readyLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(500f, 80f);
        }
        else
        {
            readyMutTmp.text = "";
            readyMutTmp.gameObject.SetActive(false);
            readyLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 40f);
        }
    }

    void UpdateReadyLabel()
    {
        if (plot.State != FarmPlot.PlotState.Ready || !_isHovering)
        {
            HideReadyLabel(); return;
        }

        if (readyLabel == null) ShowReadyLabel();
        if (readyLabel == null) return;

        if (plot.StateChanged) RefreshReadyLabel();

        FloatingText.PositionOnCanvas(
            readyLabel.GetComponent<RectTransform>(),
            transform.position + Vector3.up * 2f);
    }

    void HideReadyLabel()
    {
        if (readyLabel != null)
        {
            Destroy(readyLabel);
            readyLabel   = null;
            readyNameTmp = null;
            readyMutTmp  = null;
        }
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
        if (plot.State == FarmPlot.PlotState.Empty) { ClearCropVisual(); return; }
        if (_burialAnimPlaying) return;
        if (plot.ActiveCrop == null || plot.CurrentStage < 0) return;

        GrowthStageVisual visual = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
        ClearCropVisual();
        cropObject = SpawnVisual(visual);

        if (plot.State != FarmPlot.PlotState.Ready)
            StartCoroutine(ScalePop(cropObject, visual.scale));
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
        float elapsed = 0f, duration = 0.25f;
        while (elapsed < duration)
        {
            if (target == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float s = finalScale + Mathf.Sin(t * Mathf.PI) * finalScale * 0.35f;
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
        var go = visual.visualPrefab != null
            ? Instantiate(visual.visualPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        go.transform.SetParent(null);
        foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);

        float   s       = visual.scale;
        Vector3 plotTop = transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);

        go.transform.localScale = new Vector3(s, s, s);
        go.transform.position   = visual.visualPrefab != null
            ? plotTop : plotTop + Vector3.up * (s * 0.5f);

        if (visual.visualPrefab == null)
            go.GetComponent<Renderer>().material =
                VFXManager.CreateMaterial(visual.stageColor);

        return go;
    }

    TextMeshProUGUI MakeTMP(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, int fontSize)
    {
        var go = new GameObject(name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt       = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var tmp                    = go.GetComponent<TextMeshProUGUI>();
        tmp.richText               = true;
        tmp.fontSize               = fontSize;
        tmp.fontStyle              = FontStyles.Bold;
        tmp.alignment              = TextAlignmentOptions.Center;
        tmp.color                  = Color.white;
        tmp.enableWordWrapping     = false;
        tmp.raycastTarget          = false;
        return tmp;
    }

    public void ForceCleanup()
    {
        _burialAnimPlaying = false;
        _isHovering        = false;
        ResetWateringColor();
        ClearCropVisual();
        HideGhost();
        HideTimerLabel();
        HideReadyLabel();
    }

    void OnDestroy()
    {
        if (cropObject  != null) Destroy(cropObject);
        if (ghostObject != null) Destroy(ghostObject);
        if (timerLabel  != null) Destroy(timerLabel);
        if (readyLabel  != null) Destroy(readyLabel);
    }
}