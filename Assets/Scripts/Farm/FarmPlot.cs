using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready }

    // ── Public state ──────────────────────────────────────────
    public PlotState State        { get; private set; } = PlotState.Empty;
    public CropData  ActiveCrop   { get; private set; }
    public int       CurrentStage { get; private set; } = -1;
    public float     StageTimer   { get; private set; } = 0f;
    public bool      IsMutated    { get; private set; } = false;
    public bool      StateChanged { get; private set; }

    public int GridX { get; set; }
    public int GridZ { get; set; }

    // ── Hover state ───────────────────────────────────────────
    private bool _hasTriedPlantingThisHover = false;

    // ── Growing ───────────────────────────────────────────────

    void Update()
    {
        StateChanged = false;
        if (State != PlotState.Growing) return;

        if (ActiveCrop == null || ActiveCrop.growthStages == null ||
            CurrentStage >= ActiveCrop.growthStages.Length)
        {
            Debug.LogWarning($"FarmPlot: invalid state on {gameObject.name}. Resetting.");
            ResetState();
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

    void OnMouseEnter()
    {
        _hasTriedPlantingThisHover = false;
        GetComponent<FarmPlotVisual>()?.OnHoverEnter();
    }

    void OnMouseExit()
    {
        _hasTriedPlantingThisHover = false;
        GetComponent<FarmPlotVisual>()?.OnHoverExit();
    }

    void OnMouseOver()
    {
        if (PlacementController.Instance != null && PlacementController.Instance.IsPlacing) return;

        GetComponent<FarmPlotVisual>()?.OnHoverStay();

        if (ShopUI.IsOpen) return;
        if (Inventory.Instance.SelectedSlot?.Type == InventoryItemType.Tool) return;

        // Reset flag when mouse button is released
        if (Input.GetMouseButtonUp(0))
        {
            _hasTriedPlantingThisHover = false;
            return;
        }

        // Hold to plant — fires once per plot per mouse press
        if (Input.GetMouseButton(0) && State == PlotState.Empty && !_hasTriedPlantingThisHover)
        {
            _hasTriedPlantingThisHover = true;
            TryPlant();
        }
    }

    void OnMouseDown()
    {
        if (PlacementController.Instance != null && PlacementController.Instance.IsPlacing) return;
        if (ShopUI.IsOpen) return;
        if (Inventory.Instance.SelectedSlot?.Type == InventoryItemType.Tool) return;
        HandleClick();
    }

    void HandleClick()
    {
        switch (State)
        {
            case PlotState.Empty:
                TryPlant();
                break;
            case PlotState.Ready:
                Harvest();
                 _hasTriedPlantingThisHover = true;
                break;
            case PlotState.Growing:
                float remaining = ActiveCrop.growthStages[CurrentStage].duration - StageTimer;
                TutorialConsole.Log($"{ActiveCrop.cropName} — stage " +
                    $"{CurrentStage + 1}/{ActiveCrop.growthStages.Length}, " +
                    $"ready in {remaining:F1}s.");
                break;
        }
    }

    // ── Planting ──────────────────────────────────────────────

    void TryPlant()
    {
        CropData selected = Inventory.Instance.SelectedSeed;

        if (selected == null)
        {
            TutorialConsole.Warn("No seed selected. Buy one from the shop!");
            AudioManager.Play(SoundEvent.NoSeedSelected);
            return;
        }

        if (!Inventory.Instance.UseSeed(selected))
        {
            TutorialConsole.Warn($"No {selected.cropName} seeds left.");
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
        AudioManager.Play(SoundEvent.SeedHarvested);

        FloatingText.Spawn(
            $"+{result.SellValue}  {RankUtility.RankLabel(result.Rank)}",
            transform.position + Vector3.up * 1.5f,
            UIColors.FloatingGold);

        TutorialConsole.Log($"Harvested {RankUtility.RankLabel(result.Rank)} " +
            $"{result.DisplayName} — worth {result.SellValue} coins.");

        // Prevent OnMouseOver from immediately trying to plant on the
        // newly emptied plot while the mouse button is still held
        _hasTriedPlantingThisHover = true;

        ResetState();
    }

    // ── Removal ───────────────────────────────────────────────

    public void RemovePlot()
    {
        GridManager.Instance.ClearCells(GridX, GridZ, 1, 1);
        EventBus.Raise_PlotRemoved(this);
        GetComponent<FarmPlotVisual>()?.ForceCleanup();
        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────

    void ResetState()
    {
        ActiveCrop   = null;
        CurrentStage = -1;
        StageTimer   = 0f;
        IsMutated    = false;
        State        = PlotState.Empty;
        StateChanged = true;
    }
}