using System.Collections.Generic;
using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready }

    public PlotState          State        { get; private set; } = PlotState.Empty;
    public CropData           ActiveCrop   { get; private set; }
    public int                CurrentStage { get; private set; } = -1;
    public float              StageTimer   { get; private set; } = 0f;
    public bool               IsEvolved    { get; private set; } = false;
    public bool               IsWatered    { get; private set; } = false;
    public bool               StateChanged { get; private set; }
    public List<MutationData> Mutations    { get; private set; } = new List<MutationData>();

    public int GridX { get; set; }
    public int GridZ { get; set; }

    private FarmPlotVisual _visual;
    private bool           _hasTriedPlantingThisHover = false;
    private float          _wateringSpeedMultiplier   = 1f;

    private static float _placementCooldownUntil = 0f;

    public static void SetPlacementCooldown(float duration = 0.15f)
        => _placementCooldownUntil = Time.time + duration;

    private static bool PlacementCooldownActive =>
        Time.time < _placementCooldownUntil;

    void Awake()
    {
        _visual = GetComponent<FarmPlotVisual>();
    }

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

        float speed = _wateringSpeedMultiplier *
                      (WorldEventManager.Instance?.GrowthSpeedMultiplier ?? 1f);
        StageTimer += Time.deltaTime * speed;

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

    // ── Mutations ─────────────────────────────────────────────

    public void ApplyMutation(MutationData mutation)
    {
        if (mutation == null) return;

        foreach (var existing in new List<MutationData>(Mutations))
        {
            if (existing.combinations == null) continue;
            foreach (var combo in existing.combinations)
            {
                if (combo.combinesWith != mutation || combo.resultsIn == null) continue;
                if (Mutations.Contains(combo.resultsIn)) return;
                Mutations.Remove(existing);
                Mutations.Add(combo.resultsIn);
                StateChanged = true;
                TutorialConsole.Log($"{ActiveCrop?.cropName} mutation combined into " +
                    $"{combo.resultsIn.mutationName}!");
                return;
            }
        }

        if (Mutations.Contains(mutation)) return;
        Mutations.Add(mutation);
        StateChanged = true;
        TutorialConsole.Log($"{ActiveCrop?.cropName} is now {mutation.mutationName}!");
    }

    // ── Watering ──────────────────────────────────────────────

    public void ApplyWatering()
    {
        if (IsWatered || State != PlotState.Growing) return;
        IsWatered                = true;
        _wateringSpeedMultiplier = 2f;
        _visual?.ShowWateringEffect();
    }

    // ── Input ─────────────────────────────────────────────────

    void OnMouseEnter()
    {
        _hasTriedPlantingThisHover = false;
        _visual?.OnHoverEnter();
    }

    void OnMouseExit()
    {
        _hasTriedPlantingThisHover = false;
        _visual?.OnHoverExit();
    }

    void OnMouseOver()
    {
        if (PlacementController.Instance != null &&
            PlacementController.Instance.IsPlacing) return;
        if (PlacementCooldownActive) return;

        _visual?.OnHoverStay();

        if (ShopUI.IsOpen) return;

        // Only shovel blocks hover planting — other tools allow normal interaction
        if (ShovelEquipped()) return;

        if (Input.GetMouseButtonUp(0))
        {
            _hasTriedPlantingThisHover = false;
            return;
        }

        if (Input.GetMouseButton(0) && State == PlotState.Empty &&
            !_hasTriedPlantingThisHover)
        {
            _hasTriedPlantingThisHover = true;
            TryPlant();
        }
    }

    void OnMouseDown()
    {
        if (PlacementController.Instance != null &&
            PlacementController.Instance.IsPlacing) return;
        if (PlacementCooldownActive) return;
        if (ShopUI.IsOpen) return;

        // Only shovel blocks plot interaction — watering can and other tools
        // should not prevent planting or harvesting
        if (ShovelEquipped()) return;

        HandleClick();
    }

    // Returns true only when the shovel is the active tool
    bool ShovelEquipped()
    {
        var tool = Inventory.Instance.SelectedSlot?.Tool;
        return tool != null && tool.toolType == ToolType.Shovel;
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
                float speed = _wateringSpeedMultiplier *
                              (WorldEventManager.Instance?.GrowthSpeedMultiplier ?? 1f);
                float remaining = (ActiveCrop.growthStages[CurrentStage].duration
                                   - StageTimer) / speed;
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
        IsEvolved    = false;
        Mutations.Clear();
        State        = PlotState.Growing;
        StateChanged = true;

        AudioManager.Play(SoundEvent.SeedPlanted);
        TutorialConsole.Log($"Planted {ActiveCrop.cropName}!");
        EventBus.Raise_PlotPlanted(this);
        _visual?.PlayBurialAnimation(ActiveCrop);
    }

    // ── Harvesting ────────────────────────────────────────────

    void Harvest()
    {
        var result = HarvestResolver.Resolve(ActiveCrop);

        if (result.IsEvolved) IsEvolved = true;

        foreach (var m in Mutations) result.Mutations.Add(m);

        Inventory.Instance.AddHarvest(result);
        AudioManager.Play(SoundEvent.SeedHarvested);

        FloatingText.Spawn(
            $"+{result.SellValue}",
            transform.position + Vector3.up * 1.5f,
            UIColors.FloatingGold, 28);

        TutorialConsole.Log($"Harvested {result.DisplayName} — worth {result.SellValue} coins.");
        _hasTriedPlantingThisHover = true;
        ResetState();
    }

    // ── Removal ───────────────────────────────────────────────

    public void RemovePlot()
    {
        GridManager.Instance.ClearCells(GridX, GridZ, 1, 1);
        EventBus.Raise_PlotRemoved(this);
        _visual?.ForceCleanup();
        Destroy(gameObject);
    }

    // ── Helpers ───────────────────────────────────────────────

    void ResetState()
    {
        ActiveCrop               = null;
        CurrentStage             = -1;
        StageTimer               = 0f;
        IsEvolved                = false;
        IsWatered                = false;
        _wateringSpeedMultiplier = 1f;
        Mutations.Clear();
        State        = PlotState.Empty;
        StateChanged = true;
        _visual?.ResetWateringColor();
    }
}