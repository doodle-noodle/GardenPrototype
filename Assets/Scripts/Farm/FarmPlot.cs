using System.Collections.Generic;
using UnityEngine;

public class FarmPlot : MonoBehaviour
{
    public enum PlotState { Empty, Growing, Ready, Regrowing, Evolved }

    public PlotState          State             { get; private set; } = PlotState.Empty;
    public CropData           ActiveCrop        { get; private set; }
    public int                CurrentStage      { get; private set; } = -1;
    public float              StageTimer        { get; private set; } = 0f;
    public float              RegrowTimer       { get; private set; } = 0f;
    public int                WaterCount        { get; private set; } = 0;
    public bool               IsWatered         => WaterCount > 0;
    public bool               IsFertilized      => _fertilizerData != null;
    public bool               IsEvolved         { get; private set; } = false;
    public bool               StateChanged      { get; private set; }
    public List<MutationData> Mutations         { get; private set; } = new List<MutationData>();
    public CharacterData      EvolvedCharacter  { get; private set; }
    public EvolutionPath      ReadyToEvolveWith { get; private set; }

    public int GridX { get; set; }
    public int GridZ { get; set; }

    private SoilData _currentSoil;
    public SoilData CurrentSoil
    {
        get  => _currentSoil ?? WorldEventManager.Instance?.currentWorld?.defaultSoil;
        set  { _currentSoil = value; _visual?.ApplySoilColor(value?.soilColor); }
    }

    private FarmPlotVisual _visual;
    private bool           _hasTriedPlantingThisHover = false;
    private FertilizerData _fertilizerData;
    private float          _fertilizerGrowthBonus     = 0f;

    private readonly HashSet<EvolutionPath> _declinedEvolutions = new HashSet<EvolutionPath>();

    private static float _placementCooldownUntil = 0f;
    public static void SetPlacementCooldown(float duration = 0.15f)
        => _placementCooldownUntil = Time.time + duration;
    private static bool PlacementCooldownActive => Time.time < _placementCooldownUntil;

    // ── Lifecycle ─────────────────────────────────────────────

    void Awake() { _visual = GetComponent<FarmPlotVisual>(); }

    void Update()
    {
        StateChanged = false;

        if (State == PlotState.Regrowing) { TickRegrowing(); return; }
        if (State != PlotState.Growing)   return;

        if (ActiveCrop == null || ActiveCrop.growthStages == null ||
            CurrentStage >= ActiveCrop.growthStages.Length)
        {
            Debug.LogWarning($"FarmPlot: invalid state on {name}. Resetting.");
            ResetState(); return;
        }

        StageTimer += Time.deltaTime * CurrentGrowthSpeed;
        var  stage        = ActiveCrop.growthStages[CurrentStage];
        bool isFinalStage = CurrentStage == ActiveCrop.growthStages.Length - 1;

        if (!isFinalStage && StageTimer >= stage.duration)
        {
            CurrentStage++; StageTimer = 0f; StateChanged = true;
        }
        else if (isFinalStage && StageTimer >= stage.duration)
        {
            State = PlotState.Ready; StateChanged = true;
            OnEnterReady();
        }
    }

    void TickRegrowing()
    {
        RegrowTimer -= Time.deltaTime;
        if (RegrowTimer > 0f) return;
        State = PlotState.Ready; StateChanged = true;
        OnEnterReady();
    }

    void OnEnterReady()
    {
        ReadyToEvolveWith = FindValidEvolutionPath();
        if (ReadyToEvolveWith != null)
        {
            TutorialConsole.Log(
                $"<color=#66FF66>{ActiveCrop.cropName} wants to evolve!</color>");
            StateChanged = true;
        }
        else
        {
            AudioManager.Play(SoundEvent.PlantReady);
            EventBus.Raise_PlotReady(this);
            TutorialConsole.Log($"{ActiveCrop.cropName} is ready to harvest!");
        }
    }

    // ── Growth speed ──────────────────────────────────────────

    float WateringMult => Mathf.Min(1f + WaterCount, 4f);

    public float CurrentGrowthSpeed =>
        WateringMult
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

    // ── Evolution conditions ──────────────────────────────────

    EvolutionPath FindValidEvolutionPath()
    {
        if (ActiveCrop?.evolutionPaths == null || ActiveCrop.evolutionPaths.Count == 0)
            return null;

        foreach (var path in ActiveCrop.evolutionPaths)
        {
            if (path?.characterData == null)              continue;
            if (_declinedEvolutions.Contains(path))       continue;
            if (RelationshipManager.Instance != null &&
                RelationshipManager.Instance.ExistingCharacters.Contains(path.characterData))
                continue;

            bool allMet = true;
            if (path.conditions != null)
                foreach (var cond in path.conditions)
                    if (!CheckCondition(cond)) { allMet = false; break; }

            if (allMet) return path;
        }
        return null;
    }

    bool CheckCondition(EvolutionCondition cond)
    {
        switch (cond.type)
        {
            case EvolutionConditionType.HasMutation:
                return cond.requiredMutation != null && Mutations.Contains(cond.requiredMutation);

            case EvolutionConditionType.IsFertilized:
                return IsFertilized;

            case EvolutionConditionType.WateredAtLeast:
                return WaterCount >= cond.minWaterCount;

            case EvolutionConditionType.WorldEvent:
            {
                var mgr = WorldEventManager.Instance;
                if (mgr == null) return false;
                foreach (var e in cond.requiredWorldEvents)
                    if (!mgr.IsEventActive(e)) return false;
                foreach (var e in cond.forbiddenWorldEvents)
                    if (mgr.IsEventActive(e)) return false;
                return true;
            }

            case EvolutionConditionType.SoilType:
                return CurrentSoil == cond.requiredSoil;

            default:
                return true;
        }
    }

    public void DeclineEvolution()
    {
        if (ReadyToEvolveWith != null)
            _declinedEvolutions.Add(ReadyToEvolveWith);

        TutorialConsole.Log($"{ActiveCrop.cropName} decided not to evolve.");
        ReadyToEvolveWith = null;
        StateChanged = true;

        ReadyToEvolveWith = FindValidEvolutionPath();
        if (ReadyToEvolveWith != null)
        {
            TutorialConsole.Log(
                $"<color=#66FF66>{ActiveCrop.cropName} wants to evolve!</color>");
        }
        else
        {
            AudioManager.Play(SoundEvent.PlantReady);
            EventBus.Raise_PlotReady(this);
            TutorialConsole.Log($"{ActiveCrop.cropName} is ready to harvest!");
        }
    }

    public void EvolvePlot(CharacterData data)
    {
        ReadyToEvolveWith = null;
        IsEvolved         = true;
        EvolvedCharacter  = data;
        State             = PlotState.Evolved;
        StateChanged      = true;
        TutorialConsole.Log($"{data.characterName} has appeared on your farm!");
        EventBus.Raise_PlotEvolved(this, data);
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
        if (data == null)             return;
        if (State != PlotState.Empty) return;
        if (IsFertilized)             return;

        _fertilizerData        = data;
        _fertilizerGrowthBonus = data.growthBonus;
        _visual?.ShowFertilizerOverlay(data);
        TutorialConsole.Log($"Applied {data.fertilizerName} to the plot!");
    }

    // ── Watering ──────────────────────────────────────────────

    public void ApplyWatering()
    {
        if (State != PlotState.Growing) return;
        WaterCount++;
        _visual?.ShowWateringOverlay();
        TutorialConsole.Log($"Watered {ActiveCrop.cropName}! (x{WaterCount})");
    }

    // ── Input ─────────────────────────────────────────────────

    void OnMouseEnter() { _hasTriedPlantingThisHover = false; _visual?.OnHoverEnter(); }
    void OnMouseExit()  { _hasTriedPlantingThisHover = false; _visual?.OnHoverExit(); }

    void OnMouseOver()
    {
        if (PlacementController.Instance != null &&
            PlacementController.Instance.IsPlacing) return;
        if (PlacementCooldownActive)        return;
        _visual?.OnHoverStay();
        if (ShopUI.IsOpen)                return;
        if (DialoguePanel.IsOpen)         return;
        if (EvolutionConfirmPanel.IsOpen)  return;
        if (AnyToolEquipped())             return;

        if (Input.GetMouseButtonUp(0)) { _hasTriedPlantingThisHover = false; return; }

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
        if (PlacementCooldownActive)        return;
        if (ShopUI.IsOpen)                return;
        if (DialoguePanel.IsOpen)         return;
        if (EvolutionConfirmPanel.IsOpen)  return;
        if (AnyToolEquipped())             return;
        HandleClick();
    }

    bool AnyToolEquipped()
    {
        return Inventory.Instance.SelectedSlot?.Tool != null;
    }

    void HandleClick()
    {
        switch (State)
        {
            case PlotState.Empty:
                TryPlant();
                break;

            case PlotState.Ready:
                if (ReadyToEvolveWith != null)
                    EvolutionConfirmPanel.Instance?.Open(this, ReadyToEvolveWith);
                else
                {
                    Harvest();
                    _hasTriedPlantingThisHover = true;
                }
                break;

            case PlotState.Growing:
            {
                float speed = CurrentGrowthSpeed;
                float rem   = (ActiveCrop.growthStages[CurrentStage].duration - StageTimer) / speed;
                for (int i = CurrentStage + 1; i < ActiveCrop.growthStages.Length - 1; i++)
                    rem += ActiveCrop.growthStages[i].duration / speed;
                TutorialConsole.Log($"{ActiveCrop.cropName} — stage " +
                    $"{CurrentStage + 1}/{ActiveCrop.growthStages.Length}, " +
                    $"ready in {rem:F1}s.");
                break;
            }

            case PlotState.Regrowing:
                TutorialConsole.Log($"{ActiveCrop.cropName} is regrowing — " +
                    $"{RegrowTimer:F1}s remaining.");
                break;

            case PlotState.Evolved:
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

        ActiveCrop   = selected;
        CurrentStage = 0;
        StageTimer   = 0f;
        IsEvolved    = false;
        WaterCount   = 0;
        _declinedEvolutions.Clear();
        Mutations.Clear();

        if (_fertilizerData != null)
            _fertilizerGrowthBonus = _fertilizerData.growthBonus;

        State = PlotState.Growing; StateChanged = true;
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

        foreach (var m in Mutations) result.Mutations.Add(m);

        if (_fertilizerData?.guaranteedMutation != null &&
            !result.Mutations.Contains(_fertilizerData.guaranteedMutation))
        {
            bool tagMatch = _fertilizerData.affectedTags.Count == 0 ||
                            TagUtility.HasAnyTag(ActiveCrop.tags, _fertilizerData.affectedTags);
            if (tagMatch) result.Mutations.Add(_fertilizerData.guaranteedMutation);
        }

        Inventory.Instance.AddHarvest(result);
        AudioManager.Play(SoundEvent.SeedHarvested);
        FloatingText.Spawn($"+{result.SellValue}",
            transform.position + Vector3.up * 1.5f, UIColors.FloatingGold, 28);
        TutorialConsole.Log($"Harvested {result.DisplayName} — worth {result.SellValue} coins.");
        _hasTriedPlantingThisHover = true;

        if (ActiveCrop.isMultiHarvest)
        {
            _fertilizerData        = null;
            _fertilizerGrowthBonus = 0f;
            WaterCount             = 0;
            Mutations.Clear();
            _visual?.HideAllOverlays();
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
        ActiveCrop             = null;
        CurrentStage           = -1;
        StageTimer             = 0f;
        RegrowTimer            = 0f;
        IsEvolved              = false;
        EvolvedCharacter       = null;
        ReadyToEvolveWith      = null;
        WaterCount             = 0;
        _fertilizerData        = null;
        _fertilizerGrowthBonus = 0f;
        Mutations.Clear();
        _declinedEvolutions.Clear();
        State        = PlotState.Empty;
        StateChanged = true;
        _visual?.ResetPlotColor();
        _visual?.HideAllOverlays();
    }

#if UNITY_EDITOR
    public void ForceReadyToEvolveDebug(int pathIndex = 0)
    {
        if (ActiveCrop?.evolutionPaths == null || ActiveCrop.evolutionPaths.Count == 0)
        {
            TutorialConsole.Warn($"Debug: {ActiveCrop?.cropName ?? "crop"} has no evolution paths.");
            return;
        }
        int idx  = Mathf.Clamp(pathIndex, 0, ActiveCrop.evolutionPaths.Count - 1);
        var path = ActiveCrop.evolutionPaths[idx];
        if (path?.characterData == null)
        {
            TutorialConsole.Warn("Debug: evolution path has no CharacterData.");
            return;
        }
        if (State == PlotState.Growing)
        {
            State        = PlotState.Ready;
            StateChanged = true;
        }
        ReadyToEvolveWith = path;
        StateChanged      = true;
        TutorialConsole.Log($"Debug: {ActiveCrop.cropName} forced → evolve into " +
            $"{path.characterData.characterName}.");
    }

    public void ForceWaterCountDebug(int count)
    {
        WaterCount = Mathf.Max(0, count);
        if (WaterCount > 0) _visual?.ShowWateringOverlay();
        TutorialConsole.Log($"Debug: water count set to {WaterCount}.");
    }

    public void ForceFertilizerDebug(FertilizerData data)
    {
        if (data == null) return;
        _fertilizerData        = data;
        _fertilizerGrowthBonus = data.growthBonus;
        _visual?.ShowFertilizerOverlay(data);
        TutorialConsole.Log($"Debug: fertilizer set to {data.fertilizerName}.");
    }
#endif
}