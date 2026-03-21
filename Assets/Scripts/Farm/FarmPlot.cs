using System.Collections.Generic;
using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready, Regrowing, Evolved }

    public PlotState          State            { get; private set; } = PlotState.Empty;
    public CropData           ActiveCrop       { get; private set; }
    public int                CurrentStage     { get; private set; } = -1;
    public float              StageTimer       { get; private set; } = 0f;
    public float              RegrowTimer      { get; private set; } = 0f;
    public bool               IsEvolved        { get; private set; } = false;
    public bool               IsWatered        { get; private set; } = false;
    public bool               StateChanged     { get; private set; }
    public List<MutationData> Mutations        { get; private set; } = new List<MutationData>();
    public CharacterData      EvolvedCharacter { get; private set; }

    public int GridX { get; set; }
    public int GridZ { get; set; }

    // Soil — lazy: falls back to world default if not explicitly assigned
    private SoilData _currentSoil;
    public SoilData CurrentSoil
    {
        get  => _currentSoil ?? WorldEventManager.Instance?.currentWorld?.defaultSoil;
        set
        {
            _currentSoil = value;
            _visual?.ApplySoilColor(value?.soilColor ?? _visual.OriginalPlotColor);
        }
    }

    private FarmPlotVisual _visual;
    private bool           _hasTriedPlantingThisHover = false;
    private float          _wateringSpeedMultiplier   = 1f;
    private FertilizerData _fertilizerData;
    private float          _fertilizerGrowthBonus     = 0f;

    private static float _placementCooldownUntil = 0f;

    public static void SetPlacementCooldown(float duration = 0.15f)
        => _placementCooldownUntil = Time.time + duration;

    private static bool PlacementCooldownActive =>
        Time.time < _placementCooldownUntil;

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake()
    {
        _visual = GetComponent<FarmPlotVisual>();
    }

    void Update()
    {
        StateChanged = false;

        if (State == PlotState.Regrowing)
        {
            TickRegrowing();
            return;
        }

        if (State != PlotState.Growing) return;

        if (ActiveCrop == null || ActiveCrop.growthStages == null ||
            CurrentStage >= ActiveCrop.growthStages.Length)
        {
            Debug.LogWarning($"FarmPlot: invalid state on {gameObject.name}. Resetting.");
            ResetState();
            return;
        }

        StageTimer += Time.deltaTime * CurrentGrowthSpeed;

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

    void TickRegrowing()
    {
        RegrowTimer -= Time.deltaTime;
        if (RegrowTimer <= 0f)
        {
            State        = PlotState.Ready;
            StateChanged = true;
            AudioManager.Play(SoundEvent.PlantReady);
            EventBus.Raise_PlotReady(this);
            TutorialConsole.Log($"{ActiveCrop.cropName} has regrown — ready to harvest again!");
        }
    }

    // ── Growth speed ──────────────────────────────────────────

    // All multipliers consolidated here — referenced by Update, HandleClick (remaining time calc)
    public float CurrentGrowthSpeed =>
        _wateringSpeedMultiplier
        * CalculateSoilMult()
        * (1f + _fertilizerGrowthBonus)
        * (WorldEventManager.Instance?.GrowthSpeedMultiplier ?? 1f);

    float CalculateSoilMult()
    {
        var soil = CurrentSoil;
        if (ActiveCrop == null || soil == null) return 1f;
        float m = soil.growthMultiplier;
        if (TagUtility.HasAnyTag(ActiveCrop.tags, soil.incompatibleCropTags))
            m *= 0.5f;
        else if (soil.compatibleCropTags.Count > 0 &&
                 !TagUtility.HasAnyTag(ActiveCrop.tags, soil.compatibleCropTags))
            m *= 0.5f;
        return m;
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

    // ── Fertilizer ────────────────────────────────────────────

    public void ApplyFertilizer(FertilizerData data)
    {
        if (data == null || State != PlotState.Growing) return;
        if (data.affectedTags.Count > 0 &&
            !TagUtility.HasAnyTag(ActiveCrop.tags, data.affectedTags))
        {
            TutorialConsole.Warn($"{data.fertilizerName} doesn't affect {ActiveCrop.cropName}.");
            return;
        }
        _fertilizerData        = data;
        _fertilizerGrowthBonus = data.growthBonus;
        _visual?.ShowFertilizerEffect(data.tintColor);
        TutorialConsole.Log($"Applied {data.fertilizerName} to {ActiveCrop.cropName}!");
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

        if (ShopUI.IsOpen)        return;
        if (DialoguePanel.IsOpen) return;
        if (ShovelEquipped())     return;

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
        if (ShopUI.IsOpen)        return;
        if (DialoguePanel.IsOpen) return;
        if (ShovelEquipped())     return;

        HandleClick();
    }

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
            {
                float speed     = CurrentGrowthSpeed;
                float remaining = (ActiveCrop.growthStages[CurrentStage].duration
                                   - StageTimer) / speed;
                for (int i = CurrentStage + 1; i < ActiveCrop.growthStages.Length - 1; i++)
                    remaining += ActiveCrop.growthStages[i].duration / speed;
                TutorialConsole.Log($"{ActiveCrop.cropName} — stage " +
                    $"{CurrentStage + 1}/{ActiveCrop.growthStages.Length}, " +
                    $"ready in {remaining:F1}s.");
                break;
            }

            case PlotState.Regrowing:
                TutorialConsole.Log($"{ActiveCrop.cropName} is regrowing — " +
                    $"{RegrowTimer:F1}s remaining.");
                break;

            case PlotState.Evolved:
                // Open the Yarn Spinner dialogue — watering can still works as an affection gesture
                if (EvolvedCharacter != null)
                    DialoguePanel.Instance?.Open(EvolvedCharacter);
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

        ActiveCrop             = selected;
        CurrentStage           = 0;
        StageTimer             = 0f;
        IsEvolved              = false;
        _fertilizerData        = null;
        _fertilizerGrowthBonus = 0f;
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
        float mutBonus = (CurrentSoil?.mutationChanceBonus ?? 0f)
                       + (_fertilizerData?.mutationChanceBonus ?? 0f);
        var result = HarvestResolver.Resolve(ActiveCrop, mutBonus);

        // Evolution path — crop stays on the plot as a character
        if (result.IsEvolved && ActiveCrop.characterData != null)
        {
            EvolvePlot(ActiveCrop.characterData);
            return;
        }

        foreach (var m in Mutations) result.Mutations.Add(m);

        // Guaranteed fertilizer mutation
        if (_fertilizerData?.guaranteedMutation != null &&
            !result.Mutations.Contains(_fertilizerData.guaranteedMutation))
            result.Mutations.Add(_fertilizerData.guaranteedMutation);

        Inventory.Instance.AddHarvest(result);
        AudioManager.Play(SoundEvent.SeedHarvested);
        FloatingText.Spawn(
            $"+{result.SellValue}",
            transform.position + Vector3.up * 1.5f,
            UIColors.FloatingGold, 28);
        TutorialConsole.Log($"Harvested {result.DisplayName} — worth {result.SellValue} coins.");
        _hasTriedPlantingThisHover = true;

        if (ActiveCrop.isMultiHarvest)
        {
            _fertilizerData          = null;
            _fertilizerGrowthBonus   = 0f;
            IsWatered                = false;
            _wateringSpeedMultiplier = 1f;
            Mutations.Clear();
            RegrowTimer  = ActiveCrop.regrowDuration;
            State        = PlotState.Regrowing;
            StateChanged = true;
            TutorialConsole.Log($"{ActiveCrop.cropName} is regrowing...");
        }
        else
        {
            ResetState();
        }
    }

    // ── Evolution ─────────────────────────────────────────────

    void EvolvePlot(CharacterData data)
    {
        IsEvolved        = true;
        EvolvedCharacter = data;
        State            = PlotState.Evolved;
        StateChanged     = true;
        TutorialConsole.Log($"{data.characterName} has appeared on your farm!");
        EventBus.Raise_PlotEvolved(this, data);
    }

    // ── Removal ───────────────────────────────────────────────

    public void RemovePlot()
    {
        GridManager.Instance.ClearCells(GridX, GridZ, 1, 1);
        EventBus.Raise_PlotRemoved(this);
        _visual?.ForceCleanup();
        Destroy(gameObject);
    }

    // ── Reset ─────────────────────────────────────────────────

    void ResetState()
    {
        ActiveCrop               = null;
        CurrentStage             = -1;
        StageTimer               = 0f;
        RegrowTimer              = 0f;
        IsEvolved                = false;
        EvolvedCharacter         = null;
        IsWatered                = false;
        _wateringSpeedMultiplier = 1f;
        _fertilizerData          = null;
        _fertilizerGrowthBonus   = 0f;
        Mutations.Clear();
        State        = PlotState.Empty;
        StateChanged = true;
        _visual?.ResetPlotColor();
    }
}
