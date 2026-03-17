using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready }

    public PlotState State        { get; private set; } = PlotState.Empty;
    public CropData  ActiveCrop   { get; private set; }
    public int       CurrentStage { get; private set; } = -1;
    public float     StageTimer   { get; private set; } = 0f;
    public bool      IsMutated    { get; private set; } = false;
    public bool      StateChanged { get; private set; }

    // Stored by GridManager when this plot is placed
    public int GridX { get; set; }
    public int GridZ { get; set; }

    // ── Growing ───────────────────────────────────────────────

    void Update()
    {
        StateChanged = false;
        if (State != PlotState.Growing) return;

        if (ActiveCrop == null || ActiveCrop.growthStages == null ||
            CurrentStage >= ActiveCrop.growthStages.Length)
        {
            Debug.LogWarning($"FarmPlot: invalid crop state on {gameObject.name}. Resetting.");
            State        = PlotState.Empty;
            ActiveCrop   = null;
            CurrentStage = -1;
            StageTimer   = 0f;
            StateChanged = true;
            return;
        }

        StageTimer += Time.deltaTime;

        GrowthStage stage        = ActiveCrop.growthStages[CurrentStage];
        bool        isFinalStage = CurrentStage == ActiveCrop.growthStages.Length - 1;

        if (!isFinalStage && StageTimer >= stage.duration)
        {
            CurrentStage++;
            StageTimer   = 0f;
            StateChanged = true;
        }
        else if (isFinalStage && StageTimer >= stage.duration)
        {
            State        = PlotState.Ready;
            StateChanged = true;
            AudioManager.Play(SoundEvent.PlantReady);
            EventBus.Raise_PlotReady(this);
            TutorialConsole.Log($"{ActiveCrop.cropName} is ready to harvest!");
        }
    }

    // ── Input ─────────────────────────────────────────────────

    void OnMouseDown()
    {
        if (ShopUI.IsOpen) return;

        // If a tool is selected, let ToolUser handle the click
        if (Inventory.Instance.SelectedSlot?.Type == InventoryItemType.Tool) return;

        switch (State)
        {
            case PlotState.Empty:   TryPlant(); break;
            case PlotState.Ready:   Harvest();  break;
            case PlotState.Growing:
                float remaining = ActiveCrop.growthStages[CurrentStage].duration - StageTimer;
                TutorialConsole.Log($"{ActiveCrop.cropName} — stage " +
                    $"{CurrentStage + 1}/{ActiveCrop.growthStages.Length}, " +
                    $"ready in {remaining:F1}s.");
                break;
        }
    }

    void OnMouseEnter() => GetComponent<FarmPlotVisual>()?.OnHoverEnter();
    void OnMouseOver()  => GetComponent<FarmPlotVisual>()?.OnHoverStay();
    void OnMouseExit()  => GetComponent<FarmPlotVisual>()?.OnHoverExit();

    // ── Planting ──────────────────────────────────────────────

    void TryPlant()
    {
        CropData selected = Inventory.Instance.SelectedSeed;

        if (selected == null)
        {
            TutorialConsole.Warn("Buy a seed from the shop first in order to plant it.");
            AudioManager.Play(SoundEvent.NoSeedSelected);
            return;
        }

        if (!Inventory.Instance.UseSeed(selected))
        {
            TutorialConsole.Warn($"No {selected.cropName} seeds left. Buy more from the shop.");
            return;
        }

        ActiveCrop   = selected;
        CurrentStage = 0;
        StageTimer   = 0f;
        IsMutated    = false;
        State        = PlotState.Growing;
        StateChanged = true;
        AudioManager.Play(SoundEvent.SeedPlanted);

        TutorialConsole.Log($"Planted {ActiveCrop.cropName}!");
        EventBus.Raise_PlotPlanted(this);
    }

    // ── Harvesting ────────────────────────────────────────────

    void Harvest()
    {
        var result = HarvestResolver.Resolve(ActiveCrop);

        if (result.IsMutated)
        {
            IsMutated = true;
            EventBus.Raise_MutationOccurred(this);
            AudioManager.Play(SoundEvent.MutationOccurred);
            TutorialConsole.Log(
                $"<color={UIColors.RarityMythical_Hex}>Mutation! " +
                $"{ActiveCrop.cropName} has mutated!</color>");
        }

        Inventory.Instance.AddHarvest(result);

        FloatingText.Spawn(
            $"+{result.SellValue}  {RankUtility.RankLabel(result.Rank)}",
            transform.position + Vector3.up * 1.5f,
            UIColors.FloatingGold);

        TutorialConsole.Log($"Harvested {RankUtility.RankLabel(result.Rank)} " +
            $"{result.DisplayName} — worth {result.SellValue} coins.");

        ActiveCrop   = null;
        CurrentStage = -1;
        StageTimer   = 0f;
        IsMutated    = false;
        State        = PlotState.Empty;
        StateChanged = true;
        AudioManager.Play(SoundEvent.SeedHarvested);
    }

    // ── Removal by shovel ─────────────────────────────────────

    public void RemovePlot()
    {
        // Free the grid cells this plot occupied
        GridManager.Instance.ClearCells(GridX, GridZ, 1, 1);

        EventBus.Raise_PlotRemoved(this);

        // Clean up the visual component first
        GetComponent<FarmPlotVisual>()?.ForceCleanup();

        Destroy(gameObject);

        AudioManager.Play(SoundEvent.PlotRemoved);
    }
}