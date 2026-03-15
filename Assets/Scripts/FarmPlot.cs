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
            Debug.Log($"{ActiveCrop.cropName} is ready to harvest!");
        }
    }

    void OnMouseDown()
    {
        if (ShopUI.IsOpen) return;

        switch (State)
        {
            case PlotState.Empty:   TryPlant(); break;
            case PlotState.Ready:   Harvest();  break;
            case PlotState.Growing:
                float remaining = ActiveCrop.growthStages[CurrentStage].duration - StageTimer;
                Debug.Log($"{ActiveCrop.cropName} — stage {CurrentStage + 1}/" +
                          $"{ActiveCrop.growthStages.Length}, {remaining:F1}s left");
                break;
        }
    }

    void TryPlant()
    {
        CropData selected = Inventory.Instance.SelectedSeed;
        if (selected == null) { Debug.Log("No seed selected."); return; }
        if (!Inventory.Instance.UseSeed(selected)) return;

        ActiveCrop   = selected;
        CurrentStage = 0;
        StageTimer   = 0f;
        IsMutated    = false;
        State        = PlotState.Growing;
        StateChanged = true;

        EventBus.Raise_PlotPlanted(this);
        Debug.Log($"Planted {ActiveCrop.cropName}!");
    }

    void Harvest()
    {
        // Roll for mutation
        bool mutated = Random.value < ActiveCrop.mutationChance;
        if (mutated)
        {
            IsMutated = true;
            EventBus.Raise_MutationOccurred(this);
            Debug.Log($"Mutation! {ActiveCrop.cropName} has mutated!");
        }

        // Roll for rank
        Rank rank = RankUtility.RollRank();

        // If mutatesInto is assigned, switch to that crop type
        CropData finalCrop = (mutated && ActiveCrop.mutatesInto != null)
            ? ActiveCrop.mutatesInto
            : ActiveCrop;

        var harvested = new HarvestedCrop(finalCrop, rank, mutated);
        Inventory.Instance.AddHarvest(harvested);

        Debug.Log($"Harvested {RankUtility.RankLabel(rank)} {harvested.DisplayName} " +
                  $"(worth {harvested.SellValue} coins)");

        ActiveCrop   = null;
        CurrentStage = -1;
        StageTimer   = 0f;
        IsMutated    = false;
        State        = PlotState.Empty;
        StateChanged = true;
    }

    void OnMouseEnter() => GetComponent<FarmPlotVisual>()?.OnHoverEnter();
    void OnMouseOver()  => GetComponent<FarmPlotVisual>()?.OnHoverStay();
    void OnMouseExit()  => GetComponent<FarmPlotVisual>()?.OnHoverExit();
}