using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

[RequireComponent(typeof(FarmPlot))]
public class FarmPlotVisual : MonoBehaviour
{
    private const float FertilizerOverlayOffset = 0.005f;
    private const float WateringOverlayOffset   = 0.010f;
    private static readonly Color WateringOverlayColor = new Color(0.10f, 0.06f, 0.02f, 0.55f);

    private FarmPlot           plot;
    private FarmPlot.PlotState lastState;
    private int                lastStage          = -1;
    private bool               _lastReadyToEvolve = false;

    private GameObject      cropObject;
    private GameObject      ghostObject;
    private GameObject      timerLabel;
    private GameObject      readyLabel;
    private GameObject      evolvedNameLabel;
    private TextMeshProUGUI readyNameTmp;
    private TextMeshProUGUI readyMutTmp;

    private Renderer plotRenderer;
    private Color    _originalPlotColor;
    private Color    _currentBaseColor;

    private GameObject _fertilizerOverlayGo;
    private GameObject _wateringOverlayGo;

    public Color OriginalPlotColor => _originalPlotColor;

    private Camera _mainCamera;
    private bool   _burialAnimPlaying = false;
    private bool   _isHovering        = false;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        plot         = GetComponent<FarmPlot>();
        lastState    = plot.State;
        plotRenderer = GetComponent<Renderer>();
        _mainCamera  = Camera.main;

        if (plotRenderer != null)
        {
            _originalPlotColor = plotRenderer.material.color;
            _currentBaseColor  = _originalPlotColor;
        }
    }

    Canvas GetLabelCanvas() => FloatingText.WorldLabelCanvas;

    // ── Update ────────────────────────────────────────────────

    void Update()
    {
        bool stateOrStageChanged = plot.StateChanged
            || plot.CurrentStage != lastStage
            || plot.State != lastState;
        bool evolveChanged = (plot.ReadyToEvolveWith != null) != _lastReadyToEvolve;

        if (stateOrStageChanged || evolveChanged)
        {
            lastState          = plot.State;
            lastStage          = plot.CurrentStage;
            _lastReadyToEvolve = plot.ReadyToEvolveWith != null;
            RefreshVisual();
        }

        if (plot.State == FarmPlot.PlotState.Ready && plot.ReadyToEvolveWith == null
            && cropObject != null)
        {
            GrowthStageVisual v = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
            float s = v.scale * (1f + Mathf.Sin(Time.time * 3f) * 0.08f);
            cropObject.transform.localScale = new Vector3(s, s, s);
        }

        if (plot.State == FarmPlot.PlotState.Evolved && cropObject != null)
        {
            GrowthStageVisual v = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
            float s = v.scale * (1f + Mathf.Sin(Time.time * 1.5f) * 0.04f);
            cropObject.transform.localScale = new Vector3(s, s, s);
        }

        UpdateTimerLabel();
        UpdateReadyLabel();
        UpdateEvolvedNameLabel();
    }

    // ── Hover ─────────────────────────────────────────────────

    public void OnHoverEnter()
    {
        _isHovering = true;
        if (ShopUI.IsOpen) return;
        switch (plot.State)
        {
            case FarmPlot.PlotState.Empty:
                if (Inventory.Instance.SelectedSeed == null) return;
                if (Inventory.Instance.GetSeedCount(Inventory.Instance.SelectedSeed) <= 0) return;
                if (plotRenderer != null)
                    plotRenderer.material.color = _currentBaseColor * 1.3f;
                ShowGhost();
                break;
            case FarmPlot.PlotState.Growing:
            case FarmPlot.PlotState.Regrowing:
                ShowTimerLabel();
                break;
        }
    }

    public void OnHoverStay()
    {
        _isHovering = true;
        if (plot.State == FarmPlot.PlotState.Empty && ghostObject == null) OnHoverEnter();
        if ((plot.State == FarmPlot.PlotState.Growing ||
             plot.State == FarmPlot.PlotState.Regrowing) && timerLabel == null) ShowTimerLabel();
    }

    public void OnHoverExit()
    {
        _isHovering = false;
        if (plotRenderer != null) plotRenderer.material.color = _currentBaseColor;
        HideGhost(); HideTimerLabel(); HideReadyLabel();
    }

    // ── Soil color ────────────────────────────────────────────

    public void ApplySoilColor(Color? color)
    {
        _currentBaseColor = color ?? _originalPlotColor;
        if (plotRenderer != null && !_isHovering)
            plotRenderer.material.color = _currentBaseColor;
    }

    public void ResetPlotColor()
    {
        _currentBaseColor = plot.CurrentSoil?.soilColor ?? _originalPlotColor;
        if (plotRenderer != null && !_isHovering)
            plotRenderer.material.color = _currentBaseColor;
    }

    // ── Overlay quads ─────────────────────────────────────────

    GameObject CreateOverlayQuad(string name, float heightOffset)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        float plotHalfY = transform.lossyScale.y * 0.5f;
        go.transform.position = new Vector3(
            transform.position.x,
            transform.position.y + plotHalfY + heightOffset,
            transform.position.z);
        go.transform.rotation   = Quaternion.Euler(90f, transform.eulerAngles.y, 0f);
        go.transform.localScale = new Vector3(transform.lossyScale.x, transform.lossyScale.z, 1f);
        return go;
    }

    static Material CreateTransparentMaterial(Color color, Texture2D texture = null)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                  ?? Shader.Find("Unlit/Transparent")
                  ?? Shader.Find("Standard");
        var mat = new Material(shader);
        mat.SetFloat("_Surface",  1f);
        mat.SetFloat("_Blend",    0f);
        mat.SetFloat("_ZWrite",   0f);
        mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        mat.SetColor("_BaseColor", color);
        if (texture != null) mat.SetTexture("_BaseMap", texture);
        mat.renderQueue = 3002;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        return mat;
    }

    public void ShowFertilizerOverlay(FertilizerData data)
    {
        if (_fertilizerOverlayGo != null) return;
        _fertilizerOverlayGo = CreateOverlayQuad("FertilizerOverlay", FertilizerOverlayOffset);
        var mat = CreateTransparentMaterial(new Color(1f, 1f, 1f, 0.90f), data.overlayTexture);
        _fertilizerOverlayGo.GetComponent<Renderer>().material = mat;
    }

    public void HideFertilizerOverlay()
    {
        if (_fertilizerOverlayGo != null) { Destroy(_fertilizerOverlayGo); _fertilizerOverlayGo = null; }
    }

    public void ShowWateringOverlay()
    {
        if (_wateringOverlayGo != null) return;
        _wateringOverlayGo = CreateOverlayQuad("WateringOverlay", WateringOverlayOffset);
        var mat = CreateTransparentMaterial(WateringOverlayColor);
        _wateringOverlayGo.GetComponent<Renderer>().material = mat;
    }

    public void HideWateringOverlay()
    {
        if (_wateringOverlayGo != null) { Destroy(_wateringOverlayGo); _wateringOverlayGo = null; }
    }

    public void HideAllOverlays()
    {
        HideFertilizerOverlay();
        HideWateringOverlay();
    }

    // ── Burial animation ──────────────────────────────────────

    public void PlayBurialAnimation(CropData crop)
    {
        GrowthStageVisual sv = GetStageVisual(crop, 0);
        _burialAnimPlaying = true;
        VFXManager.PlayBurial(transform.position, transform.lossyScale.y * 0.5f, sv,
            onComplete: () => { _burialAnimPlaying = false; RefreshVisual(); });
    }

    // ── Visual refresh ────────────────────────────────────────

    void RefreshVisual()
    {
        var rend = cropObject?.GetComponent<Renderer>();

        switch (plot.State)
        {
            case FarmPlot.PlotState.Empty:
                if (rend != null) VFXManager.StopGlow(rend);
                ClearCropVisual();
                return;

            case FarmPlot.PlotState.Regrowing:
                if (_burialAnimPlaying) return;
                if (rend != null) VFXManager.StopGlow(rend);
                ClearCropVisual();
                if (plot.ActiveCrop?.strippedPrefab != null)
                {
                    var lv = GetStageVisual(plot.ActiveCrop,
                        plot.ActiveCrop.growthStages.Length - 1);
                    cropObject = SpawnVisual(new GrowthStageVisual
                    {
                        visualPrefab = plot.ActiveCrop.strippedPrefab,
                        stageColor   = lv.stageColor,
                        scale        = lv.scale
                    });
                }
                return;

            case FarmPlot.PlotState.Ready:
                if (_burialAnimPlaying) return;
                if (plot.ActiveCrop == null || plot.CurrentStage < 0) return;

                var visual = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
                if (cropObject == null)
                {
                    ClearCropVisual();
                    cropObject = SpawnVisual(visual);
                }

                var cropRend = cropObject?.GetComponent<Renderer>();
                if (cropRend == null) return;

                if (plot.ReadyToEvolveWith != null)
                {
                    VFXManager.StopGlow(cropRend);
                    VFXManager.StartEvolutionGlow(cropRend);
                }
                else
                {
                    VFXManager.StopGlow(cropRend);
                    //VFXManager.StartGlow(cropRend); // disabled - harvest glow turned off
                }
                return;

            case FarmPlot.PlotState.Evolved:
                if (cropObject == null)
                {
                    var baseV = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
                    ClearCropVisual();
                    cropObject = SpawnVisual(
                        plot.EvolvedCharacter?.evolvedModelPrefab != null
                            ? new GrowthStageVisual
                              {
                                  visualPrefab = plot.EvolvedCharacter.evolvedModelPrefab,
                                  stageColor   = baseV.stageColor,
                                  scale        = baseV.scale
                              }
                            : baseV);
                }
                if (rend != null) VFXManager.StopGlow(rend);
                ShowEvolvedNameLabel();
                return;

            default:
                if (_burialAnimPlaying) return;
                if (plot.ActiveCrop == null || plot.CurrentStage < 0) return;
                var vis = GetStageVisual(plot.ActiveCrop, plot.CurrentStage);
                if (rend != null) VFXManager.StopGlow(rend);
                ClearCropVisual();
                cropObject = SpawnVisual(vis);
                StartCoroutine(ScalePop(cropObject, vis.scale));
                return;
        }
    }

    void ClearCropVisual()
    {
        if (cropObject == null) return;
        var r = cropObject.GetComponent<Renderer>();
        if (r != null) VFXManager.StopGlow(r);
        Destroy(cropObject);
        cropObject = null;
    }

    // ── Timer label ───────────────────────────────────────────

    void ShowTimerLabel()
    {
        if (timerLabel != null) return;
        var canvas = GetLabelCanvas(); if (canvas == null) return;
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
        string text = "";
        if (plot.State == FarmPlot.PlotState.Growing && plot.ActiveCrop != null)
        {
            float rem = plot.ActiveCrop.growthStages[plot.CurrentStage].duration - plot.StageTimer;
            for (int i = plot.CurrentStage + 1; i < plot.ActiveCrop.growthStages.Length - 1; i++)
                rem += plot.ActiveCrop.growthStages[i].duration;
            text = FormatTime(rem);
        }
        else if (plot.State == FarmPlot.PlotState.Regrowing)
        {
            text = FormatTime(plot.RegrowTimer);
        }
        else { HideTimerLabel(); return; }

        timerLabel.GetComponent<TextMeshProUGUI>().text = text;
        FloatingText.PositionOnCanvas(timerLabel.GetComponent<RectTransform>(),
            transform.position + Vector3.up * 1.5f);
    }

    void HideTimerLabel()
    {
        if (timerLabel != null) { Destroy(timerLabel); timerLabel = null; }
    }

    // ── Ready label ───────────────────────────────────────────

    void ShowReadyLabel()
    {
        if (readyLabel != null || plot.ActiveCrop == null) return;
        var canvas = GetLabelCanvas(); if (canvas == null) return;

        int fs = FloatingText.ScaledFontSize(28);
        readyLabel = new GameObject("ReadyLabel", typeof(RectTransform));
        readyLabel.transform.SetParent(canvas.transform, false);
        readyLabel.GetComponent<RectTransform>().sizeDelta = new Vector2(400f, 80f);
        readyNameTmp = MakeTMP(readyLabel.transform, "Name",
            new Vector2(0f, 0.5f), new Vector2(1f, 1f), fs);
        readyMutTmp  = MakeTMP(readyLabel.transform, "Muts",
            new Vector2(0f, 0f),   new Vector2(1f, 0.5f), fs);
        RefreshReadyLabel();
    }

    void RefreshReadyLabel()
    {
        if (readyLabel == null || plot.ActiveCrop == null) return;
        if (readyNameTmp == null || readyMutTmp == null) return;
        int    fs  = FloatingText.ScaledFontSize(28);
        string hex = "#" + ColorUtility.ToHtmlStringRGB(
            RarityUtility.RarityColor(plot.ActiveCrop.rarity));
        readyNameTmp.text     = $"<color={hex}>{plot.ActiveCrop.cropName}</color>";
        readyNameTmp.fontSize = fs;
        if (plot.Mutations != null && plot.Mutations.Count > 0)
        {
            var parts = new List<string>();
            foreach (var m in plot.Mutations)
                parts.Add($"<color=#{ColorUtility.ToHtmlStringRGB(m.tintColor)}>{m.mutationName}</color>");
            readyMutTmp.text     = string.Join(" + ", parts);
            readyMutTmp.fontSize = fs;
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
        { HideReadyLabel(); return; }
        if (readyLabel == null) ShowReadyLabel();
        if (readyLabel == null) return;
        if (plot.StateChanged) RefreshReadyLabel();
        FloatingText.PositionOnCanvas(readyLabel.GetComponent<RectTransform>(),
            transform.position + Vector3.up * 2f);
    }

    void HideReadyLabel()
    {
        if (readyLabel != null)
        {
            Destroy(readyLabel);
            readyLabel = null; readyNameTmp = null; readyMutTmp = null;
        }
    }

    // ── Evolved name label ────────────────────────────────────

    void ShowEvolvedNameLabel()
    {
        if (evolvedNameLabel != null || plot.EvolvedCharacter == null) return;
        var canvas = GetLabelCanvas(); if (canvas == null) return;
        evolvedNameLabel = new GameObject("EvolvedNameLabel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        evolvedNameLabel.transform.SetParent(canvas.transform, false);
        var rt       = evolvedNameLabel.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(220f, 44f);
        var tmp           = evolvedNameLabel.GetComponent<TextMeshProUGUI>();
        tmp.text          = plot.EvolvedCharacter.characterName;
        tmp.fontSize      = FloatingText.ScaledFontSize(24);
        tmp.fontStyle     = FontStyles.Bold;
        tmp.alignment     = TextAlignmentOptions.Center;
        tmp.color         = UIColors.EvolvedLabel;
        tmp.raycastTarget = false;
        tmp.richText      = true;
    }

    void UpdateEvolvedNameLabel()
    {
        if (plot.State != FarmPlot.PlotState.Evolved) return;
        if (evolvedNameLabel == null) { ShowEvolvedNameLabel(); return; }
        FloatingText.PositionOnCanvas(evolvedNameLabel.GetComponent<RectTransform>(),
            transform.position + Vector3.up * 2.5f);
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

    void HideGhost() { if (ghostObject != null) { Destroy(ghostObject); ghostObject = null; } }

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
        if (target != null) target.transform.localScale = Vector3.one * finalScale;
    }

    // ── Helpers ───────────────────────────────────────────────

    GrowthStageVisual GetStageVisual(CropData crop, int idx)
    {
        if (crop?.stageVisuals != null && idx < crop.stageVisuals.Length)
            return crop.stageVisuals[idx];
        return new GrowthStageVisual { stageColor = Color.green, scale = 0.3f };
    }

    GameObject SpawnVisual(GrowthStageVisual visual)
    {
        var go = visual.visualPrefab != null
            ? Instantiate(visual.visualPrefab)
            : GameObject.CreatePrimitive(PrimitiveType.Sphere);

        go.transform.SetParent(null);
        foreach (var col in go.GetComponentsInChildren<Collider>()) Destroy(col);

        float   s   = visual.scale;
        Vector3 top = transform.position + Vector3.up * (transform.lossyScale.y * 0.5f);
        go.transform.localScale = new Vector3(s, s, s);
        go.transform.position   = visual.visualPrefab != null
            ? top : top + Vector3.up * (s * 0.5f);

        if (visual.visualPrefab == null)
            go.GetComponent<Renderer>().material = VFXManager.CreateMaterial(visual.stageColor);
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
        var tmp              = go.GetComponent<TextMeshProUGUI>();
        tmp.richText         = true;
        tmp.fontSize         = fontSize;
        tmp.fontStyle        = FontStyles.Bold;
        tmp.alignment        = TextAlignmentOptions.Center;
        tmp.color            = Color.white;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.raycastTarget    = false;
        return tmp;
    }

    static string FormatTime(float seconds)
    {
        if      (seconds >= 3600f) return $"{(int)(seconds / 3600f)}h";
        else if (seconds >= 60f)   return $"{(int)(seconds / 60f)}m";
        else                       return $"{seconds:F1}s";
    }

    // ── Cleanup ───────────────────────────────────────────────

    public void ForceCleanup()
    {
        _burialAnimPlaying = false;
        _isHovering        = false;
        ResetPlotColor();
        HideAllOverlays();
        ClearCropVisual();
        HideGhost(); HideTimerLabel(); HideReadyLabel();
        if (evolvedNameLabel != null) { Destroy(evolvedNameLabel); evolvedNameLabel = null; }
    }

    void OnDestroy()
    {
        if (cropObject       != null) Destroy(cropObject);
        if (ghostObject      != null) Destroy(ghostObject);
        if (timerLabel       != null) Destroy(timerLabel);
        if (readyLabel       != null) Destroy(readyLabel);
        if (evolvedNameLabel != null) Destroy(evolvedNameLabel);
        HideAllOverlays();
    }
}