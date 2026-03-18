using System.Collections.Generic;
using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready }

    // ── Public state ──────────────────────────────────────────
    public PlotState          State        { get; private set; } = PlotState.Empty;
    public CropData           ActiveCrop   { get; private set; }
    public int                CurrentStage { get; private set; } = -1;
    public float              StageTimer   { get; private set; } = 0f;
    public bool               IsEvolved    { get; private set; } = false;
    public bool               StateChanged { get; private set; }
    public List<MutationData> Mutations    { get; private set; } = new List<MutationData>();

    public int GridX { get; set; }
    public int GridZ { get; set; }

    // ── Cached references ─────────────────────────────────────
    private FarmPlotVisual _visual;

    // ── Hover state ───────────────────────────────────────────
    private bool _hasTriedPlantingThisHover = false;

    // ── Static placement cooldown ─────────────────────────────
    private static float _placementCooldownUntil = 0f;

    public static void SetPlacementCooldown(float duration = 0.15f)
    {
        _placementCooldownUntil = Time.time + duration;
    }

    private static bool PlacementCooldownActive =>
        Time.time < _placementCooldownUntil;

    // ── Setup ─────────────────────────────────────────────────

    void Awake()
    {
        _visual = GetComponent<FarmPlotVisual>();
    }

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

    // ── Mutations ─────────────────────────────────────────────

    public void ApplyMutation(MutationData mutation)
    {
        if (mutation == null || Mutations.Contains(mutation)) return;

        // Check if this mutation combines with any existing ones
        foreach (var existing in new List<MutationData>(Mutations))
        {
            foreach (var combo in existing.combinations)
            {
                if (combo.combinesWith == mutation)
                {
                    Mutations.Remove(existing);
                    Mutations.Add(combo.resultsIn);
                    StateChanged = true;
                    TutorialConsole.Log($"{ActiveCrop?.cropName} is now " +
                        $"{combo.resultsIn.mutationName}!");
                    return;
                }
            }
        }

        Mutations.Add(mutation);
        StateChanged = true;
        TutorialConsole.Log($"{ActiveCrop?.cropName} is now {mutation.mutationName}!");
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
        if (PlacementController.Instance != null && PlacementController.Instance.IsPlacing) return;
        if (PlacementCooldownActive) return;

        _visual?.OnHoverStay();

        if (ShopUI.IsOpen) return;
        if (Inventory.Instance.SelectedSlot?.Type == InventoryItemType.Tool) return;

        if (Input.GetMouseButtonUp(0))
        {
            _hasTriedPlantingThisHover = false;
            return;
        }

        if (Input.GetMouseButton(0) && State == PlotState.Empty && !_hasTriedPlantingThisHover)
        {
            _hasTriedPlantingThisHover = true;
            TryPlant();
        }
    }

    void OnMouseDown()
    {
        if (PlacementController.Instance != null && PlacementController.Instance.IsPlacing) return;
        if (PlacementCooldownActive) return;
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

        if (result.IsEvolved)
        {
            IsEvolved = true;
            EventBus.Raise_MutationOccurred(this);
            AudioManager.Play(SoundEvent.MutationOccurred);
            TutorialConsole.Log(
                $"<color={UIColors.RarityMythical_Hex}>Evolved! " +
                $"{ActiveCrop.cropName} has evolved!</color>");
        }

        // Pass active mutations to harvested crop
        foreach (var m in Mutations)
            result.Mutations.Add(m);

        Inventory.Instance.AddHarvest(result);
        AudioManager.Play(SoundEvent.SeedHarvested);

        FloatingText.Spawn(
            $"+{result.SellValue}",
            transform.position + Vector3.up * 1.5f,
            UIColors.FloatingGold,28);

        TutorialConsole.Log($"Harvested {RankUtility.RankLabel(result.Rank)} " +
            $"{result.DisplayName} — worth {result.SellValue} coins.");

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
        ActiveCrop   = null;
        CurrentStage = -1;
        StageTimer   = 0f;
        IsEvolved    = false;
        Mutations.Clear();
        State        = PlotState.Empty;
        StateChanged = true;
    }
}