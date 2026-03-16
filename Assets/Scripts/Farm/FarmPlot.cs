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

    // ── Growing ───────────────────────────────────────────────

    void Update()
    {
        StateChanged = false;
        if (State != PlotState.Growing) return;

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
            EventBus.Raise_PlotReady(this);
            TutorialConsole.Log($"{ActiveCrop.cropName} is ready to harvest!");
        }
    }

    // ── Input ─────────────────────────────────────────────────

    void OnMouseDown()
    {
        if (ShopUI.IsOpen) return;

        switch (State)
        {
            case PlotState.Empty:   TryPlant(); break;
            case PlotState.Ready:   Harvest();  break;
            case PlotState.Growing:
                float remaining = ActiveCrop.growthStages[CurrentStage].duration - StageTimer;
                TutorialConsole.Log($"{ActiveCrop.cropName} — stage {CurrentStage + 1}/" +
                    $"{ActiveCrop.growthStages.Length}, ready in {remaining:F1}s.");
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

        TutorialConsole.Log($"Planted {ActiveCrop.cropName}!");
        EventBus.Raise_PlotPlanted(this);
    }

    // ── Harvesting ────────────────────────────────────────────

    void Harvest()
    {
        // Roll for mutation
        bool mutated = Random.value < ActiveCrop.mutationChance;
        if (mutated)
        {
            IsMutated = true;
            EventBus.Raise_MutationOccurred(this);
            TutorialConsole.Log($"<color=#CC88FF>Mutation! {ActiveCrop.cropName} has mutated!</color>");
        }

        // Roll for rank
        Rank rank = RankUtility.RollRank();

        // Switch crop type if mutatesInto is assigned
        CropData finalCrop = (mutated && ActiveCrop.mutatesInto != null)
            ? ActiveCrop.mutatesInto
            : ActiveCrop;

        var harvested = new HarvestedCrop(finalCrop, rank, mutated);
        FloatingText.Spawn(
            $"+{harvested.SellValue}  {RankUtility.RankLabel(rank)}",
            transform.position + Vector3.up * 1.5f,
            UIColors.FloatingGold);

        Inventory.Instance.AddHarvest(harvested);

        TutorialConsole.Log($"Harvested {RankUtility.RankLabel(rank)} " +
            $"{harvested.DisplayName} — worth {harvested.SellValue} coins.");

        ActiveCrop   = null;
        CurrentStage = -1;
        StageTimer   = 0f;
        IsMutated    = false;
        State        = PlotState.Empty;
        StateChanged = true;
    }
}