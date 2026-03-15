using UnityEngine;

[RequireComponent(typeof(FarmPlot))]
public class FarmPlotVisual : MonoBehaviour
{
    private FarmPlot plot;
    private FarmPlot.PlotState lastState;
    private int lastStage = -1;

    private GameObject cropObject;
    private GameObject ghostObject;

    void Awake()
    {
        plot      = GetComponent<FarmPlot>();
        lastState = plot.State;
    }

    void Update()
    {
        // Only rebuild the visual when something actually changed
        if (plot.StateChanged || plot.CurrentStage != lastStage || plot.State != lastState)
        {
            lastState = plot.State;
            lastStage = plot.CurrentStage;
            RefreshVisual();
        }
    }

    // ── Hover ghost ───────────────────────────────────────────

    public void OnHoverEnter()
    {
        if (ShopUI.IsOpen) return;
        if (plot.State != FarmPlot.PlotState.Empty) return;
        if (Inventory.Instance.SelectedSeed == null) return;
        if (Inventory.Instance.GetSeedCount(Inventory.Instance.SelectedSeed) <= 0) return;
        ShowGhost();
    }

    public void OnHoverStay()
    {
        if (ghostObject == null) OnHoverEnter();
    }

    public void OnHoverExit()
    {
        HideGhost();
    }

    void ShowGhost()
    {
        HideGhost();

        CropData seed = Inventory.Instance.SelectedSeed;
        if (seed == null || seed.growthStages.Length == 0) return;

        GrowthStage firstStage = seed.growthStages[0];

        ghostObject = SpawnVisual(firstStage);

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

        // Tint ready crops to make them obviously harvestable
        if (plot.State == FarmPlot.PlotState.Ready)
        {
            var rend = cropObject.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.material.color = Color.Lerp(stage.stageColor, Color.yellow, 0.4f);
            }
        }
    }

    void ClearCropVisual()
    {
        if (cropObject != null) { Destroy(cropObject); cropObject = null; }
    }

    // ── Shared spawn helper ───────────────────────────────────

    GameObject SpawnVisual(GrowthStage stage)
    {
        GameObject go;

        if (stage.visualPrefab != null)
        {
            go = Instantiate(stage.visualPrefab);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.GetComponent<Renderer>().material.color = stage.stageColor;
        }

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
    }
}