using System;
using UnityEngine;

public static class EventBus
{
    // ── Farm ──────────────────────────────────────────────────
    public static event Action<FarmPlot>                OnPlotPlanted;
    public static event Action<FarmPlot>                OnPlotReady;
    public static event Action<FarmPlot>                OnPlotRemoved;
    public static event Action<FarmPlot, CharacterData> OnPlotEvolved;

    public static void Raise_PlotPlanted(FarmPlot p)                   => OnPlotPlanted?.Invoke(p);
    public static void Raise_PlotReady(FarmPlot p)                     => OnPlotReady?.Invoke(p);
    public static void Raise_PlotRemoved(FarmPlot p)                   => OnPlotRemoved?.Invoke(p);
    public static void Raise_PlotEvolved(FarmPlot p, CharacterData d)  => OnPlotEvolved?.Invoke(p, d);

    // ── Inventory ─────────────────────────────────────────────
    public static event Action<CropData>      OnSeedAdded;
    public static event Action<CropData>      OnSeedUsed;
    public static event Action<HarvestedCrop> OnCropHarvested;
    public static event Action<HarvestedCrop> OnCropSold;
    public static event Action<ToolData>      OnToolAdded;
    public static event Action<ToolData>      OnToolUsed;

    public static void Raise_SeedAdded(CropData c)          => OnSeedAdded?.Invoke(c);
    public static void Raise_SeedUsed(CropData c)           => OnSeedUsed?.Invoke(c);
    public static void Raise_CropHarvested(HarvestedCrop h) => OnCropHarvested?.Invoke(h);
    public static void Raise_CropSold(HarvestedCrop h)      => OnCropSold?.Invoke(h);
    public static void Raise_ToolAdded(ToolData t)          => OnToolAdded?.Invoke(t);
    public static void Raise_ToolUsed(ToolData t)           => OnToolUsed?.Invoke(t);

    // ── Economy ───────────────────────────────────────────────
    public static event Action<int> OnCurrencyChanged;

    public static void Raise_CurrencyChanged(int amount) => OnCurrencyChanged?.Invoke(amount);

    // Alias kept for GameManager.cs compatibility
    public static void Raise_CoinsChanged(int amount)    => OnCurrencyChanged?.Invoke(amount);

    // ── Shop ──────────────────────────────────────────────────
    public static event Action OnShopStockRefreshed;
    public static event Action OnShopOpened;
    public static event Action OnShopClosed;

    public static void Raise_ShopStockRefreshed() => OnShopStockRefreshed?.Invoke();
    public static void Raise_ShopOpened()         => OnShopOpened?.Invoke();
    public static void Raise_ShopClosed()         => OnShopClosed?.Invoke();

    // ── World events ──────────────────────────────────────────
    public static event Action<WorldEventData> OnWorldEventStarted;
    public static event Action<WorldEventData> OnWorldEventEnded;

    public static void Raise_WorldEventStarted(WorldEventData e) => OnWorldEventStarted?.Invoke(e);
    public static void Raise_WorldEventEnded(WorldEventData e)   => OnWorldEventEnded?.Invoke(e);

    // ── Relationships ─────────────────────────────────────────
    public static event Action<CharacterData, int> OnRelationshipChanged;

    public static void Raise_RelationshipChanged(CharacterData c, int level)
        => OnRelationshipChanged?.Invoke(c, level);

    // ── Dialogue ─────────────────────────────────────────────
    public static event Action OnDialogueComplete;
    public static void Raise_DialogueComplete() => OnDialogueComplete?.Invoke();
}